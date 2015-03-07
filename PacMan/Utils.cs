using System;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Xml.Serialization;

namespace PacMan
{
	/// <summary>
	/// Mesh class.
	/// </summary>
	class Mesh : IDisposable
	{
		private static Mesh CurrentBound;

		private uint bufferId;

		private double[] Vertex_v = null;
		private double[] Normal_v = null;
		private double[] Color_v = null;

		private void updateData()
		{
			if (CurrentBound != null)
				CurrentBound.Unbind();
			Bind();

			int size = 0;
			if (Vertex_v != null)
				size += Vertex_v.Length;

			if (Normal_v != null)
				size += Normal_v.Length;

			if (Color_v != null)
				size += Color_v.Length;

			double[] data = new double[size];

			int offset = 0;

			if (Vertex_v != null)
			{
				Buffer.BlockCopy(Vertex_v, 0, data, offset * sizeof(double), Vertex_v.Length * sizeof(double));
				offset += Vertex_v.Length;
			}

			if (Normal_v != null)
			{
				Buffer.BlockCopy(Normal_v, 0, data, offset * sizeof(double), Normal_v.Length * sizeof(double));
				offset += Normal_v.Length;
			}

			if (Color_v != null)
			{
				Buffer.BlockCopy(Color_v, 0, data, offset * sizeof(double), Color_v.Length * sizeof(double));
				offset += Color_v.Length;
			}

			GL.BufferData(
				 BufferTarget.ArrayBuffer,
				 (IntPtr)(data.Length * sizeof(double)),
				 data,
				 BufferUsageHint.StaticDraw);
		}

		/// <summary>
		/// Vertex components.
		/// </summary>
		public double[] Vertex
		{
			get { return Vertex_v; }
			set
			{
				Vertex_v = value;
				updateData();
			}
		}
		/// <summary>
		/// Normal components.
		/// </summary>
		public double[] Normal
		{
			get { return Normal_v; }
			set
			{
				Normal_v = value;
				updateData();
			}
		}
		/// <summary>
		/// Color components.
		/// </summary>
		public double[] Color
		{
			get { return Color_v; }
			set
			{
				Color_v = value;
				updateData();
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public Mesh()
		{
			GL.GenBuffers(1, out bufferId);
		}

		private void Bind()
		{
			if (CurrentBound != this)
			{
				GL.BindBuffer(BufferTarget.ArrayBuffer, bufferId);

				int offset = 0;

				if (Vertex_v != null)
				{
					GL.EnableClientState(ArrayCap.VertexArray);
					GL.VertexPointer(3, VertexPointerType.Double, 0, offset);
					offset += sizeof(double) * Vertex_v.Length;
				}

				if (Normal_v != null)
				{
					GL.EnableClientState(ArrayCap.NormalArray);
					GL.NormalPointer(NormalPointerType.Double, 0, offset);
					offset += sizeof(double) * Normal_v.Length;
				}

				if (Color_v != null)
				{
					GL.EnableClientState(ArrayCap.ColorArray);
					GL.ColorPointer(3, ColorPointerType.Double, 0, offset);
					offset += sizeof(double) * Color_v.Length;
				}

				CurrentBound = this;
			}
		}

		/// <summary>
		/// Render.
		/// </summary>
		/// <param name="type">Primitive type.</param>
		public void Render(PrimitiveType type)
		{
			if (Vertex_v == null)
				return;

			if (CurrentBound != this)
			{
				if (CurrentBound != null)
					CurrentBound.Unbind();
				Bind();
			}

			GL.DrawArrays(type, 0, Vertex_v.Length / 3);
		}

		/// <summary>
		/// Render.
		/// </summary>
		public void Render()
		{
			Render(PrimitiveType.Quads);
		}

		protected void Unbind()
		{
			if (CurrentBound == this)
			{
				if (Vertex_v != null)
					GL.DisableClientState(ArrayCap.VertexArray);
				if (Normal_v != null)
					GL.DisableClientState(ArrayCap.NormalArray);
				if (Color_v != null)
					GL.DisableClientState(ArrayCap.ColorArray);

				CurrentBound = null;
			}
		}

		public void Dispose()
		{
			GL.DeleteBuffer(bufferId);
		}
	}

	class ShaderProgram : IDisposable
	{
		private int programId;
		private int vertexShaderId;
		private int fragmentShaderId;

		private int loadShader(String filename, ShaderType type, int program)
		{
			int address = GL.CreateShader(type);
			using (StreamReader sr = new StreamReader(filename))
			{
				GL.ShaderSource(address, sr.ReadToEnd());
			}
			GL.CompileShader(address);
			GL.AttachShader(program, address);
			string log = GL.GetShaderInfoLog(address);
			return address;
		}

		public ShaderProgram(string vs, string fs)
		{
			programId = GL.CreateProgram();
			vertexShaderId = loadShader(vs, ShaderType.VertexShader, programId);
			fragmentShaderId = loadShader(fs, ShaderType.FragmentShader, programId);
			GL.LinkProgram(programId);
			string log = GL.GetProgramInfoLog(programId);
		}

		public void Enable()
		{
			GL.UseProgram(programId);
		}

		public void SetUniform(string name, Vector4 data)
		{
			GL.Uniform4(GL.GetUniformLocation(programId, name), data);
		}

		public void SetUniform(string name, float data)
		{
			GL.Uniform1(GL.GetUniformLocation(programId, name), data);
		}

		public void Disable()
		{
			GL.UseProgram(0);
		}

		public void Dispose()
		{
			GL.DetachShader(programId, fragmentShaderId);
			GL.DetachShader(programId, vertexShaderId);
			GL.DeleteShader(fragmentShaderId);
			GL.DeleteShader(vertexShaderId);
			GL.DeleteProgram(programId);
		}

		private static ShaderProgram Default_v = null;
		public static ShaderProgram Default
		{
			get
			{
				if (Default_v == null)
					Default_v = new ShaderProgram("Shaders\\Default_Vert.glsl", "Shaders\\Default_Frag.glsl");
				return Default_v;
			}
		}

		private static ShaderProgram StaticColor_v = null;
		public static ShaderProgram StaticColor
		{
			get
			{
				if (StaticColor_v == null)
					StaticColor_v = new ShaderProgram("Shaders\\StaticColor_Vert.glsl", "Shaders\\StaticColor_Frag.glsl");
				return StaticColor_v;
			}
		}

	}

	struct Vector3i
	{
		public int X;
		public int Y;
		public int Z;
		public Vector3i(int x, int y, int z)
		{
			X = x;
			Y = y;
			Z = z;
		}
	}

	class Utils
	{
		/// <summary>
		/// Calc distance between two points.
		/// </summary>
		/// <param name="x1">First point X.</param>
		/// <param name="y1">First point Y.</param>
		/// <param name="x2">Second point X.</param>
		/// <param name="y2">Second point Y.</param>
		/// <returns>Distance.</returns>
		public static double Distance(double x1, double y1, double x2, double y2)
		{
			x1 -= x2;
			y1 -= y2;
			return Math.Sqrt(x1 * x1 + y1 * y1);
		}

		/// <summary>
		/// Convert coordinates from spheric to decart.
		/// </summary>
		/// <param name="alpha">First angle.</param>
		/// <param name="beta">Second angle.</param>
		/// <param name="r">Radius.</param>
		/// <returns>Point in decart coordinates.</returns>
		public static Vector3d FromSpheric(double alpha, double beta, double r)
		{
			return new Vector3d(Math.Cos(alpha) * Math.Cos(beta) * r, Math.Sin(alpha) * r, Math.Cos(alpha) * Math.Sin(beta) * r);
		}

		/// <summary>
		/// Push vector into mesh components array.
		/// </summary>
		/// <param name="array">Array.</param>
		/// <param name="vector">Vector.</param>
		/// <param name="offset">First free index in array.</param>
		public static void Push(double[] array, Vector3d vector, ref int offset)
		{
			array[offset + 0] = (double)vector.X;
			array[offset + 1] = (double)vector.Y;
			array[offset + 2] = (double)vector.Z;
			offset += 3;
		}

		/// <summary>
		/// Push vector into mesh components array.
		/// </summary>
		/// <param name="array">Array.</param>
		/// <param name="vector">Vector.</param>
		/// <param name="offset">First free index in array.</param>
		public static void Push(double[] array, Vector4d vector, ref int offset)
		{
			array[offset + 0] = (double)vector.X;
			array[offset + 1] = (double)vector.Y;
			array[offset + 2] = (double)vector.Z;
			array[offset + 2] = (double)vector.W;
			offset += 4;
		}

		public static double NormSin(double param)
		{
			return (Math.Sin((param * 2 - 1) * Math.PI / 2) + 1) / 2;
		}
	}

	class ExtendableEnum
	{
		public static bool operator ==(ExtendableEnum left, ExtendableEnum right)
		{
			return ((object)left == null && (object)right == null) || (object)left != null && (object)right != null && left.Value == right.Value;
		}

		public static bool operator !=(ExtendableEnum left, ExtendableEnum right)
		{
			return !((object)left == null && (object)right == null) && ((object)left == null || (object)right == null || left.Value != right.Value);
		}

		public override bool Equals(object obj)
		{
			return base.Equals(obj);
		}

		public override string ToString()
		{
			return Value;
		}

		protected ExtendableEnum(string value)
		{
			Value = value;
		}

		public readonly string Value;
	}

	class AnimationEnum : ExtendableEnum
	{
		protected AnimationEnum(string value, double duration, ExtendableEnum result)
			: base(value)
		{
			Duration = duration;
			Result = result;
		}
		public readonly double Duration = 0;
		public readonly ExtendableEnum Result = null;
	}
}
