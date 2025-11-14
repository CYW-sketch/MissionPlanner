using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace MissionPlanner.Controls
{
	public class RadialDPad : Control
	{
		[Browsable(true)]
		[DefaultValue(12)]
		public int RingThickness { get; set; } =40;

		[Browsable(true)]
		[DefaultValue(typeof(Color), "#66000000")]
		public Color RingColor { get; set; } = Color.FromArgb(102, 0, 0, 0);

		[Browsable(true)]
		[DefaultValue(typeof(Color), "#99FFFFFF")]
		public Color HighlightColor { get; set; } = Color.FromArgb(153, 255, 255, 255);

		// Quadrant labels (for UI hints)
		[Browsable(true)]
		[DefaultValue("")]
		public string UpText { get; set; } = string.Empty;

		[Browsable(true)]
		[DefaultValue("")]
		public string DownText { get; set; } = string.Empty;

		[Browsable(true)]
		[DefaultValue("")]
		public string LeftText { get; set; } = string.Empty;

		[Browsable(true)]
		[DefaultValue("")]
		public string RightText { get; set; } = string.Empty;

		[Browsable(true)]
		[DefaultValue(typeof(Color), "White")]
		public Color LabelColor { get; set; } = Color.White;

		[Browsable(true)]
		public Font LabelFont { get; set; } = new Font("Microsoft YaHei", 10f, FontStyle.Bold);

		[Browsable(true)]
		[DefaultValue(0.7f)]
		public float LabelRadiusFactor { get; set; } = 0.7f; // 0..1 inside the ring band

		// 摇杆类型：true=左摇杆(WASD)，false=右摇杆(方向键)
		[Browsable(true)]
		[DefaultValue(true)]
		[Description("true=左摇杆(WASD控制), false=右摇杆(方向键控制)")]
		public bool IsLeftStick { get; set; } = true;

		[Browsable(false)] public bool UpActive { get; private set; }
		[Browsable(false)] public bool DownActive { get; private set; }
		[Browsable(false)] public bool LeftActive { get; private set; }
		[Browsable(false)] public bool RightActive { get; private set; }

		public event EventHandler<bool> UpChanged;
		public event EventHandler<bool> DownChanged;
		public event EventHandler<bool> LeftChanged;
		public event EventHandler<bool> RightChanged;
		
		// 标记是否正在使用鼠标操作
		private bool isMouseActive = false;	
		
		// Windows API 用于检测按键状态
		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(int vKey);
		
		// Windows API 用于获取当前活动窗口句柄
		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		public RadialDPad()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor | ControlStyles.EnableNotifyMessage, true);
			Size = new Size(360, 360);
			BackColor = Color.Transparent; // allowed after enabling SupportsTransparentBackColor
			this.TabStop = false; // 取消Tab控制，不需要焦点即可使用
			
			// 启用触摸支持
			EnableTouch();
			
			// 添加定时器检测按键释放
			var timer = new Timer { Interval = 50 }; // 每50ms检查一次
			timer.Tick += Timer_Tick;
			timer.Start();
		}
		
		// 启用触摸支持
		private void EnableTouch()
		{
			// Windows Forms 会自动将触摸事件转换为鼠标事件
			// 这里不需要特殊处理
		}

		// 检查控件是否应该响应键盘输入
		private bool ShouldRespondToKeyboard()
		{
			// 检查控件是否可见且启用
			if (!Visible || !Enabled)
				return false;
			
			// 检查父窗体是否存在
			Form parentForm = FindForm();
			if (parentForm == null)
				return false;
			
			// 检查窗体是否处于活动状态（使用Windows API检查当前活动窗口）
			IntPtr foregroundWindow = GetForegroundWindow();
			IntPtr formHandle = parentForm.Handle;
			
			// 如果当前活动窗口就是我们的窗体，或者窗体包含焦点，则响应键盘输入
			return foregroundWindow == formHandle || parentForm.ContainsFocus;
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			// 如果正在使用鼠标操作，定时器不处理键盘输入
			if (isMouseActive)
				return;
			
			// 只有在控件应该响应键盘输入时才检测按键
			if (!ShouldRespondToKeyboard())
			{
				// 如果控件不应该响应，但之前有激活状态，则清除所有状态
				if (UpActive || DownActive || LeftActive || RightActive)
				{
					SetUp(false);
					SetDown(false);
					SetLeft(false);
					SetRight(false);
				}
				return;
			}
			
			if (IsLeftStick)
			{
				// 左摇杆：使用WASD键
				bool upPressed = (GetAsyncKeyState((int)Keys.W) & 0x8000) != 0;
				bool downPressed = (GetAsyncKeyState((int)Keys.S) & 0x8000) != 0;
				bool leftPressed = (GetAsyncKeyState((int)Keys.A) & 0x8000) != 0;
				bool rightPressed = (GetAsyncKeyState((int)Keys.D) & 0x8000) != 0;
				
				// 分别检查每个按键的状态变化，只更新变化的状态
				if (UpActive != upPressed)
				{
					SetUp(upPressed);
				}
				if (DownActive != downPressed)
				{
					SetDown(downPressed);
				}
				if (LeftActive != leftPressed)
				{
					SetLeft(leftPressed);
				}
				if (RightActive != rightPressed)
				{
					SetRight(rightPressed);
				}
			}
			else
			{
				// 右摇杆：使用方向键
				bool upPressed = (GetAsyncKeyState((int)Keys.Up) & 0x8000) != 0;
				bool downPressed = (GetAsyncKeyState((int)Keys.Down) & 0x8000) != 0;
				bool leftPressed = (GetAsyncKeyState((int)Keys.Left) & 0x8000) != 0;
				bool rightPressed = (GetAsyncKeyState((int)Keys.Right) & 0x8000) != 0;
				
				// 分别检查每个按键的状态变化，只更新变化的状态
				if (UpActive != upPressed)
				{
					SetUp(upPressed);
				}
				if (DownActive != downPressed)
				{
					SetDown(downPressed);
				}
				if (LeftActive != leftPressed)
				{
					SetLeft(leftPressed);
				}
				if (RightActive != rightPressed)
				{
					SetRight(rightPressed);
				}
			}
		}
		// 重写OnKeyDown方法处理按键按下
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (IsLeftStick)
			{
				// 左摇杆：只响应WASD键
				switch (e.KeyCode)
				{
					case Keys.W:
						TriggerMouseClickForDirection("up");
						e.Handled = true;
						break;
					case Keys.S:
						TriggerMouseClickForDirection("down");
						e.Handled = true;
						break;
					case Keys.A:
						TriggerMouseClickForDirection("left");
						e.Handled = true;
						break;
					case Keys.D:
						TriggerMouseClickForDirection("right");
						e.Handled = true;
						break;
					default:
						base.OnKeyDown(e);
						return;
				}
			}
			else
			{
				// 右摇杆：只响应方向键
				switch (e.KeyCode)
				{
					case Keys.Up:
						TriggerMouseClickForDirection("up");
						e.Handled = true;
						break;
					case Keys.Down:
						TriggerMouseClickForDirection("down");
						e.Handled = true;
						break;
					case Keys.Left:
						TriggerMouseClickForDirection("left");
						e.Handled = true;
						break;
					case Keys.Right:
						TriggerMouseClickForDirection("right");
						e.Handled = true;
						break;
					default:
						base.OnKeyDown(e);
						return;
				}
			}
			base.OnKeyDown(e);
		}

		// 重写OnKeyUp方法处理按键释放
		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (IsLeftStick)
			{
				// 左摇杆：只响应WASD键
				switch (e.KeyCode)
				{
					case Keys.W:
						SetUp(false);
						e.Handled = true;
						break;
					case Keys.S:
						SetDown(false);
						e.Handled = true;
						break;
					case Keys.A:
						SetLeft(false);
						e.Handled = true;
						break;
					case Keys.D:
						SetRight(false);
						e.Handled = true;
						break;
					default:
						base.OnKeyUp(e);
						return;
				}
			}
			else
			{
				// 右摇杆：只响应方向键
				switch (e.KeyCode)
				{
					case Keys.Up:
						SetUp(false);
						e.Handled = true;
						break;
					case Keys.Down:
						SetDown(false);
						e.Handled = true;
						break;
					case Keys.Left:
						SetLeft(false);
						e.Handled = true;
						break;
					case Keys.Right:
						SetRight(false);
						e.Handled = true;
						break;
					default:
						base.OnKeyUp(e);
						return;
				}
			}
			Invalidate();
			base.OnKeyUp(e);
		}

		// 重写ProcessCmdKey方法，使控件无需焦点也能接收键盘输入
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (IsLeftStick)
			{
				// 左摇杆：只响应WASD键
				switch (keyData)
				{
					case Keys.W:
						TriggerMouseClickForDirection("up");
						return true;
					case Keys.S:
						TriggerMouseClickForDirection("down");
						return true;
					case Keys.A:
						TriggerMouseClickForDirection("left");
						return true;
					case Keys.D:
						TriggerMouseClickForDirection("right");
						return true;
				}
			}
			else
			{
				// 右摇杆：只响应方向键
				switch (keyData)
				{
					case Keys.Up:
						TriggerMouseClickForDirection("up");
						return true;
					case Keys.Down:
						TriggerMouseClickForDirection("down");
						return true;
					case Keys.Left:
						TriggerMouseClickForDirection("left");
						return true;
					case Keys.Right:
						TriggerMouseClickForDirection("right");
						return true;
				}
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		// 重写IsInputKey方法确保对应的按键不会被父控件处理
		protected override bool IsInputKey(Keys keyData)
		{
			if (IsLeftStick)
			{
				// 左摇杆：WASD键
				switch (keyData)
				{
					case Keys.W:
					case Keys.A:
					case Keys.S:
					case Keys.D:
						return true;
				}
			}
			else
			{
				// 右摇杆：方向键
				switch (keyData)
				{
					case Keys.Up:
					case Keys.Down:
					case Keys.Left:
					case Keys.Right:
						return true;
				}
			}
			return base.IsInputKey(keyData);
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
			var rect = ClientRectangle;
			rect.Inflate(-4, -4);
			var center = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
			int radiusOuter = Math.Min(rect.Width, rect.Height) / 2;
			int radiusInner = Math.Max(0, radiusOuter - RingThickness);

			// Draw base ring as a filled annulus so it matches highlight band exactly
			using (var path = new System.Drawing.Drawing2D.GraphicsPath())
			{
				var outer = new Rectangle(center.X - radiusOuter, center.Y - radiusOuter, radiusOuter * 2, radiusOuter * 2);
				var inner = new Rectangle(center.X - radiusInner, center.Y - radiusInner, radiusInner * 2, radiusInner * 2);
				path.AddEllipse(outer);
				path.AddEllipse(inner);
				using (var brush = new SolidBrush(RingColor))
				{
					e.Graphics.FillPath(brush, path);
				}
			}

			// Draw quadrant highlights (Right, Down, Left, Up)
			DrawQuadrant(e.Graphics, center, radiusInner, radiusOuter, 315, 90, RightActive);
			DrawQuadrant(e.Graphics, center, radiusInner, radiusOuter, 45, 90, DownActive);
			DrawQuadrant(e.Graphics, center, radiusInner, radiusOuter, 135, 90, LeftActive);
			DrawQuadrant(e.Graphics, center, radiusInner, radiusOuter, 225, 90, UpActive);

			// Draw labels around the ring
			DrawLabel(e.Graphics, center, radiusOuter, radiusInner, 270, UpText);    // Top
			DrawLabel(e.Graphics, center, radiusOuter, radiusInner, 90, DownText);   // Bottom
			DrawLabel(e.Graphics, center, radiusOuter, radiusInner, 180, LeftText);  // Left
			DrawLabel(e.Graphics, center, radiusOuter, radiusInner, 0, RightText);   // Right
		}

		private void DrawQuadrant(Graphics g, Point center, int rInner, int rOuter, float startAngle, float sweepAngle, bool active)
		{
			if (!active)
				return;
			using (var path = new System.Drawing.Drawing2D.GraphicsPath())
			{
				var outer = new Rectangle(center.X - rOuter, center.Y - rOuter, rOuter * 2, rOuter * 2);
				var inner = new Rectangle(center.X - rInner, center.Y - rInner, rInner * 2, rInner * 2);
				path.AddArc(outer, startAngle, sweepAngle);
				path.AddArc(inner, startAngle + sweepAngle, -sweepAngle);
				path.CloseFigure();
				using (var brush = new SolidBrush(HighlightColor))
				{
					g.FillPath(brush, path);
				}
			}
		}

		private void DrawLabel(Graphics g, Point center, int rOuter, int rInner, float angleDeg, string text)
		{
			if (string.IsNullOrEmpty(text))
				return;
			using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
			using (var brush = new SolidBrush(LabelColor))
			{
				// Place text inside the ring band using configurable factor
				double angleRad = angleDeg * Math.PI / 180.0;
				float factor = Math.Max(0f, Math.Min(1.0f, LabelRadiusFactor));
				int radiusForText = (int)(rInner + (rOuter - rInner) * factor);
				var pos = new Point(center.X + (int)(radiusForText * Math.Cos(angleRad)), center.Y + (int)(radiusForText * Math.Sin(angleRad)));
				var rect = new Rectangle(pos.X - 60, pos.Y - 16, 120, 32);
				g.DrawString(text, LabelFont, brush, rect, sf);
			}
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			// 对于触摸操作，确保控件能够接收事件
			Capture = true; // 捕获鼠标/触摸输入
			isMouseActive = true; // 标记开始鼠标操作
			UpdateActiveFromPoint(e.Location, true);
		}
		
		// 重写 WndProc 来处理触摸消息，确保触摸事件能正确触发
		protected override void WndProc(ref Message m)
		{
			// WM_LBUTTONDOWN (0x0201) - 鼠标左键按下（触摸也会触发此消息）
			// WM_MOUSEMOVE (0x0200) - 鼠标移动（触摸长按移动也会触发）
			// WM_LBUTTONUP (0x0202) - 鼠标左键释放（触摸释放也会触发）
			const int WM_LBUTTONDOWN = 0x0201;
			const int WM_MOUSEMOVE = 0x0200;
			const int WM_LBUTTONUP = 0x0202;
			
			// 对于触摸操作，Windows会自动转换为鼠标消息
			// 但第一次触摸时，控件可能没有焦点，导致事件没有被触发
			// 所以在这里直接处理，确保第一次触摸就能触发事件
			if (m.Msg == WM_LBUTTONDOWN)
			{
				// 获取点击位置（已经是客户端坐标）
				int x = (short)(m.LParam.ToInt32() & 0xFFFF);
				int y = (short)((m.LParam.ToInt32() >> 16) & 0xFFFF);
				Point clientPoint = new Point(x, y);
				
				// 如果点击在控件内，直接处理
				if (ClientRectangle.Contains(clientPoint))
				{
					// 确保控件能够接收事件
					if (!Focused && TabStop)
					{
						Focus();
					}
					
					// 直接调用处理逻辑，确保第一次触摸就能触发
					// 注意：这里可能会和OnMouseDown重复处理，但SetLeft等方法有检查，不会重复触发事件
					if (!isMouseActive)
					{
						isMouseActive = true;
						Capture = true;
						UpdateActiveFromPoint(clientPoint, true);
					}
					
					// 继续处理消息，让系统也触发OnMouseDown事件
					// 这样即使第一次触摸没有触发OnMouseDown，我们也能处理
				}
			}
			else if (m.Msg == WM_MOUSEMOVE && isMouseActive)
			{
				// 触摸长按移动时，持续更新状态
				// 检查是否有鼠标按钮按下（触摸时也会设置）
				int wParam = m.WParam.ToInt32();
				bool buttonPressed = (wParam & 0x0001) != 0; // MK_LBUTTON
				
				if (buttonPressed)
				{
					// 获取移动位置（已经是客户端坐标）
					int x = (short)(m.LParam.ToInt32() & 0xFFFF);
					int y = (short)((m.LParam.ToInt32() >> 16) & 0xFFFF);
					Point clientPoint = new Point(x, y);
					
					// 持续更新状态，支持触摸长按
					UpdateActiveFromPoint(clientPoint, true);
				}
			}
			else if (m.Msg == WM_LBUTTONUP && isMouseActive)
			{
				// 触摸释放时，清除状态
				isMouseActive = false;
				Capture = false;
				ClearActive();
			}
			
			base.WndProc(ref m);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (e.Button != MouseButtons.None)
			{
				isMouseActive = true; // 标记正在鼠标操作
				UpdateActiveFromPoint(e.Location, true);
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			Capture = false; // 释放鼠标/触摸捕获
			isMouseActive = false; // 标记结束鼠标操作
			ClearActive();
		}

		private void ClearActive()
		{
			SetUp(false);
			SetDown(false);
			SetLeft(false);
			SetRight(false);
			Invalidate();
		}

		private void UpdateActiveFromPoint(Point p, bool exclusive)
		{
			// var rect = ClientRectangle;
			// var center = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
			// double dx = p.X - center.X;
			// double dy = p.Y - center.Y;
			// double distance = Math.Sqrt(dx * dx + dy * dy);
			// double angle = (Math.Atan2(dy, dx) * 180.0 / Math.PI + 360.0) % 360.0; // 0=right, 90=down

			// int rOuter = Math.Min(rect.Width, rect.Height) / 2;
			// int rInner = Math.Max(0, rOuter - Math.Max(6, RingThickness));
			// bool inRing = distance >= rInner && distance <= rOuter;

			// if (exclusive)
			// {
			// 	SetUp(false); SetDown(false); SetLeft(false); SetRight(false);
			// }

			// if (inRing)
			// {
			// 	// Use mutually exclusive half-open sectors to avoid boundary overlaps
			// 	bool right = false, down = false, left = false, up = false;
			// 	if (AngleInSector(angle, 315, 45)) right = true; // [315, 360) U [0, 45]
			// 	else if (AngleInSector(angle, 45, 135)) down = true; // (45, 135]
			// 	else if (AngleInSector(angle, 135, 225)) left = true; // (135, 225]
			// 	else up = true; // remaining sector (225, 315]

			// 	SetUp(up);
			// 	SetRight(right);
			// 	SetDown(down);
			// 	SetLeft(left);
			// }

			// Invalidate();
			{
				var rect = ClientRectangle;
				var center = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
				double dx = p.X - center.X;
				double dy = p.Y - center.Y;
				double distance = Math.Sqrt(dx * dx + dy * dy);
				double angle = (Math.Atan2(dy, dx) * 180.0 / Math.PI + 360.0) % 360.0; // 0=right, 90=down

				// 移除圆环限制，允许整个圆形区域内点击
				int maxRadius = Math.Min(rect.Width, rect.Height) / 2;
				bool inCircle = distance <= maxRadius; // 改为圆形区域判断

				if (exclusive)
				{
					SetUp(false); SetDown(false); SetLeft(false); SetRight(false);
				}

				if (inCircle) // 在圆形区域内进行扇形判断
				{
					bool right = false, down = false, left = false, up = false;
					if (AngleInSector(angle, 315, 45)) right = true;
					else if (AngleInSector(angle, 45, 135)) down = true;
					else if (AngleInSector(angle, 135, 225)) left = true;
					else up = true;

					SetUp(up);
					SetRight(right);
					SetDown(down);
					SetLeft(left);
				}

				Invalidate();
			}
		}

		private bool AngleInSector(double angleDeg, double startDeg, double endDeg)
		{
			// sector wraps clockwise; handles wrap-around at 0/360
			if (startDeg <= endDeg)
				return angleDeg >= startDeg && angleDeg <= endDeg;
			return angleDeg >= startDeg || angleDeg <= endDeg;
		}

		// 根据方向计算对应的点击位置，用于模拟鼠标点击
		private Point GetPointForDirection(string direction)
		{
			var rect = ClientRectangle;
			var center = new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
			int maxRadius = Math.Min(rect.Width, rect.Height) / 2;
			// 使用半径的70%作为点击位置，确保在圆形区域内
			int clickRadius = (int)(maxRadius * 0.7);
			
			double angleRad = 0;
			switch (direction.ToLower())
			{
				case "up":
					angleRad = 270 * Math.PI / 180.0; // 270度 = 上
					break;
				case "down":
					angleRad = 90 * Math.PI / 180.0;  // 90度 = 下
					break;
				case "left":
					angleRad = 180 * Math.PI / 180.0; // 180度 = 左
					break;
				case "right":
					angleRad = 0 * Math.PI / 180.0;   // 0度 = 右
					break;
			}
			
			int x = center.X + (int)(clickRadius * Math.Cos(angleRad));
			int y = center.Y + (int)(clickRadius * Math.Sin(angleRad));
			return new Point(x, y);
		}

		// 根据按键触发对应的鼠标点击效果
		// 直接设置对应方向的状态，模拟鼠标点击的效果
		private void TriggerMouseClickForDirection(string direction)
		{
			// 直接设置对应方向的状态，这样可以支持多键同时按下
			switch (direction.ToLower())
			{
				case "up":
					SetUp(true);
					break;
				case "down":
					SetDown(true);
					break;
				case "left":
					SetLeft(true);
					break;
				case "right":
					SetRight(true);
					break;
			}
			Invalidate();
		}

		private void SetUp(bool v) { if (UpActive == v) return; UpActive = v; UpChanged?.Invoke(this, v); Invalidate(); }
		private void SetDown(bool v) { if (DownActive == v) return; DownActive = v; DownChanged?.Invoke(this, v); Invalidate(); }
		private void SetLeft(bool v) { if (LeftActive == v) return; LeftActive = v; LeftChanged?.Invoke(this, v); Invalidate(); }
		private void SetRight(bool v) { if (RightActive == v) return; RightActive = v; RightChanged?.Invoke(this, v); Invalidate(); }
	}
}


