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
			/// <summary>
			/// Normal.
			/// </summary>
			Normal,
			/// <summary>
			/// Super.
			/// </summary>
			Super
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

		/// <summary>
		/// Init on level start.
		/// </summary>
		/// <param name="map">Map.</param>
		public override void Init(Map map)
		{
			State = States.Normal;
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

		/// <summary>
		/// Key press handling.
		/// </summary>
		/// <param name="keyboard">Pressed key.</param>
		public void KeyDown(Key key)
		{
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
			double mouthOpen = Math.Max(Math.Abs(X - Math.Round(X)), Math.Abs(Y - Math.Round(Y)));
			mouthOpen *= 2 * Math.PI / 2;
			mouthOpen = Math.Sin(mouthOpen);
			mouthOpen *= Math.PI / 4;

			double angleStep = Math.PI / 10;
			double smallerAngleStep = (mouthOpen > Math.PI / 2 && mouthOpen < 3 * Math.PI / 2) ? angleStep : (Math.PI / 2 - mouthOpen) / 5;

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

			GL.Begin(PrimitiveType.Quads);

			//upper jaw
			GL.Color3(Color.DarkRed);
			for (double yAngle = -Math.PI / 2; yAngle < Math.PI / 2; yAngle += angleStep)
			{

				Vector3d jawAxis = new Vector3d(Math.Cos(mouthOpen), 0, Math.Sin(mouthOpen));
				Vector3d yAxis = new Vector3d(0, 1, 0);

				GL.Normal3(Vector3d.Cross(yAxis, jawAxis));

				GL.Vertex3(0, 0, 0);

				GL.Vertex3(Utils.FromSpheric(yAngle + angleStep, mouthOpen, 0.45));
				GL.Vertex3(Utils.FromSpheric(yAngle, mouthOpen, 0.45));

				GL.Vertex3(0, 0, 0);

			}

			//body
			GL.Color3(Color.Yellow);
			for (double yAngle = -Math.PI / 2; yAngle < Math.PI / 2; yAngle += angleStep)
				for (double xAngle = 0; xAngle <= Math.PI * 2; xAngle += angleStep)
				{
					double rxAngle = xAngle * (Math.PI * 2 - mouthOpen * 2) / (Math.PI * 2) + mouthOpen;
					double rxAngleNext = (xAngle + angleStep) * (Math.PI * 2 - mouthOpen * 2) / (Math.PI * 2) + mouthOpen;

					GL.Normal3(Utils.FromSpheric(yAngle, rxAngle, 1));
					GL.Vertex3(Utils.FromSpheric(yAngle, rxAngle, 0.45));

					GL.Normal3(Utils.FromSpheric(yAngle + angleStep, rxAngle, 1));
					GL.Vertex3(Utils.FromSpheric(yAngle + angleStep, rxAngle, 0.45));

					GL.Normal3(Utils.FromSpheric(yAngle + angleStep, rxAngleNext, 1));
					GL.Vertex3(Utils.FromSpheric(yAngle + angleStep, rxAngleNext, 0.45));

					GL.Normal3(Utils.FromSpheric(yAngle, rxAngleNext, 1));
					GL.Vertex3(Utils.FromSpheric(yAngle, rxAngleNext, 0.45));
				}

			//lower jaw
			GL.Color3(Color.DarkRed);
			for (double yAngle = -Math.PI / 2; yAngle < Math.PI / 2; yAngle += angleStep)
			{
				Vector3d jawAxis = new Vector3d(Math.Cos(-mouthOpen), 0, Math.Sin(-mouthOpen));
				Vector3d yAxis = new Vector3d(0, 1, 0);

				GL.Normal3(Vector3d.Cross(jawAxis, yAxis));
				GL.Vertex3(0, 0, 0);
				GL.Vertex3(Utils.FromSpheric(yAngle, -mouthOpen, 0.45));
				GL.Vertex3(Utils.FromSpheric(yAngle + angleStep, -mouthOpen, 0.45));
				GL.Vertex3(0, 0, 0);
			}
			GL.End();

			//eyes
			renderEye(Math.Cos(Math.PI / 6 + mouthOpen) * Math.Cos(Math.PI / 6) * 0.45, Math.Sin(Math.PI / 6) * 0.45, Math.Sin(Math.PI / 6 + mouthOpen) * Math.Cos(Math.PI / 6) * 0.45,
			0.1, Math.PI / 6 + mouthOpen, Math.PI / 2 - Math.PI / 6, Math.PI / 6, State == States.Super ? Color.Red : Color.White);
			renderEye(Math.Cos(Math.PI / 6 + mouthOpen) * Math.Cos(-Math.PI / 6) * 0.45, Math.Sin(-Math.PI / 6) * 0.45, Math.Sin(Math.PI / 6 + mouthOpen) * Math.Cos(-Math.PI / 6) * 0.45,
			0.1, Math.PI / 6 + mouthOpen, Math.PI / 2 - Math.PI / 6, Math.PI / 6, State == States.Super ? Color.Red : Color.White);

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
