using System;
using System.Drawing;
using System.Windows.Forms;

namespace OmenMon.AppGui {
    public partial class GuiFormOverlay : Form {
        private Timer HideTimer;
        private Label LblWarning;

        // Ensure form doesn't steal focus
        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                // WS_EX_LAYERED (0x00080000) and WS_EX_TRANSPARENT (0x00000020)
                cp.ExStyle |= 0x00080000 | 0x00000020;
                // WS_EX_NOACTIVATE (0x08000000)
                cp.ExStyle |= 0x08000000;
                // WS_EX_TOOLWINDOW (0x00000080) to hide from taskbar/alt-tab
                cp.ExStyle |= 0x00000080;
                return cp;
            }
        }

        public GuiFormOverlay() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            this.HideTimer = new Timer();
            this.LblWarning = new Label();
            this.SuspendLayout();

            // Timer
            this.HideTimer.Interval = 3000; // 3 seconds
            this.HideTimer.Tick += HideTimer_Tick;

            // Label
            this.LblWarning.AutoSize = false;
            this.LblWarning.Dock = DockStyle.Fill;
            this.LblWarning.Font = new Font("Arial", 36F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            this.LblWarning.ForeColor = Color.Red;
            this.LblWarning.Text = "⚠ WARNING ⚠\nFan safety threshold exceeded.\nReverting to Dynamic Fan Mode.";
            this.LblWarning.TextAlign = ContentAlignment.MiddleCenter;
            this.LblWarning.BackColor = Color.Magenta;

            // Form
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;
            this.ClientSize = new Size(1200, 400);
            this.Controls.Add(this.LblWarning);
            this.FormBorderStyle = FormBorderStyle.None;
            this.Name = "GuiFormOverlay";
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = true;

            this.ResumeLayout(false);
        }

        private void HideTimer_Tick(object sender, EventArgs e) {
            this.HideTimer.Stop();
            this.Hide();
        }

        // Static instance for easy calling
        private static GuiFormOverlay _instance;
        public static void ShowOverlay() {
            if (GuiTray.Context != null && GuiTray.Context.FormMain != null && GuiTray.Context.FormMain.InvokeRequired) {
                GuiTray.Context.FormMain.Invoke(new Action(ShowOverlay));
                return;
            }

            if (_instance == null || _instance.IsDisposed) {
                _instance = new GuiFormOverlay();
            }

            _instance.Show();
            
            // Restart timer
            _instance.HideTimer.Stop();
            _instance.HideTimer.Start();
        }
    }
}
