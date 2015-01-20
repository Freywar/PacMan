﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace PacMan
{
	class Map
	{
		/// <summary>
		/// Possible map objects
		/// </summary>
		public enum Objects
		{
			/// <summary>
			/// Wall
			/// </summary>
			Wall,
			/// <summary>
			/// Point
			/// </summary>
			Point,
			/// <summary>
			/// Powerup
			/// </summary>
			Powerup,
			/// <summary>
			/// Empty field
			/// </summary>
			None
		}

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

		/// <summary>
		/// Start point of PacMan
		/// </summary>
		public Point PacManStart = Point.Empty;
		/// <summary>
		/// Start point of ghosts
		/// </summary>
		public Point GhostStart = Point.Empty;

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

			StringBuilder sb = new StringBuilder();
			using (StreamReader sr = new StreamReader(Path))
			{
				String line;
				// Read and display lines from the file until the end of 
				// the file is reached.
				while ((line = sr.ReadLine()) != null)
				{
					sb.AppendLine(line);
				}
			}
			string data = sb.ToString();

			string[] rows = Regex.Replace(Regex.Replace(data, "\\r", ""), "\\n*$", "").Split('\n');
			Height = rows.Length;
			Width = rows[0].Length;
			Fields = new Objects[Height][];

			for (int y = 0; y < rows.Length; y++)
			{
				if (rows[y].Length != Width)
					throw new Exception("Invalid map data");
				Fields[y] = new Objects[Width];
				for (int x = 0; x < rows[y].Length; x++)
					switch (rows[y][x])
					{
						case '.':
							Fields[y][x] = Objects.Point;
							break;
						case 'O':
							Fields[y][x] = Objects.Powerup;
							break;
						case '#':
							Fields[y][x] = Objects.Wall;
							break;
						case 'C':
							PacManStart = new Point(x, y);
							Fields[y][x] = Objects.None;
							break;
						case 'M':
							GhostStart = new Point(x, y);
							Fields[y][x] = Objects.None;
							break;
						case '-':
							Fields[y][x] = Objects.None;
							break;

					}
			}
			if (PointsCount == 0 || PacManStart == Point.Empty || GhostStart == Point.Empty)
				throw new Exception("Invalid map data");
		}

		private Mesh sphere = null;
		private Mesh sphereMesh
		{
			get
			{
				if (sphere == null)
				{
					Vector3d color = new Vector3d(1, 1, 1);
					double r = 1;

					double step = Math.PI / 10;
					int pointsCount = 800;
					int vp = 0;
					int np = 0;
					int cp = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];
					double[] c = new double[pointsCount * 4];


					for (double alpha = -Math.PI / 2; alpha < Math.PI / 2; alpha += step)
						for (double beta = 0; beta < Math.PI * 2; beta += step)
						{
							GL.Color3(Color.White);

							Vector3d normal = Utils.FromSpheric(alpha, beta, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(r);
							Utils.Push(v, normal, ref vp);
							Utils.Push(c, color, ref cp);

							normal = Utils.FromSpheric(alpha + step, beta, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(r);
							Utils.Push(v, normal, ref vp);
							Utils.Push(c, color, ref cp);

							normal = Utils.FromSpheric(alpha + step, beta + step, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(r);
							Utils.Push(v, normal, ref vp);
							Utils.Push(c, color, ref cp);

							normal = Utils.FromSpheric(alpha, beta + step, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(r);
							Utils.Push(v, normal, ref vp);
							Utils.Push(c, color, ref cp);
						}

					sphere = new Mesh();
					sphere.Vertices = v;
					sphere.Normals = n;
					sphere.Colors = c;
				}

				return sphere;
			}
		}

		#region Wall rendering

		private Mesh wallCenter_v = null;
		private Mesh wallCenter
		{
			get
			{
				if (wallCenter_v == null)
				{
					double ps2 = 1.0 / 6.0;
					Vector3d color = new Vector3d(
					Color.DarkBlue.R / 255.0, Color.DarkBlue.G / 255.0,
					Color.DarkBlue.B / 255.0);
					Vector3d normal = new Vector3d(0, 1, 0);

					int pointsCount = 4;
					int vp = 0;
					int np = 0;
					int cp = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];
					double[] c = new double[pointsCount * 4];

					Utils.Push(v, new Vector3d(-ps2, 1.0, -ps2), ref vp);
					Utils.Push(v, new Vector3d(-ps2, 1.0, ps2), ref vp);
					Utils.Push(v, new Vector3d(ps2, 1.0, ps2), ref vp);
					Utils.Push(v, new Vector3d(ps2, 1.0, -ps2), ref vp);

					for (int i = 0; i < 4; i++)
					{
						Utils.Push(n, normal, ref np);
						Utils.Push(c, color, ref cp);
					}

					wallCenter_v = new Mesh();
					wallCenter_v.Vertices = v;
					wallCenter_v.Normals = n;
					wallCenter_v.Colors = c;
				}

				return wallCenter_v;
			}
		}

		private Mesh wallSideMesh_v = null;
		private Mesh wallSideMesh
		{
			get
			{
				if (wallSideMesh_v == null)
				{
					double ps2 = 1.0 / 6.0;
					Vector3d color = new Vector3d(
					Color.DarkBlue.R / 255.0, Color.DarkBlue.G / 255.0,
					Color.DarkBlue.B / 255.0);

					int pointsCount = 8;
					int vp = 0;
					int np = 0;
					int cp = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];
					double[] c = new double[pointsCount * 3];

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
					{
						Utils.Push(n, normal, ref np);
						Utils.Push(c, color, ref cp);
					}

					normal = new Vector3d(1, 0, 0);
					for (int i = 4; i < 8; i++)
					{
						Utils.Push(n, normal, ref np);
						Utils.Push(c, color, ref cp);
					}

					wallSideMesh_v = new Mesh();
					wallSideMesh_v.Vertices = v;
					wallSideMesh_v.Normals = n;
					wallSideMesh_v.Colors = c;
				}

				return wallSideMesh_v;
			}
		}

		private Mesh wallclosedCornerMesh_v = null;
		private Mesh wallclosedCornerMesh
		{
			get
			{
				if (wallclosedCornerMesh_v == null)
				{
					double ps2 = 1.0 / 6.0;
					double step = Math.PI / 10;
					Vector3d color = new Vector3d(
					Color.DarkBlue.R / 255.0, Color.DarkBlue.G / 255.0,
					Color.DarkBlue.B / 255.0);

					int pointsCount = 52;
					int vp = 0;
					int np = 0;
					int cp = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];
					double[] c = new double[pointsCount * 3];

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

					for (int i = 0; i < pointsCount; i++)
						Utils.Push(c, color, ref cp);

					wallclosedCornerMesh_v = new Mesh();
					wallclosedCornerMesh_v.Vertices = v;
					wallclosedCornerMesh_v.Normals = n;
					wallclosedCornerMesh_v.Colors = c;
				}

				return wallclosedCornerMesh_v;
			}
		}

		private Mesh wallOpenCornerMesh_v = null;
		private Mesh wallOpenCornerMesh
		{
			get
			{
				if (wallOpenCornerMesh_v == null)
				{
					double ps2 = 1.0 / 6.0;
					double step = Math.PI / 10;
					Vector3d color = new Vector3d(
					Color.DarkBlue.R / 255.0, Color.DarkBlue.G / 255.0,
					Color.DarkBlue.B / 255.0);

					int pointsCount = 40;
					int vp = 0;
					int np = 0;
					int cp = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];
					double[] c = new double[pointsCount * 3];

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

					for (int i = 0; i < pointsCount; i++)
						Utils.Push(c, color, ref cp);

					wallOpenCornerMesh_v = new Mesh();
					wallOpenCornerMesh_v.Vertices = v;
					wallOpenCornerMesh_v.Normals = n;
					wallOpenCornerMesh_v.Colors = c;
				}

				return wallOpenCornerMesh_v;
			}
		}

	

		private void renderWall(int x, int y)
		{
			GL.Translate(x, 0, y);

			GL.Color3(Color.DarkBlue);

			double ps = 1.0 / 3.0;

			//center
			wallCenter.Render();

			//right
			GL.Translate(ps, 0, 0);
			if (x < Width - 1 && Fields[y][x + 1] == Objects.Wall)
				wallCenter.Render();
			else
				wallSideMesh.Render();
			GL.Translate(-ps, 0, 0);

			//left
			GL.Translate(-ps, 0, 0);
			GL.Rotate(180, 0, 1, 0);
			if (x > 0 && Fields[y][x - 1] == Objects.Wall)
				wallCenter.Render();
			else
				wallSideMesh.Render();
			GL.Rotate(-180, 0, 1, 0);
			GL.Translate(ps, 0, 0);

			//bottom
			GL.Translate(0, 0, ps);
			GL.Rotate(-90, 0, 1, 0);
			if (y < Height - 1 && Fields[y + 1][x] == Objects.Wall)
				wallCenter.Render();
			else
				wallSideMesh.Render();
			GL.Rotate(90, 0, 1, 0);
			GL.Translate(0, 0, -ps);

			//top
			GL.Translate(0, 0, -ps);
			GL.Rotate(90, 0, 1, 0);
			if (y > 0 && Fields[y - 1][x] == Objects.Wall)
				wallCenter.Render();
			else
				wallSideMesh.Render();
			GL.Rotate(-90, 0, 1, 0);
			GL.Translate(0, 0, ps);

			//rightbottom
			GL.Translate(ps, 0, ps);
			if (x < Width - 1 && y < Height - 1 && Fields[y + 1][x] == Objects.Wall && Fields[y][x + 1] == Objects.Wall && Fields[y + 1][x + 1] == Objects.Wall)
				wallCenter.Render();
			else if (x < Width - 1 && y < Height - 1 && Fields[y + 1][x] == Objects.Wall && Fields[y][x + 1] == Objects.Wall)
				wallclosedCornerMesh.Render();
			else if (x < Width - 1 && Fields[y][x + 1] == Objects.Wall)
			{
				GL.Rotate(-90, 0, 1, 0);
				wallSideMesh.Render();
				GL.Rotate(90, 0, 1, 0);
			}

			else if (y < Height - 1 && Fields[y + 1][x] == Objects.Wall)
				wallSideMesh.Render();
			else
				wallOpenCornerMesh.Render();
			GL.Translate(-ps, 0, -ps);


			//righttop
			GL.Translate(ps, 0, -ps);
			GL.Rotate(90, 0, 1, 0);
			if (x < Width - 1 && y > 0 && Fields[y - 1][x] == Objects.Wall && Fields[y][x + 1] == Objects.Wall && Fields[y - 1][x + 1] == Objects.Wall)
				wallCenter.Render();
			else if (x < Width - 1 && y > 0 && Fields[y - 1][x] == Objects.Wall && Fields[y][x + 1] == Objects.Wall)
				wallclosedCornerMesh.Render();
			else if (x < Width - 1 && Fields[y][x + 1] == Objects.Wall)
				wallSideMesh.Render();

			else if (y > 0 && Fields[y - 1][x] == Objects.Wall)
			{
				GL.Rotate(-90, 0, 1, 0);
				wallSideMesh.Render();
				GL.Rotate(90, 0, 1, 0);
			}
			else
				wallOpenCornerMesh.Render();
			GL.Rotate(-90, 0, 1, 0);
			GL.Translate(-ps, 0, ps);

			//lefttop
			GL.Translate(-ps, 0, -ps);
			GL.Rotate(180, 0, 1, 0);
			if (x > 0 && y > 0 && Fields[y - 1][x] == Objects.Wall && Fields[y][x - 1] == Objects.Wall && Fields[y - 1][x - 1] == Objects.Wall)
				wallCenter.Render();
			else if (x > 0 && y > 0 && Fields[y - 1][x] == Objects.Wall && Fields[y][x - 1] == Objects.Wall)
				wallclosedCornerMesh.Render();
			else if (x > 0 && Fields[y][x - 1] == Objects.Wall)
			{
				GL.Rotate(-90, 0, 1, 0);
				wallSideMesh.Render();
				GL.Rotate(90, 0, 1, 0);
			}
			else if (y > 0 && Fields[y - 1][x] == Objects.Wall)
				wallSideMesh.Render();
			else
				wallOpenCornerMesh.Render();
			GL.Rotate(-180, 0, 1, 0);
			GL.Translate(ps, 0, ps);

			//leftbottom
			GL.Translate(-ps, 0, ps);
			GL.Rotate(-90, 0, 1, 0);
			if (x > 0 && y < Height - 1 && Fields[y + 1][x] == Objects.Wall && Fields[y][x - 1] == Objects.Wall && Fields[y + 1][x - 1] == Objects.Wall)
				wallCenter.Render();
			else if (x > 0 && y < Height - 1 && Fields[y + 1][x] == Objects.Wall && Fields[y][x - 1] == Objects.Wall)
				wallclosedCornerMesh.Render();
			else if (x > 0 && Fields[y][x - 1] == Objects.Wall)

				wallSideMesh.Render();
			else if (y < Height - 1 && Fields[y + 1][x] == Objects.Wall)
			{
				GL.Rotate(-90, 0, 1, 0);
				wallSideMesh.Render();
				GL.Rotate(90, 0, 1, 0);
			}

			else
				wallOpenCornerMesh.Render();
			GL.Rotate(90, 0, 1, 0);
			GL.Translate(ps, 0, -ps);

			GL.Translate(-x, 0, -y);

			GL.End();
		}

		#endregion

		/// <summary>
		/// Render method
		/// </summary>
		public void Render()
		{

			for (int y = 0; y < Height; y++)
				for (int x = 0; x < Width; x++)
					switch (Fields[y][x])
					{

						case Objects.Wall:
							renderWall(x, y);
							break;

						case Objects.Point:
						case Objects.Powerup:
							double r = Fields[y][x] == Objects.Point ? 0.1 : 0.3;
							GL.PushMatrix();
							GL.Translate(x, 0.5, y);
							GL.Scale(r, r, r);
							sphereMesh.Render(PrimitiveType.Quads);
							GL.PopMatrix();
							break;

						case Objects.None:
						default:
							break;
					}
		}
	}
}
