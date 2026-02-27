using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace _3D_Rendering_Engine
{

	public class Mesh
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

		public List<Mesh> SceneMeshes = new List<Mesh>();

		Vector2 ProjectPoints(Vector3 point)
		{
			int ScreenX = (int)(this.Width / 2 + (point.X / point.Z) * focalLength);
			int ScreenY = (int)(this.Height / 2 - (point.Y / point.Z) * focalLength);

			return new Vector2(ScreenX, ScreenY);
		}

		public Form1()
		{
			InitializeComponent();

			SceneMeshes.Add(ExtractMeshFromOBJ("car.obj", new Bitmap("CarTexture.png")));
			
		}

		public Mesh ExtractMeshFromOBJ(string ObjPath, Bitmap Texture)
		{
			Mesh mesh = new Mesh();

			BitmapData textureData = Texture.LockBits(new Rectangle(0, 0, Texture.Width, Texture.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

			mesh.texture = new Byte[textureData.Stride * Texture.Height];

			Marshal.Copy(textureData.Scan0, mesh.texture, 0, mesh.texture.Length);

			mesh.TextureStride = textureData.Stride;
			mesh.TextureHeight = Texture.Height;
			mesh.TextureWidth = Texture.Width;

			Texture.UnlockBits(textureData);
			Texture.Dispose();

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
					string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

					uvs.Add(new Vector2(
						float.Parse(parts[1]),
						1 - float.Parse(parts[2])
						));

				} else if(line.StartsWith("f "))
				{
					string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

					if(parts.Length == 4)
					{
						int[] vIndex = new int[3];
						int[] uvIndex = new int[3];

						for(int i = 0; i < 3; i++)
						{
							string[] Indexes = parts[i + 1].Split('/');

							vIndex[i] = int.Parse(Indexes[0]) - 1;
							uvIndex[i] = int.Parse(Indexes[1]) - 1;

						}

						mesh.Triangles.Add((vertices[vIndex[0]], vertices[vIndex[1]], vertices[vIndex[2]], uvs[uvIndex[0]], uvs[uvIndex[1]], uvs[uvIndex[2]]));

					}
				}
			}

			return mesh;
		}

		private void Form1_Paint(object sender, PaintEventArgs e)
		{
			foreach(var mesh in SceneMeshes) {
				foreach (var triangle in mesh.Triangles) 
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

					e.Graphics.FillPolygon(new SolidBrush(Color.Red), TrianglePoints);
				}
			}
		}
	}
}
