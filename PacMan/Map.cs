using System;
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
		public double WrapX(double X)
		{
			if (X > Height)
				return X - Height;
			if (X < 0)
				return X + Height;
			return X;
		}
		public double WrapY(double Y)
		{
			if (Y > Height)
				return Y - Height;
			if (Y < 0)
				return Y + Height;
			return Y;
		}


		public int WrapX(int X)
		{
			if (X > Height)
				return X - Height;
			if (X < 0)
				return X + Height;
			return X;
		}
		public int WrapY(int Y)
		{
			if (Y > Height)
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


		public bool IsWalkable(int y, int x)
		{
			return this[y, x] != Objects.Wall;
		}

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

		/// <summary>
		/// Render method
		/// </summary>
		public void Render()
		{


			GL.Begin(PrimitiveType.Quads);
			GL.Color3(Color.Black);
			GL.Normal3(0.0, 1.0, 0.0);
			GL.Vertex3(-0.5, 0, -0.5);
			GL.Vertex3(-0.5, 0, Height + 0.5);
			GL.Vertex3(Width + 0.5, 0, Height + 0.5);
			GL.Vertex3(Width + 0.5, 0, -0.5);
			GL.End();

			for (int y = 0; y < Height; y++)
				for (int x = 0; x < Width; x++)
					switch (Fields[y][x])
					{

						case Objects.Wall:




							GL.Translate(x, 0, y);
							GL.Begin(PrimitiveType.Quads);
							GL.Color3(Color.DarkBlue);

							GL.Normal3(0.0, 1.0, 0.0);
							GL.Vertex3(-0.5, 1.0, -0.5);
							GL.Vertex3(-0.5, 1.0, 0.5);
							GL.Vertex3(0.5, 1.0, 0.5);
							GL.Vertex3(0.5, 1.0, -0.5);

							GL.Normal3(1.0, 0.0, 0.0);
							GL.Vertex3(0.5, 0.0, -0.5);
							GL.Vertex3(0.5, 1.0, -0.5);
							GL.Vertex3(0.5, 1.0, 0.5);
							GL.Vertex3(0.5, 0.0, 0.5);


							GL.Normal3(-1.0, 0.0, 0.0);
							GL.Vertex3(-0.5, 0.0, -0.5);
							GL.Vertex3(-0.5, 0.0, 0.5);
							GL.Vertex3(-0.5, 1.0, 0.5);
							GL.Vertex3(-0.5, 1.0, -0.5);


							GL.Normal3(0.0, 0.0, 1.0);
							GL.Vertex3(0.5, 0.0, 0.5);
							GL.Vertex3(0.5, 1.0, 0.5);
							GL.Vertex3(-0.5, 1.0, 0.5);
							GL.Vertex3(-0.5, 0.0, 0.5);



							GL.Normal3(0.0, 0.0, -1.0);
							GL.Vertex3(0.5, 0.0, -0.5);
							GL.Vertex3(-0.5, 0.0, -0.5);
							GL.Vertex3(-0.5, 1.0, -0.5);
							GL.Vertex3(0.5, 1.0, -0.5);





							GL.End();
							GL.Translate(-x, 0, -y);
							break;

						case Objects.Point:

							GL.Translate(x, 0, y);
							GL.Begin(PrimitiveType.Quads);

							for (double alpha = -Math.PI / 2; alpha < Math.PI / 2; alpha += Math.PI / 10)
								for (double beta = 0; beta < Math.PI * 2; beta += Math.PI / 10)
								{
									GL.Color3(Color.White);
									GL.Normal3(Math.Cos(alpha) * Math.Cos(beta), Math.Sin(alpha), Math.Cos(alpha) * Math.Sin(beta));
									GL.Vertex3(Math.Cos(alpha) * Math.Cos(beta) * 0.1, 0.5 + Math.Sin(alpha) * 0.1, Math.Cos(alpha) * Math.Sin(beta) * 0.1);
									GL.Vertex3(Math.Cos(alpha + Math.PI / 10) * Math.Cos(beta) * 0.1, 0.5 + Math.Sin(alpha + Math.PI / 10) * 0.1, Math.Cos(alpha + Math.PI / 10) * Math.Sin(beta) * 0.1);
									GL.Vertex3(Math.Cos(alpha + Math.PI / 10) * Math.Cos(beta + Math.PI / 10) * 0.1, 0.5 + Math.Sin(alpha + Math.PI / 10) * 0.1, Math.Cos(alpha + Math.PI / 10) * Math.Sin(beta + Math.PI / 10) * 0.1);
									GL.Vertex3(Math.Cos(alpha) * Math.Cos(beta + Math.PI / 10) * 0.1, 0.5 + Math.Sin(alpha) * 0.1, Math.Cos(alpha) * Math.Sin(beta + Math.PI / 10) * 0.1);


								}

							GL.End();
							GL.Translate(-x, 0, -y);

							break;

						case Objects.Powerup:

							GL.Translate(x, 0, y);
							GL.Begin(PrimitiveType.Quads);

							for (double alpha = -Math.PI / 2; alpha < Math.PI / 2; alpha += Math.PI / 10)
								for (double beta = 0; beta < Math.PI * 2; beta += Math.PI / 10)
								{
									GL.Color3(Color.White);
									GL.Normal3(Math.Cos(alpha) * Math.Cos(beta), Math.Sin(alpha), Math.Cos(alpha) * Math.Sin(beta));
									GL.Vertex3(Math.Cos(alpha) * Math.Cos(beta) * 0.3, 0.5 + Math.Sin(alpha) * 0.3, Math.Cos(alpha) * Math.Sin(beta) * 0.3);
									GL.Vertex3(Math.Cos(alpha + Math.PI / 10) * Math.Cos(beta) * 0.3, 0.5 + Math.Sin(alpha + Math.PI / 10) * 0.3, Math.Cos(alpha + Math.PI / 10) * Math.Sin(beta) * 0.3);
									GL.Vertex3(Math.Cos(alpha + Math.PI / 10) * Math.Cos(beta + Math.PI / 10) * 0.3, 0.5 + Math.Sin(alpha + Math.PI / 10) * 0.3, Math.Cos(alpha + Math.PI / 10) * Math.Sin(beta + Math.PI / 10) * 0.3);
									GL.Vertex3(Math.Cos(alpha) * Math.Cos(beta + Math.PI / 10) * 0.3, 0.5 + Math.Sin(alpha) * 0.3, Math.Cos(alpha) * Math.Sin(beta + Math.PI / 10) * 0.3);


								}

							GL.End();
							GL.Translate(-x, 0, -y);

							break;

						case Objects.None:
						default:
							break;
					}
		}
	}
}
