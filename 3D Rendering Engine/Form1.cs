using System.Numerics;

namespace _3D_Rendering_Engine
{
	public partial class Form1 : Form
	{
		float focalLength = 200f;

		List<Vector3> points = new List<Vector3>();

		public Form1()
		{
			InitializeComponent();
		}

		private void Form1_Paint(object sender, PaintEventArgs e)
		{
			foreach (var point in points) 
			{
				int ScreenX = (int)(this.Width / 2 + (point.X / point.Z) * focalLength);
				int ScreenY = (int)(this.Height / 2 - (point.Y / point.Z) * focalLength);

				e.Graphics.DrawRectangle(new Pen(Color.Red), ScreenX, ScreenY, 2, 2);
			}
		}
	}
}
