using System.Numerics;
using System.Runtime.CompilerServices;

namespace _3D_Rendering_Engine
{

	class Mesh
	{
		public List<(Vector3, Vector3, Vector3, Vector2, Vector2, Vector2)> Triangles = new List<(Vector3, Vector3, Vector3, Vector2, Vector2, Vector2)>();
		public Vector3 location;
		public Vector3 rotation;

		public byte[] texture;
		public int TextureWidth;
		public int TextureHeight;
		public int TextureStride;

	}

	public partial class Form1 : Form
	{
		float focalLength = 200f;



		Vector2 ProjectPoints(Vector3 point)
		{
			int ScreenX = (int)(this.Width / 2 + (point.X / point.Z) * focalLength);
			int ScreenY = (int)(this.Height / 2 - (point.Y / point.Z) * focalLength);

			return new Vector2(ScreenX, ScreenY);
		}

		public Form1()
		{
			InitializeComponent();
			
			Triangles.Add((new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(1, 1, 1)));
		}

		public Mesh(string ObjPath, Bitmap Texture)
		{
			Mesh mesh = new Mesh();

			List<Vector3> vertices = new List<Vector3>();
			List<Vector2> uvs = new List<Vector2>();

			foreach (string line in File.ReadLines(ObjPath)) 
			{
				if(line.StartsWith("v "))
				{
					string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

					vertices.Add(new Vector3(
						float.Parse(parts[1]), 
						float.Parse(parts[2]), 
						float.Parse(parts[3])
						));

				} else if(line.StartsWith("vt "))
				{
					string[] parts = line.Split('',)
				} else if(line.StartsWith("f "))
				{

				}
			}
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

				e.Graphics.FillPolygon(new SolidBrush(Color.RebeccaPurple), TrianglePoints);
			}
		}
	}
}
