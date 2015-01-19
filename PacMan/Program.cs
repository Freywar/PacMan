using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Collections.Generic;

namespace PacMan
{

	class Application
	{
		private static Game Game;
		private static GameWindow Window;
		private static Dictionary<Key, bool> pressedKeys = new Dictionary<Key, bool>();

		public static void OnKeyDown(Object sender, KeyboardKeyEventArgs e)
		{
			if (!pressedKeys.ContainsKey(e.Key) || !pressedKeys[e.Key])
				if (Game.KeyDown(e.Key))
					Window.Exit();
			pressedKeys[e.Key] = true;
		}

		public static void OnKeyUp(Object sender, KeyboardKeyEventArgs e)
		{
			if (pressedKeys[e.Key])
				if (Game.KeyUp(e.Key))
					Window.Exit();
			pressedKeys[e.Key] = false;
		}

		public static void OnWindowLoad(Object sender, EventArgs e)
		{
			Game = new Game();
			Game.Init();
		}

		public static void OnWindowResize(Object sender, EventArgs e)
		{
			GL.Viewport(0, 0, Window.Width, Window.Height);
			Game.Width = Window.Width;
			Game.Height = Window.Height;
		}

		public static void OnWindowUpdate(Object sender, FrameEventArgs e)
		{
			if (Game.Update(e.Time))
				Window.Exit();
		}

		public static void OnWindowRender(Object sender, FrameEventArgs e)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			Game.Render();

			Window.SwapBuffers();
		}

		[STAThread]
		public static void Main()
		{
			Window = new GameWindow();
			Game = new Game();
			Window.Load += OnWindowLoad;
			Window.Resize += OnWindowResize;
			Window.UpdateFrame += OnWindowUpdate;
			Window.RenderFrame += OnWindowRender;
			Window.KeyDown += OnKeyDown;
			Window.KeyUp += OnKeyUp;
			Window.Run(60.0);
		}
	}
}