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
	/// PacMan class.
	/// </summary>
	class PacMan : Creature
	{
		/// <summary>
		/// PacMan states.
		/// </summary>
		public enum States
		{
			AppearAnimation,
			/// <summary>
			/// Normal.
			/// </summary>
			Normal,
			/// <summary>
			/// Super.
			/// </summary>
			Super,
			DisappearAnimation,
			None
		}

		private const double maxMouthAngle = Math.PI / 4;
		private const double radius = 0.45;

		private Mesh body_v = null;
		private Mesh body
		{
			get
			{
				if (body_v == null)
				{
					Vector3d color = new Vector3d(Color.Yellow.R / 255.0, Color.Yellow.G / 255.0, Color.Yellow.B / 255.0);
					double r = radius;

					double yAngleStep = Math.PI / 10;
					double xAngleStep = (Math.PI - maxMouthAngle) / 10;
					int pointsCount = 896;
					int vp = 0;
					int np = 0;
					int cp = 0;

					double[] v = new double[pointsCount * 3];
					double[] n = new double[pointsCount * 3];
					double[] c = new double[pointsCount * 3];

					for (double yAngle = -Math.PI / 2; yAngle < Math.PI / 2; yAngle += yAngleStep)
						for (double xAngle = maxMouthAngle; xAngle <= Math.PI * 2 - maxMouthAngle; xAngle += xAngleStep)
						{
							Vector3d normal = Utils.FromSpheric(yAngle, xAngle, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(r);
							Utils.Push(v, normal, ref vp);

							normal = Utils.FromSpheric(yAngle + yAngleStep, xAngle, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(r);
							Utils.Push(v, normal, ref vp);

							normal = Utils.FromSpheric(yAngle + yAngleStep, xAngle + xAngleStep, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(r);
							Utils.Push(v, normal, ref vp);

							normal = Utils.FromSpheric(yAngle, xAngle + xAngleStep, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(r);
							Utils.Push(v, normal, ref vp);
						}
					for (int i = 0; i < pointsCount; i++)
						Utils.Push(c, color, ref cp);

					body_v = new Mesh();
					body_v.Vertices = v;
					body_v.Normals = n;
					body_v.Colors = c;
				}
				return body_v;
			}
		}

		private Mesh jaw_v = null;
		private Mesh jaw
		{
			get
			{
				if (jaw_v == null)
				{

					double r = radius;

					double yAngleStep = Math.PI / 10;
					double xAngleStep = maxMouthAngle / 10;
					int pointsCount = 480;
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
						Utils.Push(v, Utils.FromSpheric(yAngle + yAngleStep, 0, r), ref vp);
						Utils.Push(v, Utils.FromSpheric(yAngle, 0, r), ref vp);
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
							normal.Mult(r);
							Utils.Push(v, normal, ref vp);

							normal = Utils.FromSpheric(yAngle + yAngleStep, xAngle, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(r);
							Utils.Push(v, normal, ref vp);

							normal = Utils.FromSpheric(yAngle + yAngleStep, xAngle + xAngleStep, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(r);
							Utils.Push(v, normal, ref vp);

							normal = Utils.FromSpheric(yAngle, xAngle + xAngleStep, 1);
							Utils.Push(n, normal, ref np);
							normal.Mult(r);
							Utils.Push(v, normal, ref vp);
						}
					for (int i = cp / 3; i < pointsCount; i++)
						Utils.Push(c, color, ref cp);

					jaw_v = new Mesh();
					jaw_v.Vertices = v;
					jaw_v.Normals = n;
					jaw_v.Colors = c;
				}
				return jaw_v;
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
		/// State.
		/// </summary>
		public States State = States.Normal;
		/// <summary>
		/// Remaining time in Super state.
		/// </summary>
		public double SuperTime = 0;
		public double AnimationState = 0;

		private const double animationDuration = 0.5;

		/// <summary>
		/// Init on level start.
		/// </summary>
		/// <param name="map">Map.</param>
		public override void Init(Map map)
		{
			State = States.None;
			AnimationState = 0;
			X = map.PacManStart.X;
			Y = map.PacManStart.Y;
		}

		protected override void updateDirection(Map map)
		{
			if (desiredDirection == Direction)
				return;

			switch (desiredDirection)
			{
				case Directions.None:
					break;
				case Directions.Up:
					if (Y > 0 && map[(int)(Y - 1)][(int)X] != Map.Objects.Wall)
						Direction = desiredDirection;
					else if (Y == 0 && map[map.Height - 1][(int)X] != Map.Objects.Wall)
						Direction = desiredDirection;
					break;
				case Directions.Down:
					if (Y < map.Height - 1 && map[(int)(Y + 1)][(int)X] != Map.Objects.Wall)
						Direction = desiredDirection;
					else if (Y == map.Height - 1 && map[0][(int)X] != Map.Objects.Wall)
						Direction = desiredDirection;
					break;

				case Directions.Left:
					if (X > 0 && map[(int)Y][(int)(X - 1)] != Map.Objects.Wall)
						Direction = desiredDirection;
					else if (X == 0 && map[(int)Y][map.Width - 1] != Map.Objects.Wall)
						Direction = desiredDirection;
					break;
				case Directions.Right:
					if (X < map.Width - 1 && map[(int)Y][(int)(X + 1)] != Map.Objects.Wall)
						Direction = desiredDirection;
					else if (X == map.Width - 1 && map[(int)Y][0] != Map.Objects.Wall)
						Direction = desiredDirection;
					break;
			}
		}

		public override Point Update(double dt, Map map)
		{
			if (State == States.AppearAnimation)
			{
				AnimationState += dt / animationDuration;
				if (AnimationState >= 1)
				{
					State = States.Normal;
					AnimationState = 0;
				}
				return Point.Empty;
			}
			if (State == States.DisappearAnimation)
			{
				AnimationState += dt / animationDuration;
				if (AnimationState >= 1)
					State = States.None;
				return Point.Empty;
			}
			return base.Update(dt, map);
		}

		/// <summary>
		/// Key press handling.
		/// </summary>
		/// <param name="keyboard">Pressed key.</param>
		public void KeyDown(Key key)
		{
			if (State == States.AppearAnimation || State == States.DisappearAnimation)
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
			if (State == States.None)
				return;

			double mouthAngle = Math.Max(Math.Abs(X - Math.Round(X)), Math.Abs(Y - Math.Round(Y)));
			mouthAngle *= 2 * Math.PI / 2;
			mouthAngle = Math.Sin(mouthAngle);
			mouthAngle *= maxMouthAngle;

			double angleStep = Math.PI / 10;
			double smallerAngleStep = (mouthAngle > Math.PI / 2 && mouthAngle < 3 * Math.PI / 2) ? angleStep : (Math.PI / 2 - mouthAngle) / 5;

			GL.Translate(X, 0.5, Y);
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

			GL.PushMatrix();
			if (State == States.AppearAnimation)
				GL.Scale(AnimationState, AnimationState, AnimationState);
			if (State == States.DisappearAnimation)
				GL.Scale(1 - AnimationState, 1 - AnimationState, 1 - AnimationState);

			GL.Rotate(-mouthAngle * 180 / Math.PI, 0, 1, 0);
			jaw.Render();
			GL.Rotate(mouthAngle * 180 / Math.PI, 0, 1, 0);

			body.Render();

			GL.Rotate(mouthAngle * 180 / Math.PI, 0, 1, 0);
			GL.Rotate(180, 1, 0, 0);
			jaw.Render();
			GL.Rotate(-180, 1, 0, 0);
			GL.Rotate(-mouthAngle * 180 / Math.PI, 0, 1, 0);

			GL.Translate(Math.Cos(Math.PI / 6 + mouthAngle) * Math.Cos(Math.PI / 6) * radius, Math.Sin(Math.PI / 6) * radius, Math.Sin(Math.PI / 6 + mouthAngle) * Math.Cos(Math.PI / 6) * radius);
			eye.Render();
			GL.Translate(-Math.Cos(Math.PI / 6 + mouthAngle) * Math.Cos(Math.PI / 6) * radius, -Math.Sin(Math.PI / 6) * radius, -Math.Sin(Math.PI / 6 + mouthAngle) * Math.Cos(Math.PI / 6) * radius);

			GL.Translate(Math.Cos(Math.PI / 6 + mouthAngle) * Math.Cos(-Math.PI / 6) * radius, Math.Sin(-Math.PI / 6) * radius, Math.Sin(Math.PI / 6 + mouthAngle) * Math.Cos(-Math.PI / 6) * radius);
			eye.Render();
			GL.Translate(-Math.Cos(Math.PI / 6 + mouthAngle) * Math.Cos(-Math.PI / 6) * radius, -Math.Sin(-Math.PI / 6) * radius, -Math.Sin(Math.PI / 6 + mouthAngle) * Math.Cos(-Math.PI / 6) * radius);

			GL.PopMatrix();

			GL.Rotate(90, 1, 0, 0);

			switch (Direction)
			{
				case Directions.Down:
					GL.Rotate(90, 0, 1, 0);
					break;
				case Directions.Up:
					GL.Rotate(-90, 0, 1, 0);
					break;
				case Directions.Left:
					GL.Rotate(-180, 0, 1, 0);
					break;
				case Directions.Right:
					GL.Rotate(0, 0, 1, 0);
					break;
			}

			GL.Translate(-X, -0.5, -Y);
		}
	}
}
