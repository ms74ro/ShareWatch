// ShareWatch - monitorizeaza accesarile la folderele partajate (SMB) din Windows.
// Compilat cu csc.exe din .NET Framework 4 (inclus in Windows) -> un singur .exe, fara setup.
// Fisiere: ShareWatch.cs (nucleu, monitor, NetApi), MainForm.cs (fereastra), Tray.cs, Toast.cs, Support.cs, ShareDialog.cs.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;

// La schimbarea versiunii: actualizeaza AppInfo.Version de mai jos, cele doua atribute SI CHANGELOG.md.
[assembly: AssemblyTitle("ShareWatch")]
[assembly: AssemblyDescription("Monitorizare accesari foldere partajate")]
[assembly: AssemblyProduct("ShareWatch")]
// Fara URL in informatiile de versiune (scos in 0.7.3). Detectia Microsoft Trojan:Win32/Wacatac.C!ml
// de pe VirusTotal apare si dispare de la o compilare la alta (si fara URL, la 0.7.4). Linkul e in Despre.
[assembly: AssemblyCopyright("by ms74ro & Claude")]
[assembly: AssemblyVersion("0.7.5.0")]
[assembly: AssemblyFileVersion("0.7.5.0")]

namespace ShareWatch
{
    static class AppInfo
    {
        public const string Version = "0.7.5";
        public const string RepoUrl = "https://github.com/ms74ro/ShareWatch";
        // Istoricul versiunilor e in CHANGELOG.md (nu mai e inclus in exe).
        // Cand proiectul ajunge pe GitHub, pune aici adresa fisierului; daca e completata, linkul din Despre o foloseste.
        public const string ChangelogUrl = "https://github.com/ms74ro/ShareWatch/blob/main/CHANGELOG.md";
        public const string ChangelogFile = "CHANGELOG.md";

        // numele acestui calculator (in modul de capturi pentru README: un nume fictiv)
        public static string Machine
        {
            get
            {
#if TEST
                if (Demo.On) return Demo.Machine;
#endif
                return Environment.MachineName;
            }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
#if TEST
            // ShareWatch-test.exe --export-lang language\en.json : modelul pentru traduceri
            int ex = Array.IndexOf(args, "--export-lang");
            if (ex >= 0) { L.Export(ex + 1 < args.Length ? args[ex + 1] : "en.json"); return; }
            const string suffix = "_Test";   // varianta de test nu se loveste de instanta normala
            Demo.Init(args);
#else
            const string suffix = "";
#endif
            bool created;
            Mutex m;
            EventWaitHandle show;
            try
            {
                m = new Mutex(true, "Global\\ShareWatch_SingleInstance" + suffix, out created);
                show = new EventWaitHandle(false, EventResetMode.AutoReset, "Global\\ShareWatch_Show" + suffix);
            }
            catch (UnauthorizedAccessException)
            {
                // ruleaza deja o instanta cu drepturi de administrator, iar aceasta nu are acces la ea: iesim fara eroare
                return;
            }
            using (m)
            using (show)
            {
                if (!created) { show.Set(); return; }   // instanta existenta isi deschide fereastra
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Settings.Init();
                L.Apply(Settings.Lang, Settings.LangCode);
                Theme.Pref = Settings.Theme;
                Theme.Update();
                Application.Run(new TrayApp(Array.IndexOf(args, "--tray") >= 0, Array.IndexOf(args, "--demo") >= 0, show));
            }
        }
    }

    // ------------------------------------------------------------------ setari + jurnal
    static class Settings
    {
        public static string Dir;
        public static string LogPath;
        static string iniPath;
        public static bool Notify = true;
        public static bool IgnoreLocal = true;
        public static int ToastSeconds = 9;
        public static int CooldownSec = 30;
        public static bool Sound = true;
        public static bool BlockEnabled = true;
        public static bool HideAdminShares = true;    // implicit activat; ascunde C$, ADMIN$ etc. din liste
        public static bool NotifyBlocked = false;   // notificare (fara sunet) cand un PC blocat e deconectat
        public static bool BlockFirewall = false;   // regula in Windows Firewall pentru IP-urile blocate
        public static int Theme = 0;   // 0 = auto (urmeaza Windows), 1 = luminos, 2 = intunecat
        public static int Lang = 0;    // 0 = auto, 1 = romana, 2 = engleza, 3 = limba din language\LangCode.json
        public static string LangCode = "";
        public static bool TrayOneClick = false;   // deschide fereastra la un singur clic pe iconita din tray
        public static int WinX, WinY, WinW, WinH;   // pozitia ferestrei (WinW = 0 -> nesalvata)
        public static bool WinMax;
        public const int PollMs = 1000;
        static readonly Dictionary<string, bool> shares = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public static void Init()
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            Dir = exeDir;
            try
            {
                string probe = Path.Combine(exeDir, ".w");
                File.WriteAllText(probe, "x");
                File.Delete(probe);
            }
            catch
            {
                Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShareWatch");
                Directory.CreateDirectory(Dir);
            }
            LogPath = Path.Combine(Dir, "acces_foldere.log");
            iniPath = Path.Combine(Dir, "sharewatch.ini");
            try
            {
                if (File.Exists(iniPath))
                    foreach (string line in File.ReadAllLines(iniPath))
                    {
                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;
                        string k = line.Substring(0, eq).Trim(), v = line.Substring(eq + 1).Trim();
                        int n;
                        if (k == "notify") Notify = v == "1";
                        else if (k == "ignorelocal") IgnoreLocal = v == "1";
                        else if (k == "toastsec" && int.TryParse(v, out n)) ToastSeconds = Math.Max(2, Math.Min(60, n));
                        else if (k == "cooldownsec" && int.TryParse(v, out n)) CooldownSec = Math.Max(0, Math.Min(600, n));
                        else if (k == "sound") Sound = v == "1";
                        else if (k == "block") BlockEnabled = v == "1";
                        else if (k == "hideadmin") HideAdminShares = v == "1";
                        else if (k == "notifyblocked") NotifyBlocked = v == "1";
                        else if (k == "blockfirewall") BlockFirewall = v == "1";
                        else if (k == "theme" && int.TryParse(v, out n)) Theme = Math.Max(0, Math.Min(2, n));
                        else if (k == "lang" && int.TryParse(v, out n)) Lang = Math.Max(0, Math.Min(3, n));
                        else if (k == "langcode") LangCode = v;
                        else if (k == "trayclick") TrayOneClick = v == "1";
                        else if (k == "winx" && int.TryParse(v, out n)) WinX = n;
                        else if (k == "winy" && int.TryParse(v, out n)) WinY = n;
                        else if (k == "winw" && int.TryParse(v, out n)) WinW = n;
                        else if (k == "winh" && int.TryParse(v, out n)) WinH = n;
                        else if (k == "winmax") WinMax = v == "1";
                        else if (k.StartsWith("share.")) shares[k.Substring(6)] = v == "1";
                    }
            }
            catch { }
        }

        // implicit: partajarile obisnuite sunt monitorizate, cele administrative (C$, ADMIN$) nu
        public static bool IsMonitored(NetApi.ShareItem s)
        {
            lock (shares)
            {
                bool v;
                if (shares.TryGetValue(s.Name, out v)) return v;
                return !s.Special;
            }
        }

        public static void SetMonitored(string name, bool v)
        {
            lock (shares) { shares[name] = v; }
            Save();
        }

        public static void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("notify=").Append(Notify ? 1 : 0).Append("\r\n");
                sb.Append("ignorelocal=").Append(IgnoreLocal ? 1 : 0).Append("\r\n");
                sb.Append("toastsec=").Append(ToastSeconds).Append("\r\n");
                sb.Append("cooldownsec=").Append(CooldownSec).Append("\r\n");
                sb.Append("sound=").Append(Sound ? 1 : 0).Append("\r\n");
                sb.Append("block=").Append(BlockEnabled ? 1 : 0).Append("\r\n");
                sb.Append("hideadmin=").Append(HideAdminShares ? 1 : 0).Append("\r\n");
                sb.Append("notifyblocked=").Append(NotifyBlocked ? 1 : 0).Append("\r\n");
                sb.Append("blockfirewall=").Append(BlockFirewall ? 1 : 0).Append("\r\n");
                sb.Append("theme=").Append(Theme).Append("\r\n");
                sb.Append("lang=").Append(Lang).Append("\r\n");
                sb.Append("langcode=").Append(LangCode).Append("\r\n");
                sb.Append("trayclick=").Append(TrayOneClick ? 1 : 0).Append("\r\n");
                sb.Append("winx=").Append(WinX).Append("\r\nwiny=").Append(WinY).Append("\r\nwinw=").Append(WinW).Append("\r\nwinh=").Append(WinH).Append("\r\n");
                sb.Append("winmax=").Append(WinMax ? 1 : 0).Append("\r\n");
                lock (shares) { foreach (KeyValuePair<string, bool> kv in shares) sb.Append("share.").Append(kv.Key).Append('=').Append(kv.Value ? 1 : 0).Append("\r\n"); }
                File.WriteAllText(iniPath, sb.ToString());
            }
            catch { }
        }
    }

    static class Logger
    {
        static readonly object gate = new object();

        public static void Write(string kind, string share, string host, string ip, string user, string path)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.Append(" | ").Append(kind.PadRight(11));
            sb.Append(" | share: ").Append(share);
            sb.Append(" | pc: ").Append(host);
            sb.Append(" | ip: ").Append(ip);
            sb.Append(" | user: ").Append(user);
            if (!string.IsNullOrEmpty(path)) sb.Append(" | cale: ").Append(path);
            Raw(sb.ToString());
        }

        public static void Raw(string line)
        {
            lock (gate)
            {
                try
                {
                    FileInfo fi = new FileInfo(Settings.LogPath);
                    if (fi.Exists && fi.Length > 10 * 1024 * 1024)
                    {
                        string old = Settings.LogPath + ".old";
                        if (File.Exists(old)) File.Delete(old);
                        File.Move(Settings.LogPath, old);
                    }
                    File.AppendAllText(Settings.LogPath, line + "\r\n", new UTF8Encoding(false));
                }
                catch { }
            }
        }

        public static void Clear()
        {
            lock (gate) { try { File.WriteAllText(Settings.LogPath, ""); } catch { } }
        }
    }

    // ------------------------------------------------------------------ netapi32
    static class NetApi
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHARE_INFO_2
        {
            public string netname; public uint type; public string remark; public uint permissions;
            public uint max_uses; public uint current_uses; public string path; public string passwd;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHARE_INFO_1
        {
            public string netname; public uint type; public string remark;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CONNECTION_INFO_1
        {
            public uint id; public uint type; public uint num_opens; public uint num_users; public uint time;
            public string username; public string netname;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct FILE_INFO_3
        {
            public uint id; public uint permissions; public uint num_locks; public string pathname; public string username;
        }

        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
        static extern int NetShareEnum(string server, int level, out IntPtr buf, int prefmax, out int read, out int total, ref int resume);
        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
        static extern int NetConnectionEnum(string server, string qualifier, int level, out IntPtr buf, int prefmax, out int read, out int total, ref int resume);
        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
        static extern int NetFileEnum(string server, string basepath, string user, int level, out IntPtr buf, int prefmax, out int read, out int total, ref IntPtr resume);
        [DllImport("netapi32.dll")]
        static extern int NetApiBufferFree(IntPtr buf);

        static List<T> ToList<T>(IntPtr buf, int n)
        {
            List<T> l = new List<T>();
            int sz = Marshal.SizeOf(typeof(T));
            for (int i = 0; i < n; i++)
                l.Add((T)Marshal.PtrToStructure(new IntPtr(buf.ToInt64() + (long)i * sz), typeof(T)));
            return l;
        }

        public class ShareItem { public string Name; public string Path; public string Remark; public bool Special; public bool Disk; }

        public static List<ShareItem> Shares(out int err)
        {
#if TEST
            if (Demo.On) { err = 0; return Demo.Shares(); }
#endif
            List<ShareItem> res = new List<ShareItem>();
            IntPtr buf; int read, total, resume = 0;
            err = NetShareEnum(null, 2, out buf, -1, out read, out total, ref resume);
            if (err == 0 || err == 234)
            {
                try
                {
                    foreach (SHARE_INFO_2 s in ToList<SHARE_INFO_2>(buf, read))
                    {
                        ShareItem it = new ShareItem();
                        it.Name = s.netname; it.Path = s.path; it.Remark = s.remark;
                        it.Disk = (s.type & 0xFF) == 0;
                        it.Special = (s.type & 0x80000000) != 0;
                        res.Add(it);
                    }
                }
                finally { NetApiBufferFree(buf); }
                err = 0;
                return res;
            }
            // fara drepturi de administrator: nivel 1 (fara cale)
            resume = 0;
            int err1 = NetShareEnum(null, 1, out buf, -1, out read, out total, ref resume);
            if (err1 == 0 || err1 == 234)
            {
                try
                {
                    foreach (SHARE_INFO_1 s in ToList<SHARE_INFO_1>(buf, read))
                    {
                        ShareItem it = new ShareItem();
                        it.Name = s.netname; it.Path = ""; it.Remark = s.remark;
                        it.Disk = (s.type & 0xFF) == 0;
                        it.Special = (s.type & 0x80000000) != 0;
                        res.Add(it);
                    }
                }
                finally { NetApiBufferFree(buf); }
                return res;
            }
            return null;
        }

        public static List<CONNECTION_INFO_1> Connections(string share, out int err)
        {
#if TEST
            if (Demo.On) { err = 0; return Demo.Connections(share); }
#endif
            IntPtr buf; int read, total, resume = 0;
            err = NetConnectionEnum(null, share, 1, out buf, -1, out read, out total, ref resume);
            if (err != 0 && err != 234) return null;
            try { return ToList<CONNECTION_INFO_1>(buf, read); }
            finally { NetApiBufferFree(buf); }
        }

        // ---- actiuni (necesita drepturi de administrator)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct SHARE_INFO_502
        {
            public string netname; public uint type; public string remark; public uint permissions;
            public uint max_uses; public uint current_uses; public string path; public string passwd;
            public uint reserved; public IntPtr security_descriptor;
        }
        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
        static extern int NetSessionDel(string server, string client, string user);
        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
        static extern int NetShareAdd(string server, int level, ref SHARE_INFO_502 buf, out int parmErr);
        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
        static extern int NetShareDel(string server, string netname, int reserved);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string sddl, int revision, out IntPtr sd, IntPtr size);
        [DllImport("kernel32.dll")]
        static extern IntPtr LocalFree(IntPtr h);

        // inchide sesiunea SMB a unui client (toate partajarile si fisierele lui deschise)
        public static int Disconnect(string rawClient, string user)
        {
            string unc = "\\\\" + rawClient.TrimStart('\\');
            int rc = NetSessionDel(null, unc, string.IsNullOrEmpty(user) ? null : user);
            if (rc == 2312 && !string.IsNullOrEmpty(user)) rc = NetSessionDel(null, unc, null); // NERR_ClientNameNotFound
            return rc;
        }

        // partajeaza un folder; permisiune de partajare pentru Everyone: citire sau citire+scriere
        public static int AddShare(string name, string path, string remark, bool write)
        {
            IntPtr sd;
            string sddl = write ? "D:(A;;0x1301bf;;;WD)" : "D:(A;;0x1200a9;;;WD)";
            if (!ConvertStringSecurityDescriptorToSecurityDescriptor(sddl, 1, out sd, IntPtr.Zero)) return Marshal.GetLastWin32Error();
            try
            {
                SHARE_INFO_502 si = new SHARE_INFO_502();
                si.netname = name; si.type = 0; si.remark = remark ?? ""; si.permissions = 0;
                si.max_uses = 0xFFFFFFFF; si.path = path; si.security_descriptor = sd;
                int perr;
                return NetShareAdd(null, 502, ref si, out perr);
            }
            finally { LocalFree(sd); }
        }

        public static int DelShare(string name) { return NetShareDel(null, name, 0); }

        public static string ErrText(int rc)
        {
            switch (rc)
            {
                case 0: return "OK";
                case 5: return L.T("Acces refuzat - rulează aplicația ca Administrator.");
                case 2118: return L.T("Există deja o partajare cu acest nume.");
                case 2116: return L.T("Calea nu există sau nu este un folder local.");
                case 2310: return L.T("Partajarea nu există.");
                case 2312: return L.T("Calculatorul nu mai este conectat.");
                case 87: return L.T("Parametru invalid (verifică numele partajării).");
                default: return L.F("Eroare Windows: {0}", rc);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SESSION_INFO_10
        {
            public string cname; public string username; public uint time; public uint idle_time;
        }
        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
        static extern int NetSessionEnum(string server, string client, string user, int level, out IntPtr buf, int prefmax, out int read, out int total, ref int resume);

        // toate sesiunile SMB (inclusiv cele doar pe IPC$), indiferent de partajare
        public static List<SESSION_INFO_10> Sessions(out int err)
        {
#if TEST
            if (Demo.On) { err = 0; return new List<SESSION_INFO_10>(); }
#endif
            IntPtr buf; int read, total, resume = 0;
            err = NetSessionEnum(null, null, null, 10, out buf, -1, out read, out total, ref resume);
            if (err != 0 && err != 234) return null;
            try { return ToList<SESSION_INFO_10>(buf, read); }
            finally { NetApiBufferFree(buf); }
        }

        public static List<FILE_INFO_3> Files(out int err)
        {
#if TEST
            if (Demo.On) { err = 0; return Demo.Files(); }
#endif
            IntPtr buf; int read, total; IntPtr resume = IntPtr.Zero;
            err = NetFileEnum(null, null, null, 3, out buf, -1, out read, out total, ref resume);
            if (err != 0 && err != 234) return null;
            try { return ToList<FILE_INFO_3>(buf, read); }
            finally { NetApiBufferFree(buf); }
        }
    }

    // ------------------------------------------------------------------ rezolvare IP / nume PC
    class Peer
    {
        public string Ip, Host;
        public Peer(string ip, string host) { Ip = ip; Host = host; }
    }

    static class Resolver
    {
        static readonly Dictionary<string, Peer> cache = new Dictionary<string, Peer>(StringComparer.OrdinalIgnoreCase);
        static readonly HashSet<string> localIps = new HashSet<string>();

        static Resolver()
        {
            try { foreach (IPAddress a in Dns.GetHostAddresses(Dns.GetHostName())) localIps.Add(a.ToString()); } catch { }
            localIps.Add("127.0.0.1"); localIps.Add("::1");
        }

        public static bool IsLocal(Peer p)
        {
            if (p == null) return false;
#if TEST
            if (Demo.On) return false;
#endif
            if (localIps.Contains(p.Ip)) return true;
            return string.Equals(p.Host, Environment.MachineName, StringComparison.OrdinalIgnoreCase);
        }

        // rezultatele incomplete (IP sau nume necunoscut) se pastreaza doar 60 s, apoi se reincearca
        static readonly Dictionary<string, DateTime> partial = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        static readonly HashSet<string> pending = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static Peer Cached(string raw)
        {
            lock (cache)
            {
                Peer p;
                if (!cache.TryGetValue(raw, out p)) return null;
                DateTime t;
                if (partial.TryGetValue(raw, out t) && (DateTime.Now - t).TotalSeconds > 60) { cache.Remove(raw); partial.Remove(raw); return null; }
                return p;
            }
        }

        // porneste rezolvarea in fundal, o singura data pe client
        public static void Prefetch(string raw)
        {
            lock (cache) { if (!pending.Add(raw)) return; }
            ResolveAsync(raw, delegate { lock (cache) { pending.Remove(raw); } });
        }

        public static void ResolveAsync(string raw, Action<Peer> done)
        {
            Peer c = Cached(raw);
            if (c != null) { done(c); return; }
            ThreadPool.QueueUserWorkItem(delegate
            {
                Peer r;
                try { r = Resolve(raw); } catch { r = new Peer("?", raw.TrimStart('\\')); }
                lock (cache)
                {
                    cache[raw] = r;
                    if (r.Ip != "?" && r.Host != "?") partial.Remove(raw); else partial[raw] = DateTime.Now;
                }
                done(r);
            });
        }

        static Peer Resolve(string raw)
        {
#if TEST
            if (Demo.On) return Demo.Resolve(raw);
#endif
            string s = raw.TrimStart('\\').Trim();
            string ip = null, host = null;
            IPAddress a;
            if (IPAddress.TryParse(s, out a))
            {
                if (a.IsIPv4MappedToIPv6) a = a.MapToIPv4();
                ip = a.ToString();
                if (a.AddressFamily == AddressFamily.InterNetwork) host = NetBiosName(a);
                if (host == null) host = ReverseDns(a);
            }
            else
            {
                host = s;
                try
                {
                    foreach (IPAddress x in Dns.GetHostAddresses(s))
                        if (x.AddressFamily == AddressFamily.InterNetwork) { ip = x.ToString(); break; }
                }
                catch { }
            }
            return new Peer(ip ?? "?", host ?? "?");
        }

        static string ReverseDns(IPAddress a)
        {
            try
            {
                IAsyncResult ar = Dns.BeginGetHostEntry(a, null, null);
                if (!ar.AsyncWaitHandle.WaitOne(1500)) return null;
                string h = Dns.EndGetHostEntry(ar).HostName;
                if (string.IsNullOrEmpty(h) || h == a.ToString()) return null;
                int dot = h.IndexOf('.');
                return dot > 0 ? h.Substring(0, dot) : h;
            }
            catch { return null; }
        }

        // interogare NetBIOS node status (UDP 137) - da numele calculatoarelor Windows din LAN
        static string NetBiosName(IPAddress a)
        {
            try
            {
                byte[] q = new byte[50];
                q[0] = 0x4B; q[1] = 0x57;
                q[5] = 1;
                q[12] = 0x20;
                string enc = "CKAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
                for (int i = 0; i < 32; i++) q[13 + i] = (byte)enc[i];
                q[45] = 0;
                q[46] = 0x00; q[47] = 0x21;
                q[48] = 0x00; q[49] = 0x01;
                using (UdpClient c = new UdpClient())
                {
                    c.Client.ReceiveTimeout = 700;
                    c.Send(q, q.Length, new IPEndPoint(a, 137));
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                    byte[] r = c.Receive(ref ep);
                    if (r.Length < 57) return null;
                    int n = r[56];
                    for (int i = 0; i < n; i++)
                    {
                        int o = 57 + i * 18;
                        if (o + 18 > r.Length) break;
                        bool group = (r[o + 16] & 0x80) != 0;
                        if (r[o + 15] == 0x00 && !group)
                            return Encoding.ASCII.GetString(r, o, 15).Trim();
                    }
                }
            }
            catch { }
            return null;
        }
    }

    // ------------------------------------------------------------------ monitor
    class ConnInfo { public string Share, Raw, User; public Peer Peer; public DateTime Since; }
    class FileRow { public string Share, User, Path; public Peer Peer; }
    class Snapshot
    {
        public List<ConnInfo> Conns = new List<ConnInfo>();
        public List<FileRow> Files = new List<FileRow>();
    }

    class Monitor
    {
        readonly TrayApp app;
        volatile bool stop;
        Thread th;
        Dictionary<string, ConnInfo> conns = new Dictionary<string, ConnInfo>();
        HashSet<uint> knownFiles = new HashSet<uint>();
        readonly Dictionary<string, DateTime> lastToast = new Dictionary<string, DateTime>();
        public volatile int LastError;
        public volatile Snapshot Snap = new Snapshot();

        public Monitor(TrayApp a) { app = a; }

        public void Start()
        {
            th = new Thread(Loop);
            th.IsBackground = true;
            th.Start();
        }
        public void Stop() { stop = true; }

        void Loop()
        {
            bool first = true;
            while (!stop)
            {
                try { Poll(first); first = false; }
                catch (Exception ex) { Logger.Raw(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | EROARE      | " + ex.Message); }
                Thread.Sleep(Settings.PollMs);
            }
        }

        static string ShareOf(string path, List<NetApi.ShareItem> shares)
        {
            string name = "?"; int best = -1;
            foreach (NetApi.ShareItem sh in shares)
            {
                if (!sh.Disk || string.IsNullOrEmpty(sh.Path)) continue;
                string root = sh.Path.TrimEnd('\\');
                if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase) && root.Length > best) { best = root.Length; name = sh.Name; }
            }
            return name;
        }

        ConnInfo Match(string share, string user)
        {
            foreach (ConnInfo ci in conns.Values)
                if (ci.Share == share && string.Equals(ci.User, user, StringComparison.OrdinalIgnoreCase)) return ci;
            return null;
        }

        readonly Dictionary<string, DateTime> lastKick = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        // inchide sesiunea unui calculator blocat; jurnal + notificare cel mult o data pe minut per client
        // (Windows reincearca singur conexiunea, deci deconectarea se repeta la fiecare incercare)
        void Kick(string raw, string user, Peer p)
        {
            int rc = NetApi.Disconnect(raw, user);
            string key = raw.TrimStart('\\') + "|" + user;
            lock (lastKick)
            {
                DateTime t;
                if (lastKick.TryGetValue(key, out t) && (DateTime.Now - t).TotalSeconds < 60) return;
                lastKick[key] = DateTime.Now;
            }
            if (p == null) p = new Peer("?", raw.TrimStart('\\'));
            Logger.Write("BLOCAT", "-", p.Host, p.Ip, user, rc == 0 ? null : NetApi.ErrText(rc));
            app.Activity("BLOCAT", "-", p, user, null);
            if (Settings.Notify && Settings.NotifyBlocked) app.ShowToast("-", p, user, null, true);
        }

        void EnforceBlockList()
        {
            BlockList.ReloadIfChanged();
            FirewallBlock.Tick();
            if (!Settings.BlockEnabled || BlockList.Count == 0) return;
            int err;
            List<NetApi.SESSION_INFO_10> ses = NetApi.Sessions(out err);
            if (ses == null) return;
            foreach (NetApi.SESSION_INFO_10 s in ses)
            {
                if (string.IsNullOrEmpty(s.cname)) continue;
                Peer p = Resolver.Cached(s.cname);
                if (p == null) Resolver.Prefetch(s.cname);   // numele va fi cunoscut la ciclul urmator
                if (BlockList.Match(s.cname, p)) Kick(s.cname, s.username, p);
            }
        }

        void Poll(bool first)
        {
            int err;
            EnforceBlockList();
            List<NetApi.ShareItem> shares = NetApi.Shares(out err);
            if (shares == null) { SetErr(err); return; }

            HashSet<string> active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (NetApi.ShareItem sh in shares)
                if (sh.Disk && Settings.IsMonitored(sh)) active.Add(sh.Name);

            Dictionary<string, ConnInfo> cur = new Dictionary<string, ConnInfo>();
            int connErr = 0;
            foreach (NetApi.ShareItem sh in shares)
            {
                if (!active.Contains(sh.Name)) continue;
                List<NetApi.CONNECTION_INFO_1> list = NetApi.Connections(sh.Name, out err);
                if (list == null) { connErr = err; continue; }
                foreach (NetApi.CONNECTION_INFO_1 c in list)
                {
                    string key = sh.Name + "|" + c.netname + "|" + c.username;
                    if (cur.ContainsKey(key)) continue;
                    ConnInfo old;
                    ConnInfo ci = conns.TryGetValue(key, out old) ? old
                        : new ConnInfo { Share = sh.Name, Raw = c.netname, User = c.username, Since = DateTime.Now.AddSeconds(-(double)c.time) };
                    cur[key] = ci;
                }
            }
            SetErr(connErr);

            foreach (KeyValuePair<string, ConnInfo> kv in cur)
            {
                if (conns.ContainsKey(kv.Key)) continue;
                ConnInfo ci = kv.Value;
                bool startup = first;
                Resolver.ResolveAsync(ci.Raw, delegate(Peer p)
                {
                    ci.Peer = p;
                    if (BlockList.Match(ci.Raw, p)) return;   // va fi deconectat (vezi EnforceBlockList)
                    Report(startup ? "ACTIV" : "CONECTARE", ci.Share, p, ci.User, null, !startup, true);
                });
            }
            foreach (KeyValuePair<string, ConnInfo> kv in conns)
            {
                if (cur.ContainsKey(kv.Key)) continue;
                ConnInfo ci = kv.Value;
                if (!active.Contains(ci.Share)) continue; // partajare scoasa din monitorizare
                Peer p = ci.Peer ?? new Peer("?", ci.Raw.TrimStart('\\'));
                if (Settings.IgnoreLocal && Resolver.IsLocal(p)) continue;
                Logger.Write("DECONECTARE", ci.Share, p.Host, p.Ip, ci.User, null);
            }
            conns = cur;

            Snapshot snap = new Snapshot();
            snap.Conns.AddRange(cur.Values);

            List<NetApi.FILE_INFO_3> files = NetApi.Files(out err);
            if (files != null)
            {
                HashSet<uint> curFiles = new HashSet<uint>();
                foreach (NetApi.FILE_INFO_3 f in files)
                {
                    string path = f.pathname ?? "";
                    if (path.StartsWith("\\PIPE\\", StringComparison.OrdinalIgnoreCase)) continue;
                    string shareName = ShareOf(path, shares);
                    if (shareName != "?" && !active.Contains(shareName)) continue;
                    ConnInfo match = Match(shareName, f.username);
                    FileRow row = new FileRow { Share = shareName, User = f.username, Path = path, Peer = match != null ? match.Peer : null };
                    snap.Files.Add(row);
                    curFiles.Add(f.id);
                    if (first || knownFiles.Contains(f.id)) continue;

                    string shr = shareName, usr = f.username, pth = path;
                    if (match == null)
                        Report("FISIER", shr, new Peer("?", "?"), usr, pth, true, false);
                    else
                    {
                        string mraw = match.Raw;
                        Resolver.ResolveAsync(mraw, delegate(Peer p) { if (!BlockList.Match(mraw, p)) Report("FISIER", shr, p, usr, pth, true, false); });
                    }
                }
                knownFiles = curFiles;
            }
            Snap = snap;
        }

        void SetErr(int err)
        {
            if (err != LastError) { LastError = err; app.ErrorChanged(err); }
        }

        void Report(string kind, string share, Peer p, string user, string path, bool toast, bool forceToast)
        {
            if (Settings.IgnoreLocal && Resolver.IsLocal(p)) return;
            Logger.Write(kind, share, p.Host, p.Ip, user, path);
            app.Activity(kind, share, p, user, path);
            if (!toast || !Settings.Notify) return;
            string key = p.Ip + "|" + share;
            lock (lastToast)
            {
                DateTime t;
                if (!forceToast && lastToast.TryGetValue(key, out t) && (DateTime.Now - t).TotalSeconds < Settings.CooldownSec) return;
                lastToast[key] = DateTime.Now;
            }
            app.ShowToast(share, p, user, path);
        }
    }

}
