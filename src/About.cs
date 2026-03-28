// About.cs — Friendly "About GameBarNull" dialog
// Shared by Setup.exe, Uninstaller.exe, and GameBarNull.exe
// Compile into whichever exe needs it; the dialog is self-contained.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GameBarNull
{
    class AboutForm : Form
    {
        const string KoFiUrl   = "https://ko-fi.com/asharpd";
        const string GithubUrl = "https://github.com/ehounds/GameBarNull";
        const string Version   = "1.0.0";

        public AboutForm()
        {
            BuildUI();
        }

        void BuildUI()
        {
            Text            = "About GameBarNull";
            Size            = new Size(420, 420);
            MinimumSize     = Size;
            MaximumSize     = Size;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.White;
            Font            = new Font("Segoe UI", 9.5f);

            // ── Gradient header panel ──────────────────────────────────────
            var header = new GradientPanel
            {
                Dock       = DockStyle.Top,
                Height     = 110,
                TopColor   = Color.FromArgb(18, 24, 48),
                BottomColor= Color.FromArgb(40, 55, 100)
            };

            // App name
            header.Controls.Add(new Label
            {
                Text      = "GameBarNull",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 20f, FontStyle.Bold),
                Location  = new Point(20, 14),
                AutoSize  = true
            });

            // Version badge
            var verLabel = new Label
            {
                Text      = "v" + Version,
                ForeColor = Color.FromArgb(160, 180, 230),
                Font      = new Font("Segoe UI", 8.5f),
                Location  = new Point(24, 52),
                AutoSize  = true
            };
            header.Controls.Add(verLabel);

            // Tagline
            header.Controls.Add(new Label
            {
                Text      = "Because nobody asked for that popup.  \uD83D\uDE0C",
                ForeColor = Color.FromArgb(180, 195, 230),
                Font      = new Font("Segoe UI", 9f, FontStyle.Italic),
                Location  = new Point(22, 76),
                AutoSize  = true
            });

            Controls.Add(header);

            // ── Body ──────────────────────────────────────────────────────
            int y = 126;

            // What it does
            var descPanel = new Panel { Location = new Point(20, y), Width = 362, Height = 82 };
            descPanel.Controls.Add(new Label
            {
                Text =
                    "You're mid-game. Windows decides it's a great time to ask " +
                    "if you\u2019d like to find an app for ms-gamebar://. You would not.\r\n\r\n" +
                    "GameBarNull sits in the background, catches those calls, " +
                    "and quietly bins them. No dialog. No Xbox. Just vibes.",
                Location = new Point(0, 0),
                Width    = 362,
                Height   = 82,
                Font     = new Font("Segoe UI", 9f),
                ForeColor= Color.FromArgb(50, 50, 50)
            });
            Controls.Add(descPanel);
            y += 92;

            // Divider
            Controls.Add(MakeLine(y)); y += 16;

            // GitHub link
            Controls.Add(MakeIconLabel("\uD83D\uDCBB", "Peek under the hood on GitHub", GithubUrl, y)); y += 30;

            // Ko-fi
            Controls.Add(MakeIconLabel("\u2615", "Enjoyed the silence? Buy me a coffee!", KoFiUrl, y)); y += 34;

            // Divider
            Controls.Add(MakeLine(y)); y += 16;

            // Footer credit
            Controls.Add(new Label
            {
                Text      = "Handcrafted with \u2665 by Adam Sharp  \u2014  E-Hounds, Inc.",
                Location  = new Point(20, y),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(130, 130, 130)
            });
            y += 22;

            Controls.Add(new Label
            {
                Text      = "Free \u0026 open source \u2014 grab it, fork it, do whatever.",
                Location  = new Point(20, y),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(160, 160, 160)
            });

            // ── Close button ──────────────────────────────────────────────
            var btnClose = new Button
            {
                Text     = "Close",
                Size     = new Size(88, 30),
                Location = new Point(312, 362),
                FlatStyle= FlatStyle.System
            };
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);
            CancelButton = btnClose;
        }

        // Horizontal rule
        static Panel MakeLine(int y)
        {
            return new Panel
            {
                Location  = new Point(20, y),
                Width     = 362,
                Height    = 1,
                BackColor = Color.FromArgb(220, 220, 220)
            };
        }

        // Icon + clickable link label row
        Control MakeIconLabel(string icon, string text, string url, int y)
        {
            var row = new Panel { Location = new Point(20, y), Width = 362, Height = 22 };

            row.Controls.Add(new Label
            {
                Text     = icon,
                Location = new Point(0, 1),
                Width    = 22,
                Font     = Font
            });

            var link = new LinkLabel
            {
                Text      = text,
                Location  = new Point(26, 1),
                AutoSize  = true,
                Font      = Font,
                LinkColor = Color.FromArgb(30, 90, 200),
                ActiveLinkColor = Color.FromArgb(180, 60, 0)
            };
            link.Click += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                catch { }
            };
            row.Controls.Add(link);
            return row;
        }

        // ── Static helper so any form can open the About dialog ──────────
        public static void ShowAbout(IWin32Window owner = null)
        {
            using (var dlg = new AboutForm())
                dlg.ShowDialog(owner);
        }
    }

    // ── Simple gradient panel (no extra DLLs needed) ──────────────────────
    class GradientPanel : Panel
    {
        public Color TopColor    = Color.FromArgb(18, 24, 48);
        public Color BottomColor = Color.FromArgb(40, 55, 100);

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var brush = new LinearGradientBrush(
                ClientRectangle, TopColor, BottomColor,
                LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
        }
    }
}
