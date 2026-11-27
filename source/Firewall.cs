// Blocare in Windows Firewall: o singura regula de intrare care blocheaza porturile SMB (TCP 139, 445)
// pentru IP-urile din lista de blocare. Se poate elimina oricand din Optiuni.
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace ShareWatch
{
    static class FirewallBlock
    {
        public const string RuleName = "ShareWatch - blocare SMB";
        const string Group = "ShareWatch";

        public static volatile string Status = "";
        static volatile int syncedVersion = -1;
        static DateTime lastSync = DateTime.MinValue;
        static int running;

        // cere o resincronizare la urmatorul ciclu al monitorului (dupa o schimbare de setare)
        public static void RequestSync() { Status = ""; syncedVersion = -1; }

        // apelat de monitor la fiecare ciclu; lucrul efectiv se face in fundal
        public static void Tick()
        {
            bool due = syncedVersion != BlockList.Version || (DateTime.Now - lastSync).TotalMinutes >= 10;   // numele se re-rezolva periodic
            if (!due) return;
            if (Interlocked.Exchange(ref running, 1) == 1) return;
            int ver = BlockList.Version;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try { Sync(); syncedVersion = ver; lastSync = DateTime.Now; }
                catch (Exception ex) { Status = Err(ex); syncedVersion = ver; lastSync = DateTime.Now; }
                finally { Interlocked.Exchange(ref running, 0); }
            });
        }

        static string Err(Exception ex)
        {
            if (ex is UnauthorizedAccessException) return L.T("Acces refuzat - rulează aplicația ca Administrator.");
            return L.F("Eroare: {0}", ex.Message);
        }

        static void Sync()
        {
            bool want = Settings.BlockEnabled && Settings.BlockFirewall;
            List<string> ips = want ? CollectIps() : new List<string>();
            if (ips.Count == 0)
            {
                RemoveRules();
                Status = want ? L.T("Activă, dar nu există adrese IP de blocat.") : L.T("Nicio regulă ShareWatch în firewall.");
                return;
            }
            ReplaceRule(ips);
            Status = L.F("Regulă activă: {0} adrese blocate ({1}).", ips.Count, string.Join(", ", ips.ToArray()));
        }

        // IP-urile din lista; numele de calculator se transforma in IP-uri (DNS / NetBIOS prin Windows)
        static List<string> CollectIps()
        {
            List<string> res = new List<string>();
            HashSet<string> local = new HashSet<string>();
            try { foreach (IPAddress a in Dns.GetHostAddresses(Dns.GetHostName())) local.Add(a.ToString()); } catch { }
            local.Add("127.0.0.1"); local.Add("::1");
            foreach (BlockList.Entry e in BlockList.Entries)
                foreach (string tok in e.Tokens)
                {
                    IPAddress a;
                    List<IPAddress> addrs = new List<IPAddress>();
                    if (IPAddress.TryParse(tok, out a)) addrs.Add(a);
                    else
                    {
                        try
                        {
                            IAsyncResult ar = Dns.BeginGetHostAddresses(tok, null, null);
                            if (ar.AsyncWaitHandle.WaitOne(2000)) addrs.AddRange(Dns.EndGetHostAddresses(ar));
                        }
                        catch { }
                    }
                    foreach (IPAddress x in addrs)
                    {
                        IPAddress y = x.IsIPv4MappedToIPv6 ? x.MapToIPv4() : x;
                        if (y.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) y.ScopeId = 0;   // fara %interfata (firewall-ul nu accepta)
                        string s = y.ToString();
                        if (!local.Contains(s) && !res.Contains(s)) res.Add(s);
                    }
                }
            return res;
        }

        static dynamic Policy()
        {
            return Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
        }

        static bool Exists(dynamic pol)
        {
            foreach (dynamic r in pol.Rules)
                if ((string)r.Name == RuleName) return true;
            return false;
        }

        static void ReplaceRule(List<string> ips)
        {
            dynamic pol = Policy();
            RemoveRulesFrom(pol);
            dynamic rule = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));
            rule.Name = RuleName;
            rule.Description = "Creată de ShareWatch pentru calculatoarele din lista de blocare. Se elimină din ShareWatch > Opțiuni.";
            rule.Grouping = Group;
            rule.Protocol = 6;                    // TCP
            rule.LocalPorts = "139,445";          // SMB
            rule.RemoteAddresses = string.Join(",", ips.ToArray());
            rule.Direction = 1;                   // intrare
            rule.Action = 0;                      // blocare
            rule.Profiles = 0x7FFFFFFF;           // toate profilurile (domeniu, privat, public)
            rule.Enabled = true;
            pol.Rules.Add(rule);
        }

        static int RemoveRulesFrom(dynamic pol)
        {
            int n = 0;
            while (n < 50 && Exists(pol)) { pol.Rules.Remove(RuleName); n++; }
            return n;
        }

        // elimina toate regulile ShareWatch; intoarce numarul de reguli sterse (-1 = eroare)
        public static int RemoveRules()
        {
            try { dynamic pol = Policy(); return RemoveRulesFrom(pol); }
            catch (Exception ex) { Status = Err(ex); return -1; }
        }

        // butonul "Elimina regulile": opreste blocarea in firewall si sterge regula imediat
        public static int RemoveAllNow()
        {
            Settings.BlockFirewall = false;
            Settings.Save();
            RequestSync();
            int n = RemoveRules();
            if (n >= 0) Status = L.T("Nicio regulă ShareWatch în firewall.");
            return n;
        }
    }
}
