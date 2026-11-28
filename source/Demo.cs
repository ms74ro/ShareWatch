// Doar pentru varianta de test (build.ps1 -Test): date fictive + capturi automate pentru README.
// ShareWatch-test.exe --screenshots <folder>  -> salveaza cate un PNG pentru fiecare tab si pentru notificari (normala si calculator blocat), apoi iese.
#if TEST
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ShareWatch
{
    static class Demo
    {
        public static bool On;
        public static string ShotDir;
        public const string Machine = "SERVER-BIROU";

        public static void Init(string[] args)
        {
            int i = Array.IndexOf(args, "--screenshots");
            if (i < 0) return;
            On = true;
            ShotDir = i + 1 < args.Length ? args[i + 1] : AppDomain.CurrentDomain.BaseDirectory;
            Directory.CreateDirectory(ShotDir);
        }

        // ---- date fictive
        static readonly Dictionary<string, string> hosts = new Dictionary<string, string>
        {
            { "10.0.5.23", "LAPTOP-ANA" }, { "10.0.5.41", "PC-ANDREI" }, { "10.0.5.57", "SCANER-ETAJ1" }
        };

        public static Peer Resolve(string raw)
        {
            string s = raw.TrimStart('\\');
            string h;
            return new Peer(s, hosts.TryGetValue(s, out h) ? h : "?");
        }

        static NetApi.ShareItem Sh(string name, string path, string remark, bool special, bool disk)
        {
            return new NetApi.ShareItem { Name = name, Path = path, Remark = remark, Special = special, Disk = disk };
        }

        public static List<NetApi.ShareItem> Shares()
        {
            return new List<NetApi.ShareItem>
            {
                Sh("ADMIN$", "C:\\Windows", "Remote Admin", true, true),
                Sh("C$", "C:\\", "Default share", true, true),
                Sh("IPC$", "", "Remote IPC", true, false),
                Sh("Contabilitate", "D:\\Contabilitate", "Facturi și rapoarte", false, true),
                Sh("Documente", "D:\\Documente", "Documente firmă", false, true),
                Sh("Public", "D:\\Public", "", false, true),
                Sh("Scanari", "D:\\Scanari", "Scanner etaj 1", false, true),
            };
        }

        static NetApi.CONNECTION_INFO_1 Cn(string client, string user, uint sec)
        {
            return new NetApi.CONNECTION_INFO_1 { netname = client, username = user, time = sec, num_opens = 1, num_users = 1 };
        }

        public static List<NetApi.CONNECTION_INFO_1> Connections(string share)
        {
            List<NetApi.CONNECTION_INFO_1> l = new List<NetApi.CONNECTION_INFO_1>();
            if (share == "Documente") { l.Add(Cn("10.0.5.23", "ana", 1520)); l.Add(Cn("10.0.5.41", "andrei", 310)); }
            else if (share == "Contabilitate") l.Add(Cn("10.0.5.23", "ana", 1490));
            else if (share == "Scanari") l.Add(Cn("10.0.5.57", "scanner", 75));
            return l;
        }

        static NetApi.FILE_INFO_3 Fi(uint id, string path, string user)
        {
            return new NetApi.FILE_INFO_3 { id = id, pathname = path, username = user, permissions = 1 };
        }

        public static List<NetApi.FILE_INFO_3> Files()
        {
            return new List<NetApi.FILE_INFO_3>
            {
                Fi(101, "D:\\Documente\\Oferte\\Oferta-2026-11.docx", "ana"),
                Fi(102, "D:\\Contabilitate\\Facturi\\2026\\noiembrie.xlsx", "ana"),
                Fi(103, "D:\\Documente\\Proiecte\\plan-2027.pdf", "andrei"),
            };
        }

        // ---- capturi
        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int L, T, R, B; }
        [DllImport("dwmapi.dll")]
        static extern int DwmGetWindowAttribute(IntPtr h, int attr, out RECT r, int size);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")]
        static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);

        // PrintWindow deseneaza doar fereastra noastra (nimic din ce se afla in spate sau deasupra)
        static void Capture(Form f, string name)
        {
            RECT w, r;
            GetWindowRect(f.Handle, out w);
            Rectangle full = Rectangle.FromLTRB(w.L, w.T, w.R, w.B);
            Rectangle vis = full;
            if (DwmGetWindowAttribute(f.Handle, 9, out r, Marshal.SizeOf(typeof(RECT))) == 0) vis = Rectangle.FromLTRB(r.L, r.T, r.R, r.B);   // fara umbra
            using (Bitmap bmp = new Bitmap(full.Width, full.Height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    IntPtr hdc = g.GetHdc();
                    PrintWindow(f.Handle, hdc, 2);   // PW_RENDERFULLCONTENT
                    g.ReleaseHdc(hdc);
                }
                Rectangle crop = new Rectangle(vis.X - full.X, vis.Y - full.Y, vis.Width, vis.Height);
                using (Bitmap c = bmp.Clone(crop, bmp.PixelFormat)) c.Save(Path.Combine(ShotDir, name + ".png"), ImageFormat.Png);
            }
        }

        public static void Run(Func<MainForm> main, Action quit)
        {
            string[] names = { "monitor", "history", "folders", "options", "about" };   // in ordinea taburilor
            // cateva secunde de pauza la inceput: jurnalul (tabul Istoric, al doilea) trebuie sa aiba deja intrari
            int step = -3;
            Timer t = new Timer { Interval = 1500 };
            t.Tick += delegate
            {
                MainForm m = main();
                int k = step / 2;
                if (step < 0) { }
                else if (k < names.Length)
                {
                    if (step % 2 == 0) { m.TopMost = true; m.SelectTab(k); m.Activate(); }
                    else Capture(m, names[k]);
                }
                else if (step == names.Length * 2)
                    Toast.ShowToast("Documente", new Peer("10.0.5.41", "PC-ANDREI"), "andrei", "\\Proiecte\\plan-2027.pdf");
                else if (step == names.Length * 2 + 1)
                {
                    foreach (Form f in Application.OpenForms) if (f is Toast) { Capture(f, "toast"); f.Close(); break; }
                }
                else if (step == names.Length * 2 + 2)
                    // notificarea pentru un calculator blocat (deconectat imediat)
                    Toast.ShowToast("-", new Peer("10.0.5.99", "LAPTOP-VIZITATOR"), "guest", null, true);
                else if (step == names.Length * 2 + 3)
                {
                    foreach (Form f in Application.OpenForms) if (f is Toast) { Capture(f, "toast-blocked"); break; }
                }
                else { t.Stop(); quit(); }
                step++;
            };
            t.Interval = 1500;
            t.Start();
        }
    }
}
#endif
