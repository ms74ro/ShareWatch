// Notificarea proprie (popup in coltul din dreapta-jos)
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ShareWatch
{
    class Toast : Form
    {
        static readonly List<Toast> open = new List<Toast>();
        readonly string share, pc, ip, user, path;
        readonly float s;
        readonly bool dark;
        System.Windows.Forms.Timer life = new System.Windows.Forms.Timer();
        System.Windows.Forms.Timer fade = new System.Windows.Forms.Timer();

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateRoundRectRgn(int l, int t, int r, int b, int w, int h);

        readonly bool blocked;

        public static void ShowToast(string share, Peer p, string user, string path, bool blocked = false)
        {
            while (open.Count >= 4) open[0].Close();
            Toast t = new Toast(share, p, user, path, blocked);
            open.Add(t);
            Reposition();
            t.Show();
            if (Settings.Sound && !blocked) Sfx.Play();   // PC-urile blocate: notificare silentioasa
        }

        int S(int v) { return (int)Math.Round(v * s); }

        Toast(string share, Peer p, string user, string path, bool blocked)
        {
            this.share = share; this.user = user; this.path = path; this.blocked = blocked;
            pc = (p.Host == "?" || string.IsNullOrEmpty(p.Host)) ? L.T("necunoscut") : p.Host;
            ip = p.Ip;
            dark = Theme.Dark;
            using (Graphics g = CreateGraphics()) s = g.DpiY / 96f;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(S(440), S(186));
            DoubleBuffered = true;
            BackColor = dark ? Color.FromArgb(28, 33, 43) : Color.FromArgb(252, 253, 254);
            Cursor = Cursors.Hand;
            Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width + 1, Height + 1, S(16), S(16)));
            Click += delegate { Close(); };
            life.Interval = Settings.ToastSeconds * 1000;
            life.Tick += delegate { life.Stop(); fade.Start(); };
            fade.Interval = 30;
            fade.Tick += delegate { if (Opacity <= 0.1) { fade.Stop(); Close(); } else Opacity -= 0.08; };
            life.Start();
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000 | 0x00000080 | 0x00000008; // NOACTIVATE | TOOLWINDOW | TOPMOST
                return cp;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            life.Dispose(); fade.Dispose();
            open.Remove(this);
            Reposition();
            base.OnFormClosed(e);
        }

        static void Reposition()
        {
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            int margin = 12;
            int y = wa.Bottom - margin;
            for (int i = open.Count - 1; i >= 0; i--)
            {
                y -= open[i].Height;
                open[i].Location = new Point(wa.Right - open[i].Width - margin, y);
                y -= 8;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            Color accent = blocked ? (dark ? Color.FromArgb(255, 110, 100) : Color.FromArgb(200, 40, 40))
                         : dark ? Color.FromArgb(61, 220, 132) : Color.FromArgb(30, 150, 90);
            Color main = dark ? Color.White : Color.FromArgb(20, 24, 32);
            Color sub = dark ? Color.FromArgb(215, 222, 232) : Color.FromArgb(50, 58, 70);
            Color dim = dark ? Color.FromArgb(150, 160, 176) : Color.FromArgb(105, 114, 128);

            using (SolidBrush b = new SolidBrush(accent)) g.FillRectangle(b, 0, 0, S(7), Height);
            if (!dark)
                using (Pen pen = new Pen(Color.FromArgb(205, 210, 218)))
                    g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

            using (Font f1 = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (Font f2 = new Font("Segoe UI Semibold", 16f))
            using (Font f3 = new Font("Segoe UI", 12f))
            using (Font f4 = new Font("Segoe UI", 10f))
            using (SolidBrush acc = new SolidBrush(accent))
            using (SolidBrush bMain = new SolidBrush(main))
            using (SolidBrush bSub = new SolidBrush(sub))
            using (SolidBrush bDim = new SolidBrush(dim))
            {
                int x = S(24);
                g.DrawString(blocked ? L.T("CONEXIUNE BLOCATĂ") : L.T("ACCES LA FOLDER PARTAJAT"), f1, acc, x, S(12));
                g.DrawString(blocked ? L.T("Calculator din lista de blocare") : "\\\\" + Environment.MachineName + "\\" + share, f2, bMain, x - 2, S(32));
                g.DrawString(L.T("PC: ") + pc + "      IP: " + ip, f3, bSub, x, S(76));
                g.DrawString(L.T("Utilizator: ") + (string.IsNullOrEmpty(user) ? "-" : user) + "      " + DateTime.Now.ToString("HH:mm:ss"), f4, bDim, x, S(108));
                if (!string.IsNullOrEmpty(path))
                {
                    StringFormat sf = new StringFormat();
                    sf.Trimming = StringTrimming.EllipsisPath;
                    sf.FormatFlags = StringFormatFlags.NoWrap;
                    g.DrawString(path, f4, bDim, new RectangleF(x, S(140), Width - x - S(14), S(30)), sf);
                }
            }
        }
    }
}
