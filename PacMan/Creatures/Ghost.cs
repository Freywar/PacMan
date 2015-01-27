using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
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
			/// <summary>
			/// Appear animation.
			/// </summary>
			AppearAnimation,
			/// <summary>
			/// Normal.
			/// </summary>
			Normal,
			/// <summary>
			/// Frightened.
			/// </summary>
			Frightened,
			/// <summary>
			/// Disappear animation.
			/// </summary>
			DisappearAnimation,
			/// <summary>
			/// Eaten.
			/// </summary>
			Eaten,
			/// <summary>
			/// Not in game.
			/// </summary>
			None
		}

		/// <summary>
		/// Radius(map cells).
		/// </summary>
		private const double radius = 0.45;
		/// <summary>
		/// Details count per 360 degrees or 1 map cell.
		/// </summary>
		private const int detailsCount = 20;
		/// <summary>
		/// Appear animation duration(seconds);
		/// </summary>
		private const double appearAnimationDuration = 2;
		/// <summary>
		/// Disappear animation duration(seconds);
		/// </summary>
		private const double disappearAnimationDuration = 0.5;

		private Mesh cap_v = null;
		private Mesh skirt_v = null;
		private ShaderProgram skirtProgram_v = null;
		private Color Color_v = Color.Red;

		/// <summary>
		/// Animation progress in [0..1].
		/// </summary>
		private double animationState = 0;
		/// <summary>
		/// Time elapsed in Waiting state(seconds).
		/// </summary>
		private double waitTimeElapsed = 0;
		/// <summary>
		/// Total time elapsed from level start(seconds).
		/// </summary>
		private double totalTimeElapsed = 0;

		/// <summary>
		/// Cap mesh.
		/// </summary>
		private Mesh cap
		{
			get
			{
				if (cap_v == null)
				{
					double step = Math.PI * 2 / detailsCount;
					int pointsCount = (int)(Math.PI / step) * (int)(Math.PI * 2 / step) * 4;
					int vp = 0;
					int np = 0;
					int cp = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];

					for (double alpha = -Math.PI / 2; alpha < Math.PI / 2; alpha += step)
						for (double beta = 0; beta < Math.PI * 2; beta += step)
						{
							Vector3d normal = Utils.FromSpheric(alpha, beta, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(radius);
							Utils.Push(v, normal, ref vp);

							normal = Utils.FromSpheric(alpha + step, beta, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(radius);
							Utils.Push(v, normal, ref vp);

							normal = Utils.FromSpheric(alpha + step, beta + step, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(radius);
							Utils.Push(v, normal, ref vp);

							normal = Utils.FromSpheric(alpha, beta + step, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(radius);
							Utils.Push(v, normal, ref vp);
						}

					cap_v = new Mesh();
					cap_v.Vertices = v;
					cap_v.Normals = n;
				}

				return cap_v;
			}
		}
		/// <summary>
		/// Skirt mesh.
		/// </summary>
		private Mesh skirt
		{
			get
			{
				if (skirt_v == null)
				{

					double angleStep = Math.PI * 2.0 / detailsCount;
					double yStep = 2.0 / detailsCount;
					int pointsCount = (int)(0.5 / yStep) * (int)(Math.PI * 2 / angleStep) * 4;
					int vp = 0;
					int np = 0;
					int cp = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];

					for (double dy = 0; dy < 0.5; dy += yStep)
						for (double beta = 0; beta < Math.PI * 2; beta += angleStep)
						{
							Utils.Push(v, new Vector3d(beta, dy, 0), ref vp);
							Utils.Push(v, new Vector3d(beta + angleStep, dy, 0), ref vp);
							Utils.Push(v, new Vector3d(beta + angleStep, dy + yStep, 0), ref vp);
							Utils.Push(v, new Vector3d(beta, dy + yStep, 0), ref vp);
						}

					skirt_v = new Mesh();
					skirt_v.Vertices = v;
				}

				return skirt_v;
			}
		}

		private ShaderProgram skirtProgram
		{
			get
			{
				if (skirtProgram_v == null)
					skirtProgram_v = new ShaderProgram("Shaders\\GhostSkirt_Vert.glsl", "Shaders\\GhostSkirt_Frag.glsl");
				return skirtProgram_v;
			}
		}

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

			y = 4 * y * y * 0.2;

			double delta = 0;
			if (State == States.AppearAnimation)
			{
				if (animationState < 0.25)
					delta = -y * Utils.NormSin(animationState * 4);
				if (animationState >= 0.25 && animationState < 0.5)
					delta = -y * (1 - Utils.NormSin(animationState * 4 - 1));
				if (animationState >= 0.5 && animationState < 0.75)
					delta = y * Utils.NormSin(animationState * 4 - 2);
				if (animationState >= 0.75 && animationState < 1)
					delta = y * (1 - Utils.NormSin(animationState * 4 - 3));
			}

			if (State == States.DisappearAnimation)
			{
				if (animationState < 0.5)
					delta = y * Utils.NormSin(animationState * 2);
				else
					delta = y * (1 - Utils.NormSin(animationState * 2 - 1));
			}

			return (
			Math.Cos(beta * 4 + totalTimeElapsed) +
			Math.Cos(beta * 8 + totalTimeElapsed * 2) +
			Math.Cos(beta * 16 + totalTimeElapsed * 4) +
			Math.Cos(beta * 32 + totalTimeElapsed * 8)
			) * y / 4 + delta;
		}

		#region Path detection.

		private int[][][] distanceMap = null;

		private void fillDistanceMapRec(Map map, int x, int z, int y, int distance, bool afterLift = false)
		{
			if ((distanceMap[y][z][x] != -1 && distanceMap[y][z][x] <= distance) || !map.IsWalkable(y, z, x))
				return;

			distanceMap[y][z][x] = distance;

			if ((map[y, z, x] == Map.Objects.LiftUp || map[y, z, x] == Map.Objects.LiftDown) && !afterLift)
				distanceMap[y][z][x] = -1;

			if (map[y, z, x] == Map.Objects.LiftUp && !afterLift)
				fillDistanceMapRec(map, x, z, y + 1, distance + 1, true);
			else if (map[y, z, x] == Map.Objects.LiftDown && !afterLift)
				fillDistanceMapRec(map, x, z, y - 1, distance + 1, true);
			else
			{
				fillDistanceMapRec(map, x, map.WrapZ(z - 1), y, distance + 1);
				fillDistanceMapRec(map, x, map.WrapZ(z + 1), y, distance + 1);
				fillDistanceMapRec(map, map.WrapX(x - 1), z, y, distance + 1);
				fillDistanceMapRec(map, map.WrapX(x + 1), z, y, distance + 1);
			}

		}

		private void fillDistanceMap(Map map, Vector3i target)
		{
			if (distanceMap == null || distanceMap.Length != map.Depth || distanceMap[0].Length != map.Width)
			{
				distanceMap = new int[map.Height][][];
				for (int y = 0; y < distanceMap.Length; y++)
				{
					distanceMap[y] = new int[map.Depth][];
					for (int z = 0; z < distanceMap[y].Length; z++)
						distanceMap[y][z] = new int[map.Width];
				}
			}

			for (int y = 0; y < map.Height; y++)
				for (int z = 0; z < map.Depth; z++)
					for (int x = 0; x < map.Width; x++)
						distanceMap[y][z][x] = -1;

			fillDistanceMapRec(map, target.X, target.Z, target.Y, 0);
		}

		/// <summary>
		/// Choose best direction to reach target.
		/// </summary>
		/// <param name="map">Map.</param>
		/// <param name="target">Target.</param>
		/// <returns>Direction.</returns>
		private Directions chooseDirection(Map map, Vector3i target, Directions currentDirection)
		{
			fillDistanceMap(map, target);
			if (distanceMap[Y][(int)Z][(int)X] == -1)
				currentDirection = cw(currentDirection);

			int px = (int)map.WrapX(X);
			int py = Y;
			int pz = (int)map.WrapZ(Z);
			int bestDistance = distanceMap[py][pz][px];

			pz = (int)map.WrapZ(Z - 1);
			if (distanceMap[py][pz][px] != -1 && distanceMap[py][pz][px] < bestDistance)
			{
				currentDirection = Directions.Up;
				bestDistance = distanceMap[py][pz][px];
			}

			pz = (int)map.WrapX(Z + 1);
			if (distanceMap[py][pz][px] != -1 && distanceMap[py][pz][px] < bestDistance)
			{
				currentDirection = Directions.Down;
				bestDistance = distanceMap[py][pz][px];
			}

			pz = (int)map.WrapZ(Z);

			px = (int)map.WrapX(X - 1);
			if (distanceMap[py][pz][px] != -1 && distanceMap[py][pz][px] < bestDistance)
			{
				currentDirection = Directions.Left;
				bestDistance = distanceMap[py][pz][px];
			}

			px = (int)map.WrapX(X + 1);
			if (distanceMap[py][pz][px] != -1 && distanceMap[py][pz][px] < bestDistance)
			{
				currentDirection = Directions.Right;
				bestDistance = distanceMap[py][pz][px];
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
			Vector3i target;
			switch (State)
			{
				case States.Waiting:
				case States.AppearAnimation:
				case States.Normal:
					target = new Vector3i((int)pacman.X, pacman.Y, (int)pacman.Z);
					break;
				case States.Frightened:
					target = new Vector3i((int)pacman.X, pacman.Y, (int)pacman.Z);
					fillDistanceMap(map, target);
					double maxDistance = 0;
					for (int y = 0; y < map.Height; y++)
						for (int z = 0; z < map.Depth; z++)
							for (int x = 0; x < map.Width; x++)
								if (distanceMap[y][z][x] > maxDistance)
								{
									target = new Vector3i(x, y, z);
									maxDistance = distanceMap[y][z][x];
								}
					break;
				case States.Eaten:
					target = map.GhostStart;
					break;
				default:
					target = new Vector3i((int)X, Y, (int)Z);
					break;
			}

			Direction = chooseDirection(map, target, Direction);
		}

		/// <summary>
		/// Name.
		/// </summary>
		public String Name = "Ghost";
		/// <summary>
		/// Color.
		/// </summary>
		public Color Color
		{
			get
			{
				return Color_v;
			}
			set
			{
				Color_v = value;
				cap_v = null;
			}
		}
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
		public override double CurrentSpeed
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

		public override void Init(Map map)
		{
			State = States.None;
			animationState = 0;
			waitTimeElapsed = 0;
			totalTimeElapsed = 0;
			X = map.GhostStart.X;
			Z = map.GhostStart.Z;
		}

		public override Vector3i? Update(double dt, Map map)
		{
			throw new NotSupportedException("PacMan required.");
		}

		/// <summary>
		/// Position and direction update.
		/// </summary>
		/// <param name="dt">Time passed from last call(seconds).</param>
		/// <param name="map">Map.</param>
		/// <param name="pacman">PacMan.</param>
		/// <returns>Visited cell center or null if none visited.</returns>
		public Vector3i? Update(double dt, Map map, PacMan pacman)
		{
			totalTimeElapsed += dt;

			Vector3i? result = null;

			switch (State)
			{
				case States.Waiting:
					waitTimeElapsed += dt;
					if (waitTimeElapsed >= Delay)
					{
						updateDirection(map, pacman);
						State = States.AppearAnimation;
					}

					break;
				case States.AppearAnimation:
					animationState += dt / appearAnimationDuration;
					if (animationState >= 1)
					{
						State = States.Normal;
						animationState = 0;
					}
					break;
				case States.Normal:
				case States.Frightened:
				case States.Eaten:
					if (pacman.State != PacMan.States.Normal && pacman.State != PacMan.States.Super)
						break;

					double dtAfterMove;
					while ((dtAfterMove = moveToClosestCenter(dt, map)) != dt)
					{
						result = new Vector3i((int)X, Y, (int)Z);

						updateDirection(map, pacman);
						dt = dtAfterMove;
					}
					if (Math.Floor(X) == X && Math.Floor(Z) == Z)
						updateDirection(map, pacman);
					if (dt > 0)
						moveRemainingCellPart(dt, map);
					break;
				case States.DisappearAnimation:
					animationState += dt / disappearAnimationDuration;
					if (animationState >= 1)
					{
						State = States.None;
						animationState = 0;
					}
					break;
			}

			return result;
		}

		public override void Render()
		{
			if (State == States.Waiting || State == States.None)
				return;

			GL.PushMatrix();
			GL.Translate(X, Y, Z);
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

			if (State == States.AppearAnimation)
			{
				GL.Rotate(360 * Utils.NormSin(animationState), 0, 1, 0);
				if (animationState < 0.5)
					GL.Translate(0, -1 + 1.5 * Utils.NormSin(animationState * 2), 0);
				else
					GL.Translate(0, 0.5 * (1 - Utils.NormSin(animationState * 2 - 1)), 0);
			}
			if (State == States.DisappearAnimation)
				GL.Translate(0, -Utils.NormSin(animationState), 0);


			if (State != States.Eaten)
			{
				Color color = State == States.Frightened ? Color.LightBlue : Color;

				ShaderProgram.StaticColor.Enable();

				ShaderProgram.StaticColor.SetUniform("meshColor",
					new Vector4(color.R / (float)255.0, color.G / (float)255.0, color.B / (float)255.0, (float)1.0));
				cap.Render();

				ShaderProgram.StaticColor.Disable();


				skirtProgram.Enable();

				skirtProgram.SetUniform("meshColor",
					new Vector4(color.R / (float)255.0, color.G / (float)255.0, color.B / (float)255.0, (float)1.0));



				skirtProgram.SetUniform("radius", (float)radius);
				skirtProgram.SetUniform("yStep", (float)0.1);

				double delta = 0;
				if (State == States.AppearAnimation)
				{
					if (animationState < 0.25)
						delta = -1 * Utils.NormSin(animationState * 4);
					if (animationState >= 0.25 && animationState < 0.5)
						delta = -1 * (1 - Utils.NormSin(animationState * 4 - 1));
					if (animationState >= 0.5 && animationState < 0.75)
						delta = 1 * Utils.NormSin(animationState * 4 - 2);
					if (animationState >= 0.75 && animationState < 1)
						delta = 1 * (1 - Utils.NormSin(animationState * 4 - 3));
				}

				if (State == States.DisappearAnimation)
				{
					if (animationState < 0.5)
						delta = 1 * Utils.NormSin(animationState * 2);
					else
						delta = 1 * (1 - Utils.NormSin(animationState * 2 - 1));
				}
				skirtProgram.SetUniform("delta", (float)delta);
				skirtProgram.SetUniform("totalTimeElapsed", (float)totalTimeElapsed);
				skirt.Render();

				skirtProgram.Disable();
			}

			ShaderProgram.Default.Enable();

			double s = Math.Sin(Math.PI / 6), c = Math.Cos(Math.PI / 6);
			GL.PushMatrix();
			GL.Translate(s * radius, s * c * radius, c * c * radius);
			GL.Rotate(-90, 0, 1, 0);
			eye.Render();
			GL.PopMatrix();

			GL.PushMatrix();
			GL.Translate(-s * radius, s * c * radius, c * c * radius);
			GL.Rotate(-90, 0, 1, 0);
			eye.Render();
			GL.PopMatrix();

			ShaderProgram.Default.Disable();

			GL.PopMatrix();
		}

		public override void Dispose()
		{
			base.Dispose();
			if (cap_v != null)
				cap_v.Dispose();
			if (skirt_v != null)
				skirt_v.Dispose();
			if (skirtProgram_v != null)
				skirtProgram_v.Dispose();
		}
	}
}
