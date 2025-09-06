using System;
using System.Drawing;
using System.Windows.Forms;
using MissionPlanner.Utilities;

namespace MissionPlanner.Controls
{
    public class RemoteTakeoffLandingForm : Form
    {
        public double TakeoffLat { get; private set; }
        public double TakeoffLng { get; private set; }
        public double TakeoffAlt { get; private set; }
        public double LandLat { get; private set; }
        public double LandLng { get; private set; }
        public double LandAlt { get; private set; }
        public bool ShouldLand { get; private set; }
        public double LandingHeight { get; private set; }
        public bool SetAsHome { get; private set; }

        TextBox txtTLat, txtTLng, txtTAlt, txtLLat, txtLLng, txtLAlt, txtLandingHeight;
        RadioButton rbPassThrough, rbLand;
        CheckBox chkSetAsHome;
        Button btnOK, btnCancel;

        public RemoteTakeoffLandingForm()
        {
            InitializeComponent();
            ThemeManager.ApplyThemeTo(this);
            // 设置默认值：与添加航点一致，使用当前地图中心与默认高度
            // 初始化降落高度锁定状态
            UpdateLandingHeightLock();
            try
            {
                if (MainV2.instance != null && MainV2.instance.FlightPlanner != null)
                {
                    var map = MainV2.instance.FlightPlanner.MainMap;
                    if (map != null)
                    {
                        string lat = map.Position.Lat.ToString("F6");
                        string lng = map.Position.Lng.ToString("F6");
                        txtTLat.Text = lat;
                        txtTLng.Text = lng;
                        txtLLat.Text = lat;
                        txtLLng.Text = lng;
                    }
                    // 设置默认高度为30米
                    txtTAlt.Text = "30";
                    txtLAlt.Text = "30";
                    txtLandingHeight.Text = "0";
                    
                    // 绑定选项框事件，当选择降落时锁定降落高度
                    rbLand.CheckedChanged += (s, e) => UpdateLandingHeightLock();
                    // 初始化降落高度锁定状态
                    UpdateLandingHeightLock();
                }
            }
            catch { }
        }

        // 重载：锁定起点（起飞点）为给定值，并禁止编辑（后续追加时使用）
        public RemoteTakeoffLandingForm(double lockedStartLat, double lockedStartLng, double lockedStartAlt) : this()
        {
            try
            {
                txtTLat.Text = lockedStartLat.ToString("F6");
                txtTLng.Text = lockedStartLng.ToString("F6");
                txtTAlt.Text = lockedStartAlt.ToString("0.##");
                txtLandingHeight.Text = "30";

                txtTLat.ReadOnly = true;
                txtTLng.ReadOnly = true;
                txtTAlt.ReadOnly = true;

                txtTLat.TabStop = false;
                txtTLng.TabStop = false;
                txtTAlt.TabStop = false;

                // 提示被锁定的视觉效果
                txtTLat.BackColor = SystemColors.Control;
                txtTLng.BackColor = SystemColors.Control;
                txtTAlt.BackColor = SystemColors.Control;
                
                // 追加模式：保持显示，但锁定Home选项，不可再次更改
                chkSetAsHome.Checked = false;
                chkSetAsHome.Enabled = false;
                
                // 调整按钮位置，因为隐藏了Home点选项
                btnOK.Location = new Point(160, 470);
                btnCancel.Location = new Point(290, 470);
                this.ClientSize = new Size(520, 520);
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.Text = "异地起降";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            var lblT = new Label { Text = "起飞点", AutoSize = true, Location = new Point(28, 18) };
            var lblTLAT = new Label { Text = "纬度:", AutoSize = true, Location = new Point(28, 50) };
            var lblTLNG = new Label { Text = "经度:", AutoSize = true, Location = new Point(28, 84) };
            var lblTALT = new Label { Text = "高度(米):", AutoSize = true, Location = new Point(28, 118) };
            txtTLat = new TextBox { Location = new Point(150, 46), Size = new Size(260, 23), Text = "0" };
            txtTLng = new TextBox { Location = new Point(150, 80), Size = new Size(260, 23), Text = "0" };
            txtTAlt = new TextBox { Location = new Point(150, 114), Size = new Size(260, 23), Text = "30" };

            // 设置为Home放在起飞点和降落点之间
            var lblSetHome = new Label { Text = "设置为home:", AutoSize = true, Location = new Point(28, 154) };
            chkSetAsHome = new CheckBox { Text = "", AutoSize = true, Location = new Point(150, 152), Checked = true };

            var lblL = new Label { Text = "降落点", AutoSize = true, Location = new Point(28, 196) };
            var lblLLAT = new Label { Text = "纬度:", AutoSize = true, Location = new Point(28, 230) };
            var lblLLNG = new Label { Text = "经度:", AutoSize = true, Location = new Point(28, 264) };
            var lblLALT = new Label { Text = "高度(米):", AutoSize = true, Location = new Point(28, 298) };
            txtLLat = new TextBox { Location = new Point(150, 226), Size = new Size(260, 23), Text = "0" };
            txtLLng = new TextBox { Location = new Point(150, 260), Size = new Size(260, 23), Text = "0" };
            txtLAlt = new TextBox { Location = new Point(150, 294), Size = new Size(260, 23), Text = "30" };
            
            // 上方已添加"设置为home"控件

            // 添加选项框
            var lblOptions = new Label { Text = "降落选项", AutoSize = true, Location = new Point(28, 332) };
            rbPassThrough = new RadioButton { Text = "直接经过航点不降落", AutoSize = true, Location = new Point(28, 358), Checked = true };
            rbLand = new RadioButton { Text = "直接降落", AutoSize = true, Location = new Point(28, 386) };
            var lblLandingHeight = new Label { Text = "降落高度(米):", AutoSize = true, Location = new Point(28, 414) };
            txtLandingHeight = new TextBox { Location = new Point(150, 410), Size = new Size(260, 23), Text = "0" };

            btnOK = new Button { Text = "确定", Location = new Point(160, 520), Size = new Size(100, 30), DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "取消", Location = new Point(290, 520), Size = new Size(100, 30), DialogResult = DialogResult.Cancel };
            btnOK.Click += BtnOK_Click;

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
            this.ClientSize = new Size(520, 600);
            this.Controls.AddRange(new Control[] { lblT, lblTLAT, lblTLNG, lblTALT, txtTLat, txtTLng, txtTAlt, lblL, lblLLAT, lblLLNG, lblLALT, txtLLat, txtLLng, txtLAlt, lblSetHome, chkSetAsHome, lblOptions, rbPassThrough, rbLand, lblLandingHeight, txtLandingHeight, btnOK, btnCancel });
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (!validateNumber(txtTLat, -90, 90, "起飞点纬度")) { this.DialogResult = DialogResult.None; return; }
            if (!validateNumber(txtTLng, -180, 180, "起飞点经度")) { this.DialogResult = DialogResult.None; return; }
            if (!validateNumber(txtTAlt, null, null, "起飞点高度")) { this.DialogResult = DialogResult.None; return; }

            if (!validateNumber(txtLLat, -90, 90, "降落点纬度")) { this.DialogResult = DialogResult.None; return; }
            if (!validateNumber(txtLLng, -180, 180, "降落点经度")) { this.DialogResult = DialogResult.None; return; }
            if (!validateNumber(txtLAlt, null, null, "降落点高度")) { this.DialogResult = DialogResult.None; return; }

            if (!validateNumber(txtLandingHeight, null, null, "降落高度")) { this.DialogResult = DialogResult.None; return; }

            TakeoffLat = double.Parse(txtTLat.Text);
            TakeoffLng = double.Parse(txtTLng.Text);
            TakeoffAlt = double.Parse(txtTAlt.Text);
            LandLat = double.Parse(txtLLat.Text);
            LandLng = double.Parse(txtLLng.Text);
            LandAlt = double.Parse(txtLAlt.Text);
            ShouldLand = rbLand.Checked;
            LandingHeight = double.Parse(txtLandingHeight.Text);
            SetAsHome = chkSetAsHome.Checked;
        }

        private bool validateNumber(TextBox tb, double? min, double? max, string name)
        {
            double v;
            if (!double.TryParse(tb.Text, out v)) { MessageBox.Show(name + "必须是数字", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); tb.Focus(); return false; }
            if (min.HasValue && v < min.Value) { MessageBox.Show(name + $"必须≥{min}", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); tb.Focus(); return false; }
            if (max.HasValue && v > max.Value) { MessageBox.Show(name + $"必须≤{max}", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); tb.Focus(); return false; }
            return true;
        }
        
        private void UpdateLandingHeightLock()
        {
            if (rbLand.Checked)
            {
                // 选择降落时，解锁降落高度输入框，用户可自定义
                txtLandingHeight.ReadOnly = false;
                txtLandingHeight.TabStop = true;
            }
            else
            {
                // 选择经过时，锁定降落高度输入框（因为不需要降落）
                // 保持当前文本内容不变，只是设为只读
                txtLandingHeight.ReadOnly = true;
                txtLandingHeight.TabStop = false;
                // 不修改Text内容，保持用户上次输入的值
            }
        }
    }
}


