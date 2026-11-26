// Limbi (RO/EN), teme (luminos/intunecat/auto) si sunetul de notificare
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ShareWatch
{
    // ------------------------------------------------------------------ limba
    // Textele din cod sunt in romana (cheia); pentru engleza se cauta traducerea in dictionar.
    static class L
    {
        public static bool En;

        public static void Apply(int pref)
        {
            En = pref == 2 || (pref == 0 && CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "ro");
        }

        public static string T(string ro)
        {
            string e;
            if (En && map.TryGetValue(ro, out e)) return e;
            return ro;
        }

        public static string F(string ro, params object[] args) { return string.Format(T(ro), args); }

        static readonly Dictionary<string, string> map = new Dictionary<string, string>
        {
            // taburi
            { "Monitorizare", "Monitoring" },
            { "Foldere monitorizate", "Monitored folders" },
            { "Istoric", "History" },
            { "Opțiuni", "Options" },
            { "Despre", "About" },
            // coloane
            { "Calculator (PC)", "Computer (PC)" },
            { "Utilizator", "User" },
            { "Partajare", "Share" },
            { "Conectat de la", "Connected since" },
            { "Fișier / folder deschis", "Open file / folder" },
            { "Folder partajat", "Shared folder" },
            { "Cale locală", "Local path" },
            { "Tip", "Type" },
            { "Comentariu", "Comment" },
            { "Data / ora", "Date / time" },
            { "Eveniment", "Event" },
            { "Cale / detalii", "Path / details" },
            // evenimente (jurnalul pastreaza valorile romanesti)
            { "CONECTARE", "CONNECT" },
            { "FISIER", "FILE" },
            { "DECONECTARE", "DISCONNECT" },
            { "ACTIV", "ACTIVE" },
            { "EROARE", "ERROR" },
            // notificare
            { "necunoscut", "unknown" },
            { "ACCES LA FOLDER PARTAJAT", "SHARED FOLDER ACCESS" },
            { "Utilizator: ", "User: " },
            { "utilizator", "user" },
            // tray
            { "Deschide fereastra", "Open window" },
            { "Foldere partajate", "Shared folders" },
            { "Deschide jurnalul", "Open log" },
            { "Notificări", "Notifications" },
            { "Ignoră accesările de pe acest calculator", "Ignore access from this computer" },
            { "Pornește automat cu Windows", "Start with Windows" },
            { "Test notificare", "Test notification" },
            { "Ieșire", "Exit" },
            { " - monitorizare activă", " - monitoring active" },
            { "Ultimul acces: ", "Last access: " },
            { "Eroare: acces refuzat - rulează ca Administrator", "Error: access denied - run as Administrator" },
            { "Eroare netapi32: {0}", "netapi32 error: {0}" },
            { "Acces refuzat. Rulează aplicația ca Administrator pentru a vedea accesările.", "Access denied. Run the application as Administrator to see accesses." },
            { "(nu pot citi lista)", "(cannot read the list)" },
            { "(niciun folder partajat)", "(no shared folders)" },
            { "Pornește automat la autentificare, direct în tray.", "Starts automatically at sign-in, straight to the tray." },
            { "Pornirea automată a fost dezactivată.", "Automatic start has been disabled." },
            { "Nu am putut modifica pornirea automată (rulează ca Administrator).", "Could not change automatic start (run as Administrator)." },
            // fereastra - monitorizare
            { " - monitorizare foldere partajate", " - shared folder monitoring" },
            { "Acces refuzat - rulează aplicația ca Administrator pentru a vedea conexiunile.", "Access denied - run the application as Administrator to see connections." },
            { "Eroare la citirea conexiunilor (netapi32: {0}).", "Error reading connections (netapi32: {0})." },
            { "Conectați acum ({0})", "Connected now ({0})" },
            { "Fișiere deschise acum ({0})", "Files open now ({0})" },
            // fereastra - foldere
            { "Bifează folderele partajate care vor fi monitorizate (notificări și jurnal). Modificările se aplică imediat. Partajările administrative (C$, ADMIN$ etc.) sunt dezactivate implicit.",
              "Check the shared folders to monitor (notifications and log). Changes apply immediately. Administrative shares (C$, ADMIN$ etc.) are disabled by default." },
            { "Reîmprospătează", "Refresh" },
            { "Bifează tot", "Check all" },
            { "Debifează tot", "Uncheck all" },
            { "Administrativ", "Administrative" },
            // fereastra - istoric
            { "Filtru:", "Filter:" },
            { "Eveniment:", "Event:" },
            { "Toate", "All" },
            { "Deschide în Notepad", "Open in Notepad" },
            { "Golește jurnalul", "Clear log" },
            { "Ștergi tot conținutul jurnalului? Operația nu poate fi anulată.", "Delete the entire log? This cannot be undone." },
            { "{0} înregistrări", "{0} records" },
            { " (afișate ultimele {0})", " (showing the last {0})" },
            // fereastra - optiuni
            { "Afișează notificare la accesarea folderelor partajate", "Show a notification when shared folders are accessed" },
            { "Sunet discret la notificare", "Subtle sound on notification" },
            { "Ignoră accesările făcute de pe acest calculator", "Ignore access made from this computer" },
            { "Pornește automat cu Windows (în tray, cu drepturi de administrator, fără UAC)", "Start with Windows (in the tray, with administrator rights, no UAC prompt)" },
            { "Durata notificării (secunde):", "Notification duration (seconds):" },
            { "Pauză între notificările pentru fișiere, același PC și folder (secunde):", "Pause between file notifications for the same PC and folder (seconds):" },
            { "Automat (ca Windows)", "Automatic (like Windows)" },
            { "Luminos", "Light" },
            { "Întunecat", "Dark" },
            { "Temă:", "Theme:" },
            { "Limbă:", "Language:" },
            { "Setările se salvează automat în sharewatch.ini, lângă executabil. Închiderea ferestrei (X) lasă aplicația activă în tray; pentru oprire folosește Ieșire din meniul iconiței din tray.",
              "Settings are saved automatically in sharewatch.ini, next to the executable. Closing the window (X) keeps the application running in the tray; to stop it use Exit from the tray icon menu." },
            { "Nu am putut modifica pornirea automată. Rulează aplicația ca Administrator.", "Could not change automatic start. Run the application as Administrator." },
            // fereastra - despre
            { "Versiunea ", "Version " },
            { "Monitorizează accesările la folderele partajate (SMB) ale acestui calculator: afișează notificări cu IP-ul și numele calculatorului care accesează și păstrează un jurnal al accesărilor.",
              "Monitors access to this computer's shared folders (SMB): shows notifications with the IP and name of the accessing computer and keeps an access log." },
            { "Calculator: ", "Computer: " },
            { "Drepturi de administrator: ", "Administrator rights: " },
            { "da", "yes" },
            { "NU (monitorizarea nu va funcționa)", "NO (monitoring will not work)" },
            { "Jurnal: ", "Log: " },
            { "Executabil: ", "Executable: " },
            { "Istoric versiuni", "Version history" },
            // evenimente noi
            { "DECONECTAT", "KICKED" },
            { "BLOCAT", "BLOCKED" },
            // lista de blocare
            { "CONEXIUNE BLOCATĂ", "CONNECTION BLOCKED" },
            { "Calculator din lista de blocare", "Computer on the block list" },
            { "Listă de blocare", "Block list" },
            { "Închide imediat conexiunile calculatoarelor din lista de blocare", "Immediately close connections from computers on the block list" },
            { "Calculator (IP și/sau nume)", "Computer (IP and/or name)" },
            { "Adaugă", "Add" },
            { "Șterge selectate", "Remove selected" },
            { "Deschide fișierul", "Open file" },
            { "Șterge din listă (deblochează)", "Remove from list (unblock)" },
            { "IP și/sau nume de calculator, separate prin spațiu (ex.: 192.168.1.50 PC-BIROU). Lista e salvată în blocklist.txt, lângă executabil, și poate fi editată și direct; modificările se aplică în câteva secunde.",
              "IP and/or computer name, separated by spaces (e.g. 192.168.1.50 OFFICE-PC). The list is saved in blocklist.txt next to the executable and can also be edited directly; changes apply within a few seconds." },
            { "adăugat manual {0}", "added manually {0}" },
            { "blocat din Monitorizare {0}", "blocked from Monitoring {0}" },
            { "„{0}” este chiar acest calculator și nu poate fi blocat.", "\"{0}\" is this computer and cannot be blocked." },
            { "„{0}” este deja în lista de blocare.", "\"{0}\" is already on the block list." },
            { "Nu am putut scrie fișierul blocklist.txt.", "Could not write the blocklist.txt file." },
            { "Blochează calculatorul", "Block computer" },
            { "Ascunde partajările administrative (C$, ADMIN$ etc.) din liste", "Hide administrative shares (C$, ADMIN$ etc.) from the lists" },
            { "Notificare (fără sunet) când un calculator blocat este deconectat", "Notification (silent) when a blocked computer is disconnected" },
            { "Blochează și în Windows Firewall (porturile SMB 139 și 445): calculatorul nu se mai poate conecta deloc", "Also block in Windows Firewall (SMB ports 139 and 445): the computer cannot connect at all" },
            { "Se actualizează...", "Updating..." },
            { "Elimină regulile ShareWatch din firewall", "Remove ShareWatch rules from the firewall" },
            { "Elimini regula ShareWatch din Windows Firewall și oprești blocarea prin firewall? Lista de blocare rămâne neschimbată.",
              "Remove the ShareWatch rule from Windows Firewall and stop firewall blocking? The block list stays unchanged." },
            { "Eroare: {0}", "Error: {0}" },
            { "Activă, dar nu există adrese IP de blocat.", "Enabled, but there are no IP addresses to block." },
            { "Nicio regulă ShareWatch în firewall.", "No ShareWatch rule in the firewall." },
            { "Regulă activă: {0} adrese blocate ({1}).", "Rule active: {0} addresses blocked ({1})." },
            { "Blochezi:\r\n{0}\r\n\r\nCalculatorul este deconectat acum și la fiecare încercare de conectare, până îl ștergi din lista de blocare (Opțiuni).",
              "Block:\r\n{0}\r\n\r\nThe computer is disconnected now and on every connection attempt, until you remove it from the block list (Options)." },
            { "PARTAJARE", "SHARE ADDED" },
            { "ANULARE", "SHARE REMOVED" },
            // deconectare
            { "Deconectează calculatorul", "Disconnect computer" },
            { "Deconectezi:\r\n{0}\r\n\r\nFișierele deschise de pe acest calculator se închid, iar datele nesalvate se pot pierde. Windows se poate reconecta automat la următoarea accesare.",
              "Disconnect:\r\n{0}\r\n\r\nFiles opened from this computer will be closed and unsaved data may be lost. Windows may reconnect automatically on the next access." },
            // erori netapi
            { "Acces refuzat - rulează aplicația ca Administrator.", "Access denied - run the application as Administrator." },
            { "Există deja o partajare cu acest nume.", "A share with this name already exists." },
            { "Calea nu există sau nu este un folder local.", "The path does not exist or is not a local folder." },
            { "Partajarea nu există.", "The share does not exist." },
            { "Calculatorul nu mai este conectat.", "The computer is no longer connected." },
            { "Parametru invalid (verifică numele partajării).", "Invalid parameter (check the share name)." },
            { "Eroare Windows: {0}", "Windows error: {0}" },
            // partajare
            { "Partajează un folder...", "Share a folder..." },
            { "Partajează un folder", "Share a folder" },
            { "Oprește partajarea", "Stop sharing" },
            { "Deschide folderul", "Open folder" },
            { "Nu am putut partaja folderul.", "Could not share the folder." },
            { " (citire și scriere)", " (read and write)" },
            { " (doar citire)", " (read only)" },
            { "Selectează o partajare obișnuită (nu administrativă) din listă.", "Select a regular (not administrative) share from the list." },
            { "Oprești partajarea pentru:\r\n{0}\r\n\r\nFolderul și fișierele rămân pe disc; doar nu mai sunt accesibile din rețea. Cine e conectat acum pierde accesul.",
              "Stop sharing:\r\n{0}\r\n\r\nThe folder and its files stay on disk; they are just no longer reachable from the network. Anyone connected now loses access." },
            { "Răsfoiește...", "Browse..." },
            { "Folder:", "Folder:" },
            { "Nume partajare:", "Share name:" },
            { "Descriere:", "Description:" },
            { "Doar citire", "Read only" },
            { "Citire și scriere", "Read and write" },
            { "Acces pentru toți (Everyone):", "Access for Everyone:" },
            { "Permisiunile NTFS ale folderului se aplică în continuare. Pentru acces din rețea, utilizatorii au nevoie și de drept pe folder (fila Securitate).",
              "The folder's NTFS permissions still apply. For network access, users also need rights on the folder (Security tab)." },
            { "Partajează", "Share" },
            { "Anulează", "Cancel" },
            { "Alege folderul de partajat", "Choose the folder to share" },
            { "Alege un folder care există.", "Choose a folder that exists." },
            { "Folderul trebuie să fie pe acest calculator, nu în rețea.", "The folder must be on this computer, not on the network." },
            { "Numele partajării trebuie să aibă între 1 și 80 de caractere.", "The share name must be 1 to 80 characters long." },
            { "Numele partajării conține caractere nepermise: \\ / [ ] : | < > + = ; , ? * \"", "The share name contains invalid characters: \\ / [ ] : | < > + = ; , ? * \"" }
        };
    }

    // ------------------------------------------------------------------ tema
    static class Theme
    {
        public static int Pref;   // 0 = auto, 1 = luminos, 2 = intunecat
        public static Color Bg, Panel, Fg, Muted, Border, Accent, InputBg, Hover, Good, Info, Bad, HeaderBg, SelBg, SelBgInactive, SelFg;

        public static bool Dark { get { return Pref == 2 || (Pref == 0 && SystemDark()); } }

        static bool SystemDark()
        {
            try
            {
                object v = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
                return v is int && (int)v == 0;
            }
            catch { return false; }
        }

        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        static extern int SetPreferredAppMode(int mode);

        public static void Update()
        {
            bool d = Dark;
            // mod intunecat nativ pentru antete de liste, bare de derulare, meniuri (Windows 10 1903+; ignorat daca nu exista)
            try { SetPreferredAppMode(d ? 2 : 3); } catch { }
            Bg = d ? Color.FromArgb(30, 33, 40) : Color.FromArgb(240, 242, 245);
            Panel = d ? Color.FromArgb(38, 42, 52) : Color.White;
            Fg = d ? Color.FromArgb(230, 234, 240) : Color.FromArgb(28, 32, 40);
            Muted = d ? Color.FromArgb(150, 160, 176) : Color.FromArgb(105, 114, 128);
            Border = d ? Color.FromArgb(66, 72, 86) : Color.FromArgb(208, 213, 220);
            Accent = d ? Color.FromArgb(61, 220, 132) : Color.FromArgb(30, 150, 90);
            InputBg = d ? Color.FromArgb(46, 51, 63) : Color.White;
            Hover = d ? Color.FromArgb(58, 65, 80) : Color.FromArgb(222, 232, 245);
            HeaderBg = d ? Color.FromArgb(46, 51, 63) : Color.FromArgb(238, 241, 245);
            SelBg = d ? Color.FromArgb(52, 104, 170) : Color.FromArgb(173, 208, 246);
            SelBgInactive = d ? Color.FromArgb(58, 70, 92) : Color.FromArgb(210, 222, 238);
            SelFg = d ? Color.White : Color.FromArgb(10, 16, 28);
            Good = d ? Color.FromArgb(90, 205, 130) : Color.FromArgb(0, 128, 60);
            Info = d ? Color.FromArgb(120, 175, 255) : Color.FromArgb(0, 90, 180);
            Bad = d ? Color.FromArgb(255, 125, 115) : Color.Firebrick;
        }

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        static extern int SetWindowTheme(IntPtr h, string app, string idlist);
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr h, int attr, ref int val, int size);

        public static void TitleBar(Form f)
        {
            if (!f.IsHandleCreated) return;
            try
            {
                int v = Dark ? 1 : 0;
                if (DwmSetWindowAttribute(f.Handle, 20, ref v, 4) != 0) DwmSetWindowAttribute(f.Handle, 19, ref v, 4);
            }
            catch { }
        }

        // aplica culorile recursiv pe un control si copiii lui
        public static void Apply(Control c)
        {
            if (c is ListView)
            {
                ListView lv = (ListView)c;
                lv.BackColor = Panel; lv.ForeColor = Fg; lv.BorderStyle = BorderStyle.FixedSingle;
                try { SetWindowTheme(lv.Handle, Dark ? "DarkMode_Explorer" : "Explorer", null); } catch { }
            }
            else if (c is TextBox)
            {
                c.BackColor = InputBg; c.ForeColor = Fg; ((TextBox)c).BorderStyle = BorderStyle.FixedSingle;
            }
            else if (c is ComboBox)
            {
                ComboBox cb = (ComboBox)c;
                cb.FlatStyle = FlatStyle.Flat; cb.BackColor = InputBg; cb.ForeColor = Fg;
            }
            else if (c is NumericUpDown)
            {
                c.BackColor = InputBg; c.ForeColor = Fg;
            }
            else if (c is Button)
            {
                Button b = (Button)c;
                b.FlatStyle = FlatStyle.Flat; b.UseVisualStyleBackColor = false;
                b.FlatAppearance.BorderColor = Border; b.BackColor = InputBg; b.ForeColor = Fg;
            }
            else if (c is CheckBox)
            {
                c.BackColor = Color.Transparent; c.ForeColor = Fg;
            }
            else if (c is Label)
            {
                c.BackColor = Color.Transparent;
                string tag = c.Tag as string;
                c.ForeColor = tag == "muted" ? Muted : tag == "error" ? Bad : Fg;
                LinkLabel ll = c as LinkLabel;
                if (ll != null) { ll.LinkColor = Info; ll.VisitedLinkColor = Info; ll.ActiveLinkColor = Accent; }
            }
            else if (c is SplitContainer)
            {
                c.BackColor = Border;
            }
            else if (c is Panel)
            {
                c.BackColor = Bg; c.ForeColor = Fg;
            }
            foreach (Control ch in c.Controls) Apply(ch);
        }
    }

    // ------------------------------------------------------------------ sunet discret
    static class Sfx
    {
        static byte[] wav;

        public static void Play()
        {
            try
            {
                if (wav == null) wav = Make();
                new SoundPlayer(new MemoryStream(wav)).Play();   // asincron
            }
            catch { }
        }

        // doua note scurte, blande (~0,3 s, volum redus) generate in memorie
        static byte[] Make()
        {
            const int rate = 22050;
            int n = (int)(rate * 0.32);
            MemoryStream ms = new MemoryStream();
            BinaryWriter w = new BinaryWriter(ms);
            w.Write(new char[] { 'R', 'I', 'F', 'F' }); w.Write(36 + n * 2);
            w.Write(new char[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(new char[] { 'd', 'a', 't', 'a' }); w.Write(n * 2);
            for (int i = 0; i < n; i++)
            {
                double t = (double)i / rate;
                double v = Math.Sin(2 * Math.PI * 988 * t) * Math.Exp(-t * 18);
                if (t >= 0.08)
                {
                    double t2 = t - 0.08;
                    v += Math.Sin(2 * Math.PI * 1319 * t2) * Math.Exp(-t2 * 14);
                }
                double env = Math.Min(1.0, t / 0.004) * Math.Min(1.0, (0.32 - t) / 0.02);
                w.Write((short)(v * env * 0.16 * 32767));
            }
            w.Flush();
            return ms.ToArray();
        }
    }
}
