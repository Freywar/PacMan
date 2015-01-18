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

		/// <summary>
		/// Eye rendering.
		/// </summary>
		/// <param name="x">Center X(map cells).</param>
		/// <param name="y">Center Y(map cells).</param>
		/// <param name="z">Center Z(map cells).</param>
		/// <param name="r">Radius(map cells).</param>
		/// <param name="pupilAlpha">Pupil alpha(radians)..</param>
		/// <param name="pupilBeta">Pupil beta(radians)..</param>
		/// <param name="pupilRad">Pupil radius(radians).</param>
		/// <param name="color">Color.</param>
		public void renderEye(double x, double y, double z, double r, double pupilAlpha, double pupilBeta, double pupilRad, Color color)
		{
			double angleStep = Math.PI / 10;

			GL.Translate(x, y, z);
			GL.Begin(PrimitiveType.Quads);
			for (double alpha = -Math.PI / 2; alpha < Math.PI / 2; alpha += angleStep)
				for (double beta = 0; beta < Math.PI * 2; beta += angleStep)
				{
					if (Geometry.Distance(alpha, beta, pupilAlpha, pupilBeta) < pupilRad)
						GL.Color3(Color.Black);
					else
						GL.Color3(color);
					GL.Normal3(Geometry.FromSpheric(alpha, beta, 1));
					GL.Vertex3(Geometry.FromSpheric(alpha, beta, r));


					if (Geometry.Distance(alpha+angleStep, beta, pupilAlpha, pupilBeta) < pupilRad)
						GL.Color3(Color.Black);
					else
						GL.Color3(color);
					GL.Normal3(Geometry.FromSpheric(alpha + angleStep, beta, 1));
					GL.Vertex3(Geometry.FromSpheric(alpha + angleStep, beta, r));


					if (Geometry.Distance(alpha+angleStep, beta+angleStep, pupilAlpha, pupilBeta) < pupilRad)
						GL.Color3(Color.Black);
					else
						GL.Color3(color);
					GL.Normal3(Geometry.FromSpheric(alpha + angleStep, beta + angleStep, 1));
					GL.Vertex3(Geometry.FromSpheric(alpha + angleStep, beta + angleStep, r));

					if (Geometry.Distance(alpha, beta+angleStep, pupilAlpha, pupilBeta) < pupilRad)
						GL.Color3(Color.Black);
					else
						GL.Color3(color);
					GL.Normal3(Geometry.FromSpheric(alpha, beta + angleStep, 1));
					GL.Vertex3(Geometry.FromSpheric(alpha, beta + angleStep, r));
				}

			GL.End();
			GL.Translate(-x, -y, -z);

		}

		/// <summary>
		/// Render.
		/// </summary>
		abstract public void Render();
	}
}
