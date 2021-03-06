﻿using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

namespace PacMan
{
	class Map : IDisposable
	{
		/// <summary>
		/// Map states.
		/// </summary>
		public enum States
		{
			/// <summary>
			/// Appear animation.
			/// </summary>
			AppearAnimation,
			/// <summary>
			/// Normal.
			/// </summary>
			Normal,
			/// <summary>
			/// Disappear animation.
			/// </summary>
			DisappearAnimation,
			/// <summary>
			/// Not in game.
			/// </summary>
			None
		}

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
			/// <summary>
			/// Empty field.
			/// </summary>
			None
		}

		/// <summary>
		/// Appear and disappear animation duration(seconds).
		/// </summary>
		private const double animationDuration = 0.5;
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

		/// <summary>
		/// Animation progress in [0..1]
		/// </summary>
		private double animationState = 0;

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
					sphere_v.Vertices = v;
					sphere_v.Normals = n;
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

					Utils.Push(v, new Vector3d(0.5, 0.0, 0.5), ref vp);
					Utils.Push(v, new Vector3d(0.5, 0.0, Height - 0.5), ref vp);
					Utils.Push(v, new Vector3d(Width - 0.5, 0.0, Height - 0.5), ref vp);
					Utils.Push(v, new Vector3d(Width - 0.5, 0.0, 0.5), ref vp);

					for (int i = 0; i < 4; i++)
						Utils.Push(n, normal, ref np);

					floor_v = new Mesh();
					floor_v.Vertices = v;
					floor_v.Normals = n;
				}

				return floor_v;
			}
		}

		#region Wall rendering

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

					Utils.Push(v, new Vector3d(-ps2, 1.0, -ps2), ref vp);
					Utils.Push(v, new Vector3d(-ps2, 1.0, ps2), ref vp);
					Utils.Push(v, new Vector3d(ps2, 1.0, ps2), ref vp);
					Utils.Push(v, new Vector3d(ps2, 1.0, -ps2), ref vp);

					for (int i = 0; i < 4; i++)
						Utils.Push(n, normal, ref np);

					wallCenter_v = new Mesh();
					wallCenter_v.Vertices = v;
					wallCenter_v.Normals = n;
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

					Utils.Push(v, new Vector3d(-ps2, 1.0, -ps2), ref vp);
					Utils.Push(v, new Vector3d(-ps2, 1.0, ps2), ref vp);
					Utils.Push(v, new Vector3d(0, 1.0, ps2), ref vp);
					Utils.Push(v, new Vector3d(0, 1.0, -ps2), ref vp);
					Utils.Push(v, new Vector3d(0, 0.0, -ps2), ref vp);
					Utils.Push(v, new Vector3d(0, 1.0, -ps2), ref vp);
					Utils.Push(v, new Vector3d(0, 1.0, ps2), ref vp);
					Utils.Push(v, new Vector3d(0, 0.0, ps2), ref vp);

					Vector3d normal = new Vector3d(0, 1, 0);
					for (int i = 0; i < 4; i++)
						Utils.Push(n, normal, ref np);

					normal = new Vector3d(1, 0, 0);
					for (int i = 4; i < 8; i++)
						Utils.Push(n, normal, ref np);

					wallSide_v = new Mesh();
					wallSide_v.Vertices = v;
					wallSide_v.Normals = n;
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

					Utils.Push(v, new Vector3d(-ps2, 1.0, -ps2), ref vp);
					Utils.Push(v, new Vector3d(-ps2, 1.0, 0), ref vp);
					Utils.Push(v, new Vector3d(0, 1.0, 0), ref vp);
					Utils.Push(v, new Vector3d(0, 1.0, -ps2), ref vp);

					Utils.Push(v, new Vector3d(0, 1.0, -ps2), ref vp);
					Utils.Push(v, new Vector3d(0, 1.0, 0), ref vp);
					Utils.Push(v, new Vector3d(ps2, 1.0, 0), ref vp);
					Utils.Push(v, new Vector3d(ps2, 1.0, -ps2), ref vp);

					Utils.Push(v, new Vector3d(-ps2, 1.0, 0), ref vp);
					Utils.Push(v, new Vector3d(-ps2, 1.0, ps2), ref vp);
					Utils.Push(v, new Vector3d(0, 1.0, ps2), ref vp);
					Utils.Push(v, new Vector3d(0, 1.0, 0), ref vp);

					Vector3d normal = new Vector3d(0, 1, 0);
					for (int i = 0; i < 12; i++)
						Utils.Push(n, normal, ref np);

					for (double alpha = 0; alpha < Math.PI / 2; alpha += step)
					{
						Utils.Push(v, new Vector3d(0, 1.0, 0), ref vp);
						Utils.Push(v, new Vector3d(ps2 - ps2 * Math.Sin(alpha + step), 1.0,
							 ps2 - ps2 * Math.Cos(alpha + step)), ref vp);
						Utils.Push(v, new Vector3d(ps2 - ps2 * Math.Sin(alpha), 1.0,
							 ps2 - ps2 * Math.Cos(alpha)), ref vp);
						Utils.Push(v, new Vector3d(0, 1.0, 0), ref vp);
						for (int i = 0; i < 4; i++)
							Utils.Push(n, normal, ref np);

					}

					for (double alpha = 0; alpha < Math.PI / 2; alpha += step)
					{
						normal = new Vector3d(Math.Sin(alpha), 0, Math.Cos(alpha));
						Utils.Push(n, normal, ref np);
						normal.Mult(ps2);
						normal.X = ps2 - normal.X;
						normal.Z = ps2 - normal.Z;
						Utils.Push(v, normal, ref vp);

						normal = new Vector3d(Math.Sin(alpha), 0, Math.Cos(alpha));
						Utils.Push(n, normal, ref np);
						normal.Mult(ps2);
						normal.X = ps2 - normal.X;
						normal.Y = 1;
						normal.Z = ps2 - normal.Z;
						Utils.Push(v, normal, ref vp);

						normal = new Vector3d(Math.Sin(alpha + step), 0, Math.Cos(alpha + step));
						Utils.Push(n, normal, ref np);
						normal.Mult(ps2);
						normal.X = ps2 - normal.X;
						normal.Y = 1;
						normal.Z = ps2 - normal.Z;
						Utils.Push(v, normal, ref vp);


						normal = new Vector3d(Math.Sin(alpha + step), 0, Math.Cos(alpha + step));
						Utils.Push(n, normal, ref np);
						normal.Mult(ps2);
						normal.X = ps2 - normal.X;
						normal.Z = ps2 - normal.Z;
						Utils.Push(v, normal, ref vp);
					}

					wallclosedCorner_v = new Mesh();
					wallclosedCorner_v.Vertices = v;
					wallclosedCorner_v.Normals = n;
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
						Utils.Push(v, new Vector3d(-ps2, 1.0, -ps2), ref vp);
						Utils.Push(v, new Vector3d(-ps2, 1.0, -ps2), ref vp);
						Utils.Push(v, new Vector3d(-ps2 + ps2 * Math.Sin(alpha), 1.0,
							 -ps2 + ps2 * Math.Cos(alpha)), ref vp);
						Utils.Push(v, new Vector3d(-ps2 + ps2 * Math.Sin(alpha + step), 1.0,
							 -ps2 + ps2 * Math.Cos(alpha + step)), ref vp);


						for (int i = 0; i < 4; i++)
							Utils.Push(n, normal, ref np);

					}

					for (double alpha = 0; alpha < Math.PI / 2; alpha += step)
					{
						normal = new Vector3d(Math.Sin(alpha), 0, Math.Cos(alpha));
						Utils.Push(n, normal, ref np);
						normal.Mult(ps2);
						normal.X = normal.X - ps2;
						normal.Z = normal.Z - ps2;
						Utils.Push(v, normal, ref vp);

						normal = new Vector3d(Math.Sin(alpha + step), 0, Math.Cos(alpha + step));
						Utils.Push(n, normal, ref np);
						normal.Mult(ps2);
						normal.X = normal.X - ps2;
						normal.Z = normal.Z - ps2;
						Utils.Push(v, normal, ref vp);

						normal = new Vector3d(Math.Sin(alpha + step), 0, Math.Cos(alpha + step));
						Utils.Push(n, normal, ref np);
						normal.Mult(ps2);
						normal.X = normal.X - ps2;
						normal.Y = 1;
						normal.Z = normal.Z - ps2;
						Utils.Push(v, normal, ref vp);

						normal = new Vector3d(Math.Sin(alpha), 0, Math.Cos(alpha));
						Utils.Push(n, normal, ref np);
						normal.Mult(ps2);
						normal.X = normal.X - ps2;
						normal.Y = 1;
						normal.Z = normal.Z - ps2;
						Utils.Push(v, normal, ref vp);
					}

					wallOpenCorner_v = new Mesh();
					wallOpenCorner_v.Vertices = v;
					wallOpenCorner_v.Normals = n;
				}

				return wallOpenCorner_v;
			}
		}

		private void renderWall(int x, int y)
		{
			GL.Translate(x, 0, y);

			GL.PushMatrix();
			if (State == States.AppearAnimation)
				GL.Scale(1, animationState, 1);
			if (State == States.DisappearAnimation)
				GL.Scale(1, 1 - animationState, 1);
			if (State == States.None)
				GL.Scale(1, 0, 1);

			double ps = 1.0 / 3.0;

			//center
			wallCenter.Render();

			//right
			GL.Translate(ps, 0, 0);
			if (x < Width - 1 && Fields[y][x + 1] == Objects.Wall)
				wallCenter.Render();
			else
				wallSide.Render();
			GL.Translate(-ps, 0, 0);

			//left
			GL.Translate(-ps, 0, 0);
			GL.Rotate(180, 0, 1, 0);
			if (x > 0 && Fields[y][x - 1] == Objects.Wall)
				wallCenter.Render();
			else
				wallSide.Render();
			GL.Rotate(-180, 0, 1, 0);
			GL.Translate(ps, 0, 0);

			//bottom
			GL.Translate(0, 0, ps);
			GL.Rotate(-90, 0, 1, 0);
			if (y < Height - 1 && Fields[y + 1][x] == Objects.Wall)
				wallCenter.Render();
			else
				wallSide.Render();
			GL.Rotate(90, 0, 1, 0);
			GL.Translate(0, 0, -ps);

			//top
			GL.Translate(0, 0, -ps);
			GL.Rotate(90, 0, 1, 0);
			if (y > 0 && Fields[y - 1][x] == Objects.Wall)
				wallCenter.Render();
			else
				wallSide.Render();
			GL.Rotate(-90, 0, 1, 0);
			GL.Translate(0, 0, ps);

			//rightbottom
			GL.Translate(ps, 0, ps);
			if (x < Width - 1 && y < Height - 1 && Fields[y + 1][x] == Objects.Wall && Fields[y][x + 1] == Objects.Wall && Fields[y + 1][x + 1] == Objects.Wall)
				wallCenter.Render();
			else if (x < Width - 1 && y < Height - 1 && Fields[y + 1][x] == Objects.Wall && Fields[y][x + 1] == Objects.Wall)
				wallclosedCorner.Render();
			else if (x < Width - 1 && Fields[y][x + 1] == Objects.Wall)
			{
				GL.Rotate(-90, 0, 1, 0);
				wallSide.Render();
				GL.Rotate(90, 0, 1, 0);
			}

			else if (y < Height - 1 && Fields[y + 1][x] == Objects.Wall)
				wallSide.Render();
			else
				wallOpenCorner.Render();
			GL.Translate(-ps, 0, -ps);


			//righttop
			GL.Translate(ps, 0, -ps);
			GL.Rotate(90, 0, 1, 0);
			if (x < Width - 1 && y > 0 && Fields[y - 1][x] == Objects.Wall && Fields[y][x + 1] == Objects.Wall && Fields[y - 1][x + 1] == Objects.Wall)
				wallCenter.Render();
			else if (x < Width - 1 && y > 0 && Fields[y - 1][x] == Objects.Wall && Fields[y][x + 1] == Objects.Wall)
				wallclosedCorner.Render();
			else if (x < Width - 1 && Fields[y][x + 1] == Objects.Wall)
				wallSide.Render();

			else if (y > 0 && Fields[y - 1][x] == Objects.Wall)
			{
				GL.Rotate(-90, 0, 1, 0);
				wallSide.Render();
				GL.Rotate(90, 0, 1, 0);
			}
			else
				wallOpenCorner.Render();
			GL.Rotate(-90, 0, 1, 0);
			GL.Translate(-ps, 0, ps);

			//lefttop
			GL.Translate(-ps, 0, -ps);
			GL.Rotate(180, 0, 1, 0);
			if (x > 0 && y > 0 && Fields[y - 1][x] == Objects.Wall && Fields[y][x - 1] == Objects.Wall && Fields[y - 1][x - 1] == Objects.Wall)
				wallCenter.Render();
			else if (x > 0 && y > 0 && Fields[y - 1][x] == Objects.Wall && Fields[y][x - 1] == Objects.Wall)
				wallclosedCorner.Render();
			else if (x > 0 && Fields[y][x - 1] == Objects.Wall)
			{
				GL.Rotate(-90, 0, 1, 0);
				wallSide.Render();
				GL.Rotate(90, 0, 1, 0);
			}
			else if (y > 0 && Fields[y - 1][x] == Objects.Wall)
				wallSide.Render();
			else
				wallOpenCorner.Render();
			GL.Rotate(-180, 0, 1, 0);
			GL.Translate(ps, 0, ps);

			//leftbottom
			GL.Translate(-ps, 0, ps);
			GL.Rotate(-90, 0, 1, 0);
			if (x > 0 && y < Height - 1 && Fields[y + 1][x] == Objects.Wall && Fields[y][x - 1] == Objects.Wall && Fields[y + 1][x - 1] == Objects.Wall)
				wallCenter.Render();
			else if (x > 0 && y < Height - 1 && Fields[y + 1][x] == Objects.Wall && Fields[y][x - 1] == Objects.Wall)
				wallclosedCorner.Render();
			else if (x > 0 && Fields[y][x - 1] == Objects.Wall)

				wallSide.Render();
			else if (y < Height - 1 && Fields[y + 1][x] == Objects.Wall)
			{
				GL.Rotate(-90, 0, 1, 0);
				wallSide.Render();
				GL.Rotate(90, 0, 1, 0);
			}

			else
				wallOpenCorner.Render();
			GL.Rotate(90, 0, 1, 0);
			GL.Translate(ps, 0, -ps);

			GL.PopMatrix();

			GL.Translate(-x, 0, -y);

			GL.End();
		}

		#endregion

		/// <summary>
		/// Map state.
		/// </summary>
		public States State = States.Normal;

		/// <summary>
		/// Map name
		/// </summary>
		public string Name;
		/// <summary>
		/// Path to map data file
		/// </summary>
		public string Path;
		/// <summary>
		/// Width in cells
		/// </summary>
		public int Width;
		/// <summary>
		/// Height in cells
		/// </summary>
		public int Height;
		/// <summary>
		/// Map grid
		/// </summary>
		public Objects[][] Fields;
		public Objects[][] OriginalFields = null;
		/// <summary>
		/// Start point of PacMan
		/// </summary>
		public Point PacManStart = Point.Empty;
		/// <summary>
		/// Start point of ghosts
		/// </summary>
		public Point GhostStart = Point.Empty;

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
		/// Wrap Y coordinate inside map borders.
		/// </summary>
		/// <param name="Y">Y coordinate.</param>
		/// <returns>Y coordinate inside map borders.</returns>
		public double WrapY(double Y)
		{
			if (Y >= Height)
				return Y - Height;
			if (Y < 0)
				return Y + Height;
			return Y;
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
		/// Wrap Y coordinate inside map borders.
		/// </summary>
		/// <param name="Y">Y coordinate.</param>
		/// <returns>Y coordinate inside map borders.</returns>
		public int WrapY(int Y)
		{
			if (Y >= Height)
				return Y - Height;
			if (Y < 0)
				return Y + Height;
			return Y;
		}

		/// <summary>
		/// Shortcut to map row.
		/// </summary>
		/// <param name="y">Row index.</param>
		/// <returns>Row.</returns>
		public Objects[] this[int y] { get { return Fields[y]; } }

		/// <summary>
		/// Shortcut to map cell.
		/// </summary>
		/// <param name="y">Row index.</param>
		/// <param name="x">Column index.</param>
		/// <returns>Cell.</returns>
		public Objects this[int y, int x] { get { return Fields[WrapY(y)][WrapX(x)]; } }

		/// <summary>
		/// Shortcut to map cell.
		/// </summary>
		/// <param name="y">Row index.</param>
		/// <param name="x">Column index.</param>
		/// <returns>Cell.</returns>
		public Objects this[double y, double x] { get { return Fields[(int)WrapY(y)][(int)WrapX(x)]; } }

		/// <summary>
		/// Cell does not contains walls.
		/// </summary>
		/// <param name="y">Y coordinate, wrapping included.</param>
		/// <param name="x">X coordinate, wrapping included.</param>
		/// <returns>True, if cell is walkable.</returns>
		public bool IsWalkable(int y, int x)
		{
			return this[y, x] != Objects.Wall;
		}
		/// <summary>
		/// Cell does not contains walls.
		/// </summary>
		/// <param name="y">Y coordinate, wrapping included.</param>
		/// <param name="x">X coordinate, wrapping included.</param>
		/// <returns>True, if cell is walkable.</returns>
		public bool IsWalkable(double y, double x)
		{
			return this[y, x] != Objects.Wall;
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
					for (int x = 0; x < Width; x++)
						if (Fields[y][x] == Objects.Point || Fields[y][x] == Objects.Powerup)
							result++;
				return result;
			}
		}

		/// <summary>
		/// Map loading and initialization method
		/// </summary>
		public void Init()
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

				string[] rows =data.Split('\n');
				Height = rows.Length;
				Width = rows[0].Length;
				Fields = new Objects[Height][];
				OriginalFields = new Objects[Height][];

				Point? pacmanStart = null;
				Point? ghostStart = null;

				for (int y = 0; y < rows.Length; y++)
				{
					if (rows[y].Length != Width)
						throw new Exception("Invalid map data");
					Fields[y] = new Objects[Width];
					OriginalFields[y] = new Objects[Width];
					for (int x = 0; x < rows[y].Length; x++)
						switch (rows[y][x])
						{
							case '.':
								Fields[y][x] = OriginalFields[y][x] = Objects.Point;
								break;
							case 'O':
								Fields[y][x] = OriginalFields[y][x] = Objects.Powerup;
								break;
							case '#':
								Fields[y][x] = OriginalFields[y][x] = Objects.Wall;
								break;
							case 'C':
								pacmanStart = new Point(x, y);
								Fields[y][x] = OriginalFields[y][x] = Objects.None;
								break;
							case 'M':
								ghostStart = new Point(x, y);
								Fields[y][x] = OriginalFields[y][x] = Objects.None;
								break;
							case '-':
								Fields[y][x] = OriginalFields[y][x] = Objects.None;
								break;

						}
				}
				if (PointsCount == 0 || pacmanStart == null || ghostStart == null)
					throw new Exception("Invalid map data");
				PacManStart = (Point)pacmanStart;
				GhostStart = (Point)ghostStart;
			}
			else
			{
				for (int y = 0; y < Height; y++)
					for (int x = 0; x < Height; x++)
						Fields[y][x] = OriginalFields[y][x];
			}
			animationState = 0;
			State = States.None;
		}

		/// <summary>
		/// Update.
		/// </summary>
		/// <param name="dt">Time elapsed from last call(seconds)</param>
		public void Update(double dt)
		{
			if (State == States.AppearAnimation)
			{
				animationState += dt / animationDuration;
				if (animationState >= 1)
				{
					State = States.Normal;
					animationState = 0;
				}
			}

			if (State == States.DisappearAnimation)
			{
				animationState += dt / animationDuration;
				if (animationState >= 1)
				{
					State = States.None;
					animationState = 0;
				}
			}
		}

		/// <summary>
		/// Render method
		/// </summary>
		public void Render()
		{
			ShaderProgram.StaticColor.Enable();

			ShaderProgram.StaticColor.SetUniform("meshColor", new Vector4(0, 0, 0, 1));

			floor.Render();

			for (int y = 0; y < Height; y++)
				for (int x = 0; x < Width; x++)
					switch (Fields[y][x])
					{

						case Objects.Wall:
							ShaderProgram.StaticColor.SetUniform("meshColor",
							 new Vector4(wallColor.R / (float)255.0, wallColor.G / (float)255.0, wallColor.B / (float)255.0, (float)1.0));
							renderWall(x, y);
							break;

						case Objects.Point:
						case Objects.Powerup:
							ShaderProgram.StaticColor.SetUniform("meshColor", new Vector4(1, 1, 1, 1));

							double r = Fields[y][x] == Objects.Point ? 0.1 : 0.3;

							if (State == States.AppearAnimation)
								r *= animationState;
							if (State == States.DisappearAnimation)
								r *= 1 - animationState;
							if (State == States.None)
								r = 0;

							GL.PushMatrix();
							GL.Translate(x, 0.5, y);
							GL.Scale(r, r, r);
							sphere.Render();
							GL.PopMatrix();
							break;

						case Objects.None:
						default:
							break;
					}

			ShaderProgram.StaticColor.Disable();
		}

		public void Dispose()
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
