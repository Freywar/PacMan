using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace PacMan
{
	/// <summary>
	/// Camera class
	/// </summary>
	class Camera
	{
		private const double rotationSpeed = 30;
		private const double translationSpeed = 10;

		/// <summary>
		/// Follow PacMan
		/// </summary>
		public bool FollowPacMan = false;

		/// <summary>
		/// Target X in map cells
		/// </summary>
		public double X = 0;
		/// <summary>
		/// Target Y in map cells
		/// </summary>
		public double Y = 0;

		private double XAngle_v = 45;
		private double YAngle_v = 0;
		private double R_v = 10;

		/// <summary>
		/// Angle by X axis in degrees.
		/// </summary>
		public double XAngle
		{
			get { return XAngle_v; }
			set
			{
				XAngle_v = value;
				if (XAngle_v > 90)
					XAngle_v = 90;
				if (XAngle_v < 0)
					XAngle_v = 0;
			}
		}
		/// <summary>
		/// Angle by Y axis in degrees.
		/// </summary>
		public double YAngle
		{
			get { return YAngle_v; }
			set
			{
				YAngle_v = value;
				double limit = 45 * Math.Cos(XAngle * Math.PI / 180);
				if (YAngle_v < -limit)
					YAngle_v = -limit;
				if (YAngle_v > limit)
					YAngle_v = limit;
			}
		}
		/// <summary>
		/// Distance from target point in map cells.
		/// </summary>
		public double R
		{
			get { return R_v; }
			set
			{
				R_v = value;
				if (R_v > 100)
					R_v = 100;
				if (R_v < 1)
					R_v = 1;
			}
		}

		/// <summary>
		/// Shows if F button has been released after last press.
		/// </summary>
		private bool followButtonReleased = true;
		/// <summary>
		/// Rotation speed around X axis in degrees per second.
		/// </summary>
		private double xAngleSpeed;
		/// <summary>
		/// Rotation speed around Y axis in degrees per second.
		/// </summary>
		private double yAngleSpeed;
		/// <summary>
		/// Speed in map cells per second.
		/// </summary>
		private double rSpeed;

		/// <summary>
		/// Camera initialization on level start.
		/// </summary>
		/// <param name="map">Map</param>
		/// <param name="pacman">PacMan</param>
		public void Init(Map map, PacMan pacman)
		{
			XAngle = 45;
			YAngle = 0;
			R = Math.Max(map.Width / 2, map.Height / 2) / Math.Tan(30 * Math.PI / 180) * 1.2; //distance where aligned map perfectly fits into viewport * 1.2

			xAngleSpeed = 0;
			yAngleSpeed = 0;
			rSpeed = 0;

			FollowPacMan = false;
			followButtonReleased = true;

			if (FollowPacMan)
			{
				X = pacman.X;
				Y = pacman.Y;
			}
			else
			{
				X = map.Width / 2;
				Y = map.Height / 2;
			}
		}

		/// <summary>
		/// Update camera position.
		/// </summary>
		/// <param name="dt">Time elapsed from last call in seconds.</param>
		/// <param name="map">Map</param>
		/// <param name="pacman">PacMan</param>
		public void Update(double dt, Map map, PacMan pacman)
		{
			XAngle += xAngleSpeed * dt;
			YAngle += yAngleSpeed * dt;
			R += rSpeed * dt;

			if (FollowPacMan)
			{
				X = pacman.X;
				Y = pacman.Y;
			}
			else
			{
				X = map.Width / 2;
				Y = map.Height / 2;
			}
		}

		/// <summary>
		/// User input handling.
		/// </summary>
		/// <param name="keyboard">Pressed keys.</param>
		public void Control(KeyboardDevice keyboard)
		{
			if (keyboard[Key.W] && !keyboard[Key.S])
				xAngleSpeed = rotationSpeed;
			else if (keyboard[Key.S] && !keyboard[Key.W])
				xAngleSpeed = -rotationSpeed;
			else xAngleSpeed = 0;

			if (keyboard[Key.A] && !keyboard[Key.D])
				yAngleSpeed = rotationSpeed;
			else if (keyboard[Key.D] && !keyboard[Key.A])
				yAngleSpeed = -rotationSpeed;
			else yAngleSpeed = 0;

			if (keyboard[Key.Q] && !keyboard[Key.E])
				rSpeed = translationSpeed;
			else if (keyboard[Key.E] && !keyboard[Key.Q])
				rSpeed = -translationSpeed;
			else
				rSpeed = 0;

			if (keyboard[Key.F])
			{
				if (followButtonReleased)
					FollowPacMan = !FollowPacMan;
				followButtonReleased = false;
			}
			else
				followButtonReleased = true;
		}

		/// <summary>
		/// Applying camera properties to scene.
		/// </summary>
		public void Render()
		{
			GL.Translate(0, 0, -R);
			GL.Rotate(XAngle, 1, 0, 0);
			GL.Rotate(YAngle, 0, 1, 0);
			GL.Translate(-X, -0.5, -Y);
		}
	}
}
