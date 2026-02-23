using System.Numerics;

namespace _3D_Rendering_Engine
{
	public partial class Form1 : Form
	{
		float focalLength = 200f;

		List<(Vector3, Vector3, Vector3)> Triangles = new List<(Vector3, Vector3, Vector3)>();

		Vector2 ProjectPoints(Vector3 point)
		{
			int ScreenX = (int)(this.Width / 2 + (point.X / point.Z) * focalLength);
			int ScreenY = (int)(this.Height / 2 - (point.Y / point.Z) * focalLength);

			return new Vector2(ScreenX, ScreenY);
		}

		public Form1()
		{
			InitializeComponent();
			
			Triangles.Add(new Vector3(1, 1, 1), new Vector3(1, 1, 1) new Vector3(1, 1, 1))
		}

		private void Form1_Paint(object sender, PaintEventArgs e)
		{
			foreach (var triangle in Triangles) 
			{
				Vector2 p1 = ProjectPoints(triangle.Item1);
				Vector2 p2 = ProjectPoints(triangle.Item2);
				Vector2 p3 = ProjectPoints(triangle.Item3);

				PointF[] TrianglePoints = new PointF[3]
				{
					new PointF(p1.X, p1.Y),
					new PointF(p2.X, p2.Y),
					new PointF(p3.X, p3.Y),
				};

				e.Graphics.DrawPolygon(new Pen(Color.Red), TrianglePoints);
			}
		}
	}
}
