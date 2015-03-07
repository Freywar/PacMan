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

		public new class States : GameObject.States
		{
			public States(string value) : base(value) { }

			public static readonly States Waiting = new States("Waiting");
			public static readonly States Frightened = new States("Frightened");
			public static readonly States Eaten = new States("Eaten");
		}

		public new class Animations : GameObject.Animations
		{
			public Animations(string value, double duration, GameObject.States result) : base(value, duration, result) { }

			public static readonly new Animations Appear = new Animations("Appear", 2, States.Normal);
			public static readonly new Animations LiftUp = new Animations("LiftUp", 2, null);
			public static readonly new Animations LiftDown = new Animations("LiftDown", 2, null);
			public static readonly Animations ToNormal = new Animations("ToNormal", 0, States.Normal);
			public static readonly Animations ToWaiting = new Animations("ToWaiting", 0, States.Waiting);
			public static readonly Animations ToFrightened = new Animations("ToFrightened", 0, States.Frightened);
			public static readonly Animations ToEaten = new Animations("ToEaten", 0, States.Eaten);
		}

		/// <summary>
		/// Radius(map cells).
		/// </summary>
		private const double radius = 0.45;
		/// <summary>
		/// Details count per 360 degrees or 1 map cell.
		/// </summary>
		private const int detailsCount = 20;

		private Mesh cap_v = null;
		private Mesh skirt_v = null;
		private ShaderProgram skirtProgram_v = null;
		private Color Color_v = Color.Red;

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

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];

					for (double alpha = -Math.PI / 2; alpha < Math.PI / 2; alpha += step)
						for (double beta = 0; beta < Math.PI * 2; beta += step)
						{
							Vector3d normal = Utils.FromSpheric(alpha, beta, 1);
							Utils.Push(n, normal, ref np);
							Utils.Push(v, Vector3d.Multiply(normal, radius), ref vp);

							normal = Utils.FromSpheric(alpha + step, beta, 1);
							Utils.Push(n, normal, ref np);
							Utils.Push(v, Vector3d.Multiply(normal, radius), ref vp);

							normal = Utils.FromSpheric(alpha + step, beta + step, 1);
							Utils.Push(n, normal, ref np);
							Utils.Push(v, Vector3d.Multiply(normal, radius), ref vp);

							normal = Utils.FromSpheric(alpha, beta + step, 1);
							Utils.Push(n, normal, ref np);
							Utils.Push(v, Vector3d.Multiply(normal, radius), ref vp);
						}

					cap_v = new Mesh();
					cap_v.Vertex = v;
					cap_v.Normal = n;
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
					skirt_v.Vertex = v;
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
			if (Animation == Animations.Appear)
			{
				if (animationProgress < 0.25)
					delta = -y * Utils.NormSin(animationProgress * 4);
				if (animationProgress >= 0.25 && animationProgress < 0.5)
					delta = -y * (1 - Utils.NormSin(animationProgress * 4 - 1));
				if (animationProgress >= 0.5 && animationProgress < 0.75)
					delta = y * Utils.NormSin(animationProgress * 4 - 2);
				if (animationProgress >= 0.75 && animationProgress < 1)
					delta = y * (1 - Utils.NormSin(animationProgress * 4 - 3));
			}

			if (Animation == Animations.Disappear)
			{
				if (animationProgress < 0.5)
					delta = y * Utils.NormSin(animationProgress * 2);
				else
					delta = y * (1 - Utils.NormSin(animationProgress * 2 - 1));
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
			if (distanceMap[Floor][(int)Z][(int)X] == -1)
				currentDirection = cw(currentDirection);

			int px = (int)map.WrapX(X);
			int py = Floor;
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

		/// <summary>
		/// New direction selection on crossroads.
		/// </summary>
		/// <param name="map">Map.</param>
		/// <param name="pacman">PacMan.</param>
		override protected void updateDirection(Map map, Creature pacman)
		{
			Vector3i target;
			if (State == States.Waiting || State == States.Normal)
				target = new Vector3i((int)pacman.X, pacman.Floor, (int)pacman.Z);

			else if (State == States.Frightened)
			{

				target = new Vector3i((int)pacman.X, pacman.Floor, (int)pacman.Z);
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
			}
			else if (State == States.Eaten)
				target = map.GhostStart;
			else
				target = new Vector3i((int)X, Floor, (int)Z);

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
				if (State == States.Normal)
					return Speed;
				if (State == States.Frightened)
					return FrightenedSpeed;
				if (State == States.Eaten)
					return EatenSpeed;
				return 0;
			}
		}

		public override void Init(Map map)
		{
			base.Init();
			waitTimeElapsed = 0;
			totalTimeElapsed = 0;
			X = map.GhostStart.X;
			Floor = map.GhostStart.Y;
			Z = map.GhostStart.Z;
		}


		/// <summary>
		/// Position and direction update.
		/// </summary>
		/// <param name="dt">Time passed from last call(seconds).</param>
		/// <param name="map">Map.</param>
		/// <param name="pacman">PacMan.</param>
		/// <returns>Visited cell center or null if none visited.</returns>
		override public Vector3i? Update(double dt, Map map, Creature pacman)
		{
			totalTimeElapsed += dt;
			if (!IsAnimated && State == States.Waiting)
			{
				waitTimeElapsed += dt;
				if (waitTimeElapsed >= Delay)
				{
					updateDirection(map, pacman);
					Animate(Animations.Appear);
					return null;
				}
				else
					return base.Update(dt, map, pacman);
			}
			else
				return base.Update(dt, map, pacman);
		}

		public override void Render()
		{
			if (!IsAnimated && (State == States.Waiting || State == States.None))
				return;

			GL.PushMatrix();
			GL.Translate(X, Floor, Z);
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

			if (Animation == Animations.Appear)
			{
				GL.Rotate(360 * Utils.NormSin(animationProgress), 0, 1, 0);
				if (animationProgress < 0.5)
					GL.Translate(0, -1 + 1.5 * Utils.NormSin(animationProgress * 2), 0);
				else
					GL.Translate(0, 0.5 * (1 - Utils.NormSin(animationProgress * 2 - 1)), 0);
			}
			if (Animation == Animations.LiftUp)
			{
				if (animationProgress < 0.5)
					GL.Translate(0, -1 + 1.5 * Utils.NormSin(animationProgress * 2), 0);
				else
					GL.Translate(0, 0.5 * (1 - Utils.NormSin(animationProgress * 2 - 1)), 0);
			}
			if (Animation == Animations.LiftDown)
				GL.Translate(0, 1 - Utils.NormSin(animationProgress), 0);
			if (Animation == Animations.Disappear)
				GL.Translate(0, -Utils.NormSin(animationProgress), 0);


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
				if (Animation == Animations.Appear || Animation == Animations.LiftUp)
				{
					if (animationProgress < 0.25)
						delta = -1 * Utils.NormSin(animationProgress * 4);
					if (animationProgress >= 0.25 && animationProgress < 0.5)
						delta = -1 * (1 - Utils.NormSin(animationProgress * 4 - 1));
					if (animationProgress >= 0.5 && animationProgress < 0.75)
						delta = 1 * Utils.NormSin(animationProgress * 4 - 2);
					if (animationProgress >= 0.75 && animationProgress < 1)
						delta = 1 * (1 - Utils.NormSin(animationProgress * 4 - 3));
				}

				if (Animation == Animations.Disappear || Animation == Animations.LiftDown)
				{
					if (animationProgress < 0.5)
						delta = 1 * Utils.NormSin(animationProgress * 2);
					else
						delta = 1 * (1 - Utils.NormSin(animationProgress * 2 - 1));
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
