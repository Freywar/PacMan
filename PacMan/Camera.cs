using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace PacMan
{
	/// <summary>
	/// Camera class
	/// </summary>
	class Camera
	{
		/// <summary>
		/// Default rotation speed(degrees per second).
		/// </summary>
		private const double rotationSpeed = 30;
		/// <summary>
		/// Default translation speed(map cells per second).
		/// </summary>
		private const double translationSpeed = 10;

		private double XAngle_v = 45;
		private double YAngle_v = 0;
		private double R_v = 10;

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
		/// Follow PacMan
		/// </summary>
		public bool FollowPacMan = false;

		/// <summary>
		/// Target X in map cells
		/// </summary>
		public double X = 0;
		/// <summary>
		/// Target Z in map cells
		/// </summary>
		public double Z = 0;

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
		/// Camera initialization on level start.
		/// </summary>
		/// <param name="map">Map</param>
		/// <param name="pacman">PacMan</param>
		public void Init(Map map, PacMan pacman)
		{
			XAngle = 45;
			YAngle = 0;
			R = Math.Max(map.Width / 2, map.Depth / 2) / Math.Tan(30 * Math.PI / 180) * 1.2; //distance where aligned map perfectly fits into viewport * 1.2

			xAngleSpeed = 0;
			yAngleSpeed = 0;
			rSpeed = 0;

			FollowPacMan = false;

			if (FollowPacMan)
			{
				X = pacman.X;
				Z = pacman.Z;
			}
			else
			{
				X = map.Width / 2;
				Z = map.Depth / 2;
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
				Z = pacman.Z;
			}
			else
			{
				X = map.Width / 2;
				Z = map.Depth / 2;
			}
		}

		/// <summary>
		/// Key press handling.
		/// </summary>
		/// <param name="key">Pressed key.</param>
		public void KeyDown(Key key)
		{

			switch (key)
			{
				case Key.W: xAngleSpeed += rotationSpeed; break;
				case Key.S: xAngleSpeed -= rotationSpeed; break;
				case Key.A: yAngleSpeed += rotationSpeed; break;
				case Key.D: yAngleSpeed -= rotationSpeed; break;
				case Key.Q: rSpeed += translationSpeed; break;
				case Key.E: rSpeed -= translationSpeed; break;
				case Key.F: FollowPacMan = !FollowPacMan; break;
			}
		}

		/// <summary>
		/// Key release handling.
		/// </summary>
		/// <param name="key">Released key.</param>
		public void KeyUp(Key key)
		{

			switch (key)
			{
				case Key.W: xAngleSpeed -= rotationSpeed; break;
				case Key.S: xAngleSpeed += rotationSpeed; break;
				case Key.A: yAngleSpeed -= rotationSpeed; break;
				case Key.D: yAngleSpeed += rotationSpeed; break;
				case Key.Q: rSpeed -= translationSpeed; break;
				case Key.E: rSpeed += translationSpeed; break;
			}
		}

		/// <summary>
		/// Applying camera properties to scene.
		/// </summary>
		public void Render()
		{
			GL.Translate(0, 0, -R);
			GL.Rotate(XAngle, 1, 0, 0);
			GL.Rotate(YAngle, 0, 1, 0);
			GL.Translate(-X, -0.5, -Z);
		}
	}
}
