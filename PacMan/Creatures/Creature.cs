using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;

namespace PacMan
{
	/// <summary>
	/// Creature class
	/// </summary>
	abstract class Creature
	{
		/// <summary>
		/// Moving directions.
		/// </summary>
		public enum Directions
		{
			/// <summary>
			/// Left.
			/// </summary>
			Left,
			/// <summary>
			/// Up/Against camera.
			/// </summary>
			Up,
			/// <summary>
			/// Right.
			/// </summary>
			Right,
			/// <summary>
			/// Down/Towards camera.
			/// </summary>
			Down,
			/// <summary>
			/// Standing still.
			/// </summary>
			None
		}

		/// <summary>
		/// Next direction on clockwise order.
		/// </summary>
		/// <param name="dir">Current direction.</param>
		/// <returns>Next direction.</returns>
		protected static Directions cw(Directions dir)
		{
			switch (dir)
			{
				case Directions.Left:
					return Directions.Up;
				case Directions.Up:
					return Directions.Right;
				case Directions.Right:
					return Directions.Down;
				case Directions.Down:
					return Directions.Left;
				case Directions.None:
				default:
					return Directions.Left;
			}
		}
		/// <summary>
		/// Next direction on counter-clockwise order.
		/// </summary>
		/// <param name="dir">Current direction.</param>
		/// <returns>Next direction.</returns>
		protected static Directions ccw(Directions dir)
		{
			switch (dir)
			{
				case Directions.Left:
					return Directions.Down;
				case Directions.Up:
					return Directions.Left;
				case Directions.Right:
					return Directions.Up;
				case Directions.Down:
					return Directions.Right;
				case Directions.None:
				default:
					return Directions.Left;
			}
		}

		/// <summary>
		/// X coordinate in map cells.
		/// </summary>
		public double X = 0;
		/// <summary>
		/// Y coordinate in map cells.
		/// </summary>
		public double Y = 0;
		/// <summary>
		/// Moving speed in map cells per second.
		/// </summary>
		public double Speed = 0;
		/// <summary>
		/// Moving direction.
		/// </summary>
		public Directions Direction = Directions.None;

		/// <summary>
		/// Current speed.
		/// </summary>
		virtual protected double CurrentSpeed { get { return Speed; } }
		/// <summary>
		/// Check if creature is in cell center.
		/// </summary>
		protected bool IsInCenter
		{
			get
			{
				return Math.Floor(X) == X && Math.Floor(Y) == Y;
			}
		}

		/// <summary>
		/// New direction selection on crossroads.
		/// </summary>
		/// <param name="map">Map</param>
		abstract protected void updateDirection(Map map);
		/// <summary>
		/// Move to closest cell center in current direction if possible(excluding current position if it is center).
		/// </summary>
		/// <param name="dt">Available time(seconds).</param>
		/// <param name="map">Map.</param>
		/// <returns>Time remaining after move.</returns>
		virtual protected double moveToClosestCenter(double dt, Map map)
		{
			double dx = 0, dy = 0;
			switch (Direction)
			{
				case Directions.Left:
					dx = IsInCenter ? -1 : Math.Floor(X) - X;
					break;
				case Directions.Right:
					dx = IsInCenter ? 1 : Math.Ceiling(X) - X;
					break;
				case Directions.Up:
					dy = IsInCenter ? -1 : Math.Floor(Y) - Y;
					break;
				case Directions.Down:
					dy = IsInCenter ? 1 : Math.Ceiling(Y) - Y;
					break;

				case Directions.None:
				default:
					return dt;
			}

			double distance = Math.Abs(dx) + Math.Abs(dy);
			if (dt * CurrentSpeed < distance || !map.IsWalkable(Y + dy, X + dx))
				dx = dy = distance = 0;

			if (distance > 0)
			{
				X = map.WrapX(X + dx);
				Y = map.WrapY(Y + dy);
			}

			return dt - distance / CurrentSpeed;
		}
		/// <summary>
		/// Move any distance available in defined time in current direction.
		/// </summary>
		/// <param name="dt">Available time(seconds).</param>
		/// <param name="map">Map.</param>
		virtual protected void moveRemainingCellPart(double dt, Map map)
		{
			double m = 0;
			switch (Direction)
			{
				case Directions.None:
					break;
				case Directions.Up:
				case Directions.Down:
					m = Direction == Directions.Up ? -1 : 1;
					if (!IsInCenter || map.IsWalkable(Y + m, X))
						Y = map.WrapY(Y + dt * CurrentSpeed * m);
					break;

				case Directions.Left:
				case Directions.Right:
					m = Direction == Directions.Left ? -1 : 1;
					if (!IsInCenter || map.IsWalkable(Y, X + m))
						X = map.WrapX(X + dt * CurrentSpeed * m);
					break;
			}
		}

		/// <summary>
		/// Initialization on level start.
		/// </summary>
		/// <param name="map">Map</param>
		abstract public void Init(Map map);
		/// <summary>
		/// Position and direction update.
		/// </summary>
		/// <param name="dt">Time passed from last call(seconds).</param>
		/// <param name="map">Map.</param>
		/// <returns>Visited cell center or Point.Empty.</returns>
		virtual public Point Update(double dt, Map map)
		{
			Point result = Point.Empty;
			double dtAfterMove;
			while ((dtAfterMove = moveToClosestCenter(dt, map)) != dt)
			{
				result = new Point((int)X, (int)Y);
				updateDirection(map);
				dt = dtAfterMove;
			}
			if (IsInCenter)
			{
				result = new Point((int)X, (int)Y);
				updateDirection(map);
			}
			if (dt > 0)
				moveRemainingCellPart(dt, map);
			return result;
		}

		private Mesh eye_v = null;
		protected Mesh eye
		{
			get
			{
				if (eye_v == null)
				{
					Vector3d whiteColor = new Vector3d(1, 1, 1);
					Vector3d blackColor = new Vector3d(0, 0, 0);
					double r = 0.1;

					double step = Math.PI / 30;
					int pointsCount = 7564;
					int vp = 0;
					int np = 0;
					int cp = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];
					double[] c = new double[pointsCount * 3];

					for (double alpha = -Math.PI / 2; alpha < Math.PI / 2; alpha += step)
						for (double beta = -Math.PI; beta < Math.PI; beta += step)
						{
							Vector3d color = Utils.Distance(alpha, beta, 0, 0) < Math.PI / 6 ? blackColor : whiteColor;
							Vector3d normal = Utils.FromSpheric(alpha, beta, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(r);
							Utils.Push(v, normal, ref vp);
							Utils.Push(c, color, ref cp);

							color = Utils.Distance(alpha + step, beta, 0, 0) < Math.PI / 6 ? blackColor : whiteColor;
							normal = Utils.FromSpheric(alpha + step, beta, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(r);
							Utils.Push(v, normal, ref vp);
							Utils.Push(c, color, ref cp);

							color = Utils.Distance(alpha + step, beta + step, 0, 0) < Math.PI / 6 ? blackColor : whiteColor;
							normal = Utils.FromSpheric(alpha + step, beta + step, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(r);
							Utils.Push(v, normal, ref vp);
							Utils.Push(c, color, ref cp);

							color = Utils.Distance(alpha, beta + step, 0, 0) < Math.PI / 6 ? blackColor : whiteColor;
							normal = Utils.FromSpheric(alpha, beta + step, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(r);
							Utils.Push(v, normal, ref vp);
							Utils.Push(c, color, ref cp);
						}

					eye_v = new Mesh();
					eye_v.Vertices = v;
					eye_v.Normals = n;
					eye_v.Colors = c;
				}

				return eye_v;
			}
		}

		/// <summary>
		/// Render.
		/// </summary>
		abstract public void Render();
	}
}
