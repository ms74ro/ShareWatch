// Fereastra principala cu taburi: Monitorizare | Foldere monitorizate | Istoric | Optiuni | Despre
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace ShareWatch
{
    // buton de tab (bara de taburi proprie, ca sa putem aplica tema)
    class TabButton : Control
    {
        public bool Selected;
        readonly int bar, gap, pad;
        readonly string glyph;
        static Font iconFont;

        // font de iconite din Windows: Segoe Fluent Icons (Win 11) sau Segoe MDL2 Assets (Win 10)
        internal static Font IconFont()
        {
            if (iconFont != null) return iconFont;
            foreach (string n in new string[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" })
            {
                Font f = new Font(n, 11f);
                if (f.Name == n) { iconFont = f; return f; }
                f.Dispose();
            }
            iconFont = new Font("Segoe UI Symbol", 11f);
            return iconFont;
        }

        public TabButton(string text, string glyph, float scale)
        {
            Text = text; this.glyph = glyph;
            bar = (int)Math.Round(3 * scale);
            gap = (int)Math.Round(7 * scale);
            pad = (int)Math.Round(16 * scale);
            Cursor = Cursors.Hand;
            Margin = new Padding(0);
            Font = new Font("Segoe UI", 9.5f);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            int iw = TextRenderer.MeasureText(glyph, IconFont(), Size.Empty, TextFormatFlags.NoPadding).Width;
            int tw = TextRenderer.MeasureText(text, Font, Size.Empty, TextFormatFlags.NoPadding).Width;
            Width = pad + iw + gap + tw + pad;
            Height = (int)Math.Round(40 * scale);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
        bool hover;

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush b = new SolidBrush(hover && !Selected ? Theme.Hover : BackColor)) g.FillRectangle(b, ClientRectangle);
            Color fg = Selected ? Theme.Fg : Theme.Muted;
            Color ic = Selected ? Theme.Accent : Theme.Muted;
            Size isz = TextRenderer.MeasureText(glyph, IconFont(), Size.Empty, TextFormatFlags.NoPadding);
            int x = pad;
            TextRenderer.DrawText(g, glyph, IconFont(), new Rectangle(x, 0, isz.Width, Height - bar), ic,
                TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            x += isz.Width + gap;
            TextRenderer.DrawText(g, Text, Font, new Rectangle(x, 0, Width - x, Height - bar), fg,
                TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
            if (Selected)
                using (SolidBrush b = new SolidBrush(Theme.Accent))
                    g.FillRectangle(b, 0, Height - bar, Width, bar);
        }
    }

    // iconita clicabila langa un link (Despre): sigla GitHub sau o glifa din fontul de iconite
    class LinkIcon : Control
    {
        // sigla GitHub (Octicons mark-github, MIT), 16x16, curbe Bezier continue
        static readonly float[] GitHubMark = {
            6.766f, 11.328f, 4.703f, 11.078f, 3.250f, 9.594f, 3.250f, 7.672f, 3.250f, 6.891f, 3.531f, 6.047f, 4f, 5.484f,
            3.797f, 4.969f, 3.828f, 3.875f, 4.063f, 3.422f, 4.688f, 3.344f, 5.531f, 3.672f, 6.031f, 4.125f, 6.625f,
            3.938f, 7.250f, 3.844f, 8.016f, 3.844f, 8.781f, 3.844f, 9.406f, 3.938f, 9.969f, 4.109f, 10.453f, 3.672f,
            11.313f, 3.344f, 11.938f, 3.422f, 12.156f, 3.844f, 12.188f, 4.937f, 11.984f, 5.469f, 12.484f, 6.062f, 12.750f,
            6.859f, 12.750f, 7.672f, 12.750f, 9.594f, 11.297f, 11.047f, 9.203f, 11.312f, 9.734f, 11.656f, 10.093f,
            12.406f, 10.093f, 13.266f, 10.093f, 13.266f, 10.093f, 14.891f, 10.093f, 14.891f, 10.093f, 15.359f, 10.484f,
            15.625f, 10.953f, 15.438f, 13.781f, 14.359f, 16f, 11.530f, 16f, 8.030f, 16f, 3.610f, 12.406f, 0f, 7.984f, 0f,
            3.563f, 0f, 0f, 3.610f, 0f, 8.031f, -0.009f, 11.347f, 2.058f, 14.314f, 5.172f, 15.453f, 5.594f, 15.609f, 6f,
            15.328f, 6f, 14.906f, 6f, 14.906f, 6f, 13.656f, 6f, 13.656f, 5.781f, 13.750f, 5.500f, 13.812f, 5.250f,
            13.812f, 4.219f, 13.812f, 3.610f, 13.250f, 3.172f, 12.203f, 3f, 11.781f, 2.812f, 11.531f, 2.453f, 11.484f,
            2.266f, 11.469f, 2.203f, 11.391f, 2.203f, 11.297f, 2.203f, 11.109f, 2.516f, 10.969f, 2.828f, 10.969f, 3.281f,
            10.969f, 3.672f, 11.250f, 4.078f, 11.829f, 4.391f, 12.281f, 4.718f, 12.484f, 5.109f, 12.484f, 5.500f, 12.484f,
            5.750f, 12.344f, 6.109f, 11.984f, 6.375f, 11.719f, 6.579f, 11.484f, 6.766f, 11.328f };

        readonly string glyph;   // null = sigla GitHub

        public LinkIcon(string glyph, int size)
        {
            this.glyph = glyph;
            Size = new Size(size, size);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if (glyph != null)
            {
                TextRenderer.DrawText(g, glyph, TabButton.IconFont(), ClientRectangle, Theme.Fg,
                    TextFormatFlags.NoPadding | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }
            PointF[] pts = new PointF[GitHubMark.Length / 2];
            float k = Math.Min(Width, Height) / 16f * 0.9f, o = Math.Min(Width, Height) * 0.05f;
            for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(o + GitHubMark[2 * i] * k, o + GitHubMark[2 * i + 1] * k);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath())
            using (SolidBrush b = new SolidBrush(Theme.Fg))
            {
                gp.AddBeziers(pts);
                gp.CloseFigure();
                g.FillPath(b, gp);
            }
        }
    }

    class MainForm : Form
    {
        readonly TrayApp app;
        readonly float sc;
        public bool Reloading;
        int current = -1;

        readonly FlowLayoutPanel bar = new FlowLayoutPanel();
        readonly Panel content = new Panel();
        readonly List<TabButton> buttons = new List<TabButton>();
        readonly List<Panel> pages = new List<Panel>();
        readonly System.Windows.Forms.Timer tick = new System.Windows.Forms.Timer();

        // ordinea canonica a tipurilor de eveniment din jurnal (jurnalul pastreaza mereu aceste valori)
        static readonly string[] Kinds = { "CONECTARE", "FISIER", "DECONECTARE", "DECONECTAT", "BLOCAT", "PARTAJARE", "ANULARE", "ACTIV", "START", "STOP", "EROARE" };

        // Monitorizare
        Label lblStatus, lblConn, lblFiles;
        ListView lvConn, lvFiles;
        SplitContainer split;
        string sigConn = null, sigFiles = null;

        // Foldere
        ListView lvShares;
        bool loadingShares;

        // Istoric
        ListView lvHist;
        TextBox txtFilter;
        ComboBox cbKind;
        Label lblHist;
        long histLen = -1;
        DateTime histTime = DateTime.MinValue;

        // Optiuni
        CheckBox chkNotify, chkLocal, chkAuto, chkSound, chkBlock, chkNotifyBlocked, chkFirewall, chkHideAdmin, chkTrayClick;
        List<L.LangFile> langFiles = new List<L.LangFile>();
        Label lblFirewall;
        ListView lvBlock;
        TextBox txtBlock;
        int blockVersion = -1;
        NumericUpDown numToast, numCool;
        ComboBox cbTheme, cbLang;
        bool loadingOpt;

        // ordinea taburilor (trebuie sa corespunda cu names/glyphs din constructor)
        const int TabMonitor = 0, TabHistory = 1, TabFolders = 2, TabOptions = 3, TabAbout = 4;

        public int CurrentTab { get { return current < 0 ? 0 : current; } }
        int S(int v) { return (int)Math.Round(v * sc); }

        public MainForm(TrayApp app, int startTab)
        {
            this.app = app;
            using (Graphics g = CreateGraphics()) sc = g.DpiY / 96f;
            Text = "ShareWatch " + AppInfo.Version + L.T(" - monitorizare foldere partajate");
            Font = new Font("Segoe UI", 9f);
            Icon = app.IconOk;
            Size = new Size(S(1000), S(660));
            MinimumSize = new Size(S(760), S(480));
            RestoreGeometry();

            bar.Dock = DockStyle.Top; bar.Height = S(40); bar.WrapContents = false; bar.Padding = new Padding(S(8), 0, 0, 0);
            content.Dock = DockStyle.Fill;
            Controls.Add(content);
            Controls.Add(bar);

            string[] names = { "Monitorizare", "Istoric", "Foldere monitorizate", "Opțiuni", "Despre" };
            // glife Segoe: ecran/monitorizare, istoric, folder, setari, informatii
            string[] glyphs = { "", "", "", "", "" };
            for (int i = 0; i < names.Length; i++)
            {
                int idx = i;
                TabButton b = new TabButton(L.T(names[i]), glyphs[i], sc);
                b.Click += delegate { SelectTab(idx); };
                buttons.Add(b);
                bar.Controls.Add(b);
                Panel p = new Panel();
                p.Dock = DockStyle.Fill; p.Padding = new Padding(S(10)); p.Visible = false;
                pages.Add(p);
                content.Controls.Add(p);
            }

            BuildMonitor(pages[TabMonitor]);
            BuildHistory(pages[TabHistory]);
            BuildFolders(pages[TabFolders]);
            BuildOptions(pages[TabOptions]);
            BuildAbout(pages[TabAbout]);

            ApplyTheme();
            SelectTab(startTab);

            tick.Interval = 1000;
            tick.Tick += delegate { if (Visible) TickRefresh(); };
            tick.Start();
            Shown += delegate { split.SplitterDistance = Math.Max(S(80), split.Height / 2); };
        }

        // ------------------------------------------------------------ pozitie / dimensiune
        void RestoreGeometry()
        {
            StartPosition = FormStartPosition.CenterScreen;
            if (Settings.WinW <= 0) return;
            Rectangle r = new Rectangle(Settings.WinX, Settings.WinY, Settings.WinW, Settings.WinH);
            foreach (Screen s in Screen.AllScreens)
            {
                Rectangle i = Rectangle.Intersect(s.WorkingArea, r);
                if (i.Width >= S(120) && i.Height >= S(60))
                {
                    StartPosition = FormStartPosition.Manual;
                    Bounds = r;
                    if (Settings.WinMax) WindowState = FormWindowState.Maximized;
                    return;
                }
            }
        }

        void SaveGeometry()
        {
            Rectangle b = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            if (b.Width < 100 || b.Height < 100) return;
            Settings.WinX = b.X; Settings.WinY = b.Y; Settings.WinW = b.Width; Settings.WinH = b.Height;
            Settings.WinMax = WindowState == FormWindowState.Maximized;
            Settings.Save();
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            SaveGeometry();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveGeometry();
            if (!app.Quitting && !Reloading && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); return; }
            tick.Stop();
            base.OnFormClosing(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.TitleBar(this);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible) RefreshTab();
        }

        // ------------------------------------------------------------ tema
        public void ApplyTheme()
        {
            SuspendLayout();
            BackColor = Theme.Bg; ForeColor = Theme.Fg;
            bar.BackColor = Theme.Panel;
            foreach (TabButton b in buttons) { b.BackColor = Theme.Panel; b.ForeColor = Theme.Fg; b.Invalidate(); }
            foreach (Panel p in pages) Theme.Apply(p);
            ThemeMenus();
            Theme.TitleBar(this);
            ResumeLayout(true);
            // listele cu culori proprii pe elemente trebuie reincarcate
            if (lvShares != null && current == TabFolders) LoadShares();
            if (lvHist != null && current == TabHistory) LoadHistory();
            Invalidate(true);
        }

        // ------------------------------------------------------------ utilitare
        public void SelectTab(int i)
        {
            if (i < 0 || i >= pages.Count) i = 0;
            current = i;
            for (int k = 0; k < pages.Count; k++)
            {
                pages[k].Visible = k == i;
                buttons[k].Selected = k == i;
                buttons[k].Invalidate();
            }
            RefreshTab();
        }

        ListView NewList(string[] cols, int[] widths)
        {
            ListView lv = new ListView();
            lv.Dock = DockStyle.Fill;
            lv.View = View.Details;
            lv.FullRowSelect = true;
            lv.GridLines = false;
            lv.HideSelection = false;
            lv.MultiSelect = true;
            for (int i = 0; i < cols.Length; i++) lv.Columns.Add(L.T(cols[i]), S(widths[i]));
            // antet desenat de noi, ca sa respecte tema; randurile raman cele implicite
            lv.OwnerDraw = true;
            lv.DrawItem += delegate(object o, DrawListViewItemEventArgs e) { };
            lv.DrawSubItem += delegate(object o, DrawListViewSubItemEventArgs e)
            {
                // randuri desenate de noi: selectia trebuie sa ramana lizibila in orice tema
                bool sel = e.Item.Selected;
                Color bg = sel ? (lv.Focused ? Theme.SelBg : Theme.SelBgInactive) : lv.BackColor;
                Color fg = sel ? Theme.SelFg : e.Item.ForeColor;
                using (SolidBrush b = new SolidBrush(bg)) e.Graphics.FillRectangle(b, e.Bounds);
                int left = e.Bounds.Left + S(6);
                if (e.ColumnIndex == 0 && lv.CheckBoxes)
                {
                    Size cs = CheckBoxRenderer.GetGlyphSize(e.Graphics, System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
                    Point cp = new Point(e.Bounds.Left + S(4), e.Bounds.Top + (e.Bounds.Height - cs.Height) / 2);
                    CheckBoxRenderer.DrawCheckBox(e.Graphics, cp, e.Item.Checked
                        ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal
                        : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal);
                    left = cp.X + cs.Width + S(6);
                }
                Rectangle tr = new Rectangle(left, e.Bounds.Top, Math.Max(0, e.Bounds.Right - left - S(4)), e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, lv.Font, tr, fg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            };
            lv.DrawColumnHeader += delegate(object o, DrawListViewColumnHeaderEventArgs e)
            {
                using (SolidBrush bg = new SolidBrush(Theme.HeaderBg)) e.Graphics.FillRectangle(bg, e.Bounds);
                using (Pen pen = new Pen(Theme.Border))
                {
                    e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                    e.Graphics.DrawLine(pen, e.Bounds.Right - 1, e.Bounds.Top + 3, e.Bounds.Right - 1, e.Bounds.Bottom - 4);
                }
                Rectangle tr = new Rectangle(e.Bounds.Left + S(6), e.Bounds.Top, Math.Max(0, e.Bounds.Width - S(10)), e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, e.Header.Text, lv.Font, tr, Theme.Fg,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            };
            // ultima coloana umple restul latimii (altfel ramane o zona de antet nedesenata)
            int lastMin = S(widths[widths.Length - 1]);
            Action fit = delegate
            {
                int n = lv.Columns.Count;
                if (n == 0) return;
                int used = 0;
                for (int i = 0; i < n - 1; i++) used += lv.Columns[i].Width;
                int w = Math.Max(lastMin, lv.ClientSize.Width - used);
                if (lv.Columns[n - 1].Width != w) lv.Columns[n - 1].Width = w;
            };
            lv.Resize += delegate { fit(); };
            lv.ColumnWidthChanged += delegate(object o, ColumnWidthChangedEventArgs e) { fit(); };
            typeof(Control).InvokeMember("DoubleBuffered", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic, null, lv, new object[] { true });
            return lv;
        }

        static Label Header(string text, int h)
        {
            Label l = new Label();
            l.Text = text; l.Dock = DockStyle.Top; l.Height = h; l.Tag = "head";
            l.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            l.TextAlign = ContentAlignment.MiddleLeft;
            return l;
        }

        static void Fill(ListView lv, List<string[]> rows, List<object> tags, ref string sig)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string[] r in rows) sb.Append(string.Join("\u0001", r)).Append('\u0002');
            string ns = sb.ToString();
            if (ns == sig)
            {
                if (tags != null) for (int i = 0; i < lv.Items.Count && i < tags.Count; i++) lv.Items[i].Tag = tags[i];
                return;
            }
            sig = ns;
            lv.BeginUpdate();
            lv.Items.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                ListViewItem it = new ListViewItem(rows[i]);
                if (tags != null) it.Tag = tags[i];
                lv.Items.Add(it);
            }
            lv.EndUpdate();
        }

        // meniu contextual cu tema aplicata; apare doar cand click-ul dreapta e pe un rand
        readonly List<ContextMenuStrip> menus = new List<ContextMenuStrip>();
        ContextMenuStrip RowMenu(ListView lv, params ToolStripItem[] items)
        {
            ContextMenuStrip cm = new ContextMenuStrip();
            cm.Items.AddRange(items);
            cm.Opening += delegate(object o, System.ComponentModel.CancelEventArgs e)
            {
                if (lv.HitTest(lv.PointToClient(Cursor.Position)).Item == null || lv.SelectedItems.Count == 0) e.Cancel = true;
            };
            lv.ContextMenuStrip = cm;
            menus.Add(cm);
            return cm;
        }

        void ThemeMenus()
        {
            foreach (ContextMenuStrip m in menus)
            {
                m.Renderer = new ThemedRenderer();
                m.BackColor = Theme.Panel; m.ForeColor = Theme.Fg;
                foreach (ToolStripItem it in m.Items) it.ForeColor = Theme.Fg;
            }
        }

        void TickRefresh()
        {
            if (current == TabMonitor) UpdateMonitor();
            else if (current == TabHistory) CheckHistoryChanged();
            else if (current == TabOptions)
            {
                if (BlockList.Version != blockVersion) LoadBlockList();   // fisierul a fost editat din afara
                if (lblFirewall.Text != FirewallBlock.Status && FirewallBlock.Status.Length > 0) lblFirewall.Text = FirewallBlock.Status;
            }
        }

        void RefreshTab()
        {
            if (!Visible) return;
            if (current == TabMonitor) UpdateMonitor();
            else if (current == TabFolders) LoadShares();
            else if (current == TabHistory) LoadHistory();
            else if (current == TabOptions) LoadOptions();
        }

        // ------------------------------------------------------------ tab Monitorizare
        void BuildMonitor(Panel page)
        {
            lblStatus = new Label();
            lblStatus.Dock = DockStyle.Top; lblStatus.Height = S(24); lblStatus.Tag = "error";
            lblStatus.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblStatus.Visible = false;

            split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.Orientation = Orientation.Horizontal;

            lvConn = NewList(new string[] { "Calculator (PC)", "IP", "Utilizator", "Partajare", "Conectat de la" }, new int[] { 200, 150, 170, 150, 150 });
            lblConn = Header("", S(26));
            split.Panel1.Controls.Add(lvConn);
            split.Panel1.Controls.Add(lblConn);
            ToolStripMenuItem miDisc = new ToolStripMenuItem(L.T("Deconectează calculatorul"));
            miDisc.Click += delegate { DisconnectSelected(); };
            ToolStripMenuItem miBlock = new ToolStripMenuItem(L.T("Blochează calculatorul"));
            miBlock.Click += delegate { BlockSelected(); };
            RowMenu(lvConn, miDisc, miBlock);

            lvFiles = NewList(new string[] { "Utilizator", "Calculator (PC)", "IP", "Partajare", "Fișier / folder deschis" }, new int[] { 150, 170, 130, 120, 500 });
            lblFiles = Header("", S(26));
            split.Panel2.Controls.Add(lvFiles);
            split.Panel2.Controls.Add(lblFiles);

            page.Controls.Add(split);
            page.Controls.Add(lblStatus);
        }

        static string Host(Peer p, string raw) { return p == null ? raw.TrimStart('\\') : p.Host; }
        static string Ip(Peer p) { return p == null ? "..." : p.Ip; }

        void UpdateMonitor()
        {
            int err = app.Monitor.LastError;
            lblStatus.Visible = err != 0;
            if (err != 0) lblStatus.Text = err == 5 ? L.T("Acces refuzat - rulează aplicația ca Administrator pentru a vedea conexiunile.") : L.F("Eroare la citirea conexiunilor (netapi32: {0}).", err);

            Snapshot s = app.Monitor.Snap;
            List<string[]> cr = new List<string[]>();
            List<object> ct = new List<object>();
            foreach (ConnInfo c in s.Conns)
            {
                if (Settings.IgnoreLocal && Resolver.IsLocal(c.Peer)) continue;
                cr.Add(new string[] { Host(c.Peer, c.Raw), Ip(c.Peer), c.User, c.Share, c.Since.ToString("dd.MM HH:mm:ss") });
                ct.Add(c);
            }
            List<string[]> fr = new List<string[]>();
            foreach (FileRow f in s.Files)
            {
                if (Settings.IgnoreLocal && Resolver.IsLocal(f.Peer)) continue;
                fr.Add(new string[] { f.User, f.Peer == null ? "?" : f.Peer.Host, Ip(f.Peer), f.Share, f.Path });
            }
            lblConn.Text = L.F("Conectați acum ({0})", cr.Count);
            lblFiles.Text = L.F("Fișiere deschise acum ({0})", fr.Count);
            Fill(lvConn, cr, ct, ref sigConn);
            Fill(lvFiles, fr, null, ref sigFiles);
        }

        void BlockSelected()
        {
            // pe fiecare calculator: IP-ul si numele (cand le stim), pe aceeasi linie din blocklist.txt
            List<string> lines = new List<string>(), names = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ListViewItem it in lvConn.SelectedItems)
            {
                ConnInfo c = it.Tag as ConnInfo;
                if (c == null || !seen.Add(c.Raw)) continue;
                string raw = c.Raw.TrimStart('\\');
                List<string> toks = new List<string>();
                if (c.Peer != null && c.Peer.Ip != "?") toks.Add(c.Peer.Ip);
                if (c.Peer != null && c.Peer.Host != "?" && !toks.Contains(c.Peer.Host)) toks.Add(c.Peer.Host);
                if (!toks.Contains(raw)) toks.Add(raw);
                lines.Add(string.Join(" ", toks.ToArray()));
                names.Add(Host(c.Peer, c.Raw) + " (" + Ip(c.Peer) + ")");
            }
            if (lines.Count == 0) return;
            if (MessageBox.Show(this, L.F("Blochezi:\r\n{0}\r\n\r\nCalculatorul este deconectat acum și la fiecare încercare de conectare, până îl ștergi din lista de blocare (Opțiuni).",
                    string.Join("\r\n", names.ToArray())), "ShareWatch", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            if (!Settings.BlockEnabled)
            {
                Settings.BlockEnabled = true; Settings.Save();   // altfel blocarea n-ar avea efect
            }
            foreach (string l in lines) AddBlockEntry(l, L.F("blocat din Monitorizare {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm")));
            sigConn = null;
        }

        void DisconnectSelected()
        {
            // un calculator poate aparea pe mai multe randuri (cate unul pe partajare): o singura deconectare pe client+utilizator
            List<ConnInfo> list = new List<ConnInfo>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ListViewItem it in lvConn.SelectedItems)
            {
                ConnInfo c = it.Tag as ConnInfo;
                if (c != null && seen.Add(c.Raw + "|" + c.User)) list.Add(c);
            }
            if (list.Count == 0) return;
            List<string> names = new List<string>();
            foreach (ConnInfo c in list) names.Add(Host(c.Peer, c.Raw) + " (" + Ip(c.Peer) + ")" + (string.IsNullOrEmpty(c.User) ? "" : ", " + c.User));
            if (MessageBox.Show(this, L.F("Deconectezi:\r\n{0}\r\n\r\nFișierele deschise de pe acest calculator se închid, iar datele nesalvate se pot pierde. Windows se poate reconecta automat la următoarea accesare.",
                    string.Join("\r\n", names.ToArray())), "ShareWatch", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

            List<string> errors = new List<string>();
            foreach (ConnInfo c in list)
            {
                int rc = NetApi.Disconnect(c.Raw, c.User);
                Peer p = c.Peer ?? new Peer("?", c.Raw.TrimStart('\\'));
                if (rc == 0) Logger.Write("DECONECTAT", c.Share, p.Host, p.Ip, c.User, null);
                else errors.Add(p.Host + ": " + NetApi.ErrText(rc));
            }
            if (errors.Count > 0)
                MessageBox.Show(this, string.Join("\r\n", errors.ToArray()), "ShareWatch", MessageBoxButtons.OK, MessageBoxIcon.Error);
            sigConn = null;
            UpdateMonitor();
        }

        // ------------------------------------------------------------ tab Foldere monitorizate
        void BuildFolders(Panel page)
        {
            lvShares = NewList(new string[] { "Folder partajat", "Cale locală", "Tip", "Comentariu" }, new int[] { 200, 420, 130, 200 });
            lvShares.CheckBoxes = true;
            lvShares.ItemChecked += delegate(object o, ItemCheckedEventArgs e)
            {
                if (loadingShares) return;
                Settings.SetMonitored((string)e.Item.Tag, e.Item.Checked);
            };

            FlowLayoutPanel top = new FlowLayoutPanel();
            top.Dock = DockStyle.Top; top.Height = S(80); top.WrapContents = true;
            Label info = new Label();
            info.AutoSize = false; info.Width = S(900); info.Height = S(38); info.Tag = "muted";
            info.Text = L.T("Bifează folderele partajate care vor fi monitorizate (notificări și jurnal). Modificările se aplică imediat. Partajările administrative (C$, ADMIN$ etc.) sunt dezactivate implicit.");
            Button bRef = new Button { Text = L.T("Reîmprospătează"), AutoSize = true };
            Button bAll = new Button { Text = L.T("Bifează tot"), AutoSize = true };
            Button bNone = new Button { Text = L.T("Debifează tot"), AutoSize = true };
            Button bAdd = new Button { Text = L.T("Partajează un folder..."), AutoSize = true, Margin = new Padding(S(16), 3, 3, 3) };
            Button bDel = new Button { Text = L.T("Oprește partajarea"), AutoSize = true };
            bRef.Click += delegate { LoadShares(); };
            bAll.Click += delegate { foreach (ListViewItem it in lvShares.Items) it.Checked = true; };
            bNone.Click += delegate { foreach (ListViewItem it in lvShares.Items) it.Checked = false; };
            bAdd.Click += delegate { NewShare(); };
            bDel.Click += delegate { StopSharing(); };
            top.Controls.Add(info);
            top.SetFlowBreak(info, true);
            top.Controls.Add(bRef); top.Controls.Add(bAll); top.Controls.Add(bNone); top.Controls.Add(bAdd); top.Controls.Add(bDel);

            ToolStripMenuItem miOpenF = new ToolStripMenuItem(L.T("Deschide folderul"));
            ToolStripMenuItem miStop = new ToolStripMenuItem(L.T("Oprește partajarea"));
            miOpenF.Click += delegate
            {
                if (lvShares.SelectedItems.Count == 0) return;
                string pth = lvShares.SelectedItems[0].SubItems[1].Text;
                if (Directory.Exists(pth)) System.Diagnostics.Process.Start("explorer.exe", "\"" + pth + "\"");
            };
            miStop.Click += delegate { StopSharing(); };
            RowMenu(lvShares, miOpenF, miStop);

            page.Controls.Add(lvShares);
            page.Controls.Add(top);
        }

        void NewShare()
        {
            using (ShareDialog d = new ShareDialog(sc))
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                int rc = NetApi.AddShare(d.ShareName, d.FolderPath, d.Remark, d.AllowWrite);
                if (rc != 0)
                {
                    MessageBox.Show(this, L.T("Nu am putut partaja folderul.") + "\r\n" + NetApi.ErrText(rc), "ShareWatch", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                Settings.SetMonitored(d.ShareName, true);
                Logger.Write("PARTAJARE", d.ShareName, Environment.MachineName, "-", Environment.UserName,
                    d.FolderPath + (d.AllowWrite ? L.T(" (citire și scriere)") : L.T(" (doar citire)")));
                LoadShares();
            }
        }

        void StopSharing()
        {
            List<string[]> sel = new List<string[]>();   // nume, cale
            foreach (ListViewItem it in lvShares.SelectedItems)
            {
                if (it.SubItems[2].Text == L.T("Administrativ")) continue;   // C$, ADMIN$: le gestioneaza Windows
                sel.Add(new string[] { (string)it.Tag, it.SubItems[1].Text });
            }
            if (sel.Count == 0)
            {
                MessageBox.Show(this, L.T("Selectează o partajare obișnuită (nu administrativă) din listă."), "ShareWatch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            List<string> names = new List<string>();
            foreach (string[] s in sel) names.Add(s[0] + "  →  " + s[1]);
            if (MessageBox.Show(this, L.F("Oprești partajarea pentru:\r\n{0}\r\n\r\nFolderul și fișierele rămân pe disc; doar nu mai sunt accesibile din rețea. Cine e conectat acum pierde accesul.",
                    string.Join("\r\n", names.ToArray())), "ShareWatch", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            List<string> errors = new List<string>();
            foreach (string[] s in sel)
            {
                int rc = NetApi.DelShare(s[0]);
                if (rc == 0) Logger.Write("ANULARE", s[0], Environment.MachineName, "-", Environment.UserName, s[1]);
                else errors.Add(s[0] + ": " + NetApi.ErrText(rc));
            }
            if (errors.Count > 0)
                MessageBox.Show(this, string.Join("\r\n", errors.ToArray()), "ShareWatch", MessageBoxButtons.OK, MessageBoxIcon.Error);
            LoadShares();
        }

        void LoadShares()
        {
            int err;
            List<NetApi.ShareItem> shares = NetApi.Shares(out err);
            loadingShares = true;
            lvShares.BeginUpdate();
            lvShares.Items.Clear();
            if (shares != null)
                foreach (NetApi.ShareItem sh in shares)
                {
                    if (!sh.Disk) continue;
                    if (sh.Special && Settings.HideAdminShares) continue;
                    ListViewItem it = new ListViewItem(new string[] { sh.Name, sh.Path ?? "", sh.Special ? L.T("Administrativ") : L.T("Utilizator"), sh.Remark ?? "" });
                    it.Tag = sh.Name;
                    it.Checked = Settings.IsMonitored(sh);
                    if (sh.Special) it.ForeColor = Theme.Muted;
                    lvShares.Items.Add(it);
                }
            lvShares.EndUpdate();
            loadingShares = false;
        }

        // ------------------------------------------------------------ tab Istoric
        void BuildHistory(Panel page)
        {
            lvHist = NewList(new string[] { "Data / ora", "Eveniment", "Partajare", "Calculator (PC)", "IP", "Utilizator", "Cale / detalii" },
                             new int[] { 150, 110, 110, 150, 120, 130, 450 });

            FlowLayoutPanel top = new FlowLayoutPanel();
            top.Dock = DockStyle.Top; top.Height = S(44); top.WrapContents = false;
            Label l1 = new Label { Text = L.T("Filtru:"), AutoSize = true, Margin = new Padding(0, S(7), 4, 0) };
            txtFilter = new TextBox { Width = S(200) };
            Label l2 = new Label { Text = L.T("Eveniment:"), AutoSize = true, Margin = new Padding(S(12), S(7), 4, 0) };
            cbKind = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = S(130) };
            cbKind.Items.Add(L.T("Toate"));
            foreach (string k in Kinds) cbKind.Items.Add(L.T(k));
            cbKind.SelectedIndex = 0;
            Button bRef = new Button { Text = L.T("Reîmprospătează"), AutoSize = true };
            Button bOpen = new Button { Text = L.T("Deschide în Notepad"), AutoSize = true };
            Button bClear = new Button { Text = L.T("Golește jurnalul"), AutoSize = true };
            lblHist = new Label { AutoSize = true, Margin = new Padding(S(12), S(7), 0, 0), Tag = "muted" };
            txtFilter.TextChanged += delegate { LoadHistory(); };
            cbKind.SelectedIndexChanged += delegate { LoadHistory(); };
            bRef.Click += delegate { LoadHistory(); };
            bOpen.Click += delegate { app.OpenLog(); };
            bClear.Click += delegate
            {
                if (MessageBox.Show(this, L.T("Ștergi tot conținutul jurnalului? Operația nu poate fi anulată."), "ShareWatch",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                { Logger.Clear(); LoadHistory(); }
            };
            top.Controls.AddRange(new Control[] { l1, txtFilter, l2, cbKind, bRef, bOpen, bClear, lblHist });

            page.Controls.Add(lvHist);
            page.Controls.Add(top);
        }

        static string[] Parse(string line)
        {
            string[] p = line.Split(new string[] { " | " }, StringSplitOptions.None);
            string[] r = new string[7];
            r[0] = p[0];
            if (p.Length > 1) r[1] = p[1].Trim();
            for (int i = 2; i < p.Length; i++)
            {
                string s = p[i];
                if (s.StartsWith("share: ")) r[2] = s.Substring(7);
                else if (s.StartsWith("pc: ")) r[3] = s.Substring(4);
                else if (s.StartsWith("ip: ")) r[4] = s.Substring(4);
                else if (s.StartsWith("user: ")) r[5] = s.Substring(6);
                else if (s.StartsWith("cale: ")) r[6] = s.Substring(6);
                else if (r[6] == null) r[6] = s;
            }
            for (int i = 0; i < r.Length; i++) if (r[i] == null) r[i] = "";
            return r;
        }

        void LoadHistory()
        {
            List<string> lines = new List<string>();
            try
            {
                FileInfo fi = new FileInfo(Settings.LogPath);
                if (fi.Exists)
                {
                    histLen = fi.Length; histTime = fi.LastWriteTime;
                    using (FileStream fs = new FileStream(Settings.LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                    {
                        string l;
                        while ((l = sr.ReadLine()) != null) if (l.Length > 0) lines.Add(l);
                    }
                }
                else { histLen = -1; }
            }
            catch { }

            string filter = txtFilter.Text.Trim();
            string kind = cbKind.SelectedIndex <= 0 ? null : Kinds[cbKind.SelectedIndex - 1];
            const int Max = 5000;
            int total = 0;
            lvHist.BeginUpdate();
            lvHist.Items.Clear();
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                string line = lines[i];
                if (filter.Length > 0 && line.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                string[] r = Parse(line);
                if (kind != null && r[1] != kind) continue;
                total++;
                if (lvHist.Items.Count >= Max) continue;
                string canon = r[1];
                r[1] = L.T(canon);
                ListViewItem it = new ListViewItem(r);
                switch (canon)
                {
                    case "CONECTARE": it.ForeColor = Theme.Good; break;
                    case "FISIER": it.ForeColor = Theme.Info; break;
                    case "DECONECTARE": it.ForeColor = Theme.Muted; break;
                    case "EROARE": it.ForeColor = Theme.Bad; break;
                    case "DECONECTAT": case "BLOCAT": it.ForeColor = Theme.Bad; break;
                    case "PARTAJARE": case "ANULARE": it.ForeColor = Theme.Accent; break;
                }
                lvHist.Items.Add(it);
            }
            lvHist.EndUpdate();
            lblHist.Text = L.F("{0} înregistrări", total) + (total > Max ? L.F(" (afișate ultimele {0})", Max) : "");
        }

        void CheckHistoryChanged()
        {
            try
            {
                FileInfo fi = new FileInfo(Settings.LogPath);
                long len = fi.Exists ? fi.Length : -1;
                if (len != histLen || (fi.Exists && fi.LastWriteTime != histTime)) LoadHistory();
            }
            catch { }
        }

        // ------------------------------------------------------------ tab Optiuni
        void BuildOptions(Panel page)
        {
            FlowLayoutPanel p = new FlowLayoutPanel();
            p.Dock = DockStyle.Fill; p.FlowDirection = FlowDirection.TopDown; p.WrapContents = false;
            p.Padding = new Padding(S(12)); p.AutoScroll = true;

            chkNotify = new CheckBox { Text = L.T("Afișează notificare la accesarea folderelor partajate"), AutoSize = true, Margin = new Padding(3, 6, 3, 6) };
            chkSound = new CheckBox { Text = L.T("Sunet discret la notificare"), AutoSize = true, Margin = new Padding(3, 6, 3, 6) };
            chkLocal = new CheckBox { Text = L.T("Ignoră accesările făcute de pe acest calculator"), AutoSize = true, Margin = new Padding(3, 6, 3, 6) };
            chkAuto = new CheckBox { Text = L.T("Pornește automat cu Windows (în tray, cu drepturi de administrator, fără UAC)"), AutoSize = true, Margin = new Padding(3, 6, 3, 6) };

            numToast = new NumericUpDown { Minimum = 2, Maximum = 60, Width = S(70) };
            numCool = new NumericUpDown { Minimum = 0, Maximum = 600, Width = S(70) };
            FlowLayoutPanel r1 = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            r1.Controls.Add(new Label { Text = L.T("Durata notificării (secunde):"), AutoSize = true, Margin = new Padding(3, S(5), 6, 0) });
            r1.Controls.Add(numToast);
            FlowLayoutPanel r2 = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            r2.Controls.Add(new Label { Text = L.T("Pauză între notificările pentru fișiere, același PC și folder (secunde):"), AutoSize = true, Margin = new Padding(3, S(5), 6, 0) });
            r2.Controls.Add(numCool);

            cbTheme = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = S(160) };
            cbTheme.Items.AddRange(new object[] { L.T("Automat (ca Windows)"), L.T("Luminos"), L.T("Întunecat") });
            cbLang = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = S(160) };
            cbLang.Items.AddRange(new object[] { L.T("Automat (ca Windows)"), "Română", "English" });
            langFiles = L.Available();
            foreach (L.LangFile lf in langFiles) cbLang.Items.Add(lf.Name);
            // Tema si Limba intr-un tabel: casetele raman aliniate oricat de lunga e eticheta in limba aleasa
            TableLayoutPanel r3 = new TableLayoutPanel { AutoSize = true, ColumnCount = 3, RowCount = 2, Margin = new Padding(3, 10, 3, 3) };
            r3.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            r3.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            r3.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            cbTheme.Margin = new Padding(3, 3, 3, 3);
            cbLang.Margin = new Padding(3, 3, 3, 3);
            r3.Controls.Add(new Label { Text = L.T("Temă:"), AutoSize = true, Margin = new Padding(3, S(6), 6, 0) }, 0, 0);
            r3.Controls.Add(cbTheme, 1, 0);
            r3.Controls.Add(new Label { Text = L.T("Limbă:"), AutoSize = true, Margin = new Padding(3, S(6), 6, 0) }, 0, 1);
            r3.Controls.Add(cbLang, 1, 1);
            r3.Controls.Add(new Label { Text = L.T("Alte limbi: fișiere language\\xx.json lângă executabil (vezi README)."), AutoSize = true, Tag = "muted", Margin = new Padding(S(10), S(6), 3, 0) }, 2, 1);
            chkTrayClick = new CheckBox { Text = L.T("Deschide fereastra cu un singur clic pe iconița din tray"), AutoSize = true, Margin = new Padding(3, 6, 3, 6) };
            chkTrayClick.CheckedChanged += delegate { if (loadingOpt) return; Settings.TrayOneClick = chkTrayClick.Checked; Settings.Save(); };

            Button test = new Button { Text = L.T("Test notificare"), AutoSize = true, Margin = new Padding(3, 14, 3, 3) };
            Label note = new Label
            {
                AutoSize = false, Width = S(800), Height = S(60), Margin = new Padding(3, 20, 3, 3), Tag = "muted",
                Text = L.T("Setările se salvează automat în sharewatch.ini, lângă executabil. Închiderea ferestrei (X) lasă aplicația activă în tray; pentru oprire folosește Ieșire din meniul iconiței din tray.")
            };

            chkNotify.CheckedChanged += delegate { if (loadingOpt) return; Settings.Notify = chkNotify.Checked; Settings.Save(); };
            chkSound.CheckedChanged += delegate { if (loadingOpt) return; Settings.Sound = chkSound.Checked; Settings.Save(); if (chkSound.Checked) Sfx.Play(); };
            chkLocal.CheckedChanged += delegate { if (loadingOpt) return; Settings.IgnoreLocal = chkLocal.Checked; Settings.Save(); };
            chkHideAdmin = new CheckBox { Text = L.T("Ascunde partajările administrative (C$, ADMIN$ etc.) din liste"), AutoSize = true, Margin = new Padding(3, 6, 3, 6) };
            chkHideAdmin.CheckedChanged += delegate { if (loadingOpt) return; Settings.HideAdminShares = chkHideAdmin.Checked; Settings.Save(); };
            numToast.ValueChanged += delegate { if (loadingOpt) return; Settings.ToastSeconds = (int)numToast.Value; Settings.Save(); };
            numCool.ValueChanged += delegate { if (loadingOpt) return; Settings.CooldownSec = (int)numCool.Value; Settings.Save(); };
            cbTheme.SelectedIndexChanged += delegate
            {
                if (loadingOpt) return;
                Settings.Theme = cbTheme.SelectedIndex; Theme.Pref = Settings.Theme; Settings.Save();
                app.ThemeChanged();
            };
            cbLang.SelectedIndexChanged += delegate
            {
                if (loadingOpt) return;
                int sel = cbLang.SelectedIndex;
                if (sel >= 3) { Settings.Lang = 3; Settings.LangCode = langFiles[sel - 3].Code; }
                else { Settings.Lang = sel; Settings.LangCode = ""; }
                Settings.Save();
                app.ReloadUi();
            };
            chkAuto.Click += delegate
            {
                if (loadingOpt) return;
                bool want = chkAuto.Checked;
                if (!app.SetAutostart(want))
                {
                    MessageBox.Show(this, L.T("Nu am putut modifica pornirea automată. Rulează aplicația ca Administrator."), "ShareWatch", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    loadingOpt = true; chkAuto.Checked = !want; loadingOpt = false;
                }
            };
            test.Click += delegate { app.TestToast(); };

            p.Controls.AddRange(new Control[] { chkNotify, chkSound, chkLocal, chkHideAdmin, chkAuto, chkTrayClick, r1, r2, r3, test });
            BuildBlockList(p);
            p.Controls.Add(note);
            page.Controls.Add(p);
        }

        // ------------------------------------------------------------ lista de blocare (in Optiuni)
        void BuildBlockList(FlowLayoutPanel p)
        {
            Label head = new Label { Text = L.T("Listă de blocare"), AutoSize = true, Tag = "head", Font = new Font("Segoe UI", 10f, FontStyle.Bold), Margin = new Padding(3, S(18), 3, 3) };
            chkBlock = new CheckBox { Text = L.T("Închide imediat conexiunile calculatoarelor din lista de blocare"), AutoSize = true, Margin = new Padding(3, 6, 3, 6) };
            chkBlock.CheckedChanged += delegate { if (loadingOpt) return; Settings.BlockEnabled = chkBlock.Checked; Settings.Save(); FirewallBlock.RequestSync(); };

            lvBlock = NewList(new string[] { "Calculator (IP și/sau nume)", "Comentariu" }, new int[] { 260, 340 });
            lvBlock.Dock = DockStyle.None;
            lvBlock.Size = new Size(S(640), S(150));
            lvBlock.HeaderStyle = ColumnHeaderStyle.Nonclickable;

            FlowLayoutPanel row = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            txtBlock = new TextBox { Width = S(260), Margin = new Padding(3, S(4), 3, 3) };
            Button bAdd = new Button { Text = L.T("Adaugă"), AutoSize = true };
            Button bDel = new Button { Text = L.T("Șterge selectate"), AutoSize = true };
            Button bFile = new Button { Text = L.T("Deschide fișierul"), AutoSize = true };
            row.Controls.AddRange(new Control[] { txtBlock, bAdd, bDel, bFile });
            Label hint = new Label
            {
                AutoSize = false, Width = S(800), Height = S(40), Tag = "muted",
                Text = L.T("IP și/sau nume de calculator, separate prin spațiu (ex.: 192.168.1.50 PC-BIROU). Lista e salvată în blocklist.txt, lângă executabil, și poate fi editată și direct; modificările se aplică în câteva secunde.")
            };

            bAdd.Click += delegate { AddBlockEntry(txtBlock.Text, L.F("adăugat manual {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm"))); };
            txtBlock.KeyDown += delegate(object o, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddBlockEntry(txtBlock.Text, L.F("adăugat manual {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm"))); } };
            bDel.Click += delegate { RemoveBlockEntries(); };
            bFile.Click += delegate { BlockList.OpenFileInEditor(); };
            ToolStripMenuItem miUnblock = new ToolStripMenuItem(L.T("Șterge din listă (deblochează)"));
            miUnblock.Click += delegate { RemoveBlockEntries(); };
            RowMenu(lvBlock, miUnblock);
            lvBlock.KeyDown += delegate(object o, KeyEventArgs e) { if (e.KeyCode == Keys.Delete) RemoveBlockEntries(); };

            chkNotifyBlocked = new CheckBox { Text = L.T("Notificare (fără sunet) când un calculator blocat este deconectat"), AutoSize = true, Margin = new Padding(3, 2, 3, 6) };
            chkNotifyBlocked.CheckedChanged += delegate { if (loadingOpt) return; Settings.NotifyBlocked = chkNotifyBlocked.Checked; Settings.Save(); };

            // firewall
            chkFirewall = new CheckBox { Text = L.T("Blochează și în Windows Firewall (porturile SMB 139 și 445): calculatorul nu se mai poate conecta deloc"), AutoSize = true, Margin = new Padding(3, S(10), 3, 4) };
            chkFirewall.CheckedChanged += delegate
            {
                if (loadingOpt) return;
                Settings.BlockFirewall = chkFirewall.Checked; Settings.Save();
                lblFirewall.Text = L.T("Se actualizează...");
                FirewallBlock.RequestSync();
            };
            lblFirewall = new Label { AutoSize = false, Width = S(800), Height = S(36), Tag = "muted", Margin = new Padding(S(20), 0, 3, 3) };
            Button bFwRemove = new Button { Text = L.T("Elimină regulile ShareWatch din firewall"), AutoSize = true, Margin = new Padding(S(20), 0, 3, 3) };
            bFwRemove.Click += delegate
            {
                if (MessageBox.Show(this, L.T("Elimini regula ShareWatch din Windows Firewall și oprești blocarea prin firewall? Lista de blocare rămâne neschimbată."),
                    "ShareWatch", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                int n = FirewallBlock.RemoveAllNow();
                loadingOpt = true; chkFirewall.Checked = false; loadingOpt = false;
                lblFirewall.Text = FirewallBlock.Status;
                if (n < 0) MessageBox.Show(this, FirewallBlock.Status, "ShareWatch", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            p.Controls.AddRange(new Control[] { head, chkBlock, chkNotifyBlocked, lvBlock, row, hint, chkFirewall, lblFirewall, bFwRemove });
        }

        void LoadBlockList()
        {
            BlockList.ReloadIfChanged();
            blockVersion = BlockList.Version;
            lvBlock.BeginUpdate();
            lvBlock.Items.Clear();
            foreach (BlockList.Entry e in BlockList.Entries)
            {
                ListViewItem it = new ListViewItem(new string[] { string.Join(" ", e.Tokens), e.Comment });
                it.Tag = e;
                lvBlock.Items.Add(it);
            }
            lvBlock.EndUpdate();
        }

        // true = adaugat; refuza calculatorul local (te-ai bloca singur din propriile partajari)
        bool AddBlockEntry(string text, string comment)
        {
            string t = (text ?? "").Replace("#", " ").Trim();
            if (t.Length == 0) return false;
            foreach (string tok in t.Split(new char[] { ' ', '\t', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string k = tok.TrimStart('\\');
                if (Resolver.IsLocal(new Peer(k, k)))
                {
                    MessageBox.Show(this, L.F("„{0}” este chiar acest calculator și nu poate fi blocat.", k), "ShareWatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                if (AlreadyListed(k))
                {
                    MessageBox.Show(this, L.F("„{0}” este deja în lista de blocare.", k), "ShareWatch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
            }
            if (!BlockList.Add(t, comment))
            {
                MessageBox.Show(this, L.T("Nu am putut scrie fișierul blocklist.txt."), "ShareWatch", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            txtBlock.Text = "";
            LoadBlockList();
            return true;
        }

        static bool AlreadyListed(string k)
        {
            foreach (BlockList.Entry e in BlockList.Entries)
                foreach (string t in e.Tokens)
                    if (string.Equals(t, k, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        void RemoveBlockEntries()
        {
            if (lvBlock.SelectedItems.Count == 0) return;
            List<BlockList.Entry> sel = new List<BlockList.Entry>();
            foreach (ListViewItem it in lvBlock.SelectedItems) sel.Add((BlockList.Entry)it.Tag);
            foreach (BlockList.Entry e in sel) BlockList.Remove(e);
            LoadBlockList();
        }

        void LoadOptions()
        {
            loadingOpt = true;
            chkNotify.Checked = Settings.Notify;
            chkSound.Checked = Settings.Sound;
            chkLocal.Checked = Settings.IgnoreLocal;
            chkHideAdmin.Checked = Settings.HideAdminShares;
            chkAuto.Checked = TrayApp.AutostartEnabled();
            numToast.Value = Settings.ToastSeconds;
            numCool.Value = Settings.CooldownSec;
            cbTheme.SelectedIndex = Settings.Theme;
            int li = Settings.Lang <= 2 ? Settings.Lang : 0;
            if (Settings.Lang == 3)
                for (int i = 0; i < langFiles.Count; i++)
                    if (string.Equals(langFiles[i].Code, Settings.LangCode, StringComparison.OrdinalIgnoreCase)) { li = 3 + i; break; }
            cbLang.SelectedIndex = li;
            chkTrayClick.Checked = Settings.TrayOneClick;
            chkBlock.Checked = Settings.BlockEnabled;
            chkNotifyBlocked.Checked = Settings.NotifyBlocked;
            chkFirewall.Checked = Settings.BlockFirewall;
            lblFirewall.Text = FirewallBlock.Status;
            LoadBlockList();
            loadingOpt = false;
        }

        // ------------------------------------------------------------ tab Despre
        void BuildAbout(Panel page)
        {
            FlowLayoutPanel p = new FlowLayoutPanel();
            p.Dock = DockStyle.Fill; p.FlowDirection = FlowDirection.TopDown; p.WrapContents = false;
            p.Padding = new Padding(S(12)); p.AutoScroll = true;

            Label name = new Label { Text = "ShareWatch", AutoSize = true, Font = new Font("Segoe UI Semibold", 20f) };
            Label by = new Label { Text = "by ms74ro & Claude", AutoSize = true, UseMnemonic = false, Tag = "muted", Margin = new Padding(0, S(22), 3, 0) };
            FlowLayoutPanel title = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            title.Controls.Add(name);
            title.Controls.Add(by);
            string logShown = Settings.LogPath, exeShown = Application.ExecutablePath;
#if TEST
            // capturi pentru README: cai fictive, nu folderul real din care ruleaza
            if (Demo.On) { logShown = @"C:\ShareWatch\acces_foldere.log"; exeShown = @"C:\ShareWatch\ShareWatch.exe"; }
#endif
            Label ver = new Label { Text = L.T("Versiunea ") + AppInfo.Version, AutoSize = true, Font = new Font("Segoe UI", 11f), Margin = new Padding(3, 0, 3, 10) };
            Label desc = new Label
            {
                AutoSize = false, Width = S(800), Height = S(50),
                Text = L.T("Monitorizează accesările la folderele partajate (SMB) ale acestui calculator: afișează notificări cu IP-ul și numele calculatorului care accesează și păstrează un jurnal al accesărilor.")
            };
            Label info = new Label
            {
                AutoSize = true, Margin = new Padding(3, 6, 3, 6),
                Text = L.T("Calculator: ") + AppInfo.Machine + "\r\n" +
                       L.T("Drepturi de administrator: ") + (TrayApp.IsAdmin() ? L.T("da") : L.T("NU (monitorizarea nu va funcționa)")) + "\r\n" +
                       L.T("Jurnal: ") + logShown + "\r\n" +
                       L.T("Executabil: ") + exeShown
            };
            p.Controls.AddRange(new Control[] { title, ver, desc, info });

            // pe un rand: [GitHub] adresa aplicatiei, apoi [istoric] Istoric versiuni
            FlowLayoutPanel links = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, S(12), 3, 3) };
            AddLink(links, null, AppInfo.RepoUrl.Replace("https://", ""), AppInfo.RepoUrl, 0);

            // istoricul versiunilor: link catre GitHub (daca e configurat) sau catre CHANGELOG.md de langa exe (daca exista)
            string changelogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppInfo.ChangelogFile);
            string target = !string.IsNullOrEmpty(AppInfo.ChangelogUrl) ? AppInfo.ChangelogUrl
                          : File.Exists(changelogFile) ? changelogFile : null;
            if (target != null) AddLink(links, "", L.T("Istoric versiuni"), target, S(28));
            p.Controls.Add(links);
            page.Controls.Add(p);
        }

        // iconita + link (iconita e si ea clicabila)
        void AddLink(FlowLayoutPanel row, string glyph, string text, string target, int gapBefore)
        {
            LinkLabel ll = new LinkLabel { Text = text, AutoSize = true, Font = new Font("Segoe UI", 10f), Margin = new Padding(S(4), 0, 3, 0) };
            LinkIcon ic = new LinkIcon(glyph, S(20)) { Margin = new Padding(3 + gapBefore, 0, 0, 0) };
            EventHandler open = delegate
            {
                try { System.Diagnostics.Process.Start(target); }
                catch
                {
                    // .md fara aplicatie asociata: il deschidem in Notepad
                    try { System.Diagnostics.Process.Start("notepad.exe", "\"" + target + "\""); } catch { }
                }
            };
            ll.LinkClicked += delegate { open(null, EventArgs.Empty); };
            ic.Click += open;
            // iconita centrata pe verticala fata de text
            int h = ll.PreferredHeight;
            if (h > ic.Height) ic.Margin = new Padding(ic.Margin.Left, (h - ic.Height) / 2, 0, 0);
            else ll.Margin = new Padding(ll.Margin.Left, (ic.Height - h) / 2, 3, 0);
            row.Controls.Add(ic);
            row.Controls.Add(ll);
        }
    }
}
