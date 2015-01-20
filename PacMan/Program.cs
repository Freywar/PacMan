using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Collections.Generic;
using System.Drawing;

namespace PacMan
{

	class Application
	{
		private static Game Game;
		private static GameWindow Window;
		private static Dictionary<Key, bool> pressedKeys = new Dictionary<Key, bool>();
		private static Info Info;
		private static bool infoIsVisible = false;

		public static void OnKeyDown(Object sender, KeyboardKeyEventArgs e)
		{
			if (!pressedKeys.ContainsKey(e.Key) || !pressedKeys[e.Key])
			{
				if (Game.KeyDown(e.Key))
					Window.Exit();
				if (e.Key == Key.F1)
					infoIsVisible = !infoIsVisible;
			}
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
			Game.Init();
		}

		public static void OnWindowResize(Object sender, EventArgs e)
		{
			GL.Viewport(0, 0, Window.Width, Window.Height);
			Game.Width = Info.Width = Window.Width;
			Game.Height = Info.Height = Window.Height;
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
			if (infoIsVisible)
			{
				Info.Items[0] = "FPS: " + Window.RenderFrequency.ToString("#.00");
				Info.Invalidate();
				Info.Render();
			}
			Window.SwapBuffers();
		}

		public static void OnWindowUnload(Object sender, EventArgs e)
		{
			Game.Exit();
		}

		[STAThread]
		public static void Main()
		{
			Window = new GameWindow();
			Game = new Game();
			Info = new Info();
			Info.Items = new string[1];
			Window.Load += OnWindowLoad;
			Window.Resize += OnWindowResize;
			Window.UpdateFrame += OnWindowUpdate;
			Window.RenderFrame += OnWindowRender;
			Window.KeyDown += OnKeyDown;
			Window.KeyUp += OnKeyUp;
			Window.Unload+=	OnWindowUnload;	
			Window.Run(60.0);

		}
	}
}