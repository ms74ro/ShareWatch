// Iconita din tray, meniul ei, pornirea automata
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ShareWatch
{
    class ThemedColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Theme.Panel; } }
        public override Color ImageMarginGradientBegin { get { return Theme.Panel; } }
        public override Color ImageMarginGradientMiddle { get { return Theme.Panel; } }
        public override Color ImageMarginGradientEnd { get { return Theme.Panel; } }
        public override Color MenuBorder { get { return Theme.Border; } }
        public override Color MenuItemBorder { get { return Theme.Border; } }
        public override Color MenuItemSelected { get { return Theme.Hover; } }
        public override Color MenuItemSelectedGradientBegin { get { return Theme.Hover; } }
        public override Color MenuItemSelectedGradientEnd { get { return Theme.Hover; } }
        public override Color MenuItemPressedGradientBegin { get { return Theme.Hover; } }
        public override Color MenuItemPressedGradientMiddle { get { return Theme.Hover; } }
        public override Color MenuItemPressedGradientEnd { get { return Theme.Hover; } }
        public override Color SeparatorDark { get { return Theme.Border; } }
        public override Color SeparatorLight { get { return Theme.Panel; } }
        public override Color CheckBackground { get { return Theme.Hover; } }
        public override Color CheckSelectedBackground { get { return Theme.Hover; } }
        public override Color CheckPressedBackground { get { return Theme.Hover; } }
    }

    // desenare proprie pentru bifa si sageata submeniului, ca sa se vada in ambele teme
    class ThemedRenderer : ToolStripProfessionalRenderer
    {
        public ThemedRenderer() : base(new ThemedColors()) { }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = e.ImageRectangle;
            r.Inflate(2, 2);
            using (SolidBrush b = new SolidBrush(Theme.Hover)) g.FillRectangle(b, r);
            using (Pen bp = new Pen(Theme.Border)) g.DrawRectangle(bp, r.X, r.Y, r.Width - 1, r.Height - 1);
            using (Pen cp = new Pen(Theme.Accent, 2.2f))
            {
                cp.StartCap = LineCap.Round; cp.EndCap = LineCap.Round; cp.LineJoin = LineJoin.Round;
                g.DrawLines(cp, new PointF[] {
                    new PointF(r.Left + r.Width * 0.24f, r.Top + r.Height * 0.52f),
                    new PointF(r.Left + r.Width * 0.43f, r.Top + r.Height * 0.72f),
                    new PointF(r.Left + r.Width * 0.77f, r.Top + r.Height * 0.30f) });
            }
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = Theme.Fg;
            base.OnRenderArrow(e);
        }
    }

    // fereastra ascunsa care primeste mesajul "TaskbarCreated" trimis de Explorer cand porneste bara de activitati
    class TaskbarWatcher : NativeWindow
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern uint RegisterWindowMessage(string s);
        [DllImport("user32.dll")]
        static extern bool ChangeWindowMessageFilterEx(IntPtr h, uint msg, uint action, IntPtr info);

        readonly uint msg;
        readonly Action onCreated;

        public TaskbarWatcher(Action onCreated)
        {
            this.onCreated = onCreated;
            msg = RegisterWindowMessage("TaskbarCreated");
            CreateHandle(new CreateParams());
            // procesul ruleaza elevat: fara asta Windows blocheaza mesajul venit de la Explorer
            try { ChangeWindowMessageFilterEx(Handle, msg, 1 /* MSGFLT_ALLOW */, IntPtr.Zero); } catch { }
        }

        protected override void WndProc(ref Message m)
        {
            if (msg != 0 && (uint)m.Msg == msg) onCreated();
            base.WndProc(ref m);
        }
    }

    class TrayApp : ApplicationContext
    {
        const string TaskName = "ShareWatch";
        readonly NotifyIcon tray = new NotifyIcon();
        readonly ContextMenuStrip menu = new ContextMenuStrip();
        readonly Control ui = new Control();
        public readonly Monitor Monitor;
        public readonly Icon IconOk, IconAlert;
        readonly System.Windows.Forms.Timer revert = new System.Windows.Forms.Timer();
        ToolStripMenuItem miOpen, miNotify, miLocal, miAuto, miShares, miErr, miTest, miLog, miExit;
        MainForm main;
        public bool Quitting;

        [DllImport("user32.dll")]
        static extern bool DestroyIcon(IntPtr h);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindow(string cls, string title);

        TaskbarWatcher taskbar;
        readonly System.Windows.Forms.Timer trayRetry = new System.Windows.Forms.Timer();

        static bool TaskbarReady() { return FindWindow("Shell_TrayWnd", null) != IntPtr.Zero; }

        void ReAddTrayIcon()
        {
            if (Quitting) return;
            tray.Visible = false;
            tray.Visible = true;
        }

        // La pornirea Windows aplicatia poate porni inaintea barei de activitati, iar iconita nu se mai inregistreaza.
        // 1) asteptam bara de activitati si readaugam iconita de cateva ori;
        // 2) ascultam mesajul "TaskbarCreated" (Explorer repornit) - permis explicit, fiindca rulam ca administrator.
        void SetupTrayIcon(bool autostart)
        {
            taskbar = new TaskbarWatcher(ReAddTrayIcon);
            bool ready = TaskbarReady();
            if (ready) tray.Visible = true;
            if (!autostart && ready) return;
            int found = 0, ticks = 0;
            trayRetry.Interval = 3000;
            trayRetry.Tick += delegate
            {
                ticks++;
                if (TaskbarReady()) { ReAddTrayIcon(); found++; }
                if (found >= 3 || ticks >= 200) trayRetry.Stop();   // ~10 minute maxim
            };
            trayRetry.Start();
        }

        public TrayApp(bool startHidden, bool demo, EventWaitHandle showEvent)
        {
            ui.CreateControl();
            IntPtr h = ui.Handle; // forteaza crearea handle-ului pe thread-ul UI

            IconOk = MakeIcon(Color.FromArgb(61, 220, 132));
            IconAlert = MakeIcon(Color.FromArgb(255, 95, 86));

            miErr = new ToolStripMenuItem("") { Visible = false, Enabled = false };
            miOpen = new ToolStripMenuItem();
            miOpen.Font = new Font(miOpen.Font, FontStyle.Bold);
            miShares = new ToolStripMenuItem();
            miNotify = new ToolStripMenuItem() { CheckOnClick = true };
            miLocal = new ToolStripMenuItem() { CheckOnClick = true };
            miAuto = new ToolStripMenuItem();
            miTest = new ToolStripMenuItem();
            miLog = new ToolStripMenuItem();
            miExit = new ToolStripMenuItem();

            menu.Items.Add(miOpen);
            menu.Items.Add(miErr);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miShares);
            menu.Items.Add(miLog);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miNotify);
            menu.Items.Add(miLocal);
            menu.Items.Add(miAuto);
            menu.Items.Add(miTest);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(miExit);

            menu.Opening += delegate
            {
                miNotify.Checked = Settings.Notify;
                miLocal.Checked = Settings.IgnoreLocal;
                miAuto.Checked = AutostartEnabled();
                FillShares();
            };
            miOpen.Click += delegate { ShowMain(); };
            miNotify.Click += delegate { Settings.Notify = miNotify.Checked; Settings.Save(); };
            miLocal.Click += delegate { Settings.IgnoreLocal = miLocal.Checked; Settings.Save(); };
            miAuto.Click += delegate { ToggleAutostart(); };
            miTest.Click += delegate { TestToast(); };
            miLog.Click += delegate { OpenLog(); };
            miExit.Click += delegate { Quit(); };

            tray.Icon = IconOk;
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { ShowMain(); };
            SetupTrayIcon(startHidden);
            LocalizeMenu();
            ApplyMenuTheme();

            revert.Interval = 8000;
            revert.Tick += delegate { revert.Stop(); tray.Icon = IconOk; };

            SystemEvents.UserPreferenceChanged += OnSysPrefs;

            Logger.Raw(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | START       | ShareWatch " + AppInfo.Version + " pornit pe " + AppInfo.Machine
                + (IsAdmin() ? "" : " (FARA drepturi de administrator)"));

            Monitor = new Monitor(this);
            Monitor.Start();

            ThreadPool.RegisterWaitForSingleObject(showEvent, delegate(object st, bool timedOut)
            {
                try { ui.BeginInvoke((MethodInvoker)ShowMain); } catch { }
            }, null, -1, false);

            if (demo) TestToast();
            if (!startHidden) ShowMain();
#if TEST
            if (Demo.On) Demo.Run(delegate { return main; }, Quit);
#endif
        }

        void OnSysPrefs(object sender, UserPreferenceChangedEventArgs e)
        {
            if (Theme.Pref != 0) return;
            try { ui.BeginInvoke((MethodInvoker)ThemeChanged); } catch { }
        }

        public static bool IsAdmin()
        {
#if TEST
            if (Demo.On) return true;
#endif
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }

        static Icon MakeIcon(Color dot)
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush b = new SolidBrush(Color.FromArgb(242, 183, 5))) g.FillRectangle(b, 3, 9, 26, 18);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(214, 150, 0))) g.FillRectangle(b, 3, 5, 12, 6);
                using (SolidBrush b = new SolidBrush(Color.White)) g.FillEllipse(b, 15, 14, 16, 16);
                using (SolidBrush b = new SolidBrush(dot)) g.FillEllipse(b, 17, 16, 12, 12);
                IntPtr hi = bmp.GetHicon();
                Icon ic = (Icon)Icon.FromHandle(hi).Clone();
                DestroyIcon(hi);
                return ic;
            }
        }

        // ---- limba / tema
        void LocalizeMenu()
        {
            miOpen.Text = L.T("Deschide fereastra");
            miShares.Text = L.T("Foldere partajate");
            miLog.Text = L.T("Deschide jurnalul");
            miNotify.Text = L.T("Notificări");
            miLocal.Text = L.T("Ignoră accesările de pe acest calculator");
            miAuto.Text = L.T("Pornește automat cu Windows");
            miTest.Text = L.T("Test notificare");
            miExit.Text = L.T("Ieșire");
            tray.Text = "ShareWatch " + AppInfo.Version + L.T(" - monitorizare activă");
            if (miErr.Visible) ErrorText(Monitor == null ? 0 : Monitor.LastError);
        }

        void ApplyMenuTheme()
        {
            menu.Renderer = new ThemedRenderer();
            menu.BackColor = Theme.Panel;
            menu.ForeColor = Theme.Fg;
            foreach (ToolStripItem it in menu.Items) it.ForeColor = Theme.Fg;
            miShares.DropDown.Renderer = menu.Renderer;
            miShares.DropDown.BackColor = Theme.Panel;
            miShares.DropDown.ForeColor = Theme.Fg;
        }

        public void ThemeChanged()
        {
            Theme.Update();
            ApplyMenuTheme();
            if (main != null && !main.IsDisposed) main.ApplyTheme();
        }

        // limba schimbata: reconstruieste meniul si fereastra (pastreaza pozitia si tabul)
        public void ReloadUi()
        {
            ui.BeginInvoke((MethodInvoker)delegate
            {
                L.Apply(Settings.Lang);
                LocalizeMenu();
                if (main != null && !main.IsDisposed)
                {
                    int idx = main.CurrentTab;
                    bool vis = main.Visible;
                    main.Reloading = true;
                    main.Close();
                    main.Dispose();
                    main = null;
                    if (vis) { main = new MainForm(this, idx); main.Show(); main.Activate(); }
                }
            });
        }

        public void ShowMain()
        {
            if (main == null || main.IsDisposed) main = new MainForm(this, 0);
            if (!main.Visible) main.Show();
            if (main.WindowState == FormWindowState.Minimized) main.WindowState = FormWindowState.Normal;
            main.Activate();
            main.BringToFront();
        }

        public void TestToast()
        {
            Toast.ShowToast("public", new Peer("192.168.1.50", "PC-DEMO"), L.T("utilizator"), "\\Documente\\Contracte\\exemplu-raport-lung.docx");
        }

        // ---- apelate din thread-ul monitorului
        public void ShowToast(string share, Peer p, string user, string path, bool blocked = false)
        {
            ui.BeginInvoke((MethodInvoker)delegate { Toast.ShowToast(share, p, user, path, blocked); });
        }

        public void Activity(string kind, string share, Peer p, string user, string path)
        {
            if (kind == "ACTIV") return;
            ui.BeginInvoke((MethodInvoker)delegate
            {
                string t = L.T("Ultimul acces: ") + p.Host + " (" + p.Ip + ")";
                tray.Text = t.Length > 63 ? t.Substring(0, 63) : t;
                tray.Icon = IconAlert;
                revert.Stop(); revert.Start();
            });
        }

        void ErrorText(int err)
        {
            miErr.Text = err == 5 ? L.T("Eroare: acces refuzat - rulează ca Administrator") : L.F("Eroare netapi32: {0}", err);
        }

        public void ErrorChanged(int err)
        {
            ui.BeginInvoke((MethodInvoker)delegate
            {
                if (err == 0) { miErr.Visible = false; return; }
                ErrorText(err);
                miErr.Visible = true;
                if (err == 5)
                    tray.ShowBalloonTip(5000, "ShareWatch", L.T("Acces refuzat. Rulează aplicația ca Administrator pentru a vedea accesările."), ToolTipIcon.Warning);
            });
        }

        // ---- meniu
        void FillShares()
        {
            miShares.DropDownItems.Clear();
            int err;
            List<NetApi.ShareItem> shares = NetApi.Shares(out err);
            if (shares == null) { miShares.DropDownItems.Add(new ToolStripMenuItem(L.T("(nu pot citi lista)")) { Enabled = false }); return; }
            foreach (NetApi.ShareItem sh in shares)
            {
                if (!sh.Disk) continue;
                if (sh.Special && Settings.HideAdminShares) continue;
                string txt = (Settings.IsMonitored(sh) ? "● " : "○ ") + sh.Name + (string.IsNullOrEmpty(sh.Path) ? "" : "   →   " + sh.Path);
                ToolStripMenuItem it = new ToolStripMenuItem(txt);
                string pth = sh.Path;
                if (!string.IsNullOrEmpty(pth) && Directory.Exists(pth))
                    it.Click += delegate { Process.Start("explorer.exe", "\"" + pth + "\""); };
                else it.Enabled = false;
                it.ForeColor = sh.Special ? Theme.Muted : Theme.Fg;
                miShares.DropDownItems.Add(it);
            }
            if (miShares.DropDownItems.Count == 0)
                miShares.DropDownItems.Add(new ToolStripMenuItem(L.T("(niciun folder partajat)")) { Enabled = false });
        }

        public void OpenLog()
        {
            try
            {
                if (!File.Exists(Settings.LogPath)) File.WriteAllText(Settings.LogPath, "");
                Process.Start("notepad.exe", "\"" + Settings.LogPath + "\"");
            }
            catch { }
        }

        static int Run(string exe, string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo(exe, args);
            psi.CreateNoWindow = true; psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true; psi.RedirectStandardError = true;
            using (Process p = Process.Start(psi))
            {
                p.StandardOutput.ReadToEnd(); p.StandardError.ReadToEnd();
                p.WaitForExit();
                return p.ExitCode;
            }
        }

        public static bool AutostartEnabled()
        {
            try { return Run("schtasks.exe", "/Query /TN " + TaskName) == 0; } catch { return false; }
        }

        // creeaza / sterge sarcina programata (logon, drepturi maxime, fara UAC); true = reusit
        public bool SetAutostart(bool on)
        {
            try
            {
                if (!on)
                {
                    if (!AutostartEnabled()) return true;
                    return Run("schtasks.exe", "/Delete /TN " + TaskName + " /F") == 0;
                }
                string user = WindowsIdentity.GetCurrent().Name;
                string exe = Application.ExecutablePath;
                string xml =
"<?xml version=\"1.0\" encoding=\"UTF-16\"?>\r\n" +
"<Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">\r\n" +
" <RegistrationInfo><Description>ShareWatch - monitorizare foldere partajate</Description></RegistrationInfo>\r\n" +
" <Triggers><LogonTrigger><Enabled>true</Enabled><UserId>" + SecurityElement.Escape(user) + "</UserId><Delay>PT10S</Delay></LogonTrigger></Triggers>\r\n" +
" <Principals><Principal id=\"Author\"><UserId>" + SecurityElement.Escape(user) + "</UserId><LogonType>InteractiveToken</LogonType><RunLevel>HighestAvailable</RunLevel></Principal></Principals>\r\n" +
" <Settings><MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy><DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries><StopIfGoingOnBatteries>false</StopIfGoingOnBatteries><AllowHardTerminate>true</AllowHardTerminate><StartWhenAvailable>true</StartWhenAvailable><Enabled>true</Enabled><ExecutionTimeLimit>PT0S</ExecutionTimeLimit></Settings>\r\n" +
" <Actions Context=\"Author\"><Exec><Command>" + SecurityElement.Escape(exe) + "</Command><Arguments>--tray</Arguments></Exec></Actions>\r\n" +
"</Task>";
                string tmp = Path.Combine(Path.GetTempPath(), "sharewatch_task.xml");
                File.WriteAllText(tmp, xml, Encoding.Unicode);
                int rc = Run("schtasks.exe", "/Create /TN " + TaskName + " /XML \"" + tmp + "\" /F");
                try { File.Delete(tmp); } catch { }
                return rc == 0;
            }
            catch { return false; }
        }

        void ToggleAutostart()
        {
            bool want = !AutostartEnabled();
            bool ok = SetAutostart(want);
            if (ok) tray.ShowBalloonTip(3000, "ShareWatch", want ? L.T("Pornește automat la autentificare, direct în tray.") : L.T("Pornirea automată a fost dezactivată."), ToolTipIcon.Info);
            else tray.ShowBalloonTip(4000, "ShareWatch", L.T("Nu am putut modifica pornirea automată (rulează ca Administrator)."), ToolTipIcon.Error);
        }

        void Quit()
        {
            Quitting = true;
            SystemEvents.UserPreferenceChanged -= OnSysPrefs;
            Monitor.Stop();
            trayRetry.Stop();
            if (taskbar != null) taskbar.DestroyHandle();
            tray.Visible = false;
            tray.Dispose();
            if (main != null && !main.IsDisposed) main.Close();
            Logger.Raw(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | STOP        | ShareWatch oprit");
            ExitThread();
        }
    }
}
