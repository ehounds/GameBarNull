using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;

namespace GameBarNull
{
    // -------------------------------------------------------------------------
    // Win32 P/Invoke — schedule file/folder deletion on reboot
    // -------------------------------------------------------------------------
    static class NativeMethods
    {
        public const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool MoveFileEx(
            string lpExistingFileName,
            string lpNewFileName,      // null = delete
            uint   dwFlags);
    }

    // -------------------------------------------------------------------------
    // Shared uninstall logic (used by both GUI and silent paths)
    // -------------------------------------------------------------------------
    static class UninstallLogic
    {
        static readonly string InstallDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "GameBarNull");

        public static void Run()
        {
            const string protoPath = @"SOFTWARE\Classes\ms-gamebar";
            const string cmdPath   = @"SOFTWARE\Classes\ms-gamebar\shell\open\command";
            const string bkPath    = @"SOFTWARE\GameBarNull";
            const string arpPath   =
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\GameBarNull";

            // --- Read backup values ---
            string origCmd     = "";
            string origDefault = "";
            bool   hadOriginal = false;

            using (var bk = Registry.LocalMachine.OpenSubKey(bkPath))
            {
                if (bk != null)
                {
                    origCmd     = (bk.GetValue("OriginalCommand")  as string) ?? "";
                    origDefault = (bk.GetValue("OriginalDefault")  as string) ?? "";
                    hadOriginal = ((int?)bk.GetValue("HadOriginal") ?? 0) == 1;
                }
            }

            // --- Remove current protocol handler ---
            Registry.LocalMachine.DeleteSubKeyTree(protoPath, throwOnMissingSubKey: false);

            // --- Restore original if it existed ---
            if (hadOriginal && origCmd.Length > 0)
            {
                using (var key = Registry.LocalMachine.CreateSubKey(protoPath))
                {
                    key.SetValue("", origDefault);
                    key.SetValue("URL Protocol", "");
                }
                using (var key = Registry.LocalMachine.CreateSubKey(cmdPath))
                {
                    key.SetValue("", origCmd);
                }
            }

            // --- Remove backup key ---
            Registry.LocalMachine.DeleteSubKeyTree(bkPath, throwOnMissingSubKey: false);

            // --- Remove Add/Remove Programs entry ---
            Registry.LocalMachine.DeleteSubKeyTree(arpPath, throwOnMissingSubKey: false);

            // --- Remove install files ---
            if (Directory.Exists(InstallDir))
            {
                string runningExe = Assembly.GetExecutingAssembly().Location;

                foreach (var f in Directory.GetFiles(InstallDir))
                {
                    if (f.Equals(runningExe, StringComparison.OrdinalIgnoreCase))
                        continue;  // can't delete ourselves while running
                    try { File.Delete(f); } catch { }
                }

                // Schedule Uninstaller.exe and the (then-empty) directory for deletion on reboot
                NativeMethods.MoveFileEx(runningExe, null, NativeMethods.MOVEFILE_DELAY_UNTIL_REBOOT);
                NativeMethods.MoveFileEx(InstallDir, null,  NativeMethods.MOVEFILE_DELAY_UNTIL_REBOOT);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Uninstaller form (interactive path)
    // -------------------------------------------------------------------------
    class UninstallerForm : Form
    {
        Label       _lblStatus;
        Button      _btnUninstall;
        Button      _btnClose;
        ProgressBar _progress;

        public UninstallerForm()
        {
            BuildUI();
        }

        void BuildUI()
        {
            Text            = "GameBarNull Uninstaller";
            Size            = new Size(500, 300);
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
            header.Controls.Add(new Label
            {
                Text      = "GameBarNull",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 17f, FontStyle.Bold),
                Location  = new Point(20, 10),
                AutoSize  = true
            });
            header.Controls.Add(new Label
            {
                Text      = "Uninstall",
                ForeColor = Color.FromArgb(160, 170, 210),
                Font      = new Font("Segoe UI", 9f),
                Location  = new Point(22, 44),
                AutoSize  = true
            });
            Controls.Add(header);

            // ---- Body ----
            int y = 88;

            Controls.Add(new Label
            {
                Text     = "This will remove GameBarNull and restore the original\r\n" +
                           "ms-gamebar:// protocol behaviour. Continue?",
                Location = new Point(22, y),
                Width    = 440,
                Height   = 46,
                Font     = Font
            });
            y += 54;

            Controls.Add(new Label
            {
                Text      = "Note: Uninstaller.exe will be deleted on the next restart.",
                Location  = new Point(22, y),
                AutoSize  = true,
                ForeColor = Color.FromArgb(100, 100, 100),
                Font      = new Font("Segoe UI", 8.5f)
            });
            y += 24;

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
            y += 16;

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
                Location = new Point(362, 238),
                Size     = new Size(100, 30)
            };
            _btnClose.Click += (s, e) => Close();
            Controls.Add(_btnClose);

            _btnUninstall = new Button
            {
                Text     = "Uninstall",
                Location = new Point(252, 238),
                Size     = new Size(100, 30)
            };
            _btnUninstall.Click += OnUninstall;
            Controls.Add(_btnUninstall);
            AcceptButton = _btnUninstall;
        }

        void SetStatus(string msg, Color? color = null)
        {
            _lblStatus.Text      = msg;
            _lblStatus.ForeColor = color ?? Color.FromArgb(80, 80, 80);
            Application.DoEvents();
        }

        void OnUninstall(object sender, EventArgs e)
        {
            _btnUninstall.Enabled = false;
            _progress.Value       = 20;

            try
            {
                SetStatus("Removing registry entries\u2026");
                _progress.Value = 50;
                UninstallLogic.Run();
                _progress.Value = 100;

                SetStatus("Uninstall complete. Restart your PC to finish cleanup.",
                          Color.FromArgb(0, 120, 60));
                _btnUninstall.Text = "\u2713 Done";
                _btnClose.Text     = "Close";
                _btnClose.Focus();
            }
            catch (Exception ex)
            {
                SetStatus("Error: " + ex.Message, Color.FromArgb(180, 0, 0));
                _btnUninstall.Enabled = true;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Entry point
    // -------------------------------------------------------------------------
    static class UninstallerEntryPoint
    {
        [STAThread]
        static void Main(string[] args)
        {
            bool silent = Array.Exists(
                args, a => a.Equals("/silent", StringComparison.OrdinalIgnoreCase));

            if (!IsElevated())
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName        = Assembly.GetExecutingAssembly().Location,
                        Verb            = "runas",
                        Arguments       = string.Join(" ", args),
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    if (!silent)
                        MessageBox.Show(
                            "Administrator privileges are required to uninstall GameBarNull.\n\n" +
                            ex.Message,
                            "GameBarNull Uninstaller",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                }
                return;
            }

            if (silent)
            {
                try { UninstallLogic.Run(); } catch { }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UninstallerForm());
        }

        static bool IsElevated()
        {
            var id = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
