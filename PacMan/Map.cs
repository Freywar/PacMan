using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

namespace PacMan
{
	class Map : GameObject
	{
		/// <summary>
		/// Possible map objects.
		/// </summary>
		public enum Objects
		{
			/// <summary>
			/// Wall.
			/// </summary>
			Wall,
			/// <summary>
			/// Point.
			/// </summary>
			Point,
			/// <summary>
			/// Powerup.
			/// </summary>
			Powerup,
			LiftUp,
			LiftDown,
			/// <summary>
			/// Empty field.
			/// </summary>
			None,
			/// <summary>
			/// Cell which PacMan or ghost can't reach (except walls). Has no floor.
			/// </summary>
			Empty
		}

		/// <summary>
		/// Details count per 360 degrees or 1 map cell.
		/// </summary>
		private const int detailsCount = 20;
		private static Color wallColor = Color.DarkBlue;

		private Mesh sphere_v = null;
		private Mesh wallCenter_v = null;
		private Mesh wallSide_v = null;
		private Mesh wallclosedCorner_v = null;
		private Mesh wallOpenCorner_v = null;
		private Mesh floor_v = null;
		private Mesh floorHole_v = null;
		private Mesh lift_v = null;
		private ShaderProgram liftProgram_v = null;


		/// <summary>
		/// Sphere mesh with radius equal to cell size.
		/// </summary>
		private Mesh sphere
		{
			get
			{
				if (sphere_v == null)
				{
					double step = Math.PI * 2.0 / detailsCount;
					int pointsCount = (int)(Math.PI / step) * (int)(Math.PI * 2 / step) * 4;
					int vp = 0;
					int np = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];

					for (double alpha = -Math.PI / 2; alpha < Math.PI / 2; alpha += step)
						for (double beta = 0; beta < Math.PI * 2; beta += step)
						{
							GL.Color3(Color.White);

							Vector3d normal = Utils.FromSpheric(alpha, beta, 1);
							Utils.Push(n, normal, ref np);
							Utils.Push(v, normal, ref vp);

							normal = Utils.FromSpheric(alpha + step, beta, 1);
							Utils.Push(n, normal, ref np);
							Utils.Push(v, normal, ref vp);

							normal = Utils.FromSpheric(alpha + step, beta + step, 1);
							Utils.Push(n, normal, ref np);
							Utils.Push(v, normal, ref vp);

							normal = Utils.FromSpheric(alpha, beta + step, 1);
							Utils.Push(n, normal, ref np);
							Utils.Push(v, normal, ref vp);
						}

					sphere_v = new Mesh();
					sphere_v.Vertex = v;
					sphere_v.Normal = n;
				}

				return sphere_v;
			}
		}

		private Mesh floor
		{
			get
			{
				if (floor_v == null)
				{
					Vector3d normal = new Vector3d(0, 1, 0);

					int pointsCount = 4;
					int vp = 0;
					int np = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];

					Utils.Push(v, new Vector3d(-0.5, -0.5, -0.5), ref vp);
					Utils.Push(v, new Vector3d(-0.5, -0.5, 0.5), ref vp);
					Utils.Push(v, new Vector3d(0.5, -0.5, 0.5), ref vp);
					Utils.Push(v, new Vector3d(0.5, -0.5, -0.5), ref vp);

					for (int i = 0; i < 4; i++)
						Utils.Push(n, normal, ref np);

					floor_v = new Mesh();
					floor_v.Vertex = v;
					floor_v.Normal = n;
				}

				return floor_v;
			}
		}

		private Mesh floorHole
		{
			get
			{
				if (floorHole_v == null)
				{
					double step = Math.PI * 2 / detailsCount;

					int pointsCount = (int)(Math.PI / 2 / step) * 4 * 4;
					int vp = 0;
					int np = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];


					Vector3d normal = new Vector3d(0, 1, 0);


					for (double alpha = 0; alpha < Math.PI / 2; alpha += step)
					{
						Utils.Push(v, new Vector3d(-0.5 + 0, -0.5, -0.5 + 0), ref vp);
						Utils.Push(v, new Vector3d(-0.5 + 0.5 - 0.5 * Math.Sin(alpha + step), -0.5, -0.5 + 0.5 - 0.5 * Math.Cos(alpha + step)), ref vp);
						Utils.Push(v, new Vector3d(-0.5 + 0.5 - 0.5 * Math.Sin(alpha), -0.5, -0.5 + 0.5 - 0.5 * Math.Cos(alpha)), ref vp);
						Utils.Push(v, new Vector3d(-0.5 + 0, -0.5, -0.5 + 0), ref vp);

						Utils.Push(v, new Vector3d(0.5 + 0, -0.5, -0.5 + 0), ref vp);
						Utils.Push(v, new Vector3d(0.5 - 0.5 + 0.5 * Math.Sin(alpha), -0.5, -0.5 + 0.5 - 0.5 * Math.Cos(alpha)), ref vp);
						Utils.Push(v, new Vector3d(0.5 - 0.5 + 0.5 * Math.Sin(alpha + step), -0.5, -0.5 + 0.5 - 0.5 * Math.Cos(alpha + step)), ref vp);
						Utils.Push(v, new Vector3d(0.5 + 0, -0.5, -0.5 + 0), ref vp);

						Utils.Push(v, new Vector3d(0.5 + 0, -0.5, 0.5 + 0), ref vp);
						Utils.Push(v, new Vector3d(0.5 - 0.5 + 0.5 * Math.Sin(alpha + step), -0.5, 0.5 - 0.5 + 0.5 * Math.Cos(alpha + step)), ref vp);
						Utils.Push(v, new Vector3d(0.5 - 0.5 + 0.5 * Math.Sin(alpha), -0.5, 0.5 - 0.5 + 0.5 * Math.Cos(alpha)), ref vp);
						Utils.Push(v, new Vector3d(0.5 + 0, -0.5, 0.5 + 0), ref vp);

						Utils.Push(v, new Vector3d(-0.5 + 0, -0.5, 0.5 + 0), ref vp);
						Utils.Push(v, new Vector3d(-0.5 + 0.5 - 0.5 * Math.Sin(alpha), -0.5, 0.5 - 0.5 + 0.5 * Math.Cos(alpha)), ref vp);
						Utils.Push(v, new Vector3d(-0.5 + 0.5 - 0.5 * Math.Sin(alpha + step), -0.5, 0.5 - 0.5 + 0.5 * Math.Cos(alpha + step)), ref vp);
						Utils.Push(v, new Vector3d(-0.5 + 0, -0.5, 0.5 + 0), ref vp);
						for (int i = 0; i < 4 * 4; i++)
							Utils.Push(n, normal, ref np);

					}



					floorHole_v = new Mesh();
					floorHole_v.Vertex = v;
					floorHole_v.Normal = n;
				}

				return floorHole_v;
			}
		}


		private Mesh lift
		{
			get
			{
				if (lift_v == null)
				{
					double step = Math.PI / 10;
					double yStep = 1.0 / 100;

					int pointsCount = (int)(Math.PI / 2 / step) * 4 * 4 * 2 * (int)(1.0 / yStep) * 3;
					int vp = 0;
					int np = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];

					Vector3d normal = new Vector3d(0, 1, 0);

					for (double c = 0, r = 0.5 - 0.1 - 0.1; c < 3; c += 1.0, r += 0.1)
					{
						for (double y = -0.5; y < 0.5; y += yStep)
							for (double alpha = 0; alpha < Math.PI * 2; alpha += step)
							{
								normal = new Vector3d(Math.Sin(alpha), 0, Math.Cos(alpha));
								Utils.Push(n, normal, ref np);
								normal = Vector3d.Multiply(normal, r); ;
								normal.Y = y;
								Utils.Push(v, normal, ref vp);

								normal = new Vector3d(Math.Sin(alpha + step), 0, Math.Cos(alpha + step));
								Utils.Push(n, normal, ref np);
								normal = Vector3d.Multiply(normal, r); ;
								normal.Y = y;
								Utils.Push(v, normal, ref vp);

								normal = new Vector3d(Math.Sin(alpha + step), 0, Math.Cos(alpha + step));
								Utils.Push(n, normal, ref np);
								normal = Vector3d.Multiply(normal, r); ;
								normal.Y = y + yStep;
								Utils.Push(v, normal, ref vp);

								normal = new Vector3d(Math.Sin(alpha), 0, Math.Cos(alpha));
								Utils.Push(n, normal, ref np);
								normal = Vector3d.Multiply(normal, r); ;
								normal.Y = y + yStep;
								Utils.Push(v, normal, ref vp);
							}

						for (double y = -0.5; y < 0.5; y += yStep)
							for (double alpha = Math.PI * 2; alpha > 0; alpha -= step)
							{
								normal = new Vector3d(-Math.Sin(alpha), 0, -Math.Cos(alpha));
								Utils.Push(n, normal, ref np);
								normal = Vector3d.Multiply(normal, -r); ;
								normal.Y = y;
								Utils.Push(v, normal, ref vp);

								normal = new Vector3d(-Math.Sin(alpha - step), 0, -Math.Cos(alpha - step));
								Utils.Push(n, normal, ref np);
								normal = Vector3d.Multiply(normal, -r); ;
								normal.Y = y;
								Utils.Push(v, normal, ref vp);

								normal = new Vector3d(-Math.Sin(alpha - step), 0, -Math.Cos(alpha - step));
								Utils.Push(n, normal, ref np);
								normal = Vector3d.Multiply(normal, -r); ;
								normal.Y = y + yStep;
								Utils.Push(v, normal, ref vp);

								normal = new Vector3d(-Math.Sin(alpha), 0, -Math.Cos(alpha));
								Utils.Push(n, normal, ref np);
								normal = Vector3d.Multiply(normal, -r); ;
								normal.Y = y + yStep;
								Utils.Push(v, normal, ref vp);
							}
					}
					lift_v = new Mesh();
					lift_v.Vertex = v;
					lift_v.Normal = n;
				}

				return lift_v;
			}
		}

		#region Wall meshes

		private Mesh wallCenter
		{
			get
			{
				if (wallCenter_v == null)
				{
					double ps2 = 1.0 / 6.0;
					Vector3d normal = new Vector3d(0, 1, 0);

					int pointsCount = 4;
					int vp = 0;
					int np = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];

					Utils.Push(v, new Vector3d(-ps2, 0.5, -ps2), ref vp);
					Utils.Push(v, new Vector3d(-ps2, 0.5, ps2), ref vp);
					Utils.Push(v, new Vector3d(ps2, 0.5, ps2), ref vp);
					Utils.Push(v, new Vector3d(ps2, 0.5, -ps2), ref vp);

					for (int i = 0; i < 4; i++)
						Utils.Push(n, normal, ref np);

					wallCenter_v = new Mesh();
					wallCenter_v.Vertex = v;
					wallCenter_v.Normal = n;
				}

				return wallCenter_v;
			}
		}

		private Mesh wallSide
		{
			get
			{
				if (wallSide_v == null)
				{
					double ps2 = 1.0 / 6.0;
					Vector3d color = new Vector3d(
					wallColor.R / 255.0, wallColor.G / 255.0, wallColor.B / 255.0);

					int pointsCount = 8;
					int vp = 0;
					int np = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];

					Utils.Push(v, new Vector3d(-ps2, 0.5, -ps2), ref vp);
					Utils.Push(v, new Vector3d(-ps2, 0.5, ps2), ref vp);
					Utils.Push(v, new Vector3d(0, 0.5, ps2), ref vp);
					Utils.Push(v, new Vector3d(0, 0.5, -ps2), ref vp);
					Utils.Push(v, new Vector3d(0, -0.5, -ps2), ref vp);
					Utils.Push(v, new Vector3d(0, 0.5, -ps2), ref vp);
					Utils.Push(v, new Vector3d(0, 0.5, ps2), ref vp);
					Utils.Push(v, new Vector3d(0, -0.5, ps2), ref vp);

					Vector3d normal = new Vector3d(0, 1, 0);
					for (int i = 0; i < 4; i++)
						Utils.Push(n, normal, ref np);

					normal = new Vector3d(1, 0, 0);
					for (int i = 4; i < 8; i++)
						Utils.Push(n, normal, ref np);

					wallSide_v = new Mesh();
					wallSide_v.Vertex = v;
					wallSide_v.Normal = n;
				}

				return wallSide_v;
			}
		}

		private Mesh wallclosedCorner
		{
			get
			{
				if (wallclosedCorner_v == null)
				{
					double ps2 = 1.0 / 6.0;
					double step = Math.PI * 2 / detailsCount;

					int pointsCount = 12 + (int)(Math.PI / 2 / step) * 4 + (int)(Math.PI / 2 / step) * 4;
					int vp = 0;
					int np = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];

					Utils.Push(v, new Vector3d(-ps2, 0.5, -ps2), ref vp);
					Utils.Push(v, new Vector3d(-ps2, 0.5, 0), ref vp);
					Utils.Push(v, new Vector3d(0, 0.5, 0), ref vp);
					Utils.Push(v, new Vector3d(0, 0.5, -ps2), ref vp);

					Utils.Push(v, new Vector3d(0, 0.5, -ps2), ref vp);
					Utils.Push(v, new Vector3d(0, 0.5, 0), ref vp);
					Utils.Push(v, new Vector3d(ps2, 0.5, 0), ref vp);
					Utils.Push(v, new Vector3d(ps2, 0.5, -ps2), ref vp);

					Utils.Push(v, new Vector3d(-ps2, 0.5, 0), ref vp);
					Utils.Push(v, new Vector3d(-ps2, 0.5, ps2), ref vp);
					Utils.Push(v, new Vector3d(0, 0.5, ps2), ref vp);
					Utils.Push(v, new Vector3d(0, 0.5, 0), ref vp);

					Vector3d normal = new Vector3d(0, 1, 0);
					for (int i = 0; i < 12; i++)
						Utils.Push(n, normal, ref np);

					for (double alpha = 0; alpha < Math.PI / 2; alpha += step)
					{
						Utils.Push(v, new Vector3d(0, 0.5, 0), ref vp);
						Utils.Push(v, new Vector3d(ps2 - ps2 * Math.Sin(alpha + step), 0.5,
							 ps2 - ps2 * Math.Cos(alpha + step)), ref vp);
						Utils.Push(v, new Vector3d(ps2 - ps2 * Math.Sin(alpha), 0.5,
							 ps2 - ps2 * Math.Cos(alpha)), ref vp);
						Utils.Push(v, new Vector3d(0, 0.5, 0), ref vp);
						for (int i = 0; i < 4; i++)
							Utils.Push(n, normal, ref np);

					}

					for (double alpha = 0; alpha < Math.PI / 2; alpha += step)
					{
						normal = new Vector3d(Math.Sin(alpha), 0, Math.Cos(alpha));
						Utils.Push(n, normal, ref np);
						normal = Vector3d.Multiply(normal, ps2);
						normal.X = ps2 - normal.X;
						normal.Y = -0.5;
						normal.Z = ps2 - normal.Z;
						Utils.Push(v, normal, ref vp);

						normal = new Vector3d(Math.Sin(alpha), 0, Math.Cos(alpha));
						Utils.Push(n, normal, ref np);
						normal = Vector3d.Multiply(normal, ps2);
						normal.X = ps2 - normal.X;
						normal.Y = 0.5;
						normal.Z = ps2 - normal.Z;
						Utils.Push(v, normal, ref vp);

						normal = new Vector3d(Math.Sin(alpha + step), 0, Math.Cos(alpha + step));
						Utils.Push(n, normal, ref np);
						normal = Vector3d.Multiply(normal, ps2);
						normal.X = ps2 - normal.X;
						normal.Y = 0.5;
						normal.Z = ps2 - normal.Z;
						Utils.Push(v, normal, ref vp);


						normal = new Vector3d(Math.Sin(alpha + step), 0, Math.Cos(alpha + step));
						Utils.Push(n, normal, ref np);
						normal = Vector3d.Multiply(normal, ps2);
						normal.X = ps2 - normal.X;
						normal.Y = -0.5;
						normal.Z = ps2 - normal.Z;
						Utils.Push(v, normal, ref vp);
					}

					wallclosedCorner_v = new Mesh();
					wallclosedCorner_v.Vertex = v;
					wallclosedCorner_v.Normal = n;
				}

				return wallclosedCorner_v;
			}
		}

		private Mesh wallOpenCorner
		{
			get
			{
				if (wallOpenCorner_v == null)
				{
					double ps2 = 1.0 / 6.0;
					double step = Math.PI / 10;

					int pointsCount = (int)(Math.PI / 2 / step) * 4 + (int)(Math.PI / 2 / step) * 4;
					int vp = 0;
					int np = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];

					Vector3d normal = new Vector3d(0, 1, 0);

					for (double alpha = 0; alpha < Math.PI / 2; alpha += step)
					{
						Utils.Push(v, new Vector3d(-ps2, 0.5, -ps2), ref vp);
						Utils.Push(v, new Vector3d(-ps2, 0.5, -ps2), ref vp);
						Utils.Push(v, new Vector3d(-ps2 + ps2 * Math.Sin(alpha), 0.5,
							 -ps2 + ps2 * Math.Cos(alpha)), ref vp);
						Utils.Push(v, new Vector3d(-ps2 + ps2 * Math.Sin(alpha + step), 0.5,
							 -ps2 + ps2 * Math.Cos(alpha + step)), ref vp);

						for (int i = 0; i < 4; i++)
							Utils.Push(n, normal, ref np);
					}

					for (double alpha = 0; alpha < Math.PI / 2; alpha += step)
					{
						normal = new Vector3d(Math.Sin(alpha), 0, Math.Cos(alpha));
						Utils.Push(n, normal, ref np);
						normal = Vector3d.Multiply(normal, ps2); ;
						normal.X = normal.X - ps2;
						normal.Y = -0.5;
						normal.Z = normal.Z - ps2;
						Utils.Push(v, normal, ref vp);

						normal = new Vector3d(Math.Sin(alpha + step), 0, Math.Cos(alpha + step));
						Utils.Push(n, normal, ref np);
						normal = Vector3d.Multiply(normal, ps2); ;
						normal.X = normal.X - ps2;
						normal.Y = -0.5;
						normal.Z = normal.Z - ps2;
						Utils.Push(v, normal, ref vp);

						normal = new Vector3d(Math.Sin(alpha + step), 0, Math.Cos(alpha + step));
						Utils.Push(n, normal, ref np);
						normal = Vector3d.Multiply(normal, ps2); ;
						normal.X = normal.X - ps2;
						normal.Y = 0.5;
						normal.Z = normal.Z - ps2;
						Utils.Push(v, normal, ref vp);

						normal = new Vector3d(Math.Sin(alpha), 0, Math.Cos(alpha));
						Utils.Push(n, normal, ref np);
						normal = Vector3d.Multiply(normal, ps2); ;
						normal.X = normal.X - ps2;
						normal.Y = 0.5;
						normal.Z = normal.Z - ps2;
						Utils.Push(v, normal, ref vp);
					}

					wallOpenCorner_v = new Mesh();
					wallOpenCorner_v.Vertex = v;
					wallOpenCorner_v.Normal = n;
				}

				return wallOpenCorner_v;
			}
		}

		#endregion

		private ShaderProgram liftProgram
		{
			get
			{
				if (liftProgram_v == null)
					liftProgram_v = new ShaderProgram("Shaders\\Lift_Vert.glsl", "Shaders\\Lift_Frag.glsl");
				return liftProgram_v;
			}
		}

		private void fillReachMap(bool[][][] reachMap, int x, int y, int z)
		{
			x = WrapX(x);
			z = WrapZ(z);

			if (!IsWalkable(y, z, x) || reachMap[y][z][x])
				return;

			reachMap[y][z][x] = true;

			if (Fields[y][z][x] == Objects.LiftUp)
				fillReachMap(reachMap, x, y + 1, z);
			if (Fields[y][z][x] == Objects.LiftDown)
				fillReachMap(reachMap, x, y - 1, z);

			fillReachMap(reachMap, x - 1, y, z);
			fillReachMap(reachMap, x + 1, y, z);
			fillReachMap(reachMap, x, y, z - 1);
			fillReachMap(reachMap, x, y, z + 1);
		}

		private void fillUnreachableCells()
		{

			bool[][][] reachMap = new bool[Height][][];
			for (int y = 0; y < Height; y++)
			{
				reachMap[y] = new bool[Depth][];
				for (int z = 0; z < Depth; z++)
				{
					reachMap[y][z] = new bool[Width];
					for (int x = 0; x < Width; x++)
						reachMap[y][z][x] = false;
				}
			}
			fillReachMap(reachMap, PacManStart.X, PacManStart.Y, PacManStart.Z);
			fillReachMap(reachMap, GhostStart.X, GhostStart.Y, GhostStart.Z);

			for (int y = 0; y < Height; y++)
				for (int z = 0; z < Depth; z++)
					for (int x = 0; x < Width; x++)
						if (!reachMap[y][z][x] && Fields[y][z][x] != Objects.Wall)
						{
							if (Fields[y][z][x] != Objects.None)
								throw new Exception("Invalid map data.");
							Fields[y][z][x] = OriginalFields[y][z][x] = Objects.Empty;
						}
		}

		private float totalTimeElapsed = 0.0f;


		/// <summary>
		/// Map name
		/// </summary>
		public string Name;
		/// <summary>
		/// Path to map data file
		/// </summary>
		public string Path;
		/// <summary>
		/// Width(left to right, cells).
		/// </summary>
		public int Width;
		/// <summary>
		/// Height(bottom to top, floors).
		/// </summary>
		public int Height;
		/// <summary>
		/// Depth(From farthest end to camera, cells).
		/// </summary>
		public int Depth;

		public Objects[][][] Fields;
		public Objects[][][] OriginalFields = null;
		/// <summary>
		/// Start point of PacMan
		/// </summary>
		public Vector3i PacManStart = new Vector3i(0, 0, 0);
		/// <summary>
		/// Start point of ghosts
		/// </summary>
		public Vector3i GhostStart = new Vector3i(0, 0, 0);

		/// <summary>
		/// Wrap X coordinate inside map borders.
		/// </summary>
		/// <param name="X">X coordinate.</param>
		/// <returns>X coordinate inside map borders.</returns>
		public double WrapX(double X)
		{
			if (X >= Width)
				return X - Width;
			if (X < 0)
				return X + Width;
			return X;
		}
		/// <summary>
		/// Wrap Z coordinate inside map borders.
		/// </summary>
		/// <param name="Z">Z coordinate.</param>
		/// <returns>Z coordinate inside map borders.</returns>
		public double WrapZ(double Z)
		{
			if (Z >= Depth)
				return Z - Depth;
			if (Z < 0)
				return Z + Depth;
			return Z;
		}

		/// <summary>
		/// Wrap X coordinate inside map borders.
		/// </summary>
		/// <param name="X">X coordinate.</param>
		/// <returns>X coordinate inside map borders.</returns>
		public int WrapX(int X)
		{
			if (X >= Width)
				return X - Width;
			if (X < 0)
				return X + Width;
			return X;
		}
		/// <summary>
		/// Wrap Z coordinate inside map borders.
		/// </summary>
		/// <param name="Z">Z coordinate.</param>
		/// <returns>Z coordinate inside map borders.</returns>
		public int WrapZ(int Z)
		{
			if (Z >= Depth)
				return Z - Depth;
			if (Z < 0)
				return Z + Depth;
			return Z;
		}

		/// <summary>
		/// Shortcut to map floor.
		/// </summary>
		/// <param name="z">Floor index.</param>
		/// <returns>Floor.</returns>
		public Objects[][] this[int y] { get { return Fields[y]; } }

		/// <summary>
		/// Shortcut to map cell.
		/// </summary>
		/// <param name="z">Row index.</param>
		/// <param name="x">Column index.</param>
		/// <returns>Cell.</returns>
		public Objects this[int y, int z, int x] { get { return Fields[y][WrapZ(z)][WrapX(x)]; } }

		/// <summary>
		/// Shortcut to map cell.
		/// </summary>
		/// <param name="z">Row index.</param>
		/// <param name="x">Column index.</param>
		/// <returns>Cell.</returns>
		public Objects this[double y, double z, double x] { get { return Fields[(int)y][(int)WrapZ(z)][(int)WrapX(x)]; } }

		/// <summary>
		/// Cell does not contains walls.
		/// </summary>
		/// <param name="z">Z coordinate, wrapping included.</param>
		/// <param name="x">X coordinate, wrapping included.</param>
		/// <returns>True, if cell is walkable.</returns>
		public bool IsWalkable(int y, int z, int x)
		{
			return this[y, z, x] != Objects.Wall;
		}
		/// <summary>
		/// Cell does not contains walls.
		/// </summary>
		/// <param name="z">Z coordinate, wrapping included.</param>
		/// <param name="x">X coordinate, wrapping included.</param>
		/// <returns>True, if cell is walkable.</returns>
		public bool IsWalkable(double y, double z, double x)
		{
			return this[y, z, x] != Objects.Wall;
		}

		/// <summary>
		/// Points left on map count
		/// </summary>
		public int PointsCount
		{
			get
			{
				int result = 0;
				for (int y = 0; y < Height; y++)
					for (int z = 0; z < Depth; z++)
						for (int x = 0; x < Width; x++)
							if (Fields[y][z][x] == Objects.Point || Fields[y][z][x] == Objects.Powerup)
								result++;
				return result;
			}
		}

		/// <summary>
		/// Map loading and initialization method
		/// </summary>
		override public void Init()
		{
			if (OriginalFields == null)
			{
				StringBuilder sb = new StringBuilder();
				using (StreamReader sr = new StreamReader(Path))
				{
					String line;
					while ((line = sr.ReadLine()) != null)
					{
						sb.AppendLine(line);
					}
				}
				string data = sb.ToString();
				data = Regex.Replace(data, "\\r", "");
				data = Regex.Replace(data, "//.*?\\n", "\n");
				data = Regex.Replace(data, "\\n*$", "");
				data = Regex.Replace(data, "^\\n*", "");


				string[] floors = data.Split(new string[] { "\n\n" }, StringSplitOptions.None);
				Height = floors.Length;
				Fields = new Objects[Height][][];
				OriginalFields = new Objects[Height][][];


				Vector3i? pacmanStart = null;
				Vector3i? ghostStart = null;

				for (int y = 0; y < Height; y++)
				{
					string[] rows = floors[y].Split('\n');
					Depth = rows.Length;
					Width = rows[0].Length;
					Fields[y] = new Objects[Depth][];
					OriginalFields[y] = new Objects[Depth][];

					for (int z = 0; z < rows.Length; z++)
					{
						if (rows[z].Length != Width)
							throw new Exception("Invalid map data");
						Fields[y][z] = new Objects[Width];
						OriginalFields[y][z] = new Objects[Width];
						for (int x = 0; x < rows[z].Length; x++)
						{
							if (y > 0 && Fields[y - 1][z][x] == Objects.LiftUp)
							{
								if (rows[z].Length != Width)
									throw new Exception("Invalid map data");
								Fields[y][z][x] = Objects.LiftDown;
							}
							else
							{
								switch (rows[z][x])
								{
									case '.':
										Fields[y][z][x] = OriginalFields[y][z][x] = Objects.Point;
										break;
									case 'O':
										Fields[y][z][x] = OriginalFields[y][z][x] = Objects.Powerup;
										break;
									case '#':
										Fields[y][z][x] = OriginalFields[y][z][x] = Objects.Wall;
										break;
									case 'C':
										pacmanStart = new Vector3i(x, y, z);
										Fields[y][z][x] = OriginalFields[y][z][x] = Objects.None;
										break;
									case 'M':
										ghostStart = new Vector3i(x, y, z);
										Fields[y][z][x] = OriginalFields[y][z][x] = Objects.None;
										break;
									case 'U':
										if (y > 0 && Fields[y][z][x] == Objects.LiftUp)
											throw new Exception("Invalid map data");
										Fields[y][z][x] = OriginalFields[y][z][x] = Objects.LiftUp;
										break;
									case '-':
										Fields[y][z][x] = OriginalFields[y][z][x] = Objects.None;
										break;

								}
							}
						}
					}
				}
				if (PointsCount == 0 || pacmanStart == null || ghostStart == null)
					throw new Exception("Invalid map data");
				PacManStart = (Vector3i)pacmanStart;
				GhostStart = (Vector3i)ghostStart;

				fillUnreachableCells();
			}
			else
			{
				for (int y = 0; y < Height; y++)
					for (int z = 0; z < Depth; z++)
						for (int x = 0; x < Width; x++)
							Fields[y][z][x] = OriginalFields[y][z][x];
			}
			Floor = PacManStart.Y;
			base.Init();
		}

		public override void Update(double dt)
		{
			totalTimeElapsed += (float)dt;
			base.Update(dt);
		}

		private double liftAnimatedY(double y, int upperFloorDelta = 100)
		{
			double ry = y;
			if (State == States.Normal || Animation == Animations.Appear || Animation == Animations.Disappear)
			{
				if (y > Floor)
					ry += upperFloorDelta;
			}
			if (Animation == Animations.LiftUp && y == Floor)
				ry += upperFloorDelta * (1 - animationProgress) * (1 - animationProgress) * (1 - animationProgress);
			if (Animation == Animations.LiftDown && y == Floor + 1)
				ry += -upperFloorDelta + upperFloorDelta * animationProgress * animationProgress * animationProgress;
			return ry;
		}

		/// <summary>
		/// Render method
		/// </summary>
		override public void Render()
		{
			ShaderProgram.StaticColor.Enable();

			#region walls
			ShaderProgram.StaticColor.SetUniform("meshColor",
				 new Vector4(wallColor.R / (float)255.0, wallColor.G / (float)255.0, wallColor.B / (float)255.0, (float)1.0));

			#region wall center mesh
			for (int y = 0; y < Height; y++)
			{
				double ry = liftAnimatedY(y);


				for (int z = 0; z < Depth; z++)
					for (int x = 0; x < Width; x++)
						if (Fields[y][z][x] == Objects.Wall)
						{
							GL.PushMatrix();

							GL.Translate(x, ry, z);

							if (Animation == Animations.Appear)
								GL.Scale(1, animationProgress, 1);
							if (Animation == Animations.Disappear)
								GL.Scale(1, 1 - animationProgress, 1);
							if (Animation == Animations.None && State == States.None)
								GL.Scale(1, 0, 1);

							double ps = 1.0 / 3.0;

							//center
							wallCenter.Render();

							if (x < Width - 1 && Fields[y][z][x + 1] == Objects.Wall)
							{
								//right
								GL.Translate(ps, 0, 0);
								wallCenter.Render();
								GL.Translate(-ps, 0, 0);

								//rightbottom
								if (z < Depth - 1 && Fields[y][z + 1][x] == Objects.Wall && Fields[y][z + 1][x + 1] == Objects.Wall)
								{
									GL.Translate(ps, 0, ps);
									wallCenter.Render();
									GL.Translate(-ps, 0, -ps);
								}

								//righttop
								if (z > 0 && Fields[y][z - 1][x] == Objects.Wall && Fields[y][z - 1][x + 1] == Objects.Wall)
								{
									GL.Translate(ps, 0, -ps);
									GL.Rotate(90, 0, 1, 0);
									wallCenter.Render();
									GL.Rotate(-90, 0, 1, 0);
									GL.Translate(-ps, 0, ps);
								}
							}

							if (x > 0 && Fields[y][z][x - 1] == Objects.Wall)
							{
								//left
								GL.Translate(-ps, 0, 0);
								GL.Rotate(180, 0, 1, 0);
								wallCenter.Render();
								GL.Rotate(-180, 0, 1, 0);
								GL.Translate(ps, 0, 0);


								//lefttop
								if (z > 0 && Fields[y][z - 1][x] == Objects.Wall && Fields[y][z - 1][x - 1] == Objects.Wall)
								{
									GL.Translate(-ps, 0, -ps);
									GL.Rotate(180, 0, 1, 0);
									wallCenter.Render();
									GL.Rotate(-180, 0, 1, 0);
									GL.Translate(ps, 0, ps);
								}

								//leftbottom
								if (z < Depth - 1 && Fields[y][z + 1][x] == Objects.Wall && Fields[y][z + 1][x - 1] == Objects.Wall)
								{
									GL.Translate(-ps, 0, ps);
									GL.Rotate(-90, 0, 1, 0);
									wallCenter.Render();
									GL.Rotate(90, 0, 1, 0);
									GL.Translate(ps, 0, -ps);
								}
							}

							//bottom
							if (z < Depth - 1 && Fields[y][z + 1][x] == Objects.Wall)
							{
								GL.Translate(0, 0, ps);
								GL.Rotate(-90, 0, 1, 0);
								wallCenter.Render();
								GL.Rotate(90, 0, 1, 0);
								GL.Translate(0, 0, -ps);
							}

							//top
							if (z > 0 && Fields[y][z - 1][x] == Objects.Wall)
							{
								GL.Translate(0, 0, -ps);
								GL.Rotate(90, 0, 1, 0);
								wallCenter.Render();
								GL.Rotate(-90, 0, 1, 0);
								GL.Translate(0, 0, ps);
							}

							GL.PopMatrix();
						}
			}
			#endregion

			#region wall sides
			for (int y = 0; y < Height; y++)
			{
				double ry = liftAnimatedY(y);

				for (int z = 0; z < Depth; z++)
					for (int x = 0; x < Width; x++)
						if (Fields[y][z][x] == Objects.Wall)
						{
							GL.PushMatrix();
							GL.Translate(x, ry, z);

							if (Animation == Animations.Appear)
								GL.Scale(1, animationProgress, 1);
							if (Animation == Animations.Disappear)
								GL.Scale(1, 1 - animationProgress, 1);
							if (Animation == Animations.None && State == States.None)
								GL.Scale(1, 0, 1);

							double ps = 1.0 / 3.0;

							if (x < Width - 1 && Fields[y][z][x + 1] == Objects.Wall)
							{
								//rightbottom
								if (z < Depth - 1 && Fields[y][z + 1][x] != Objects.Wall || z >= Depth - 1)
								{
									GL.Translate(ps, 0, ps);
									GL.Rotate(-90, 0, 1, 0);
									wallSide.Render();
									GL.Rotate(90, 0, 1, 0);
									GL.Translate(-ps, 0, -ps);
								}

								//righttop
								if (z > 0 && Fields[y][z - 1][x] != Objects.Wall || z <= 0)
								{
									GL.Translate(ps, 0, -ps);
									GL.Rotate(90, 0, 1, 0);
									wallSide.Render();
									GL.Rotate(-90, 0, 1, 0);
									GL.Translate(-ps, 0, ps);
								}
							}
							else
							{
								//right
								GL.Translate(ps, 0, 0);
								wallSide.Render();
								GL.Translate(-ps, 0, 0);
							}

							if (z > 0 && Fields[y][z - 1][x] == Objects.Wall)
							{
								//righttop
								if (x < Width - 1 && Fields[y][z][x + 1] != Objects.Wall || x >= Width - 1)
								{
									GL.Translate(ps, 0, -ps);
									wallSide.Render();
									GL.Translate(-ps, 0, ps);
								}

								//lefttop
								if (x > 0 && Fields[y][z][x - 1] != Objects.Wall || x <= 0)
								{
									GL.Translate(-ps, 0, -ps);
									GL.Rotate(180, 0, 1, 0);
									wallSide.Render();
									GL.Rotate(-180, 0, 1, 0);
									GL.Translate(ps, 0, ps);
								}
							}
							else
							{
								//top
								GL.Translate(0, 0, -ps);
								GL.Rotate(90, 0, 1, 0);
								wallSide.Render();
								GL.Rotate(-90, 0, 1, 0);
								GL.Translate(0, 0, ps);
							}

							if (z < Depth - 1 && Fields[y][z + 1][x] == Objects.Wall)
							{
								//leftbottom
								if (x > 0 && Fields[y][z][x - 1] != Objects.Wall || x <= 0)
								{
									GL.Translate(-ps, 0, ps);
									GL.Rotate(-180, 0, 1, 0);
									wallSide.Render();
									GL.Rotate(180, 0, 1, 0);
									GL.Translate(ps, 0, -ps);
								}

								//rightbottom
								if (x < Width - 1 && Fields[y][z][x + 1] != Objects.Wall || x >= Width - 1)
								{
									GL.Translate(ps, 0, ps);
									wallSide.Render();
									GL.Translate(-ps, 0, -ps);
								}
							}
							else
							{
								//bottom
								GL.Translate(0, 0, ps);
								GL.Rotate(-90, 0, 1, 0);
								wallSide.Render();
								GL.Rotate(90, 0, 1, 0);
								GL.Translate(0, 0, -ps);
							}

							if (x > 0 && Fields[y][z][x - 1] == Objects.Wall)
							{
								//lefttop
								if (z > 0 && Fields[y][z - 1][x] != Objects.Wall || z <= 0)
								{
									GL.Translate(-ps, 0, -ps);
									GL.Rotate(90, 0, 1, 0);
									wallSide.Render();
									GL.Rotate(-90, 0, 1, 0);
									GL.Translate(ps, 0, ps);
								}

								//leftbottom
								if (z < Depth - 1 && Fields[y][z + 1][x] != Objects.Wall || z >= Depth - 1)
								{
									GL.Translate(-ps, 0, ps);
									GL.Rotate(-90, 0, 1, 0);
									wallSide.Render();
									GL.Rotate(90, 0, 1, 0);
									GL.Translate(ps, 0, -ps);
								}
							}
							else
							{
								//left
								GL.Translate(-ps, 0, 0);
								GL.Rotate(180, 0, 1, 0);
								wallSide.Render();
								GL.Rotate(-180, 0, 1, 0);
								GL.Translate(ps, 0, 0);
							}

							GL.PopMatrix();
						}
			}
			#endregion

			#region wall closed corners
			for (int y = 0; y < Height; y++)
			{
				double ry = liftAnimatedY(y);

				for (int z = 0; z < Depth; z++)
					for (int x = 0; x < Width; x++)
						if (Fields[y][z][x] == Objects.Wall)
						{
							GL.PushMatrix();
							GL.Translate(x, ry, z);

							if (Animation == Animations.Appear)
								GL.Scale(1, animationProgress, 1);
							if (Animation == Animations.Disappear)
								GL.Scale(1, 1 - animationProgress, 1);
							if (Animation == Animations.None && State == States.None)
								GL.Scale(1, 0, 1);

							double ps = 1.0 / 3.0;

							if (x < Width - 1 && Fields[y][z][x + 1] == Objects.Wall)
							{
								//rightbottom
								if (z < Depth - 1 && Fields[y][z + 1][x] == Objects.Wall && Fields[y][z + 1][x + 1] != Objects.Wall)
								{
									GL.Translate(ps, 0, ps);
									wallclosedCorner.Render();
									GL.Translate(-ps, 0, -ps);
								}

								//righttop
								if (z > 0 && Fields[y][z - 1][x] == Objects.Wall && Fields[y][z - 1][x + 1] != Objects.Wall)
								{
									GL.Translate(ps, 0, -ps);
									GL.Rotate(90, 0, 1, 0);
									wallclosedCorner.Render();
									GL.Rotate(-90, 0, 1, 0);
									GL.Translate(-ps, 0, ps);
								}
							}

							if (x > 0 && Fields[y][z][x - 1] == Objects.Wall)
							{
								if (z > 0 && Fields[y][z - 1][x] == Objects.Wall && Fields[y][z - 1][x - 1] != Objects.Wall)
								{
									//lefttop
									GL.Translate(-ps, 0, -ps);
									GL.Rotate(180, 0, 1, 0);
									wallclosedCorner.Render();
									GL.Rotate(-180, 0, 1, 0);
									GL.Translate(ps, 0, ps);
								}

								if (z < Depth - 1 && Fields[y][z + 1][x] == Objects.Wall && Fields[y][z + 1][x - 1] != Objects.Wall)
								{
									//leftbottom
									GL.Translate(-ps, 0, ps);
									GL.Rotate(-90, 0, 1, 0);
									wallclosedCorner.Render();
									GL.Rotate(90, 0, 1, 0);
									GL.Translate(ps, 0, -ps);
								}
							}

							GL.PopMatrix();
						}
			}
			#endregion

			#region wall open corners
			for (int y = 0; y < Height; y++)
			{
				double ry = liftAnimatedY(y);

				for (int z = 0; z < Depth; z++)
					for (int x = 0; x < Width; x++)
						if (Fields[y][z][x] == Objects.Wall)
						{
							GL.PushMatrix();
							GL.Translate(x, ry, z);

							if (Animation == Animations.Appear)
								GL.Scale(1, animationProgress, 1);
							if (Animation == Animations.Disappear)
								GL.Scale(1, 1 - animationProgress, 1);
							if (Animation == Animations.None && State == States.None)
								GL.Scale(1, 0, 1);

							double ps = 1.0 / 3.0;

							if (x < Width - 1 && Fields[y][z][x + 1] != Objects.Wall || x >= Width - 1)
							{
								//rightbottom
								if (z < Depth - 1 && Fields[y][z + 1][x] != Objects.Wall || z >= Depth - 1)
								{
									GL.Translate(ps, 0, ps);
									wallOpenCorner.Render();
									GL.Translate(-ps, 0, -ps);
								}

								//righttop
								if (z > 0 && Fields[y][z - 1][x] != Objects.Wall || z <= 0)
								{
									GL.Translate(ps, 0, -ps);
									GL.Rotate(90, 0, 1, 0);
									wallOpenCorner.Render();
									GL.Rotate(-90, 0, 1, 0);
									GL.Translate(-ps, 0, ps);
								}
							}

							if (x > 0 && Fields[y][z][x - 1] != Objects.Wall || x <= 0)
							{
								if (z < Depth - 1 && Fields[y][z + 1][x] != Objects.Wall || z >= Depth - 1)
								{
									//leftbottom
									GL.Translate(-ps, 0, ps);
									GL.Rotate(-90, 0, 1, 0);
									wallOpenCorner.Render();
									GL.Rotate(90, 0, 1, 0);
									GL.Translate(ps, 0, -ps);
								}

								if (z > 0 && Fields[y][z - 1][x] != Objects.Wall || z <= 0)
								{
									//lefttop
									GL.Translate(-ps, 0, -ps);
									GL.Rotate(180, 0, 1, 0);
									wallOpenCorner.Render();
									GL.Rotate(-180, 0, 1, 0);
									GL.Translate(ps, 0, ps);
								}
							}

							GL.PopMatrix();
						}
			}
			#endregion

			#endregion

			#region points
			ShaderProgram.StaticColor.SetUniform("meshColor", new Vector4(1, 1, 1, 1));
			for (int y = 0; y < Height; y++)
			{
				double ry = liftAnimatedY(y);

				GL.PushMatrix();
				GL.Translate(0, ry, 0);

				for (int z = 0; z < Depth; z++)
					for (int x = 0; x < Width; x++)
						if (Fields[y][z][x] == Objects.Point || Fields[y][z][x] == Objects.Powerup)
						{
							double r = Fields[y][z][x] == Objects.Point ? 0.1 : 0.3;

							if (Animation == Animations.Appear)
								r *= animationProgress;
							if (Animation == Animations.Disappear)
								r *= 1 - animationProgress;
							if (Animation == Animations.None && State == States.None)
								r = 0;

							GL.PushMatrix();
							GL.Translate(x, 0, z);
							GL.Scale(r, r, r);
							sphere.Render();
							GL.PopMatrix();
						}

				GL.PopMatrix();
			}
			#endregion points

			#region lifts
			ShaderProgram.StaticColor.SetUniform("meshColor", new Vector4(0, 0, 0, 0.7f));
			for (int y = 0; y < Height; y++)
			{
				double ry = liftAnimatedY(y, 0);

				GL.PushMatrix();
				GL.Translate(0, ry, 0);
				for (int z = 0; z < Depth; z++)
					for (int x = 0; x < Width; x++)
						if (Fields[y][z][x] == Objects.LiftUp || Fields[y][z][x] == Objects.LiftDown)
						{
							GL.Translate(x, 0.01, z);

							liftProgram.Enable();
							liftProgram.SetUniform("meshColor", new Vector4(0.2f, 1.0f, 0.6f, 0.5f));
							liftProgram.SetUniform("totalTimeElapsed", totalTimeElapsed);
							liftProgram.SetUniform("useYOpacity", Fields[y][z][x] == Objects.LiftUp ? 0.0f : 1.0f);
							liftProgram.SetUniform("floorY", (float)ry);
							lift.Render();
							liftProgram.Disable();

							ShaderProgram.StaticColor.Enable();
							GL.Translate(-x, 0.01, -z);
						}
				GL.PopMatrix();
			}
			#endregion

			#region floors
			ShaderProgram.StaticColor.SetUniform("meshColor", new Vector4(0, 0, 0, 0.7f));
			for (int y = 0; y < Height; y++)
			{
				double ry = liftAnimatedY(y);

				GL.PushMatrix();
				GL.Translate(0, ry, 0);
				for (int z = 0; z < Depth; z++)
					for (int x = 0; x < Width; x++)
						if (Fields[y][z][x] == Objects.LiftUp || Fields[y][z][x] == Objects.LiftDown)
						{
							GL.Translate(x, 0.01, z);
							if (Fields[y][z][x] == Objects.LiftUp)
								floor.Render();
							else
								floorHole.Render();
							ShaderProgram.StaticColor.Enable();
							GL.Translate(-x, 0.01, -z);
						}
				GL.PopMatrix();
			}
			ShaderProgram.StaticColor.SetUniform("meshColor", new Vector4(0, 0, 0, 0.7f));
			for (int y = 0; y < Height; y++)
			{
				double ry = liftAnimatedY(y);


				GL.PushMatrix();
				GL.Translate(0, ry, 0);

				for (int z = 0; z < Depth; z++)
					for (int x = 0; x < Width; x++)
						switch (Fields[y][z][x])
						{
							case Objects.Point:
							case Objects.Powerup:
							case Objects.None:
								GL.Translate(x, 0.01, z);
								floor.Render();
								GL.Translate(-x, -0.01, -z);
								break;

							default:
								break;
						}

				GL.PopMatrix();
			}

			#region floor near walls
			for (int y = 0; y < Height; y++)
			{
				double ry = liftAnimatedY(y);

				for (int z = 0; z < Depth; z++)
					for (int x = 0; x < Width; x++)
						if (Fields[y][z][x] == Objects.Wall)
						{
							GL.PushMatrix();
							GL.Translate(x, ry - 0.5 + 0.01, z);

							double ps = 1.0 / 3.0;

							if (x < Width - 1 && Fields[y][z][x + 1] != Objects.Empty)
							{
								//right
								GL.Translate(ps, 0, 0);
								GL.Scale(ps, 0, ps);
								floor.Render();
								GL.Scale(1 / ps, 0, 1 / ps);
								GL.Translate(-ps, 0, 0);

								if (z < Depth - 1 && Fields[y][z + 1][x] != Objects.Empty && Fields[y][z + 1][x + 1] != Objects.Empty)
								{
									//rightbottom
									GL.Translate(ps, 0, ps);
									GL.Scale(ps, 0, ps);
									floor.Render();
									GL.Scale(1 / ps, 0, 1 / ps);
									GL.Translate(-ps, 0, -ps);
								}

								if (z > 0 && Fields[y][z - 1][x] != Objects.Empty && Fields[y][z - 1][x + 1] != Objects.Empty)
								{
									//righttop
									GL.Translate(ps, 0, -ps);
									GL.Scale(ps, 0, ps);
									floor.Render();
									GL.Scale(1 / ps, 0, 1 / ps);
									GL.Translate(-ps, 0, ps);
								}
							}

							if (z > 0 && Fields[y][z - 1][x] != Objects.Empty)
							{
								//top
								GL.Translate(0, 0, -ps);
								GL.Scale(ps, 0, ps);
								floor.Render();
								GL.Scale(1 / ps, 0, 1 / ps);
								GL.Translate(0, 0, ps);
							}

							if (z < Depth - 1 && Fields[y][z + 1][x] != Objects.Empty)
							{
								//bottom
								GL.Translate(0, 0, ps);
								GL.Scale(ps, 0, ps);
								floor.Render();
								GL.Scale(1 / ps, 0, 1 / ps);
								GL.Translate(0, 0, -ps);
							}

							if (x > 0 && Fields[y][z][x - 1] != Objects.Empty)
							{
								//left
								GL.Translate(-ps, 0, 0);
								GL.Scale(ps, 0, ps);
								floor.Render();
								GL.Scale(1 / ps, 0, 1 / ps);
								GL.Translate(ps, 0, 0);

								if (z < Depth - 1 && Fields[y][z + 1][x] != Objects.Empty && Fields[y][z + 1][x - 1] != Objects.Empty)
								{
									//leftbottom
									GL.Translate(-ps, 0, ps);
									GL.Scale(ps, 0, ps);
									floor.Render();
									GL.Scale(1 / ps, 0, 1 / ps);
									GL.Translate(ps, 0, -ps);
								}

								if (z > 0 && Fields[y][z - 1][x] != Objects.Empty && Fields[y][z - 1][x - 1] != Objects.Empty)
								{
									//rightbottom
									GL.Translate(-ps, 0, -ps);
									GL.Scale(ps, 0, ps);
									floor.Render();
									GL.Scale(1 / ps, 0, 1 / ps);
									GL.Translate(ps, 0, ps);
								}
							}

							GL.PopMatrix();
						}
			}
			#endregion

			#endregion floors

			ShaderProgram.StaticColor.Disable();
		}

		override public void Dispose()
		{
			if (wallCenter_v != null)
				wallCenter_v.Dispose();
			if (wallSide_v != null)
				wallSide_v.Dispose();
			if (wallclosedCorner_v != null)
				wallclosedCorner_v.Dispose();
			if (wallOpenCorner_v != null)
				wallOpenCorner_v.Dispose();
		}
	}
}
