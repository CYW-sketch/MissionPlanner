using System;
using System.Drawing;
using System.Windows.Forms;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;

namespace MissionPlanner.Controls
{
    public partial class CoordinateInputForm : Form
    {
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double Altitude { get; private set; }
        public double Param1 { get; private set; }
        public double Param2 { get; private set; }
        public double Param3 { get; private set; }
        public double Param4 { get; private set; }
        private ComboBox cmbCoordinateSystem;
        public Func<string, string[]> GetParamLabelsForCommand { get; set; }
        public string SelectedCommand
        {
            get
            {
                return cmbCoordinateSystem != null ? (cmbCoordinateSystem.SelectedItem as string) : null;
            }
        }
        private TextBox txtLatitude;
        private TextBox txtLongitude;
        private TextBox txtAltitude;
        private Button btnOK;
        private Button btnCancel;
        private Label lblLatitude;
        private Label lblLongitude;
        private Label lblAltitude;
        private Label lblP1;
        private Label lblP2;
        private Label lblP3;
        private Label lblP4;
        private TextBox txtP1;
        private TextBox txtP2;
        private TextBox txtP3;
        private TextBox txtP4;
        private Label lblcmbCoordinateSystem;
        public CoordinateInputForm()
        {
            InitializeComponent();
            ThemeManager.ApplyThemeTo(this);
            
            // 设置默认值为当前地图中心位置（如果可用）
            try
            {
                if (MainV2.instance != null && MainV2.instance.FlightPlanner != null)
                {
                    var map = MainV2.instance.FlightPlanner.MainMap;
                    if (map != null)
                    {
                        txtLatitude.Text = map.Position.Lat.ToString("F6");
                        txtLongitude.Text = map.Position.Lng.ToString("F6");
                    }
                }
            }
            catch
            {
                // 如果获取失败，使用默认值
                txtLatitude.Text = "0.0";
                txtLongitude.Text = "0.0";
            }
        }
        // 必须要有，初始化设计师生成的组件
        private void InitializeComponent()
        {
            this.txtLatitude = new TextBox();
            this.txtLongitude = new TextBox();
            this.txtAltitude = new TextBox();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            this.lblLatitude = new Label();
            this.lblLongitude = new Label();
            this.lblAltitude = new Label();
            this.lblP1 = new Label();
            this.lblP2 = new Label();
            this.lblP3 = new Label();
            this.lblP4 = new Label();
            this.txtP1 = new TextBox();
            this.txtP2 = new TextBox();
            this.txtP3 = new TextBox();
            this.txtP4 = new TextBox();
            this.lblcmbCoordinateSystem = new Label();
            this.SuspendLayout();

            // lblLatitude
            this.lblLatitude.AutoSize = true;
            this.lblLatitude.Location = new Point(20, 20);
            this.lblLatitude.Name = "lblLatitude";
            this.lblLatitude.Size = new Size(60, 15);
            this.lblLatitude.Text = "纬度 (-90 到 90):";

            // txtLatitude
            this.txtLatitude.Location = new Point(140, 17);
            this.txtLatitude.Name = "txtLatitude";
            this.txtLatitude.Size = new Size(150, 23);
            this.txtLatitude.Text = "0.0";

            // lblLongitude
            this.lblLongitude.AutoSize = true;
            this.lblLongitude.Location = new Point(20, 50);
            this.lblLongitude.Name = "lblLongitude";
            this.lblLongitude.Size = new Size(60, 15);
            this.lblLongitude.Text = "经度 (-180 到 180):";

            // txtLongitude
            this.txtLongitude.Location = new Point(140, 47);
            this.txtLongitude.Name = "txtLongitude";
            this.txtLongitude.Size = new Size(150, 23);
            this.txtLongitude.Text = "0.0";

            // lblAltitude
            this.lblAltitude.AutoSize = true;
            this.lblAltitude.Location = new Point(20, 80);
            this.lblAltitude.Name = "lblAltitude";
            this.lblAltitude.Size = new Size(60, 15);
            this.lblAltitude.Text = "高度 (米):";

            // txtAltitude
            this.txtAltitude.Location = new Point(140, 77);
            this.txtAltitude.Name = "txtAltitude";
            this.txtAltitude.Size = new Size(150, 23);
            this.txtAltitude.Text = "0.0";

            //lblcmbCoordinateSystem
            // lblcmbCoordinateSystem
            this.lblcmbCoordinateSystem.AutoSize = true;
            this.lblcmbCoordinateSystem.Location = new Point(20, 110);
            this.lblcmbCoordinateSystem.Name = "lblcmbCoordinateSystem";
            this.lblcmbCoordinateSystem.Size = new Size(60, 15);
            this.lblcmbCoordinateSystem.Text = "命令:";

            // 命令下拉菜单（由外部设置命令列表）
            cmbCoordinateSystem = new ComboBox 
            { 
                Location = new System.Drawing.Point(140, 107), 
                Size = new System.Drawing.Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCoordinateSystem.SelectedIndexChanged += cmbCoordinateSystem_SelectedIndexChanged;

            // dynamic param controls (hidden by default)
            int baseY = 140;
            int rowGap = 23;
            initParamControl(this.lblP1, this.txtP1, new Point(20, baseY + rowGap * 0), "参数1:");
            initParamControl(this.lblP2, this.txtP2, new Point(20, baseY + rowGap * 1), "参数2:");
            initParamControl(this.lblP3, this.txtP3, new Point(20, baseY + rowGap * 2), "参数3:");
            initParamControl(this.lblP4, this.txtP4, new Point(20, baseY + rowGap * 3), "参数4:");

            // btnOK
            this.btnOK.DialogResult = DialogResult.OK;
            this.btnOK.Location = new Point(50, baseY + rowGap * 5 + 5);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new Size(75, 25);
            this.btnOK.Text = "确定";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new EventHandler(this.btnOK_Click);

            // btnCancel
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.Location = new Point(150, baseY + rowGap * 5 + 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(75, 25);
            this.btnCancel.Text = "取消";
            this.btnCancel.UseVisualStyleBackColor = true;

            // CoordinateInputForm
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new Size(360, baseY + rowGap * 5 + 40);
            this.Controls.Add(this.lblLatitude);
            this.Controls.Add(this.txtLatitude);
            this.Controls.Add(this.lblLongitude);
            this.Controls.Add(this.txtLongitude);
            this.Controls.Add(this.lblAltitude);
            this.Controls.Add(this.txtAltitude);
            this.Controls.Add(this.lblP1);
            this.Controls.Add(this.txtP1);
            this.Controls.Add(this.lblP2);
            this.Controls.Add(this.txtP2);
            this.Controls.Add(this.lblP3);
            this.Controls.Add(this.txtP3);
            this.Controls.Add(this.lblP4);
            this.Controls.Add(this.txtP4);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.cmbCoordinateSystem);
            this.Controls.Add(this.lblcmbCoordinateSystem);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CoordinateInputForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "输入坐标";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void initParamControl(Label lbl, TextBox txt, Point location, string defaultText)
        {
            lbl.AutoSize = true;
            lbl.Location = location;
            lbl.Size = new Size(100, 15);
            lbl.Text = defaultText;
            lbl.Visible = false;

            txt.Location = new Point(140, location.Y - 3);
            txt.Size = new Size(150, 23);
            txt.Text = "0";
            txt.Visible = false;
        }
        // 设置参数标签
        public void SetParamLabels(string[] labels)
        {
            applyParamLabel(lblP1, txtP1, labels, 0, "Param1");
            applyParamLabel(lblP2, txtP2, labels, 1, "Param2");
            applyParamLabel(lblP3, txtP3, labels, 2, "Param3");
            applyParamLabel(lblP4, txtP4, labels, 3, "Param4");
        }

        private void applyParamLabel(Label lbl, TextBox txt, string[] labels, int index, string fallback)
        {
            string text = (labels != null && labels.Length > index) ? labels[index] : string.Empty;
            if (string.IsNullOrWhiteSpace(text)) text = fallback;
            lbl.Text = text + ":";
            lbl.Visible = true;
            txt.Visible = true;
        }

        public void SetParamDefaults(double? p1 = null, double? p2 = null, double? p3 = null, double? p4 = null)
        {
            if (p1.HasValue) txtP1.Text = p1.Value.ToString();
            if (p2.HasValue) txtP2.Text = p2.Value.ToString();
            if (p3.HasValue) txtP3.Text = p3.Value.ToString();
            if (p4.HasValue) txtP4.Text = p4.Value.ToString();
        }

        public void SetCommandList(System.Collections.IEnumerable commands, string defaultCommand = null)
        {
            cmbCoordinateSystem.Items.Clear();
            if (commands != null)
            {
                foreach (var cmd in commands)
                {
                    if (cmd != null)
                        cmbCoordinateSystem.Items.Add(cmd.ToString());
                }
            }
            if (!string.IsNullOrEmpty(defaultCommand))
            {
                int idx = cmbCoordinateSystem.Items.IndexOf(defaultCommand);
                if (idx >= 0)
                    cmbCoordinateSystem.SelectedIndex = idx;
            }
            if (cmbCoordinateSystem.Items.Count > 0 && cmbCoordinateSystem.SelectedIndex < 0)
                cmbCoordinateSystem.SelectedIndex = 0;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (ValidateInput())
            {
                Latitude = double.Parse(txtLatitude.Text);
                Longitude = double.Parse(txtLongitude.Text);
                Altitude = double.Parse(txtAltitude.Text);
                Param1 = parseOrDefault(txtP1);
                Param2 = parseOrDefault(txtP2);
                Param3 = parseOrDefault(txtP3);
                Param4 = parseOrDefault(txtP4);
            }
            else
            {
                DialogResult = DialogResult.None;
            }
        }

        private void cmbCoordinateSystem_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var cmd = SelectedCommand;
                if (!string.IsNullOrEmpty(cmd) && GetParamLabelsForCommand != null)
                {
                    var labels = GetParamLabelsForCommand(cmd);
                    SetParamLabels(labels);
                }
            }
            catch { }
        }

        private double parseOrDefault(TextBox tb)
        {
            if (tb == null || !tb.Visible) return 0;
            double v; if (!double.TryParse(tb.Text, out v)) return 0; return v;
        }

        private bool ValidateInput()
        {
            double lat, lng, alt;
            
            if (!double.TryParse(txtLatitude.Text, out lat))
            {
                MessageBox.Show("纬度必须是有效的数字！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLatitude.Focus();
                return false;
            }
            
            if (lat < -90 || lat > 90)
            {
                MessageBox.Show("纬度必须在 -90 到 90 之间！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLatitude.Focus();
                return false;
            }
                
            if (!double.TryParse(txtLongitude.Text, out lng))
            {
                MessageBox.Show("经度必须是有效的数字！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLongitude.Focus();
                return false;
            }
            
            if (lng < -180 || lng > 180)
            {
                MessageBox.Show("经度必须在 -180 到 180 之间！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLongitude.Focus();
                return false;
            }
                
            if (!double.TryParse(txtAltitude.Text, out alt))
            {
                MessageBox.Show("高度必须是有效的数字！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAltitude.Focus();
                return false;
            }
                
            // Validate optional params if visible (empty allowed)
            if (txtP1 != null && txtP1.Visible && !isValidOrEmpty(txtP1)) { MessageBox.Show("参数1必须是数字！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtP1.Focus(); return false; }
            if (txtP2 != null && txtP2.Visible && !isValidOrEmpty(txtP2)) { MessageBox.Show("参数2必须是数字！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtP2.Focus(); return false; }
            if (txtP3 != null && txtP3.Visible && !isValidOrEmpty(txtP3)) { MessageBox.Show("参数3必须是数字！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtP3.Focus(); return false; }
            if (txtP4 != null && txtP4.Visible && !isValidOrEmpty(txtP4)) { MessageBox.Show("参数4必须是数字！", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtP4.Focus(); return false; }

            return true;
        }

        private bool isValidOrEmpty(TextBox tb)
        {
            if (string.IsNullOrWhiteSpace(tb.Text)) return true;
            double v; return double.TryParse(tb.Text, out v);
        }
    }
}
