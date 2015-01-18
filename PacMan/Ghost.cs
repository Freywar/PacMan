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
	class Ghost : Creature
	{
		public enum States
		{
			Waiting,
			Normal,
			Frightened,
			Eaten
		}

		public String Name = "Ghost";
		public Color Color = Color.Red;
		public double Delay = 0;
		public States State = States.Normal;
		public double FrightenedSpeed = 1;
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
					case States.Waiting:
					default:
						return 0;
				}
			}
		}

		private double waitedTime = 0;
		private int[][] distanceMap = null;
		private double rAnimationTime = 0;

		private void fillDistanceMapRec(Map map, int x, int y, int distance)
		{
			if ((distanceMap[y][x] != -1 && distanceMap[y][x] <= distance) || map[y][x] == Map.Objects.Wall)
				return;

			distanceMap[y][x] = distance;

			if (y > 0)
				fillDistanceMapRec(map, x, y - 1, distance + 1);
			else
				fillDistanceMapRec(map, x, map.Height - 1, distance + 1);

			if (y < map.Height - 1)
				fillDistanceMapRec(map, x, y + 1, distance + 1);
			else
				fillDistanceMapRec(map, x, 0, distance + 1);


			if (x > 0)
				fillDistanceMapRec(map, x - 1, y, distance + 1);
			else
				fillDistanceMapRec(map, map.Width - 1, y, distance + 1);

			if (x < map.Width - 1)
				fillDistanceMapRec(map, x + 1, y, distance + 1);
			else
				fillDistanceMapRec(map, 0, y, distance + 1);
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

		protected override void updateDirection(Map map)
		{
			throw new NotImplementedException();
		}

		protected void updateDirection(Map map, PacMan pacman)
		{
			if (State == States.Waiting)
				return;



			Point target;
			switch (State)
			{
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

			fillDistanceMap(map, target);
			if (distanceMap[(int)Y][(int)X] == -1)
				Direction = cw(Direction);

			int bestDistance = distanceMap[(int)Y][(int)X];
			if (Y > 0)
			{
				if (distanceMap[(int)(Y - 1)][(int)X] != -1 && distanceMap[(int)(Y - 1)][(int)X] < bestDistance)
				{
					Direction = Directions.Up;
					bestDistance = distanceMap[(int)(Y - 1)][(int)X];
				}

			}
			else
				if (distanceMap[map.Height - 1][(int)X] != -1 && distanceMap[map.Height - 1][(int)X] < bestDistance)
				{
					Direction = Directions.Up;
					bestDistance = distanceMap[map.Height - 1][(int)X];
				}

			if (Y < map.Height - 1)
			{
				if (distanceMap[(int)(Y + 1)][(int)X] != -1 && distanceMap[(int)(Y + 1)][(int)X] < bestDistance)
				{
					Direction = Directions.Down;
					bestDistance = distanceMap[(int)(Y + 1)][(int)X];
				}

			}
			else
				if (distanceMap[0][(int)X] != -1 && distanceMap[0][(int)X] < bestDistance)
				{
					Direction = Directions.Down;
					bestDistance = distanceMap[0][(int)X];
				}

			if (X > 0)
			{
				if (distanceMap[(int)Y][(int)(X - 1)] != -1 && distanceMap[(int)Y][(int)(X - 1)] < bestDistance)
				{
					Direction = Directions.Left;
					bestDistance = distanceMap[(int)Y][(int)(X - 1)];
				}

			}
			else
				if (distanceMap[(int)Y][map.Width - 1] != -1 && distanceMap[(int)Y][map.Width - 1] < bestDistance)
				{
					Direction = Directions.Left;
					bestDistance = distanceMap[(int)Y][map.Width - 1];
				}

			if (X < map.Width - 1)
			{
				if (distanceMap[(int)Y][(int)(X + 1)] != -1 && distanceMap[(int)Y][(int)(X + 1)] < bestDistance)
				{
					Direction = Directions.Right;
					bestDistance = distanceMap[(int)Y][(int)(X + 1)];
				}

			}
			else
				if (distanceMap[(int)Y][0] != -1 && distanceMap[(int)Y][0] < bestDistance)
				{
					Direction = Directions.Right;
					bestDistance = distanceMap[(int)Y][0];
				}


		}

		public override void Init(Map map)
		{
			State = States.Waiting;
			waitedTime = 0;
			X = map.GhostStart.X;
			Y = map.GhostStart.Y;
		}

		public Point Update(double dt, Map map, PacMan pacman)
		{
			rAnimationTime += dt;

			Point result = Point.Empty;

			switch (State)
			{
				case States.Waiting:
					waitedTime += dt;
					if (waitedTime >= Delay)
						State = States.Normal;
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
			}

			return result;
		}

		public override void Render()
		{
			if (State == States.Waiting)
				return;
			

			GL.Translate(X, 0, Y);
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


			GL.Begin(PrimitiveType.Quads);
			double r = State == States.Eaten ? 0.1 : 0.45;
			if (State != States.Eaten)
			{
				//cap
				for (double alpha = 0; alpha < Math.PI / 2; alpha += Math.PI / 10)
					for (double beta = 0; beta < Math.PI * 2; beta += Math.PI / 10)
					{

						GL.Color3(State == States.Frightened ? Color.LightBlue : Color);
						GL.Normal3(Math.Cos(alpha) * Math.Cos(beta), Math.Sin(alpha), Math.Cos(alpha) * Math.Sin(beta));
						GL.Vertex3(Math.Cos(alpha) * Math.Cos(beta) * r, 0.5 + Math.Sin(alpha) * r, Math.Cos(alpha) * Math.Sin(beta) * r);
						GL.Vertex3(Math.Cos(alpha + Math.PI / 10) * Math.Cos(beta) * r, 0.5 + Math.Sin(alpha + Math.PI / 10) * r, Math.Cos(alpha + Math.PI / 10) * Math.Sin(beta) * r);
						GL.Vertex3(Math.Cos(alpha + Math.PI / 10) * Math.Cos(beta + Math.PI / 10) * r, 0.5 + Math.Sin(alpha + Math.PI / 10) * r, Math.Cos(alpha + Math.PI / 10) * Math.Sin(beta + Math.PI / 10) * r);
						GL.Vertex3(Math.Cos(alpha) * Math.Cos(beta + Math.PI / 10) * r, 0.5 + Math.Sin(alpha) * r, Math.Cos(alpha) * Math.Sin(beta + Math.PI / 10) * r);


					}




				//bottom
				for (double y = 0; y < 0.5; y += 0.1)
					for (double beta = 0; beta < Math.PI * 2; beta += Math.PI / 10)
					{

						GL.Color3(State == States.Frightened ? Color.LightBlue : Color);
						double dr = DR(y, beta);
						GL.Normal3(Math.Cos(beta), 0, Math.Sin(beta));
						dr = DR(y, beta);
						GL.Vertex3(Math.Cos(beta) * (r + dr), y, Math.Sin(beta) * (r + dr));
						dr = DR(y + 0.1, beta);
						GL.Vertex3(Math.Cos(beta) * (r + dr), (y + 0.1), Math.Sin(beta) * (r + dr));
						dr = DR(y + 0.1, beta + Math.PI / 10);
						GL.Vertex3(Math.Cos(beta + Math.PI / 10) * (r + dr), (y + 0.1), Math.Sin(beta + Math.PI / 10) * (r + dr));
						dr = DR(y, beta + Math.PI / 10);
						GL.Vertex3(Math.Cos(beta + Math.PI / 10) * (r + dr), y, Math.Sin(beta + Math.PI / 10) * (r + dr));


					}


				GL.End();
			}

			GL.Translate(0, 0.5, 0);

			//left eye
			GL.Translate(Math.Sin(Math.PI / 6) * 0.45, Math.Sin(Math.PI / 6 + 0) * Math.Cos(Math.PI / 6) * 0.45, Math.Cos(Math.PI / 6 + 0) * Math.Cos(Math.PI / 6) * 0.45);
			GL.Begin(PrimitiveType.Quads);
			for (double alpha = -Math.PI / 2; alpha < Math.PI / 2; alpha += Math.PI / 10)
				for (double beta = 0; beta < Math.PI * 2; beta += Math.PI / 10)
				{
					if (Math.Sqrt((alpha - Math.PI / 6 - 0) * (alpha - Math.PI / 6 - 0) + (beta - Math.PI / 2 + Math.PI / 6) * (beta - Math.PI / 2 + Math.PI / 6)) < Math.PI / 6)
						GL.Color3(Color.Black);
					else
						GL.Color3(Color.White);
					GL.Normal3(Math.Cos(alpha) * Math.Cos(beta), Math.Sin(alpha), Math.Cos(alpha) * Math.Sin(beta));
					GL.Vertex3(Math.Cos(alpha) * Math.Cos(beta) * 0.1, Math.Sin(alpha) * 0.1, Math.Cos(alpha) * Math.Sin(beta) * 0.1);
					GL.Vertex3(Math.Cos(alpha + Math.PI / 10) * Math.Cos(beta) * 0.1, Math.Sin(alpha + Math.PI / 10) * 0.1, Math.Cos(alpha + Math.PI / 10) * Math.Sin(beta) * 0.1);
					GL.Vertex3(Math.Cos(alpha + Math.PI / 10) * Math.Cos(beta + Math.PI / 10) * 0.1, Math.Sin(alpha + Math.PI / 10) * 0.1, Math.Cos(alpha + Math.PI / 10) * Math.Sin(beta + Math.PI / 10) * 0.1);
					GL.Vertex3(Math.Cos(alpha) * Math.Cos(beta + Math.PI / 10) * 0.1, Math.Sin(alpha) * 0.1, Math.Cos(alpha) * Math.Sin(beta + Math.PI / 10) * 0.1);


				}

			GL.End();
			GL.Translate(-Math.Sin(Math.PI / 6) * 0.45, -Math.Sin(Math.PI / 6 + 0) * Math.Cos(Math.PI / 6) * 0.45, -Math.Cos(Math.PI / 6 + 0) * Math.Cos(Math.PI / 6) * 0.45);


			//right eye
			GL.Translate(Math.Sin(-Math.PI / 6) * 0.45, Math.Sin(Math.PI / 6 + 0) * Math.Cos(-Math.PI / 6) * 0.45, Math.Cos(Math.PI / 6 + 0) * Math.Cos(-Math.PI / 6) * 0.45);
			GL.Begin(PrimitiveType.Quads);
			for (double alpha = -Math.PI / 2; alpha < Math.PI / 2; alpha += Math.PI / 10)
				for (double beta = 0; beta < Math.PI * 2; beta += Math.PI / 10)
				{
					if (Math.Sqrt((alpha - Math.PI / 6 - 0) * (alpha - Math.PI / 6 - 0) + (beta + Math.PI / 2) * (beta - Math.PI / 2 + Math.PI / 6)) < Math.PI / 6)
						GL.Color3(Color.Black);
					else
						GL.Color3(Color.White);
					GL.Normal3(Math.Cos(alpha) * Math.Cos(beta), Math.Sin(alpha), Math.Cos(alpha) * Math.Sin(beta));
					GL.Vertex3(Math.Cos(alpha) * Math.Cos(beta) * 0.1, Math.Sin(alpha) * 0.1, Math.Cos(alpha) * Math.Sin(beta) * 0.1);
					GL.Vertex3(Math.Cos(alpha + Math.PI / 10) * Math.Cos(beta) * 0.1, Math.Sin(alpha + Math.PI / 10) * 0.1, Math.Cos(alpha + Math.PI / 10) * Math.Sin(beta) * 0.1);
					GL.Vertex3(Math.Cos(alpha + Math.PI / 10) * Math.Cos(beta + Math.PI / 10) * 0.1, Math.Sin(alpha + Math.PI / 10) * 0.1, Math.Cos(alpha + Math.PI / 10) * Math.Sin(beta + Math.PI / 10) * 0.1);
					GL.Vertex3(Math.Cos(alpha) * Math.Cos(beta + Math.PI / 10) * 0.1, Math.Sin(alpha) * 0.1, Math.Cos(alpha) * Math.Sin(beta + Math.PI / 10) * 0.1);


				}
			GL.End();
			GL.Translate(-Math.Sin(-Math.PI / 6) * 0.45, -Math.Sin(Math.PI / 6 + 0) * Math.Cos(-Math.PI / 6) * 0.45, -Math.Cos(Math.PI / 6 + 0) * Math.Cos(-Math.PI / 6) * 0.45);

			GL.Translate(0, -0.5, 0);


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
			GL.Translate(-X, 0, -Y);
		}
		private double DR(double y, double beta)
		{
			return (
			Math.Cos(beta * 4 + rAnimationTime) * (y * 2 - 1) * (y * 2 - 1) * 0.2 +
			Math.Cos(beta * 8 + rAnimationTime * 2) * (y * 2 - 1) * (y * 2 - 1) * 0.2 +
			Math.Cos(beta * 16 + rAnimationTime * 4) * (y * 2 - 1) * (y * 2 - 1) * 0.2 +
			Math.Cos(beta * 32 + rAnimationTime * 8) * (y * 2 - 1) * (y * 2 - 1) * 0.2
			) / 4;
		}
	}

}
