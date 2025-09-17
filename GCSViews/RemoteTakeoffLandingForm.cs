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
        public bool RelativeFollowing { get; private set; }
        public bool WriteWaypoints { get; private set; }
        
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
        CheckBox chkSetAsHome, chkWriteWaypoints;
        RadioButton rbTerrainFollowing, rbRelativeFollowing;
        Button btnOK, btnCancel;
        Label lblCargoTime, lblDropHeight;

        public RemoteTakeoffLandingForm()
        {
            InitializeComponent();
            ThemeManager.ApplyThemeTo(this);
            
            // 自动获取地图中心作为起飞点，使用多种方案确保能获取到有效坐标
            try
            {
                double lat = 0, lng = 0, alt = 20;
                bool hasValidPosition = false;

                // 方案1：优先使用FlightPlanner的地图位置（最可靠）
                if (MainV2.instance?.FlightPlanner?.MainMap != null)
                {
                    var fpMapCenter = MainV2.instance.FlightPlanner.MainMap.Position;
                    if (!fpMapCenter.IsEmpty && Math.Abs(fpMapCenter.Lat) > 0.001 && Math.Abs(fpMapCenter.Lng) > 0.001)
                    {
                        lat = fpMapCenter.Lat;
                        lng = fpMapCenter.Lng;
                        hasValidPosition = true;
                    }
                }

                if (!hasValidPosition)
                {
                    // 方案2：从FlightData地图控件获取位置
                    if (MainV2.instance?.FlightData?.gMapControl1 != null)
                    {
                        var mapCenter = MainV2.instance.FlightData.gMapControl1.Position;
                        if (!mapCenter.IsEmpty && Math.Abs(mapCenter.Lat) > 0.001 && Math.Abs(mapCenter.Lng) > 0.001)
                        {
                            lat = mapCenter.Lat;
                            lng = mapCenter.Lng;
                            hasValidPosition = true;
                        }
                    }
                }

                if (!hasValidPosition)
                {
                    // 方案3：从MAV数据获取当前位置
                    if (MainV2.comPort?.MAV?.cs != null && 
                        Math.Abs(MainV2.comPort.MAV.cs.lat) > 0.001 && 
                        Math.Abs(MainV2.comPort.MAV.cs.lng) > 0.001)
                    {
                        lat = MainV2.comPort.MAV.cs.lat;
                        lng = MainV2.comPort.MAV.cs.lng;
                        alt = MainV2.comPort.MAV.cs.altasl;
                        hasValidPosition = true;
                    }
                }

                if (!hasValidPosition)
                {
                    // 方案4：从Home位置获取
                    if (MainV2.comPort?.MAV?.cs?.HomeLocation != null && 
                        Math.Abs(MainV2.comPort.MAV.cs.HomeLocation.Lat) > 0.001 && 
                        Math.Abs(MainV2.comPort.MAV.cs.HomeLocation.Lng) > 0.001)
                    {
                        lat = MainV2.comPort.MAV.cs.HomeLocation.Lat;
                        lng = MainV2.comPort.MAV.cs.HomeLocation.Lng;
                        alt = MainV2.comPort.MAV.cs.HomeLocation.Alt;
                        hasValidPosition = true;
                    }
                }

                if (!hasValidPosition)
                {
                    // 方案5：从设置中获取上次保存的位置
                    double savedLat = Settings.Instance.GetDouble("maplast_lat");
                    double savedLng = Settings.Instance.GetDouble("maplast_lng");
                    if (Math.Abs(savedLat) > 0.001 && Math.Abs(savedLng) > 0.001)
                    {
                        lat = savedLat;
                        lng = savedLng;
                        hasValidPosition = true;
                    }
                }

                if (!hasValidPosition)
                {
                    // 方案6：使用默认位置（北京）
                    lat = 39.9042;
                    lng = 116.4074;
                    alt = 30;
                }

                // 设置获取到的坐标
                TakeoffLat = lat;
                TakeoffLng = lng;
                TakeoffAlt = alt;
                
                // 更新起飞点显示信息
                UpdateTakeoffInfo();
                
                // 设置目的地默认值为上一个航点（如果有的话）
                SetDestinationDefaultValue();
                
                // 初始化降落选项可见性（确保初始状态正确）
                UpdateLandingOptionsVisibility();
                
                // 窗体加载完成后再次确保状态正确
                this.Load += (s, e) => {
                    UpdateLandingOptionsVisibility();
                    UpdateWaypointCheckboxState();
                };
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
                btnOK.Location = new Point(185, 540);
                btnCancel.Location = new Point(345, 540);
                this.ClientSize = new Size(650, 680);
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.Text = "广东梵亚异地起降";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            // 起飞点信息显示（只读，自动获取）
            var lblTakeoffTitle = new Label { Text = "起飞点（自动获取）", AutoSize = true, Location = new Point(25, 25), Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold) };
            var lblTakeoffInfo = new Label { Text = "经度: --, 纬度: --, 高度: 30米", AutoSize = true, Location = new Point(25, 55), ForeColor = Color.DarkBlue, Font = new Font("Microsoft YaHei", 11F) };

            // 飞行选项组
            var lblFlightOptionsTitle = new Label { Text = "飞行选项", AutoSize = true, Location = new Point(25, 90), Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold) };
            var flightOptionsPanel = new Panel { Location = new Point(20, 115), Size = new Size(580, 50) };
            rbTerrainFollowing = new RadioButton { Text = "默认仿地飞行", AutoSize = true, Location = new Point(10, 12), Checked = true, Font = new Font("Microsoft YaHei", 11F) };
            rbRelativeFollowing = new RadioButton { Text = "启用定高飞行", AutoSize = true, Location = new Point(250, 12), Font = new Font("Microsoft YaHei", 11F) };
            flightOptionsPanel.Controls.AddRange(new Control[] { rbTerrainFollowing, rbRelativeFollowing });

            // 目的地输入（用户需要输入的部分）
            var lblDestinationTitle = new Label { Text = "目的地", AutoSize = true, Location = new Point(25, 180), Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold) };
            var lblLLNG = new Label { Text = "经度:", AutoSize = true, Location = new Point(25, 215), Font = new Font("Microsoft YaHei", 11F) };
            var lblLLAT = new Label { Text = "纬度:", AutoSize = true, Location = new Point(25, 260), Font = new Font("Microsoft YaHei", 11F) };
            var lblLALT = new Label { Text = "飞行高度(米):", AutoSize = true, Location = new Point(25, 305), Font = new Font("Microsoft YaHei", 11F) };
            txtLLng = new TextBox { Location = new Point(150, 210), Size = new Size(280, 35), Text = "0", Font = new Font("Microsoft YaHei", 11F) };
            txtLLat = new TextBox { Location = new Point(150, 255), Size = new Size(280, 35), Text = "0", Font = new Font("Microsoft YaHei", 11F) };
            txtLAlt = new TextBox { Location = new Point(150, 300), Size = new Size(280, 35), Text = "20", Font = new Font("Microsoft YaHei", 11F) };

            // 飞行模式组
            var lblFlightModeTitle = new Label { Text = "飞行模式", AutoSize = true, Location = new Point(25, 355), Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold) };
            var flightModePanel = new Panel { Location = new Point(20, 380), Size = new Size(350, 140) };
            rbPassThrough = new RadioButton { Text = "经过航点（不降落）", AutoSize = true, Location = new Point(10, 5), Checked = true, Font = new Font("Microsoft YaHei", 11F) };
            rbLandGround = new RadioButton { Text = "降落地面，按键返航", AutoSize = true, Location = new Point(10, 35), Font = new Font("Microsoft YaHei", 11F) };
            rbLandCargo = new RadioButton { Text = "降落地面，释放货物____秒后返航", AutoSize = true, Location = new Point(10, 65), Font = new Font("Microsoft YaHei", 11F) };
            rbLandDrop = new RadioButton { Text = "空中抛投(需填写抛投高度)", AutoSize = true, Location = new Point(10, 95), Font = new Font("Microsoft YaHei", 11F) };
            flightModePanel.Controls.AddRange(new Control[] { rbPassThrough, rbLandGround, rbLandCargo, rbLandDrop });
            
            // 降落参数输入（移动到对应选项右侧）
            // 等待时间输入框放在第三个选项（降落地面，释放货物后返航）右侧
            lblCargoTime = new Label { Text = "等待时间(秒):", AutoSize = true, Location = new Point(400, 425), Font = new Font("Microsoft YaHei", 10F) };
            txtCargoTime = new TextBox { Location = new Point(500, 420), Size = new Size(100, 35), Text = "5", Font = new Font("Microsoft YaHei", 11F) }; // 默认5秒
            // 抛投高度输入框放在第四个选项（空中抛投）右侧
            lblDropHeight = new Label { Text = "抛投高度(米):", AutoSize = true, Location = new Point(400, 485), Font = new Font("Microsoft YaHei", 10F) };
            txtDropHeight = new TextBox { Location = new Point(500, 480), Size = new Size(100, 35), Text = "10", Font = new Font("Microsoft YaHei", 11F) }; // 默认30米
            
            // 按钮（移到自动写入航点选框上方，完全居中对齐）
            btnOK = new Button { Text = "确定", Location = new Point(185, 540), Size = new Size(140, 45), DialogResult = DialogResult.OK, Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold) };
            btnCancel = new Button { Text = "取消", Location = new Point(345, 540), Size = new Size(140, 45), DialogResult = DialogResult.Cancel, Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold) };
            
            // 写入航点选项（移到按钮下方）
            chkWriteWaypoints = new CheckBox { Text = "自动写入航点并读取", AutoSize = true, Location = new Point(25, 600), Checked = IsConnected(), Font = new Font("Microsoft YaHei", 11F) };
            var lblWaypointTip = new Label { Text = "※ 选中后按下确定将自动写入航点到飞行器并读取航点列表", AutoSize = true, Location = new Point(25, 630), ForeColor = Color.Gray, Font = new Font("Microsoft YaHei", 10F) };
            btnOK.Click += BtnOK_Click;

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
            this.ClientSize = new Size(650, 680);
            this.Controls.AddRange(new Control[] { 
                lblTakeoffTitle, lblTakeoffInfo,
                lblFlightOptionsTitle, flightOptionsPanel,
                lblDestinationTitle, lblLLAT, lblLLNG, lblLALT, txtLLat, txtLLng, txtLAlt,
                lblFlightModeTitle, flightModePanel,
                lblCargoTime, txtCargoTime, lblDropHeight, txtDropHeight,
                chkWriteWaypoints, lblWaypointTip,
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
            // 等待时间至少为1秒才能生效
            if (rbLandCargo.Checked && txtCargoTime.Enabled && !validateNumber(txtCargoTime, 1, 300, "货物释放时间")) 
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
            // 设置飞行模式
            if (rbTerrainFollowing.Checked)
                TerrainFollowing = true;
            else if (rbRelativeFollowing.Checked)
                TerrainFollowing = false;
            
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
            // TerrainFollowing = chkTerrainFollowing.Checked; // 设置仿地飞行选项：true=Terrain模式，false=Relative模式
            // RelativeFollowing = chkRelativeFollowing.Checked; // 设置定高飞行选项：true=Relative模式，false=Terrain模式
            WriteWaypoints = chkWriteWaypoints.Checked; // 设置是否写入航点
            
            // 注意：航点写入功能将在异地起降弹窗关闭后，在FlightPlanner中执行
            // 这里只设置标志，不立即执行写入操作，避免重复执行
            
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
                double lat = 0, lng = 0, alt = 20;
                bool hasValidPosition = false;

                // 方案1：优先获取上一个有效航点的坐标作为目的地默认值
                if (MainV2.comPort?.MAV?.wps != null)
                {
                    var waypoints = MainV2.comPort.MAV.wps;
                    
                    if (waypoints != null && waypoints.Count > 0)
                    {
                        for (int i = waypoints.Count - 1; i >= 0; i--)
                        {
                            var wp = waypoints[i];
                            // 自适应单位：
                            // - 如果值很大，认为是E7或厘米单位；否则直接当作度/米
                            double wpLat = Math.Abs(wp.x) > 1000 ? wp.x / 1e7 : wp.x;
                            double wpLng = Math.Abs(wp.y) > 1000 ? wp.y / 1e7 : wp.y;
                            double wpAlt = wp.z > 1000 ? wp.z / 100 : wp.z;
                            bool latValid = wpLat >= -90 && wpLat <= 90 && Math.Abs(wpLat) > 1e-6;
                            bool lngValid = wpLng >= -180 && wpLng <= 180 && Math.Abs(wpLng) > 1e-6;
                            if (latValid && lngValid)
                            {
                                lat = wpLat;
                                lng = wpLng;
                                alt = Math.Abs(wpAlt) > 1e-6 ? wpAlt : 30;
                                hasValidPosition = true;
                                break;
                            }
                        }
                    }
                }

                if (!hasValidPosition)
                {
                    // 方案2：使用FlightPlanner的地图位置
                    if (MainV2.instance?.FlightPlanner?.MainMap != null)
                    {
                        var fpMapCenter = MainV2.instance.FlightPlanner.MainMap.Position;
                        if (!fpMapCenter.IsEmpty && Math.Abs(fpMapCenter.Lat) > 0.001 && Math.Abs(fpMapCenter.Lng) > 0.001)
                        {
                            lat = fpMapCenter.Lat;
                            lng = fpMapCenter.Lng;
                            hasValidPosition = true;
                        }
                    }
                }

                if (!hasValidPosition)
                {
                    // 方案3：从FlightData地图控件获取位置
                    if (MainV2.instance?.FlightData?.gMapControl1 != null)
                    {
                        var mapCenter = MainV2.instance.FlightData.gMapControl1.Position;
                        if (!mapCenter.IsEmpty && Math.Abs(mapCenter.Lat) > 0.001 && Math.Abs(mapCenter.Lng) > 0.001)
                        {
                            lat = mapCenter.Lat;
                            lng = mapCenter.Lng;
                            hasValidPosition = true;
                        }
                    }
                }

                if (!hasValidPosition)
                {
                    // 方案4：从MAV数据获取当前位置
                    if (MainV2.comPort?.MAV?.cs != null && 
                        Math.Abs(MainV2.comPort.MAV.cs.lat) > 0.001 && 
                        Math.Abs(MainV2.comPort.MAV.cs.lng) > 0.001)
                    {
                        lat = MainV2.comPort.MAV.cs.lat;
                        lng = MainV2.comPort.MAV.cs.lng;
                        alt = MainV2.comPort.MAV.cs.altasl;
                        hasValidPosition = true;
                    }
                }

                if (!hasValidPosition)
                {
                    // 方案5：从Home位置获取
                    if (MainV2.comPort?.MAV?.cs?.HomeLocation != null && 
                        Math.Abs(MainV2.comPort.MAV.cs.HomeLocation.Lat) > 0.001 && 
                        Math.Abs(MainV2.comPort.MAV.cs.HomeLocation.Lng) > 0.001)
                    {
                        lat = MainV2.comPort.MAV.cs.HomeLocation.Lat;
                        lng = MainV2.comPort.MAV.cs.HomeLocation.Lng;
                        alt = MainV2.comPort.MAV.cs.HomeLocation.Alt;
                        hasValidPosition = true;
                    }
                }

                if (!hasValidPosition)
                {
                    // 方案6：从设置中获取上次保存的位置
                    double savedLat = Settings.Instance.GetDouble("maplast_lat");
                    double savedLng = Settings.Instance.GetDouble("maplast_lng");
                    if (Math.Abs(savedLat) > 0.001 && Math.Abs(savedLng) > 0.001)
                    {
                        lat = savedLat;
                        lng = savedLng;
                        hasValidPosition = true;
                    }
                }

                if (!hasValidPosition)
                {
                    // 方案7：使用默认位置（北京）
                    lat = 39.9042;
                    lng = 116.4074;
                    alt = 30;
                }

                // 设置目的地默认值
                txtLLat.Text = lat.ToString("F6");
                txtLLng.Text = lng.ToString("F6");
                txtLAlt.Text = alt.ToString("0");
            }
            catch
            {
                // 如果出错，使用默认位置
                txtLLat.Text = "39.904200";
                txtLLng.Text = "116.407400";
                txtLAlt.Text = "30";
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
                txtCargoTime.Enabled = true; // 保持启用状态，确保可见
                // txtCargoTime.BackColor = Color.FromArgb(240, 240, 240); // 锁定状态的浅灰色
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
                txtDropHeight.Enabled = true; // 保持启用状态，确保可见
                // txtDropHeight.BackColor = Color.FromArgb(240, 240, 240); // 锁定状态的浅灰色
                txtDropHeight.ReadOnly = true;
            }
            
            // 强制刷新控件显示
            txtCargoTime.Refresh();
            txtDropHeight.Refresh();
        }
        
        private void UpdateTakeoffInfo()
        {
            // 更新起飞点显示信息
            var lblTakeoffInfo = this.Controls.OfType<Label>().FirstOrDefault(l => l.Text.Contains("经度: --"));
            if (lblTakeoffInfo != null)
            {
                lblTakeoffInfo.Text = $"经度: {TakeoffLng:F6}, 纬度: {TakeoffLat:F6}, 高度: {TakeoffAlt}米";
            }
        }
        
        private bool IsConnected()
        {
            // 检查是否连接到飞行器
            return MainV2.comPort?.BaseStream?.IsOpen == true;
        }
        
        private void UpdateWaypointCheckboxState()
        {
            // 根据连接状态更新勾选框状态
            if (chkWriteWaypoints != null)
            {
                chkWriteWaypoints.Checked = IsConnected();
            }
        }
        
        private void WriteAndReadWaypoints()
        {
            try
            {
                // 检查连接状态
                if (!IsConnected())
                {
                    MessageBox.Show("未连接到飞行器，无法写入航点。请先连接飞行器。", "连接错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // 获取FlightPlanner实例
                var flightPlanner = MainV2.instance?.FlightPlanner;
                if (flightPlanner == null)
                {
                    MessageBox.Show("无法获取飞行计划器实例。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                // 检查是否有航点需要写入
                if (flightPlanner.Commands.Rows.Count <= 0)
                {
                    MessageBox.Show("没有航点需要写入。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                // 显示进度信息（简化版）
                var progressForm = new Form()
                {
                    Text = "正在写入航点...",
                    Size = new Size(300, 100),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                
                var progressLabel = new Label()
                {
                    Text = "正在写入航点到飞行器...",
                    Location = new Point(20, 20),
                    Size = new Size(250, 20)
                };
                
                progressForm.Controls.Add(progressLabel);
                progressForm.Show();
                progressForm.Refresh();
                
                // 写入航点到飞行器
                flightPlanner.BUT_write_Click(null, null);
                
                // 等待写入操作完成
                System.Threading.Thread.Sleep(1500);
                
                // 再次检查连接状态
                if (!IsConnected())
                {
                    progressForm.Close();
                    MessageBox.Show("写入过程中连接断开。", "连接错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // 更新进度信息
                progressLabel.Text = "正在读取航点列表...";
                progressForm.Refresh();
                
                // 读取航点列表
                flightPlanner.BUT_read_Click(null, null);
                
                // 关闭进度窗口
                progressForm.Close();
                
                // 显示完成信息（简化版）
                MessageBox.Show($"航点写入和读取操作已完成。\n共处理 {flightPlanner.Commands.Rows.Count} 个航点。", "操作完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"写入航点时发生错误：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}


