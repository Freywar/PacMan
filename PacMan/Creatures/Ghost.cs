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
	/// Ghost class.
	/// </summary>
	class Ghost : Creature
	{
		/// <summary>
		/// Ghost states.
		/// </summary>
		public enum States
		{
			/// <summary>
			/// Waiting outside map.
			/// </summary>
			Waiting,
			AppearAnimation,
			/// <summary>
			/// Normal.
			/// </summary>
			Normal,
			/// <summary>
			/// Frightened.
			/// </summary>
			Frightened,
			DisappearAnimation,
			/// <summary>
			/// Eaten.
			/// </summary>
			Eaten,
			None
		}

		private const double appearAnimationDuration = 2;
		private const double disappearAnimationDuration = 0.5;

		public double AnimationState = 0;

		private Mesh createCap(Color col)
		{
			Vector3d color = new Vector3d(col.R / 255.0, col.G / 255.0, col.B / 255.0);
			double r = 0.45;

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

			Mesh res = new Mesh();
			res.Vertices = v;
			res.Normals = n;
			res.Colors = c;
			return res;
		}

		private Mesh cap_v = null;
		private Mesh cap
		{
			get
			{
				if (cap_v == null)
				{
					cap_v = createCap(Color);
				}

				return cap_v;
			}
		}

		private Mesh frigtenedCap_v = null;
		private Mesh frigtenedCap
		{
			get
			{
				if (frigtenedCap_v == null)
				{
					frigtenedCap_v = createCap(Color.LightBlue);
				}

				return frigtenedCap_v;
			}
		}

		/// <summary>
		/// Name.
		/// </summary>
		public String Name = "Ghost";
		/// <summary>
		/// Color.
		/// </summary>
		public Color Color = Color.Red;
		/// <summary>
		/// Delay before appearance after level start or death(seconds).
		/// </summary>
		public double Delay = 0;
		/// <summary>
		/// State.
		/// </summary>
		public States State = States.Normal;
		/// <summary>
		/// Frightened speed.
		/// </summary>
		public double FrightenedSpeed = 1;
		/// <summary>
		/// Eaten speed.
		/// </summary>
		public double EatenSpeed = 1;
		protected override double CurrentSpeed
		{
			get
			{
				switch (State)
				{
					case States.Normal:
						return Speed;
					case States.Frightened:
						return FrightenedSpeed;
					case States.Eaten:
						return EatenSpeed;
					case States.AppearAnimation:
					case States.Waiting:
					case States.DisappearAnimation:
					case States.None:
					default:
						return 0;
				}
			}
		}

		private double waitedTime = 0;

		private double rAnimationTime = 0;
		/// <summary>
		/// Calculate skirt fluctuation.
		/// </summary>
		/// <param name="y">Offset from bottom.</param>
		/// <param name="beta">Offset by angle.</param>
		/// <returns>Radius delta.</returns>
		private double DR(double y, double beta)
		{
			if (y <= 0)
				return 0;
			y = y * 2;
			y = y * y * 0.2;
			double delta = 0;

			if (State == States.AppearAnimation)
			{
				if (AnimationState < 0.25)
					delta = -y * Utils.NormSin(AnimationState * 4);
				if (AnimationState >= 0.25 && AnimationState < 0.5)
					delta = -y * (1 - Utils.NormSin(AnimationState * 4 - 1));
				if (AnimationState >= 0.5 && AnimationState < 0.75)
					delta = y * Utils.NormSin(AnimationState * 4 - 2);
				if (AnimationState >= 0.75 && AnimationState < 1)
					delta = y * (1 - Utils.NormSin(AnimationState * 4 - 3));
			}

			if (State == States.DisappearAnimation)
			{
				if (AnimationState < 0.5)
					delta = y * Utils.NormSin(AnimationState * 2);
				else
					delta = y * (1 - Utils.NormSin(AnimationState * 2 - 1));
			}

			return (
			Math.Cos(beta * 4 + rAnimationTime) * y +
			Math.Cos(beta * 8 + rAnimationTime * 2) * y +
			Math.Cos(beta * 16 + rAnimationTime * 4) * y +
			Math.Cos(beta * 32 + rAnimationTime * 8) * y
			) / 4 + delta;
		}

		#region Path detection.

		private int[][] distanceMap = null;

		private void fillDistanceMapRec(Map map, int x, int y, int distance)
		{
			if ((distanceMap[y][x] != -1 && distanceMap[y][x] <= distance) || !map.IsWalkable(y, x))
				return;

			distanceMap[y][x] = distance;

			fillDistanceMapRec(map, x, map.WrapY(y - 1), distance + 1);
			fillDistanceMapRec(map, x, map.WrapY(y + 1), distance + 1);
			fillDistanceMapRec(map, map.WrapX(x - 1), y, distance + 1);
			fillDistanceMapRec(map, map.WrapX(x + 1), y, distance + 1);

		}

		private void fillDistanceMap(Map map, Point target)
		{
			if (distanceMap == null || distanceMap.Length != map.Height || distanceMap[0].Length != map.Width)
			{
				distanceMap = new int[map.Height][];
				for (int y = 0; y < distanceMap.Length; y++)
					distanceMap[y] = new int[map.Width];
			}

			for (int y = 0; y < map.Height; y++)
				for (int x = 0; x < map.Width; x++)
					distanceMap[y][x] = -1;

			fillDistanceMapRec(map, target.X, target.Y, 0);
		}

		/// <summary>
		/// Choose best direction to reach target.
		/// </summary>
		/// <param name="map">Map.</param>
		/// <param name="target">Target.</param>
		/// <returns>Direction.</returns>
		private Directions chooseDirection(Map map, Point target, Directions currentDirection)
		{
			fillDistanceMap(map, target);
			if (distanceMap[(int)Y][(int)X] == -1)
				currentDirection = cw(currentDirection);

			int px = (int)map.WrapX(X);
			int py = (int)map.WrapY(Y);
			int bestDistance = distanceMap[py][px];

			py = (int)map.WrapY(Y - 1);
			if (distanceMap[py][px] != -1 && distanceMap[py][px] < bestDistance)
			{
				currentDirection = Directions.Up;
				bestDistance = distanceMap[py][px];
			}

			py = (int)map.WrapX(Y + 1);
			if (distanceMap[py][px] != -1 && distanceMap[py][px] < bestDistance)
			{
				currentDirection = Directions.Down;
				bestDistance = distanceMap[py][px];
			}

			py = (int)map.WrapY(Y);

			px = (int)map.WrapX(X - 1);
			if (distanceMap[py][px] != -1 && distanceMap[py][px] < bestDistance)
			{
				currentDirection = Directions.Left;
				bestDistance = distanceMap[py][px];
			}

			px = (int)map.WrapX(X + 1);
			if (distanceMap[py][px] != -1 && distanceMap[py][px] < bestDistance)
			{
				currentDirection = Directions.Right;
				bestDistance = distanceMap[py][px];
			}

			return currentDirection;
		}

		#endregion

		protected override void updateDirection(Map map)
		{
			throw new NotSupportedException("PacMan required.");
		}

		/// <summary>
		/// New direction selection on crossroads.
		/// </summary>
		/// <param name="map">Map.</param>
		/// <param name="pacman">PacMan.</param>
		protected void updateDirection(Map map, PacMan pacman)
		{
			Point target;
			switch (State)
			{
				case States.Waiting:
				case States.AppearAnimation:
				case States.Normal:
					target = new Point((int)pacman.X, (int)pacman.Y);
					break;
				case States.Frightened:
					target = new Point((int)pacman.X, (int)pacman.Y);
					fillDistanceMap(map, target);
					double maxDistance = 0;
					for (int y = 0; y < map.Height; y++)
						for (int x = 0; x < map.Width; x++)
							if (distanceMap[y][x] > maxDistance)
							{
								target = new Point(x, y);
								maxDistance = distanceMap[y][x];
							}
					break;
				case States.Eaten:
					target = map.GhostStart;
					break;
				default:
					target = new Point((int)X, (int)Y);
					break;
			}

			Direction = chooseDirection(map, target, Direction);
		}

		public override void Init(Map map)
		{
			State = States.Waiting;
			waitedTime = 0;
			AnimationState = 0;
			X = map.GhostStart.X;
			Y = map.GhostStart.Y;
		}

		public override Point Update(double dt, Map map)
		{
			throw new NotSupportedException("PacMan required.");
		}
		/// <summary>
		/// Position and direction update.
		/// </summary>
		/// <param name="dt">Time passed from last call(seconds).</param>
		/// <param name="map">Map.</param>
		/// <param name="pacman">PacMan.</param>
		/// <returns>Visited cell center or Point.Empty.</returns>
		public Point Update(double dt, Map map, PacMan pacman)
		{
			rAnimationTime += dt;

			Point result = Point.Empty;

			//return result;

			switch (State)
			{
				case States.Waiting:
					waitedTime += dt;
					if (waitedTime >= Delay)
					{
						updateDirection(map, pacman);
						State = States.AppearAnimation;
					}

					break;
				case States.AppearAnimation:
					AnimationState += dt / appearAnimationDuration;
					if (AnimationState >= 1)
					{
						State = States.Normal;
						AnimationState = 0;
					}
					break;
				case States.Normal:
				case States.Frightened:
				case States.Eaten:
					double dtAfterMove;
					while ((dtAfterMove = moveToClosestCenter(dt, map)) != dt)
					{
						result = new Point((int)X, (int)Y);

						updateDirection(map, pacman);
						dt = dtAfterMove;
					}
					if (Math.Floor(X) == X && Math.Floor(Y) == Y)
						updateDirection(map, pacman);
					if (dt > 0)
						moveRemainingCellPart(dt, map);
					break;
				case States.DisappearAnimation:
					AnimationState += dt / disappearAnimationDuration;
					if (AnimationState >= 1)
					{
						State = States.None;
						AnimationState = 0;
					}
					break;
			}

			return result;
		}

		private void renderSkirtPoint(double dy, double beta, double r, double yStep)
		{
			double prevDr = DR(dy - yStep, beta);
			double dr = DR(dy, beta);
			double alpha = Math.Atan2(yStep, dr - prevDr);

			GL.Normal3(Math.Cos(beta) * Math.Sin(alpha), Math.Cos(alpha), Math.Sin(beta) * Math.Sin(alpha));
			GL.Vertex3(Math.Cos(beta) * (r + dr), -dy, Math.Sin(beta) * (r + dr));
		}

		/// <summary>
		/// Render.
		/// </summary>
		public override void Render()
		{
			//return;

			if (State == States.Waiting || State == States.None)
				return;

			GL.Translate(X, 0.5, Y);
			switch (Direction)
			{
				case Directions.Down:
					break;
				case Directions.Up:
					GL.Rotate(180, 0, 1, 0);
					break;
				case Directions.Left:
					GL.Rotate(-90, 0, 1, 0);
					break;
				case Directions.Right:
					GL.Rotate(90, 0, 1, 0);
					break;
			}

			GL.PushMatrix();
			if (State == States.AppearAnimation)
			{
				GL.Rotate(360 * Utils.NormSin(AnimationState), 0, 1, 0);
				if (AnimationState < 0.5)
					GL.Translate(0, -1 + 1.5 * Utils.NormSin(AnimationState * 2), 0);
				else
					GL.Translate(0, 0.5 * (1 - Utils.NormSin(AnimationState * 2 - 1)), 0);
			}
			if (State == States.DisappearAnimation)
				GL.Translate(0, -Utils.NormSin(AnimationState), 0);

			double r = 0.45;
			double angleStep = Math.PI / 10;
			double yStep = 0.1;
			if (State != States.Eaten)
			{
				GL.Color3(State == States.Frightened ? Color.LightBlue : Color);

				if (State == States.Frightened)
					frigtenedCap.Render();
				else
					cap.Render();

				GL.Begin(PrimitiveType.Quads);

				//skirt
				for (double dy = 0; dy < 0.5; dy += 0.1)
					for (double beta = 0; beta < Math.PI * 2; beta += angleStep)
					{
						renderSkirtPoint(dy, beta, r, yStep);
						renderSkirtPoint(dy, beta + angleStep, r, yStep);
						renderSkirtPoint(dy + yStep, beta + angleStep, r, yStep);
						renderSkirtPoint(dy + yStep, beta, r, yStep);
					}
				GL.End();
			}

			GL.Translate(Math.Sin(Math.PI / 6) * r, Math.Sin(Math.PI / 6) * Math.Cos(Math.PI / 6) * r, Math.Cos(Math.PI / 6) * Math.Cos(Math.PI / 6) * r);
			GL.Rotate(-90, 0, 1, 0);
			eye.Render();
			GL.Rotate(90, 0, 1, 0);
			GL.Translate(-Math.Sin(Math.PI / 6) * r, -Math.Sin(Math.PI / 6) * Math.Cos(Math.PI / 6) * r, -Math.Cos(Math.PI / 6) * Math.Cos(Math.PI / 6) * r);

			GL.Translate(Math.Sin(-Math.PI / 6) * r, Math.Sin(Math.PI / 6) * Math.Cos(-Math.PI / 6) * r, Math.Cos(Math.PI / 6) * Math.Cos(-Math.PI / 6) * r);
			GL.Rotate(-90, 0, 1, 0);
			eye.Render();
			GL.Rotate(90, 0, 1, 0);
			GL.Translate(-Math.Sin(-Math.PI / 6) * r, -Math.Sin(Math.PI / 6) * Math.Cos(-Math.PI / 6) * r, -Math.Cos(Math.PI / 6) * Math.Cos(-Math.PI / 6) * r);


			GL.PopMatrix();

			switch (Direction)
			{
				case Directions.Down:
					break;
				case Directions.Up:
					GL.Rotate(-180, 0, 1, 0);
					break;
				case Directions.Left:
					GL.Rotate(90, 0, 1, 0);
					break;
				case Directions.Right:
					GL.Rotate(-90, 0, 1, 0);
					break;

			}
			GL.Translate(-X, -0.5, -Y);
		}
	}
}
