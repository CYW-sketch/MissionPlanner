using System;
using System.Drawing;
using System.Linq;
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
        public bool TerrainFollowing { get; private set; }
        
        // 降落模式枚举
        public enum LandingMode
        {
            PassThrough,    // 经过航点（不降落）
            LandGround,     // 降落地面，然后返航
            LandCargo,      // 降落地面，释放货物后返航
            LandDrop        // 高空抛投
        }
        
        public LandingMode SelectedLandingMode { get; private set; }
        public double CargoTime { get; private set; }      // 货物释放时间（秒）
        public double DropHeight { get; private set; }     // 抛投高度（米）

        TextBox txtTLat, txtTLng, txtTAlt, txtLLat, txtLLng, txtLAlt, txtLandingHeight, txtCargoTime, txtDropHeight;
        RadioButton rbPassThrough, rbLandGround, rbLandCargo, rbLandDrop;
        CheckBox chkSetAsHome, chkTerrainFollowing;
        Button btnOK, btnCancel;
        Label lblCargoTime, lblDropHeight;

        public RemoteTakeoffLandingForm()
        {
            InitializeComponent();
            ThemeManager.ApplyThemeTo(this);
            
            // 自动获取地图中心作为起飞点
            try
            {
                if (MainV2.instance?.FlightPlanner?.MainMap != null)
                {
                    var map = MainV2.instance.FlightPlanner.MainMap;
                    TakeoffLat = map.Position.Lat;
                    TakeoffLng = map.Position.Lng;
                    TakeoffAlt = 30; // 默认起飞高度30米
                    
                    // 更新起飞点显示信息
                    UpdateTakeoffInfo();
                    
                    // 设置目的地默认值为上一个航点（如果有的话）
                    SetDestinationDefaultValue();
                }
                
                // 设置默认值
                txtLandingHeight.Text = "0";
                
                // 初始化降落选项可见性（确保初始状态正确）
                UpdateLandingOptionsVisibility();
                
                // 窗体加载完成后再次确保状态正确
                this.Load += (s, e) => UpdateLandingOptionsVisibility();
            }
            catch { }
        }

        // 重载：锁定起点（起飞点）为给定值，并禁止编辑（后续追加时使用）
        public RemoteTakeoffLandingForm(double lockedStartLat, double lockedStartLng, double lockedStartAlt) : this()
        {
            try
            {
                // 设置锁定的起飞点
                TakeoffLat = lockedStartLat;
                TakeoffLng = lockedStartLng;
                TakeoffAlt = lockedStartAlt;
                
                // 更新起飞点显示信息
                UpdateTakeoffInfo();
                
                // 确保降落选项状态正确
                UpdateLandingOptionsVisibility();
                
                // 调整按钮位置（因为界面更紧凑了）
                btnOK.Location = new Point(120, 380);
                btnCancel.Location = new Point(240, 380);
                this.ClientSize = new Size(450, 440);
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

            // 起飞点信息显示（只读，自动获取）
            var lblTakeoffTitle = new Label { Text = "起飞点（自动获取）", AutoSize = true, Location = new Point(20, 20), Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold) };
            var lblTakeoffInfo = new Label { Text = "纬度: --, 经度: --, 高度: 30米", AutoSize = true, Location = new Point(20, 45), ForeColor = Color.DarkBlue };

            // 仿地飞行选项（在目的地输入上方）
            chkTerrainFollowing = new CheckBox { Text = "启用仿地飞行", AutoSize = true, Location = new Point(20, 75), Checked = true };
            var lblTerrainTip = new Label { Text = "※ 启用后使用Terrain模式，禁用则使用Relative模式", AutoSize = true, Location = new Point(20, 95), ForeColor = Color.Gray, Font = new Font("Microsoft YaHei", 8F) };

            // 目的地输入（用户需要输入的部分）
            var lblDestinationTitle = new Label { Text = "目的地", AutoSize = true, Location = new Point(20, 115), Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold) };
            var lblLLAT = new Label { Text = "纬度:", AutoSize = true, Location = new Point(20, 145) };
            var lblLLNG = new Label { Text = "经度:", AutoSize = true, Location = new Point(20, 175) };
            var lblLALT = new Label { Text = "飞行高度(米):", AutoSize = true, Location = new Point(20, 205) };
            txtLLat = new TextBox { Location = new Point(120, 143), Size = new Size(200, 23), Text = "0" };
            txtLLng = new TextBox { Location = new Point(120, 173), Size = new Size(200, 23), Text = "0" };
            txtLAlt = new TextBox { Location = new Point(120, 203), Size = new Size(200, 23), Text = "30" };

            // 飞行模式选项
            var lblOptions = new Label { Text = "飞行模式", AutoSize = true, Location = new Point(20, 235), Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold) };
            rbPassThrough = new RadioButton { Text = "经过航点（不降落）", AutoSize = true, Location = new Point(20, 260), Checked = true };
            rbLandGround = new RadioButton { Text = "降落地面，按键返航(等待时间0为按键返航)", AutoSize = true, Location = new Point(20, 285) };
            rbLandCargo = new RadioButton { Text = "降落地面，释放货物____秒后返航", AutoSize = true, Location = new Point(20, 310) };
            rbLandDrop = new RadioButton { Text = "高空抛投(需填写抛投高度)", AutoSize = true, Location = new Point(20, 335) };
            
            // 降落参数输入（移动到对应选项右侧）
            lblCargoTime = new Label { Text = "等待时间(秒):", AutoSize = true, Location = new Point(250, 310) };
            txtCargoTime = new TextBox { Location = new Point(340, 310), Size = new Size(60, 23), Text = "5" }; // 默认5秒
            lblDropHeight = new Label { Text = "抛投高度(米):", AutoSize = true, Location = new Point(250, 335) };
            txtDropHeight = new TextBox { Location = new Point(340, 335), Size = new Size(60, 23), Text = "30" }; // 默认30米
            
            // 按钮
            btnOK = new Button { Text = "确定", Location = new Point(120, 380), Size = new Size(100, 30), DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "取消", Location = new Point(240, 380), Size = new Size(100, 30), DialogResult = DialogResult.Cancel };
            btnOK.Click += BtnOK_Click;

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
            this.ClientSize = new Size(450, 440);
            this.Controls.AddRange(new Control[] { 
                lblTakeoffTitle, lblTakeoffInfo,
                chkTerrainFollowing, lblTerrainTip,
                lblDestinationTitle, lblLLAT, lblLLNG, lblLALT, txtLLat, txtLLng, txtLAlt,
                lblOptions, rbPassThrough, rbLandGround, rbLandCargo, rbLandDrop,
                lblCargoTime, txtCargoTime, lblDropHeight, txtDropHeight,
                btnOK, btnCancel 
            });
            
            // 在控件添加到窗体后绑定事件
            rbPassThrough.CheckedChanged += (s, e) => UpdateLandingOptionsVisibility();
            rbLandGround.CheckedChanged += (s, e) => UpdateLandingOptionsVisibility();
            rbLandCargo.CheckedChanged += (s, e) => UpdateLandingOptionsVisibility();
            rbLandDrop.CheckedChanged += (s, e) => UpdateLandingOptionsVisibility();
            
            // 初始化时设置默认锁定状态
            UpdateLandingOptionsVisibility();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // 简化验证：只验证目的地坐标和降落高度
            if (!validateNumber(txtLLat, -90, 90, "目的地纬度")) { this.DialogResult = DialogResult.None; return; }
            if (!validateNumber(txtLLng, -180, 180, "目的地经度")) { this.DialogResult = DialogResult.None; return; }
            if (!validateNumber(txtLAlt, null, null, "目的地高度")) { this.DialogResult = DialogResult.None; return; }

            // 验证降落参数（只有在相应选项被选中且输入框可用时才验证）
            // 等待时间允许为0（0=按键解锁）
            if (rbLandCargo.Checked && txtCargoTime.Enabled && !validateNumber(txtCargoTime, 0, 300, "货物释放时间")) 
            { 
                this.DialogResult = DialogResult.None; 
                return; 
            }
            if (rbLandDrop.Checked && txtDropHeight.Enabled && !validateNumber(txtDropHeight, 10, 200, "抛投高度")) 
            { 
                this.DialogResult = DialogResult.None; 
                return; 
            }

            // 设置属性（起飞点坐标已在构造函数中设置）
            LandLat = double.Parse(txtLLat.Text);
            LandLng = double.Parse(txtLLng.Text);
            LandAlt = double.Parse(txtLAlt.Text);
            
            // 设置降落模式
            if (rbPassThrough.Checked)
                SelectedLandingMode = LandingMode.PassThrough;
            else if (rbLandGround.Checked)
                SelectedLandingMode = LandingMode.LandGround;
            else if (rbLandCargo.Checked)
                SelectedLandingMode = LandingMode.LandCargo;
            else if (rbLandDrop.Checked)
                SelectedLandingMode = LandingMode.LandDrop;
                
            ShouldLand = SelectedLandingMode != LandingMode.PassThrough;
            CargoTime = rbLandCargo.Checked ? double.Parse(txtCargoTime.Text) : 0;
            DropHeight = rbLandDrop.Checked ? double.Parse(txtDropHeight.Text) : 0;
            SetAsHome = true; // 自动设置为Home点
            TerrainFollowing = chkTerrainFollowing.Checked; // 设置仿地飞行选项：true=Terrain模式，false=Relative模式
            
            // 设置Frame模式
            // SetFrameMode();
        }
        
        // private void SetFrameMode()
        // {
        //     try
        //     {
        //         // 根据仿地飞行选项设置Frame模式
        //         if (MainV2.instance?.FlightPlanner != null)
        //         {
        //             var flightPlanner = MainV2.instance.FlightPlanner;
                    
        //             if (TerrainFollowing)
        //             {
        //                 // 启用仿地飞行：使用Terrain模式
        //                 flightPlanner.CMB_altmode.SelectedValue = (int)GCSViews.FlightPlanner.altmode.Terrain;
        //             }
        //             else
        //             {
        //                 // 不启用仿地飞行：使用Relative模式
        //                 flightPlanner.CMB_altmode.SelectedValue = (int)GCSViews.FlightPlanner.altmode.Relative;
        //             }
        //         }
        //     }
        //     catch { }
        // }

        private bool validateNumber(TextBox tb, double? min, double? max, string name)
        {
            double v;
            if (!double.TryParse(tb.Text, out v)) { MessageBox.Show(name + "必须是数字", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); tb.Focus(); return false; }
            if (min.HasValue && v < min.Value) { MessageBox.Show(name + $"必须≥{min}", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); tb.Focus(); return false; }
            if (max.HasValue && v > max.Value) { MessageBox.Show(name + $"必须≤{max}", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); tb.Focus(); return false; }
            return true;
        }
        
        private void SetDestinationDefaultValue()
        {
            try
            {
                // 获取上一个有效航点的坐标作为目的地默认值
                if (MainV2.comPort?.MAV?.wps != null)
                {
                    var waypoints = MainV2.comPort.MAV.wps;
                    
                    if (waypoints != null && waypoints.Count > 0)
                    {
                        bool foundValid = false;
                        for (int i = waypoints.Count - 1; i >= 0; i--)
                        {
                            var wp = waypoints[i];
                            // 自适应单位：
                            // - 如果值很大，认为是E7或厘米单位；否则直接当作度/米
                            double lat = Math.Abs(wp.x) > 1000 ? wp.x / 1e7 : wp.x;
                            double lng = Math.Abs(wp.y) > 1000 ? wp.y / 1e7 : wp.y;
                            double alt = wp.z > 1000 ? wp.z / 100 : wp.z;
                            bool latValid = lat >= -90 && lat <= 90 && Math.Abs(lat) > 1e-6;
                            bool lngValid = lng >= -180 && lng <= 180 && Math.Abs(lng) > 1e-6;
                            if (latValid && lngValid)
                            {
                                txtLLat.Text = lat.ToString("F6");
                                txtLLng.Text = lng.ToString("F6");
                                txtLAlt.Text = (Math.Abs(alt) > 1e-6 ? alt : 30).ToString("0");
                                foundValid = true;
                                break;
                            }
                        }
                        if (!foundValid)
                        {
                            // 无有效航点，经纬度可能为0，退回地图中心
                            if (MainV2.instance?.FlightPlanner?.MainMap != null)
                            {
                                var map = MainV2.instance.FlightPlanner.MainMap;
                                txtLLat.Text = map.Position.Lat.ToString("F6");
                                txtLLng.Text = map.Position.Lng.ToString("F6");
                                txtLAlt.Text = "30";
                            }
                        }
                    }
                    else
                    {
                        // 如果没有航点，使用地图中心作为默认值
                        if (MainV2.instance?.FlightPlanner?.MainMap != null)
                        {
                            var map = MainV2.instance.FlightPlanner.MainMap;
                            txtLLat.Text = map.Position.Lat.ToString("F6");
                            txtLLng.Text = map.Position.Lng.ToString("F6");
                            txtLAlt.Text = "30";
                        }
                    }
                }
                else
                {
                    // 如果无法获取航点，使用地图中心作为默认值
                    if (MainV2.instance?.FlightPlanner?.MainMap != null)
                    {
                        var map = MainV2.instance.FlightPlanner.MainMap;
                        txtLLat.Text = map.Position.Lat.ToString("F6");
                        txtLLng.Text = map.Position.Lng.ToString("F6");
                        txtLAlt.Text = "30";
                    }
                }
            }
            catch
            {
                // 如果出错，使用地图中心作为默认值
                try
                {
                    if (MainV2.instance?.FlightPlanner?.MainMap != null)
                    {
                        var map = MainV2.instance.FlightPlanner.MainMap;
                        txtLLat.Text = map.Position.Lat.ToString("F6");
                        txtLLng.Text = map.Position.Lng.ToString("F6");
                        txtLAlt.Text = "30";
                    }
                }
                catch { }
            }
        }
        
        private void UpdateLandingOptionsVisibility()
        {
            // 确保控件已经初始化
            if (txtCargoTime == null || txtDropHeight == null || 
                rbPassThrough == null || rbLandGround == null || 
                rbLandCargo == null || rbLandDrop == null)
                return;
                
            // 根据选择的降落模式来锁定/解锁相应的输入控件
            
            // 等待时间输入框：只在选择"降落地面，释放货物后返航"时可用
            if (rbLandCargo.Checked)
            {
                txtCargoTime.Enabled = true;
                // txtCargoTime.BackColor = SystemColors.Window;
                txtCargoTime.ReadOnly = false;
            }
            else
            {
                txtCargoTime.Enabled = false;
                // txtCargoTime.BackColor = Color.FromArgb(220, 220, 220); // 锁定状态的灰色
                txtCargoTime.ReadOnly = true;
            }
            
            // 抛投高度输入框：只在选择"高空抛投"时可用
            if (rbLandDrop.Checked)
            {
                txtDropHeight.Enabled = true;
                // txtDropHeight.BackColor = SystemColors.Window;
                txtDropHeight.ReadOnly = false;
            }
            else
            {
                txtDropHeight.Enabled = false;
                // txtDropHeight.BackColor = Color.FromArgb(220, 220, 220); // 锁定状态的灰色
                txtDropHeight.ReadOnly = true;
            }
            
            // 强制刷新控件显示
            txtCargoTime.Refresh();
            txtDropHeight.Refresh();
        }
        
        private void UpdateTakeoffInfo()
        {
            // 更新起飞点显示信息
            var lblTakeoffInfo = this.Controls.OfType<Label>().FirstOrDefault(l => l.Text.Contains("纬度: --"));
            if (lblTakeoffInfo != null)
            {
                lblTakeoffInfo.Text = $"纬度: {TakeoffLat:F6}, 经度: {TakeoffLng:F6}, 高度: {TakeoffAlt}米";
            }
        }
    }
}


