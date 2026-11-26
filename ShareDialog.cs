// Dialogul "Partajeaza un folder"
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ShareWatch
{
    class ShareDialog : Form
    {
        readonly TextBox txtPath = new TextBox(), txtName = new TextBox(), txtRemark = new TextBox();
        readonly RadioButton rbRead = new RadioButton(), rbWrite = new RadioButton();
        readonly float sc;
        bool nameEdited;

        public string FolderPath { get { return txtPath.Text.Trim().TrimEnd('\\') + (txtPath.Text.Trim().EndsWith(":\\") ? "\\" : ""); } }
        public string ShareName { get { return txtName.Text.Trim(); } }
        public string Remark { get { return txtRemark.Text.Trim(); } }
        public bool AllowWrite { get { return rbWrite.Checked; } }

        int S(int v) { return (int)Math.Round(v * sc); }

        public ShareDialog(float scale)
        {
            sc = scale;
            Text = L.T("Partajează un folder");
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(S(14));

            TableLayoutPanel t = new TableLayoutPanel { ColumnCount = 3, AutoSize = true, Dock = DockStyle.Fill };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, S(320)));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            Button browse = new Button { Text = L.T("Răsfoiește..."), AutoSize = true };
            txtPath.Dock = txtName.Dock = txtRemark.Dock = DockStyle.Fill;
            t.Controls.Add(Lbl("Folder:"), 0, 0); t.Controls.Add(txtPath, 1, 0); t.Controls.Add(browse, 2, 0);
            t.Controls.Add(Lbl("Nume partajare:"), 0, 1); t.Controls.Add(txtName, 1, 1);
            t.Controls.Add(Lbl("Descriere:"), 0, 2); t.Controls.Add(txtRemark, 1, 2);

            rbRead.Text = L.T("Doar citire"); rbRead.AutoSize = true; rbRead.Checked = true;
            rbWrite.Text = L.T("Citire și scriere"); rbWrite.AutoSize = true;
            FlowLayoutPanel perm = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
            perm.Controls.Add(rbRead); perm.Controls.Add(rbWrite);
            t.Controls.Add(Lbl("Acces pentru toți (Everyone):"), 0, 3); t.Controls.Add(perm, 1, 3);
            t.SetColumnSpan(perm, 2);

            Label note = new Label
            {
                Text = L.T("Permisiunile NTFS ale folderului se aplică în continuare. Pentru acces din rețea, utilizatorii au nevoie și de drept pe folder (fila Securitate)."),
                AutoSize = false, Width = S(520), Height = S(40), Tag = "muted", Margin = new Padding(3, S(8), 3, 3)
            };
            t.Controls.Add(note, 0, 4); t.SetColumnSpan(note, 3);

            Button ok = new Button { Text = L.T("Partajează"), AutoSize = true, DialogResult = DialogResult.None };
            Button cancel = new Button { Text = L.T("Anulează"), AutoSize = true, DialogResult = DialogResult.Cancel };
            FlowLayoutPanel btns = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, S(8), 0, 0) };
            btns.Controls.Add(cancel); btns.Controls.Add(ok);
            t.Controls.Add(btns, 0, 5); t.SetColumnSpan(btns, 3);
            Controls.Add(t);
            AcceptButton = ok; CancelButton = cancel;

            browse.Click += delegate
            {
                using (FolderBrowserDialog fb = new FolderBrowserDialog())
                {
                    fb.Description = L.T("Alege folderul de partajat");
                    if (Directory.Exists(txtPath.Text)) fb.SelectedPath = txtPath.Text;
                    if (fb.ShowDialog(this) == DialogResult.OK) txtPath.Text = fb.SelectedPath;
                }
            };
            txtPath.TextChanged += delegate
            {
                if (nameEdited) return;
                string p = txtPath.Text.Trim().TrimEnd('\\');
                string n = p.Length > 0 ? Path.GetFileName(p) : "";
                txtName.Text = n;
                nameEdited = false;
            };
            txtName.KeyPress += delegate { nameEdited = true; };
            ok.Click += delegate
            {
                string err = Validate();
                if (err != null) { MessageBox.Show(this, err, "ShareWatch", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                DialogResult = DialogResult.OK;
            };

            BackColor = Theme.Bg; ForeColor = Theme.Fg;
            Theme.Apply(t);
            foreach (RadioButton rb in new RadioButton[] { rbRead, rbWrite }) { rb.ForeColor = Theme.Fg; rb.BackColor = Color.Transparent; }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.TitleBar(this);
        }

        Label Lbl(string ro)
        {
            return new Label { Text = L.T(ro), AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, S(6), S(8), 3) };
        }

        new string Validate()
        {
            string p = txtPath.Text.Trim();
            if (p.Length == 0 || !Directory.Exists(p)) return L.T("Alege un folder care există.");
            if (p.StartsWith("\\\\")) return L.T("Folderul trebuie să fie pe acest calculator, nu în rețea.");
            string n = ShareName;
            if (n.Length == 0 || n.Length > 80) return L.T("Numele partajării trebuie să aibă între 1 și 80 de caractere.");
            if (n.IndexOfAny("\\/[]:|<>+=;,?*\"".ToCharArray()) >= 0) return L.T("Numele partajării conține caractere nepermise: \\ / [ ] : | < > + = ; , ? * \"");
            return null;
        }
    }
}
