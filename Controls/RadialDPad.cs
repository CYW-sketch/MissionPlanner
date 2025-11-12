using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

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

		[Browsable(false)] public bool UpActive { get; private set; }
		[Browsable(false)] public bool DownActive { get; private set; }
		[Browsable(false)] public bool LeftActive { get; private set; }
		[Browsable(false)] public bool RightActive { get; private set; }

		public event EventHandler<bool> UpChanged;
		public event EventHandler<bool> DownChanged;
		public event EventHandler<bool> LeftChanged;
		public event EventHandler<bool> RightChanged;

		public RadialDPad()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
			Size = new Size(120, 120);
			BackColor = Color.Transparent; // allowed after enabling SupportsTransparentBackColor
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
			UpdateActiveFromPoint(e.Location, true);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (e.Button != MouseButtons.None)
				UpdateActiveFromPoint(e.Location, true);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
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

		private void SetUp(bool v) { if (UpActive == v) return; UpActive = v; UpChanged?.Invoke(this, v); }
		private void SetDown(bool v) { if (DownActive == v) return; DownActive = v; DownChanged?.Invoke(this, v); }
		private void SetLeft(bool v) { if (LeftActive == v) return; LeftActive = v; LeftChanged?.Invoke(this, v); }
		private void SetRight(bool v) { if (RightActive == v) return; RightActive = v; RightChanged?.Invoke(this, v); }
	}
}


