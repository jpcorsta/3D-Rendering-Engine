using System.Numerics;

namespace _3D_Rendering_Engine
{
	public partial class Form1 : Form
	{
		float focalLength = 200f;

		List<(Vector3, Vector3, Vector3)> points = new List<(Vector3, Vector3, Vector3)>();

		Vector2 ProjectPoints(Vector3 point)
		{
			int ScreenX = (int)(this.Width / 2 + (point.X / point.Z) * focalLength);
			int ScreenY = (int)(this.Height / 2 - (point.Y / point.Z) * focalLength);

			return new Vector2(ScreenX, ScreenY);
		}

		public Form1()
		{
			InitializeComponent();

			// front face
			points.Add(new Vector3(-1, -1, 2));
			points.Add(new Vector3(1, -1, 2));
			points.Add(new Vector3(-1, 1, 2));
			points.Add(new Vector3(1, 1, 2));

			// front face
			points.Add(new Vector3(-1, -1, 4));
			points.Add(new Vector3(1, -1, 4));
			points.Add(new Vector3(-1, 1, 4));
			points.Add(new Vector3(1, 1, 4));


		}

		private void Form1_Paint(object sender, PaintEventArgs e)
		{
			foreach (var triangle in points) 
			{
				Vector2 p1 = ProjectPoints(triangle.Item1);

				e.Graphics.DrawRectangle(new Pen(Color.Red), ScreenX, ScreenY, 2, 2);
			}
		}
	}
}
