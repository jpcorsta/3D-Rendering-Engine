using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace _3D_Rendering_Engine
{

	public class Mesh
	{
		public List<(Vector3, Vector3, Vector3, Vector2, Vector2, Vector2, bool, Mesh)> Triangles = new List<(Vector3, Vector3, Vector3, Vector2, Vector2, Vector2, bool, Mesh)>();
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

		public int ScreenWidth;
		public int ScreenHeight;

		public float Nearplane = 0.5f;

		public bool NearestNeigbor = true;

		public Vector3 CameraLocation = new Vector3(0, 0, 0);
		public Vector3 CameraRotation = new Vector3(0, 0, 0);

		Vector2 ProjectPoints(Vector3 point)
		{
			int ScreenX = (int)(this.Width / 2 + (point.X / point.Z) * focalLength);
			int ScreenY = (int)(this.Height / 2 - (point.Y / point.Z) * focalLength);

			return new Vector2(ScreenX, ScreenY);
		}

		Vector3 ApplyTransformations(Vector3 vertex, Vector3 location, Vector3 rotation)
		{
			//Y axis
			float x1 = (float)(vertex.X * Math.Cos(rotation.Y) + vertex.Z * Math.Sin(rotation.Y));
			float z1 = (float)(-vertex.X * Math.Sin(rotation.Y) + vertex.Z * Math.Cos(rotation.Y));

			//X axis
			float y1 = (float)(vertex.Y * Math.Cos(rotation.X) - z1 * Math.Sin(rotation.X));
			float z2 = (float)(vertex.X * Math.Sin(rotation.Y) + z1 * Math.Cos(rotation.X));

			//Z axis
			float x2 = (float)(x1 * Math.Cos(rotation.Z) - y1 * Math.Sin(rotation.X));
			float y2 = (float)(x2 * Math.Sin(rotation.Z) - y1 * Math.Cos(rotation.X));

			return new Vector3(
				x2 + location.X,
				y2 + location.Y,
				z2 + location.Z
				);
		}

		Vector3 Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
		{
			float d = (b.Y - c.Y) * (a.X - c.X) + (c.X - b.X) * (a.Y - c.Y);

			float weight1 = ((b.Y - c.Y) * (p.X - c.X) + (c.X - b.X) * (p.Y - c.Y)) / d;
			float weight2 = ((c.Y - a.Y) * (p.X - c.X) + (a.X - c.X) * (p.Y - c.Y)) / d;
			float weight3 = 1 - weight1 - weight2;

			return new Vector3(weight1, weight2, weight3);	
		}

		(Vector3, Vector2) IntersectPlane(Vector3 InsideVertex, Vector3 OutsideVertex, Vector2 InsideUV, Vector2 OutsideUV, float nearplane)
		{
			float t = (nearplane - InsideVertex.Z) / (OutsideVertex.Z - InsideVertex.Z);

			Vector3 Position = InsideVertex + (OutsideVertex - InsideVertex) * t;
			Vector2 UV = InsideUV + (OutsideUV - InsideUV) * t;

			return(Position, UV);
		}

		public Form1()
		{
			InitializeComponent();

			SceneMeshes.Add(ExtractMeshFromOBJ("car.obj", new Bitmap("CarTexture.png")));

			ScreenWidth = this.Width;
			ScreenHeight = this.Height;
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

						mesh.Triangles.Add((vertices[vIndex[0]], vertices[vIndex[1]], vertices[vIndex[2]], uvs[uvIndex[0]], uvs[uvIndex[1]], uvs[uvIndex[2]], false, mesh));

					}
				}
			}

			return mesh;
		}

		private void Form1_Paint(object sender, PaintEventArgs e)
		{
			Bitmap FrameBuffer = new Bitmap(ScreenWidth, ScreenHeight);

			byte[] PixelBuffer = new byte[FrameBuffer.Width * FrameBuffer.Height * 4];//4 for red, green, blue and alpha

			float[,] DepthBuffer = new float[FrameBuffer.Width, FrameBuffer.Height];

			object[,] DepthBufferLock = new object[FrameBuffer.Width, FrameBuffer.Height];

			for (int x = 0; x < FrameBuffer.Width; x++) 
			{
				for (int y = 0; y < FrameBuffer.Height; y++) 
				{
					DepthBuffer[x, y] = float.PositiveInfinity;// going to set each of these values to the biggest number it can be, so that when we go to draw the pixels any empty spaces will be just treated as
					DepthBufferLock[x, y] = new object();
				}
			}

			List<(Vector3, Vector3, Vector3, Vector2, Vector2, Vector2, bool, Mesh)> TempTriangles = new List<(Vector3, Vector3, Vector3, Vector2, Vector2, Vector2, bool, Mesh)>();

			foreach (var mesh in SceneMeshes) 
			{
				foreach (var triangle in mesh.Triangles)
				{
					TempTriangles.Add(triangle);
				}
			}

			for(int t = 0; t < TempTriangles.Count; t++) {

					Vector3 v1b = triangle.Item1;
					Vector3 v2b = TempTriangles[t].Item2;
					Vector3 v3b = TempTriangles[t].Item3;

					Vector2 p1 = ProjectPoints(v1b);
					Vector2 p2 = ProjectPoints(v2b);
					Vector2 p3 = ProjectPoints(v3b);


					if (!TempTriangles[t].Item7) 
					{
						Vector3 v1a = ApplyTransformations(TempTriangles[t].Item1, TempTriangles[t].Item8.location, TempTriangles[t].Item8.rotation);
						Vector3 v2a = ApplyTransformations(TempTriangles[t].Item2, TempTriangles[t].Item8.location, TempTriangles[t].Item8.rotation);
						Vector3 v3a = ApplyTransformations(TempTriangles[t].Item3, TempTriangles[t].Item8.location, TempTriangles[t].Item8.rotation);

						v1b = ApplyTransformations(v1a, -CameraLocation, -CameraRotation);
						v2b = ApplyTransformations(v2a, -CameraLocation, -CameraRotation);
						v3b = ApplyTransformations(v3a, -CameraLocation, -CameraRotation);

						p1 = ProjectPoints(v1b);
						p2 = ProjectPoints(v2b);
						p3 = ProjectPoints(v3b);
					}

					if (p1.X <= 0 && p2.X <= 0 && p3.X <= 0 || p1.X >= ScreenWidth && p2.X >= ScreenWidth && p3.X >= ScreenWidth || p1.Y <= 0 && p2.Y <= 0 && p3.Y <= 0 || p1.Y >= ScreenHeight && p2.Y >= ScreenHeight && p3.Y >= ScreenHeight) { continue; }

					int VerticesBehindNearplane = 0;
					if(v1b.Z < Nearplane) { VerticesBehindNearplane++; }
					if(v2b.Z < Nearplane) { VerticesBehindNearplane++; }
					if(v3b.Z < Nearplane) { VerticesBehindNearplane++; }

					if(VerticesBehindNearplane == 3) { continue; }

					if(VerticesBehindNearplane == 2 || VerticesBehindNearplane == 1)
					{
						List<(Vector3, Vector2)> vertices = new List<(Vector3, Vector2)>() { (v1b, triangle.Item4), (v2b, triangle.Item5), (v3b, triangle.Item6) };           
						
						List<(Vector3 vertices, Vector2 uv)> InsideVertices = vertices.Where(v => v.Item1.Z >= Nearplane).ToList();
						List<(Vector3 vertices, Vector2 uv)> OutsideVertices = vertices.Where(v => v.Item1.Z < Nearplane).ToList();

						if(InsideVertices.Count == 1) 
						{
							(Vector3 Vertex, Vector2 uv) newVertex1 = IntersectPlane(InsideVertices[0].vertices, OutsideVertices[0].vertices, InsideVertices[0].uv, OutsideVertices[0].uv, Nearplane);
							(Vector3 Vertex, Vector2 uv) newVertex2 = IntersectPlane(InsideVertices[0].vertices, OutsideVertices[1].vertices, InsideVertices[0].uv, OutsideVertices[1].uv, Nearplane);

							mesh.Triangles.Add((InsideVertices[0].vertices, newVertex1.Vertex, newVertex2.Vertex, InsideVertices[0].uv, newVertex1.uv, newVertex2.uv, true));
						}
						if(InsideVertices.Count == 2)
						{
							(Vector3 Vertex, Vector2 uv) newVertex1 = IntersectPlane(InsideVertices[0].vertices, OutsideVertices[0].vertices, InsideVertices[0].uv, OutsideVertices[0].uv, Nearplane);
							(Vector3 Vertex, Vector2 uv) newVertex2 = IntersectPlane(InsideVertices[1].vertices, OutsideVertices[0].vertices, InsideVertices[1].uv, OutsideVertices[0].uv, Nearplane);
							
							mesh.Triangles.Add((InsideVertices[0].vertices, InsideVertices[1].vertices, newVertex1.Vertex, InsideVertices[0].uv, InsideVertices[1].uv, newVertex1.uv, true));
							mesh.Triangles.Add((InsideVertices[1].vertices, newVertex2.Vertex, newVertex1.Vertex, InsideVertices[1].uv, newVertex2.uv, newVertex1.uv, true));
						}

						continue;
					}

					if((p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y) < 0 && !triangle.Item7) { continue; }

					DrawTextureTriangle(e.Graphics, p1, p2, p3, triangle.Item4, triangle.Item5, triangle.Item6, v1b.Z, v2b.Z, v3b.Z, DepthBuffer, DepthBufferLock, mesh.texture, mesh.TextureWidth, mesh.TextureHeight, mesh.TextureStride, PixelBuffer);
				}
			}

			BitmapData FrameBufferData = FrameBuffer.LockBits(new Rectangle(0, 0, FrameBuffer.Width, FrameBuffer.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			Marshal.Copy(PixelBuffer, 0, FrameBufferData.Scan0, PixelBuffer.Length);

			FrameBuffer.UnlockBits(FrameBufferData); 

			if (NearestNeigbor)
			{
				e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			}
			
			e.Graphics.DrawImage(FrameBuffer, new Rectangle(0, 0, this.Width, this.Height), new Rectangle(0, 0, FrameBuffer.Width, FrameBuffer.Height), GraphicsUnit.Pixel);

			FrameBuffer.Dispose();
		}

		void DrawTextureTriangle(Graphics g, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 uv0, Vector2 uv1, Vector2 uv2, float z0, float z1, float z2, float[,] zBuffer, object[,] zBufferLock, byte[] texture, int textureWidth, int TextureHeight, int TextureStride, byte[] pixelBuffer)
		{
			int MinX = (int)(Math.Floor(Math.Min(p0.X, Math.Min(p1.X, p2.X))));
			int MaxX = (int)(Math.Ceiling(Math.Max(p0.X, Math.Max(p1.X, p2.X))));
			int MinY = (int)(Math.Floor(Math.Min(p0.Y, Math.Min(p1.Y, p2.Y))));
			int MaxY = (int)(Math.Ceiling(Math.Max(p0.Y, Math.Max(p1.Y, p2.Y))));

			for (int y = MinY; y <= MaxY; y++) 
			{
				if (y >= 0 && y < ScreenHeight) 
				{
					for (int x = MinX; x <= MaxX; x++)
					{
						if (x >= 0 && x < ScreenWidth)
						{
							Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

							Vector3 Bary = Barycentric(p, p0, p1, p2);

							if (Bary.X >= 0 && Bary.Y >= 0 && Bary.Z >= 0) 
							{

								float iz0 = 1 / z0;
								float iz1 = 1 / z1;
								float iz2 = 1 / z2;

								float InterpolatedInvZ = Bary.X * iz0 + Bary.Y * iz1 + Bary.Z * iz2;
								float InterpolatedZ = 1 / InterpolatedInvZ;

								lock (zBufferLock[x, y])
								{
									if(InterpolatedZ < zBuffer[x, y])
									{
										float u0z = uv0.X * iz0;
										float u1z = uv1.X * iz1;
										float u2z = uv2.X * iz2;

										float v0z = uv0.Y * iz0;
										float v1z = uv1.Y * iz1;
										float v2z = uv2.Y * iz2;

										float InterpolatedUZ = Bary.X * u0z + Bary.Y * u1z + Bary.Z * u2z;
										float InterpolatedVZ = Bary.X * v0z + Bary.Y * v1z + Bary.Z * v2z;

										float u = InterpolatedUZ / InterpolatedInvZ;
										float v = InterpolatedVZ / InterpolatedInvZ;

										int TexX = Math.Clamp((int)(u * textureWidth), 0, textureWidth - 1);
										int TexY = Math.Clamp((int)(v * TextureHeight), 0, TextureHeight - 1);

										Color BaseColor = Color.FromArgb(
											texture[(TexY * TextureStride + TexX * 4) + 3],
											texture[(TexY * TextureStride + TexX * 4) + 2],
											texture[(TexY * TextureStride + TexX * 4) + 1],
											texture[(TexY * TextureStride + TexX * 4) + 0]
										);

										if(BaseColor.A != 0)
										{
											int index = (y * ScreenWidth + x) * 4;
											pixelBuffer[index + 3] = BaseColor.A;
											pixelBuffer[index + 2] = BaseColor.R;
											pixelBuffer[index + 1] = BaseColor.G;
											pixelBuffer[index + 0] = BaseColor.B;

											zBuffer[x, y] = InterpolatedZ; 
										}

									}
								}
							}
						}
					}
				}
			}
		}
	}
}
