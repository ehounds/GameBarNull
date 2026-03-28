using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;

namespace GameBarNull
{
    // -------------------------------------------------------------------------
    // Setup form
    // -------------------------------------------------------------------------
    class SetupForm : Form
    {
        static readonly string InstallDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "GameBarNull");

        Label  _lblStatus;
        Button _btnInstall;
        Button _btnClose;
        ProgressBar _progress;

        public SetupForm()
        {
            BuildUI();
        }

        void BuildUI()
        {
            Text            = "GameBarNull Setup";
            Size            = new Size(500, 360);
            MinimumSize     = Size;
            MaximumSize     = Size;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = Color.White;
            Font            = new Font("Segoe UI", 9.5f);

            // ---- Header ----
            var header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 72,
                BackColor = Color.FromArgb(18, 24, 48)
            };

            var appTitle = new Label
            {
                Text      = "GameBarNull",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 17f, FontStyle.Bold),
                Location  = new Point(20, 10),
                AutoSize  = true
            };
            var appSub = new Label
            {
                Text      = "Silent ms-gamebar:// protocol handler  \u2014  v1.0.0",
                ForeColor = Color.FromArgb(160, 170, 210),
                Font      = new Font("Segoe UI", 9f),
                Location  = new Point(22, 44),
                AutoSize  = true
            };
            header.Controls.Add(appTitle);
            header.Controls.Add(appSub);
            Controls.Add(header);

            // ---- Body ----
            int y = 88;

            AddLabel("What will be installed:", 22, y, bold: true); y += 22;
            AddLabel("\u2022  GameBarNull.exe  \u2014  the silent protocol handler", 34, y); y += 20;
            AddLabel("\u2022  Uninstaller.exe  \u2014  registered in Add / Remove Programs", 34, y); y += 20;
            AddLabel("\u2022  ms-gamebar:// registry handler (original backed up)", 34, y); y += 32;

            AddLabel("Install location:", 22, y, bold: true); y += 20;
            var pathBox = new TextBox
            {
                Text      = InstallDir,
                Location  = new Point(22, y),
                Width     = 440,
                ReadOnly  = true,
                BackColor = Color.FromArgb(242, 242, 242),
                ForeColor = Color.FromArgb(50, 50, 50),
                Font      = new Font("Consolas", 9f)
            };
            Controls.Add(pathBox);
            y += 34;

            _progress = new ProgressBar
            {
                Location = new Point(22, y),
                Width    = 440,
                Height   = 8,
                Minimum  = 0,
                Maximum  = 100,
                Value    = 0
            };
            Controls.Add(_progress);
            y += 18;

            _lblStatus = new Label
            {
                Text      = "Ready.",
                Location  = new Point(22, y),
                Width     = 440,
                Height    = 20,
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            Controls.Add(_lblStatus);

            // ---- Buttons ----
            _btnClose = new Button
            {
                Text     = "Cancel",
                Location = new Point(362, 296),
                Size     = new Size(100, 30)
            };
            _btnClose.Click += (s, e) => Close();
            Controls.Add(_btnClose);

            _btnInstall = new Button
            {
                Text     = "Install",
                Location = new Point(252, 296),
                Size     = new Size(100, 30)
            };
            _btnInstall.Click += OnInstall;
            Controls.Add(_btnInstall);
            AcceptButton = _btnInstall;

            var btnAbout = new Button
            {
                Text     = "About",
                Location = new Point(22, 296),
                Size     = new Size(80, 30),
                FlatStyle= FlatStyle.System
            };
            btnAbout.Click += (s, e) => AboutForm.ShowAbout(this);
            Controls.Add(btnAbout);
        }

        void AddLabel(string text, int x, int y, bool bold = false)
        {
            Controls.Add(new Label
            {
                Text      = text,
                Location  = new Point(x, y),
                AutoSize  = true,
                Font      = bold
                              ? new Font("Segoe UI", 9.5f, FontStyle.Bold)
                              : Font
            });
        }

        void SetStatus(string msg, Color? color = null)
        {
            _lblStatus.Text      = msg;
            _lblStatus.ForeColor = color ?? Color.FromArgb(80, 80, 80);
            Application.DoEvents();
        }

        void SetProgress(int pct)
        {
            _progress.Value = pct;
            Application.DoEvents();
        }

        void OnInstall(object sender, EventArgs e)
        {
            _btnInstall.Enabled = false;

            try
            {
                // Verify sibling files exist before touching anything
                string setupDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string[] required = { "GameBarNull.exe", "Uninstaller.exe" };
                foreach (var f in required)
                {
                    if (!File.Exists(Path.Combine(setupDir, f)))
                        throw new FileNotFoundException(
                            f + " was not found next to Setup.exe.\n" +
                            "Keep Setup.exe, GameBarNull.exe, and Uninstaller.exe in the same folder.");
                }

                // Step 1 — Create install directory
                SetStatus("Creating install directory\u2026");
                Directory.CreateDirectory(InstallDir);
                SetProgress(15);

                // Step 2 — Copy files
                SetStatus("Copying files\u2026");
                foreach (var f in required)
                    File.Copy(Path.Combine(setupDir, f), Path.Combine(InstallDir, f), overwrite: true);
                // Copy Setup.exe too so it's available from the install dir
                File.Copy(Assembly.GetExecutingAssembly().Location,
                          Path.Combine(InstallDir, "Setup.exe"), overwrite: true);
                SetProgress(45);

                // Step 3 — Register protocol handler (with backup)
                SetStatus("Registering ms-gamebar protocol handler\u2026");
                RegisterProtocol();
                SetProgress(70);

                // Step 4 — Add/Remove Programs entry
                SetStatus("Registering with Add / Remove Programs\u2026");
                RegisterARP();
                SetProgress(100);

                SetStatus("Installation complete!", Color.FromArgb(0, 120, 60));
                _btnInstall.Text    = "\u2713 Done";
                _btnClose.Text      = "Close";
                _btnClose.Focus();
            }
            catch (Exception ex)
            {
                SetStatus("Error: " + ex.Message, Color.FromArgb(180, 0, 0));
                _btnInstall.Enabled = true;
            }
        }

        void RegisterProtocol()
        {
            const string protoPath = @"SOFTWARE\Classes\ms-gamebar";
            const string cmdPath   = @"SOFTWARE\Classes\ms-gamebar\shell\open\command";
            const string bkPath    = @"SOFTWARE\GameBarNull";

            string handlerExe = Path.Combine(InstallDir, "GameBarNull.exe");

            // Back up existing values (before touching anything)
            using (var bk = Registry.LocalMachine.CreateSubKey(bkPath))
            {
                object origCmd = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\" + cmdPath, "", null);
                object origDefault = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\" + protoPath, "", null);

                bk.SetValue("OriginalCommand",
                    origCmd     != null ? (string)origCmd     : "",
                    RegistryValueKind.String);
                bk.SetValue("OriginalDefault",
                    origDefault != null ? (string)origDefault : "",
                    RegistryValueKind.String);
                bk.SetValue("HadOriginal",
                    origCmd != null ? 1 : 0,
                    RegistryValueKind.DWord);
            }

            // Write new handler
            using (var key = Registry.LocalMachine.CreateSubKey(protoPath))
            {
                key.SetValue("", "URL:ms-gamebar (null handler)");
                key.SetValue("URL Protocol", "");
            }
            using (var key = Registry.LocalMachine.CreateSubKey(cmdPath))
            {
                key.SetValue("", "\"" + handlerExe + "\" \"%1\"");
            }
        }

        void RegisterARP()
        {
            const string arpPath =
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GameBarNull";

            long bytes = 0;
            try
            {
                foreach (var f in Directory.GetFiles(InstallDir))
                    bytes += new FileInfo(f).Length;
            }
            catch { }

            using (var key = Registry.LocalMachine.CreateSubKey(arpPath))
            {
                key.SetValue("DisplayName",          "GameBarNull");
                key.SetValue("DisplayVersion",       "1.0.0");
                key.SetValue("Publisher",            "GameBarNull");
                key.SetValue("InstallLocation",      InstallDir);
                key.SetValue("DisplayIcon",
                    "\"" + Path.Combine(InstallDir, "Uninstaller.exe") + "\"");
                key.SetValue("UninstallString",
                    "\"" + Path.Combine(InstallDir, "Uninstaller.exe") + "\"");
                key.SetValue("QuietUninstallString",
                    "\"" + Path.Combine(InstallDir, "Uninstaller.exe") + "\" /silent");
                key.SetValue("NoModify",      1, RegistryValueKind.DWord);
                key.SetValue("NoRepair",      1, RegistryValueKind.DWord);
                key.SetValue("EstimatedSize",
                    (int)(bytes / 1024), RegistryValueKind.DWord);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Entry point
    // -------------------------------------------------------------------------
    static class SetupEntryPoint
    {
        [STAThread]
        static void Main()
        {
            if (!IsElevated())
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName       = Assembly.GetExecutingAssembly().Location,
                        Verb           = "runas",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Administrator privileges are required to install GameBarNull.\n\n" + ex.Message,
                        "GameBarNull Setup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm());
        }

        static bool IsElevated()
        {
            var id = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
