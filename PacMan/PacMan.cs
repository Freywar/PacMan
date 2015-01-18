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
	class PacMan : Creature
	{
		public enum States
		{
			Normal,
			Super
		}

		protected Creature.Directions desiredDirection = Creature.Directions.Up;

		public int Lives = 3;
		public States State = States.Normal;
		public double SuperTime = 0;

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

		public void Control(KeyboardDevice keyboard)
		{
			if (keyboard[Key.Up])
				desiredDirection = Creature.Directions.Up;
			if (keyboard[Key.Down])
				desiredDirection = Creature.Directions.Down;
			if (keyboard[Key.Left])
				desiredDirection = Creature.Directions.Left;
			if (keyboard[Key.Right])
				desiredDirection = Creature.Directions.Right;
		}

		public override void Render()
		{

			double mouthOpen = Math.Max(Math.Abs(X - Math.Round(X)), Math.Abs(Y - Math.Round(Y)));
			mouthOpen *= 2 * Math.PI / 2;
			mouthOpen = Math.Sin(mouthOpen);
			mouthOpen *= Math.PI / 4;


			GL.Translate(X, 0.5, Y);
			switch (Direction)
			{
				case Directions.Down:
				break;
				case Directions.Up:
				GL.Rotate(180, 0,1,0);
				break;
				case Directions.Left:
				GL.Rotate(-90, 0,1,0);
				break;
				case Directions.Right:
				GL.Rotate(90, 0,1,0);
				break;

			}

			GL.Begin(PrimitiveType.Quads);

			


			GL.Color3(Color.DarkRed);
			//upper jaw
			for (double yAngle = -Math.PI / 2; yAngle < Math.PI / 2; yAngle += Math.PI / 10)
			{
				Vector3 xAxis = new Vector3(1, 0, 0);
				Vector3 jawAxis = new Vector3(0, (float)Math.Sin(mouthOpen), (float)Math.Cos(mouthOpen));
				Vector3 normal = Vector3.Cross(xAxis, jawAxis);
				GL.Normal3(normal);
				GL.Vertex3(0, 0, 0);
				GL.Vertex3(Math.Sin(yAngle + Math.PI / 10) * 0.45, Math.Sin(mouthOpen) * Math.Cos(yAngle + Math.PI / 10) * 0.45, Math.Sin(mouthOpen) * Math.Cos(yAngle + Math.PI / 10) * 0.45);
				GL.Vertex3(Math.Sin(yAngle) * 0.45, Math.Sin(mouthOpen) * Math.Cos(yAngle) * 0.45, Math.Sin(mouthOpen) * Math.Cos(yAngle) * 0.45);
				GL.Vertex3(0, 0, 0);
			}

			GL.Color3(Color.Yellow);
			//upper face
			for (double yAngle = -Math.PI / 2; yAngle < Math.PI / 2; yAngle += Math.PI / 10)
				for (double xAngle = mouthOpen; xAngle <= Math.PI / 2; xAngle += Math.PI / 10)
				{

					GL.Normal3(Math.Sin(yAngle), Math.Sin(xAngle) * Math.Cos(yAngle), Math.Cos(xAngle) * Math.Cos(yAngle));

					GL.Vertex3(Math.Sin(yAngle) * 0.45, Math.Sin(xAngle) * Math.Cos(yAngle) * 0.45, Math.Cos(xAngle) * Math.Cos(yAngle) * 0.45);
					GL.Vertex3(Math.Sin(yAngle + Math.PI / 10) * 0.45, Math.Sin(xAngle) * Math.Cos(yAngle + Math.PI / 10) * 0.45, Math.Cos(xAngle) * Math.Cos(yAngle + Math.PI / 10) * 0.45);
					GL.Vertex3(Math.Sin(yAngle + Math.PI / 10) * 0.45, Math.Sin(xAngle + Math.PI / 10) * Math.Cos(yAngle + Math.PI / 10) * 0.45, Math.Cos(xAngle + Math.PI / 10) * Math.Cos(yAngle + Math.PI / 10) * 0.45);
					GL.Vertex3(Math.Sin(yAngle) * 0.45, Math.Sin(xAngle + Math.PI / 10) * Math.Cos(yAngle) * 0.45, Math.Cos(xAngle + Math.PI / 10) * Math.Cos(yAngle) * 0.45);
				}

			//back
			for (double yAngle = -Math.PI / 2; yAngle < Math.PI / 2; yAngle += Math.PI / 10)
				for (double xAngle = Math.PI / 2; xAngle <= 3 * Math.PI / 2; xAngle += Math.PI / 10)
				{

					GL.Normal3(Math.Sin(yAngle), Math.Sin(xAngle) * Math.Cos(yAngle), Math.Cos(xAngle) * Math.Cos(yAngle));

					GL.Vertex3(Math.Sin(yAngle) * 0.45, Math.Sin(xAngle) * Math.Cos(yAngle) * 0.45, Math.Cos(xAngle) * Math.Cos(yAngle) * 0.45);
					GL.Vertex3(Math.Sin(yAngle + Math.PI / 10) * 0.45, Math.Sin(xAngle) * Math.Cos(yAngle + Math.PI / 10) * 0.45, Math.Cos(xAngle) * Math.Cos(yAngle + Math.PI / 10) * 0.45);
					GL.Vertex3(Math.Sin(yAngle + Math.PI / 10) * 0.45, Math.Sin(xAngle + Math.PI / 10) * Math.Cos(yAngle + Math.PI / 10) * 0.45, Math.Cos(xAngle + Math.PI / 10) * Math.Cos(yAngle + Math.PI / 10) * 0.45);
					GL.Vertex3(Math.Sin(yAngle) * 0.45, Math.Sin(xAngle + Math.PI / 10) * Math.Cos(yAngle) * 0.45, Math.Cos(xAngle + Math.PI / 10) * Math.Cos(yAngle) * 0.45);
				}

			//lower face
			for (double yAngle = -Math.PI / 2; yAngle < Math.PI / 2; yAngle += Math.PI / 10)
				for (double xAngle = -Math.PI / 2; xAngle <= -mouthOpen; xAngle += Math.PI / 10)
				{

					GL.Normal3(Math.Sin(yAngle), Math.Sin(xAngle) * Math.Cos(yAngle), Math.Cos(xAngle) * Math.Cos(yAngle));

					GL.Vertex3(Math.Sin(yAngle) * 0.45, Math.Sin(xAngle) * Math.Cos(yAngle) * 0.45, Math.Cos(xAngle) * Math.Cos(yAngle) * 0.45);
					GL.Vertex3(Math.Sin(yAngle + Math.PI / 10) * 0.45, Math.Sin(xAngle) * Math.Cos(yAngle + Math.PI / 10) * 0.45, Math.Cos(xAngle) * Math.Cos(yAngle + Math.PI / 10) * 0.45);
					GL.Vertex3(Math.Sin(yAngle + Math.PI / 10) * 0.45, Math.Sin(xAngle + Math.PI / 10) * Math.Cos(yAngle + Math.PI / 10) * 0.45, Math.Cos(xAngle + Math.PI / 10) * Math.Cos(yAngle + Math.PI / 10) * 0.45);
					GL.Vertex3(Math.Sin(yAngle) * 0.45, Math.Sin(xAngle + Math.PI / 10) * Math.Cos(yAngle) * 0.45, Math.Cos(xAngle + Math.PI / 10) * Math.Cos(yAngle) * 0.45);
				}

			GL.Color3(Color.DarkRed);
			//lower jaw
			for (double yAngle = -Math.PI / 2; yAngle < Math.PI / 2; yAngle += Math.PI / 10)
			{
				Vector3 xAxis = new Vector3(1, 0, 0);
				Vector3 jawAxis = new Vector3(0, (float)Math.Sin(-mouthOpen), (float)Math.Cos(-mouthOpen));
				Vector3 normal = Vector3.Cross(jawAxis, xAxis);
				GL.Normal3(normal);
				GL.Vertex3(0, 0, 0);
				GL.Vertex3(Math.Sin(yAngle) * 0.45, Math.Sin(-mouthOpen) * Math.Cos(yAngle) * 0.45, Math.Sin(mouthOpen) * Math.Cos(yAngle) * 0.45);
				GL.Vertex3(Math.Sin(yAngle + Math.PI / 10) * 0.45, Math.Sin(-mouthOpen) * Math.Cos(yAngle + Math.PI / 10) * 0.45, Math.Sin(mouthOpen) * Math.Cos(yAngle + Math.PI / 10) * 0.45);
				GL.Vertex3(0, 0, 0);
			}


			GL.End();


			//left eye
			GL.Translate(Math.Sin(Math.PI / 6) * 0.45, Math.Sin(Math.PI / 6 + mouthOpen) * Math.Cos(Math.PI / 6) * 0.45, Math.Cos(Math.PI / 6 + mouthOpen) * Math.Cos(Math.PI / 6) * 0.45);
			GL.Begin(PrimitiveType.Quads);
			for (double alpha = -Math.PI / 2; alpha < Math.PI / 2; alpha += Math.PI / 10)
				for (double beta = 0; beta < Math.PI * 2; beta += Math.PI / 10)
				{
					if (State == States.Super)
						GL.Color3(Color.Red);
					else 					if (Math.Sqrt((alpha - Math.PI / 6 - mouthOpen) * (alpha - Math.PI / 6 - mouthOpen) + (beta - Math.PI / 2 + Math.PI / 6) * (beta - Math.PI / 2 + Math.PI / 6)) < Math.PI / 6)
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
			GL.Translate(-Math.Sin(Math.PI / 6) * 0.45, -Math.Sin(Math.PI / 6 + mouthOpen) * Math.Cos(Math.PI / 6) * 0.45, -Math.Cos(Math.PI / 6 + mouthOpen) * Math.Cos(Math.PI / 6) * 0.45);


			//right eye
			GL.Translate(Math.Sin(-Math.PI / 6) * 0.45, Math.Sin(Math.PI / 6 + mouthOpen) * Math.Cos(-Math.PI / 6) * 0.45, Math.Cos(Math.PI / 6 + mouthOpen) * Math.Cos(-Math.PI / 6) * 0.45);
			GL.Begin(PrimitiveType.Quads);
			for (double alpha = -Math.PI / 2; alpha < Math.PI / 2; alpha += Math.PI / 10)
				for (double beta = 0; beta < Math.PI * 2; beta += Math.PI / 10)
				{
				if (State == States.Super)
				GL.Color3(Color.Red);
					else if (Math.Sqrt((alpha - Math.PI / 6 - mouthOpen) * (alpha - Math.PI / 6 - mouthOpen) + (beta + Math.PI / 2) * (beta - Math.PI / 2 + Math.PI / 6)) < Math.PI / 6)
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
			GL.Translate(-Math.Sin(-Math.PI / 6) * 0.45, -Math.Sin(Math.PI / 6 + mouthOpen) * Math.Cos(-Math.PI / 6) * 0.45, -Math.Cos(Math.PI / 6 + mouthOpen) * Math.Cos(-Math.PI / 6) * 0.45);


						switch (Direction)
			{
				case Directions.Down:
				break;
				case Directions.Up:
				GL.Rotate(-180, 0,1,0);
				break;
				case Directions.Left:
				GL.Rotate(90, 0,1,0);
				break;
				case Directions.Right:
				GL.Rotate(-90, 0,1,0);
				break;

			}
			GL.Translate(-X, -0.5, -Y);
		}


	}
}
