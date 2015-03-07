using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;

namespace PacMan
{
	/// <summary>
	/// PacMan class.
	/// </summary>
	class PacMan : Creature
	{
		public new class States : GameObject.States
		{
			public States(string value) : base(value) { }

			public static readonly States Super = new States("Super");
		}

		public new class Animations : GameObject.Animations
		{
			public Animations(string value, double duration, GameObject.States result) : base(value, duration, result) { }

			public static readonly Animations ToSuper = new Animations("ToSuper", 0, States.Super);
			public static readonly Animations ToNormal = new Animations("ToNormal", 0, States.Normal);
		}

		/// <summary>
		/// Max opened mouth angle(radians);
		/// </summary>
		private const double maxMouthAngle = Math.PI / 4;
		/// <summary>
		/// Radius(map cells).
		/// </summary>
		private const double radius = 0.45;
		/// <summary>
		/// Details count per 360 degrees or 1 map cell.
		/// </summary>
		private const int detailsCount = 20;

		private Mesh body_v = null;
		private Mesh jaw_v = null;
		private Mesh evilEye_v = null;

		/// <summary>
		/// Body mesh.
		/// </summary>
		private Mesh body
		{
			get
			{
				if (body_v == null)
				{
					Vector3d color = new Vector3d(Color.Yellow.R / 255.0, Color.Yellow.G / 255.0, Color.Yellow.B / 255.0);

					double yAngleStep = Math.PI * 2.0 / detailsCount;
					double xAngleStep = (Math.PI - maxMouthAngle) * 2.0 / detailsCount;
					int pointsCount = (int)(Math.PI / yAngleStep) * (int)(2.0 * (Math.PI - maxMouthAngle) / xAngleStep + 1.0) * 4;
					int vp = 0;
					int np = 0;
					int cp = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];
					double[] c = new double[pointsCount * 3];

					for (double yAngle = -Math.PI / 2; yAngle < Math.PI / 2; yAngle += yAngleStep)
						for (double xAngle = maxMouthAngle; xAngle < Math.PI * 2 - maxMouthAngle; xAngle += xAngleStep)
						{
							Vector3d normal = Utils.FromSpheric(yAngle, xAngle, 1);
							Utils.Push(n, normal, ref np);
							Utils.Push(v, Vector3d.Multiply(normal, radius), ref vp);

							normal = Utils.FromSpheric(yAngle + yAngleStep, xAngle, 1);
							Utils.Push(n, normal, ref np);
							Utils.Push(v, Vector3d.Multiply(normal, radius), ref vp);

							normal = Utils.FromSpheric(yAngle + yAngleStep, xAngle + xAngleStep, 1);
							Utils.Push(n, normal, ref np);
							Utils.Push(v, Vector3d.Multiply(normal, radius), ref vp);

							normal = Utils.FromSpheric(yAngle, xAngle + xAngleStep, 1);
							Utils.Push(n, normal, ref np);
							Utils.Push(v, Vector3d.Multiply(normal, radius), ref vp);
						}
					for (int i = 0; i < pointsCount; i++)
						Utils.Push(c, color, ref cp);

					body_v = new Mesh();
					body_v.Vertex = v;
					body_v.Normal = n;
					body_v.Color = c;
				}
				return body_v;
			}
		}
		/// <summary>
		/// Jaw mesh.
		/// </summary>
		private Mesh jaw
		{
			get
			{
				if (jaw_v == null)
				{
					double yAngleStep = Math.PI * 2 / detailsCount;
					double xAngleStep = maxMouthAngle * 2 / detailsCount;
					int pointsCount = (int)(Math.PI / yAngleStep) * 4 + (int)(Math.PI / yAngleStep * maxMouthAngle / xAngleStep) * 4;
					int vp = 0;
					int np = 0;
					int cp = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];
					double[] c = new double[pointsCount * 3];

					Vector3d normal = new Vector3d(0, 0, -1);
					Vector3d color = new Vector3d(Color.DarkRed.R / 255.0, Color.DarkRed.G / 255.0, Color.DarkRed.B / 255.0);
					for (double yAngle = -Math.PI / 2; yAngle < Math.PI / 2; yAngle += yAngleStep)
					{
						Utils.Push(v, new Vector3d(0, 0, 0), ref vp);
						Utils.Push(v, Utils.FromSpheric(yAngle + yAngleStep, 0, radius), ref vp);
						Utils.Push(v, Utils.FromSpheric(yAngle, 0, radius), ref vp);
						Utils.Push(v, new Vector3d(0, 0, 0), ref vp);
						for (int i = 0; i < 4; i++)
						{
							Utils.Push(n, normal, ref np);
							Utils.Push(c, color, ref cp);
						}
					}

					color = new Vector3d(Color.Yellow.R / 255.0, Color.Yellow.G / 255.0, Color.Yellow.B / 255.0);
					for (double yAngle = -Math.PI / 2; yAngle < Math.PI / 2; yAngle += yAngleStep)
						for (double xAngle = 0; xAngle < maxMouthAngle; xAngle += xAngleStep)
						{
							normal = Utils.FromSpheric(yAngle, xAngle, 1);
							Utils.Push(n, normal, ref np);
							Utils.Push(v, Vector3d.Multiply(normal, radius), ref vp);

							normal = Utils.FromSpheric(yAngle + yAngleStep, xAngle, 1);
							Utils.Push(n, normal, ref np);
							Utils.Push(v, Vector3d.Multiply(normal, radius), ref vp);

							normal = Utils.FromSpheric(yAngle + yAngleStep, xAngle + xAngleStep, 1);
							Utils.Push(n, normal, ref np);
							Utils.Push(v, Vector3d.Multiply(normal, radius), ref vp);

							normal = Utils.FromSpheric(yAngle, xAngle + xAngleStep, 1);
							Utils.Push(n, normal, ref np);
							Utils.Push(v, Vector3d.Multiply(normal, radius), ref vp);
						}
					for (int i = cp / 3; i < pointsCount; i++)
						Utils.Push(c, color, ref cp);

					jaw_v = new Mesh();
					jaw_v.Vertex = v;
					jaw_v.Normal = n;
					jaw_v.Color = c;
				}
				return jaw_v;
			}
		}
		/// <summary>
		/// Red eyes for super state.
		/// </summary>
		protected Mesh evilEye
		{
			get
			{
				if (evilEye_v == null)
				{
					double[] v = new double[eye.Vertex.Length];
					double[] n = new double[eye.Normal.Length];
					double[] c = new double[eye.Color.Length];

					for (int i = 0; i < v.Length; i++)
					{
						v[i] = eye.Vertex[i];
						n[i] = eye.Normal[i];
					}
					for (int i = 0; i < c.Length; i += 3)
					{
						c[i] = eye.Color[i];
						c[i + 1] = 0;
						c[i + 2] = 0;
					}

					evilEye_v = new Mesh();
					evilEye_v.Vertex = v;
					evilEye_v.Normal = n;
					evilEye_v.Color = c;
				}
				return evilEye_v;
			}
		}

		/// <summary>
		/// Direction defined by user input, will be applied on next crossroad.
		/// </summary>
		protected Creature.Directions desiredDirection = Creature.Directions.Up;

		/// <summary>
		/// Lives count.
		/// </summary>
		public int Lives = 3;

		/// <summary>
		/// Remaining time in Super state.
		/// </summary>
		public double SuperTime = 0;

		public override void Init(Map map)
		{
			base.Init();
			X = map.PacManStart.X;
			Floor = map.PacManStart.Y;
			Z = map.PacManStart.Z;
		}

		protected override void updateDirection(Map map, Creature target)
		{
			if (desiredDirection == Direction)
				return;

			switch (desiredDirection)
			{
				case Directions.None:
					break;
				case Directions.Up:
					if (Z > 0 && map[Floor][(int)(Z - 1)][(int)X] != Map.Objects.Wall)
						Direction = desiredDirection;
					else if (Z == 0 && map[Floor][map.Depth - 1][(int)X] != Map.Objects.Wall)
						Direction = desiredDirection;
					break;
				case Directions.Down:
					if (Z < map.Depth - 1 && map[Floor][(int)(Z + 1)][(int)X] != Map.Objects.Wall)
						Direction = desiredDirection;
					else if (Z == map.Depth - 1 && map[Floor][0][(int)X] != Map.Objects.Wall)
						Direction = desiredDirection;
					break;

				case Directions.Left:
					if (X > 0 && map[Floor][(int)Z][(int)(X - 1)] != Map.Objects.Wall)
						Direction = desiredDirection;
					else if (X == 0 && map[Floor][(int)Z][map.Width - 1] != Map.Objects.Wall)
						Direction = desiredDirection;
					break;
				case Directions.Right:
					if (X < map.Width - 1 && map[Floor][(int)Z][(int)(X + 1)] != Map.Objects.Wall)
						Direction = desiredDirection;
					else if (X == map.Width - 1 && map[Floor][(int)Z][0] != Map.Objects.Wall)
						Direction = desiredDirection;
					break;
			}
		}

		/// <summary>
		/// Key press handling.
		/// </summary>
		/// <param name="keyboard">Pressed key.</param>
		public void KeyDown(Key key)
		{
			if (Animation == Animations.Appear || Animation == Animations.Disappear)
				return;

			if (key == Key.Up)
				desiredDirection = Creature.Directions.Up;
			if (key == Key.Down)
				desiredDirection = Creature.Directions.Down;
			if (key == Key.Left)
				desiredDirection = Creature.Directions.Left;
			if (key == Key.Right)
				desiredDirection = Creature.Directions.Right;
		}

		/// <summary>
		/// Key release handling.
		/// </summary>
		/// <param name="keyboard">REleased key.</param>
		public void KeyUp(Key key)
		{
		}

		public override void Render()
		{
			if (State == States.None && Animation == Animations.None)
				return;


			double mouthAngle = Math.Max(Math.Abs(X - Math.Round(X)), Math.Abs(Z - Math.Round(Z)));
			mouthAngle = Math.Sin(mouthAngle * Math.PI) * maxMouthAngle;

			GL.PushMatrix();
			GL.Translate(X, Y, Z);
			switch (Direction)
			{
				case Directions.Down:
					GL.Rotate(-90, 0, 1, 0);
					break;
				case Directions.Up:
					GL.Rotate(90, 0, 1, 0);
					break;
				case Directions.Left:
					GL.Rotate(180, 0, 1, 0);
					break;
				case Directions.Right:
					GL.Rotate(0, 0, 1, 0);
					break;
			}
			GL.Rotate(-90, 1, 0, 0);

			if (Animation == Animations.Appear)
				GL.Scale(animationProgress, animationProgress, animationProgress);
			if (Animation == Animations.Disappear)
				GL.Scale(1 - animationProgress, 1 - animationProgress, 1 - animationProgress);


			ShaderProgram.Default.Enable();

			GL.Rotate(-mouthAngle * 180 / Math.PI, 0, 1, 0);
			jaw.Render();
			GL.Rotate(mouthAngle * 180 / Math.PI, 0, 1, 0);

			ShaderProgram.Default.Disable();


			ShaderProgram.StaticColor.Enable();

			ShaderProgram.StaticColor.SetUniform("meshColor",
			new Vector4(Color.Yellow.R / (float)255.0, Color.Yellow.G / (float)255.0, Color.Yellow.B / (float)255.0, (float)1.0));

			body.Render();

			ShaderProgram.StaticColor.Disable();


			ShaderProgram.Default.Enable();

			GL.PushMatrix();
			GL.Rotate(mouthAngle * 180 / Math.PI, 0, 1, 0);
			GL.Rotate(180, 1, 0, 0);
			jaw.Render();
			GL.PopMatrix();

			double cc = Math.Cos(Math.PI / 6 + mouthAngle),
				cs = Math.Sin(Math.PI / 6 + mouthAngle),
				lc = Math.Cos(Math.PI / 6),
				ls = Math.Sin(Math.PI / 6);

			Mesh currentEye = State == States.Super ? evilEye : eye;
			GL.Translate(cc * lc * radius, ls * radius, cs * lc * radius);
			currentEye.Render();
			GL.Translate(-cc * lc * radius, -ls * radius, -cs * lc * radius);

			GL.Translate(cc * lc * radius, -ls * radius, cs * lc * radius);
			currentEye.Render();
			GL.Translate(-cc * lc * radius, ls * radius, -cs * lc * radius);

			ShaderProgram.Default.Disable();


			GL.PopMatrix();
		}

		public override void Dispose()
		{
			base.Dispose();
			if (body_v != null)
				body_v.Dispose();
			if (jaw_v != null)
				jaw_v.Dispose();
		}
	}
}
