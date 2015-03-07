using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacMan
{
	/// <summary>
	/// Base class for all in-game objects.
	/// </summary>
	abstract class GameObject : IDisposable
	{
		/// <summary>
		/// Static states.
		/// </summary>
		public class States : ExtendableEnum
		{
			public States(string value) : base(value) { }

			public static readonly States Normal = new States("Normal");
			public static readonly States None = new States("None");
		}

		/// <summary>
		/// Transitions between states.
		/// </summary>
		public class Animations : AnimationEnum
		{
			public Animations(string value, double duration, States result) : base(value, duration, result) { }

			public static readonly Animations Appear = new Animations("Appear", 0.5, States.Normal);
			public static readonly Animations LiftUp = new Animations("LiftUp", 0.5, null);
			public static readonly Animations None = new Animations("None", 0, null);
			public static readonly Animations LiftDown = new Animations("LiftDown", 0.5, null);
			public static readonly Animations Disappear = new Animations("Disappear", 0.5, States.None);
		}

		/// <summary>
		/// Current state.
		/// </summary>
		public States State { get; private set; }
		/// <summary>
		/// Running animation.
		/// </summary>
		public Animations Animation { get; private set; }
		/// <summary>
		/// Current floor.
		/// </summary>
		public int Floor = 0;
		/// <summary>
		/// Start animation method.
		/// </summary>
		/// <param name="animation">Animation.</param>
		public void Animate(Animations animation)
		{
			if (animation == null || animation == Animation)
				return;
			if (Animation.Result != null)
			{
				State = (States)Animation.Result;
				Animation = Animations.None;
			}
			if (animation == Animations.LiftUp)
				Floor++;
			if (animation == Animations.LiftDown)
				Floor--;
			if (animation.Duration == 0)
			{
				if (animation.Result != null)
					State = (States)animation.Result;
				return;
			}
			Animation = animation;
		}
		/// <summary>
		/// Is some animation going.
		/// </summary>
		public bool IsAnimated
		{
			get
			{
				return Animation != Animations.None;
			}
		}
		/// <summary>
		/// Animation progress in [0..1].
		/// </summary>
		protected double animationProgress = 0;
		/// <summary>
		/// Current Y coordinate including lift animations.
		/// </summary>
		protected double Y
		{
			get
			{
				if (Animation == Animations.LiftUp)
					return Floor - (1 - animationProgress);
				if (Animation == Animations.LiftDown)
					return Floor + (1 - animationProgress);
				return Floor;
			}
		}

		virtual public void Init()
		{
			State = States.None;
			Animation = Animations.None;
			animationProgress = 0;
		}

		virtual public void Update(double dt)
		{
			if (IsAnimated)
			{
				animationProgress += dt / Animation.Duration;
				if (animationProgress >= 1)
				{
					dt = (animationProgress - 1) * Animation.Duration;
					if (Animation.Result != null)
						State = (States)Animation.Result;
					Animation = Animations.None;
					animationProgress = 0;
					Update(dt);
			}
			}
		}

		abstract public void Render();

		abstract public void Dispose();
	}
}
