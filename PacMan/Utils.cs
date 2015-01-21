using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace PacMan
{
	/// <summary>
	/// Mesh class.
	/// </summary>
	class Mesh : IDisposable
	{
		private uint verticesBufferId;
		private uint normalsBufferId;
		private uint colorsBufferId;

		private double[] Vertices_v = null;
		private double[] Colors_v = null;
		private double[] Normals_v = null;

		/// <summary>
		/// Vertex components.
		/// </summary>
		public double[] Vertices
		{
			get { return Vertices_v; }
			set
			{
				Vertices_v = value;
				GL.BindBuffer(BufferTarget.ArrayBuffer, verticesBufferId);
				GL.BufferData(
					 BufferTarget.ArrayBuffer,
					 (IntPtr)(Vertices_v.Length * sizeof(double)),
					 Vertices_v,
					 BufferUsageHint.StaticDraw);
				GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			}
		}
		/// <summary>
		/// Normals components.
		/// </summary>
		public double[] Normals
		{
			get { return Normals_v; }
			set
			{
				Normals_v = value;
				GL.BindBuffer(BufferTarget.ArrayBuffer, normalsBufferId);
				GL.BufferData(
					 BufferTarget.ArrayBuffer,
					 (IntPtr)(Normals_v.Length * sizeof(double)),
					 Normals_v,
					 BufferUsageHint.StaticDraw);
				GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			}
		}
		/// <summary>
		/// Colors components.
		/// </summary>
		public double[] Colors
		{
			get { return Colors_v; }
			set
			{
				Colors_v = value;
				GL.BindBuffer(BufferTarget.ArrayBuffer, colorsBufferId);
				GL.BufferData(
					 BufferTarget.ArrayBuffer,
					 (IntPtr)(Colors_v.Length * sizeof(double)),
					 Colors_v,
					 BufferUsageHint.StaticDraw);
				GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public Mesh()
		{
			GL.GenBuffers(1, out verticesBufferId);
			GL.GenBuffers(1, out normalsBufferId);
			GL.GenBuffers(1, out colorsBufferId);
		}

		/// <summary>
		/// Render.
		/// </summary>
		/// <param name="type">Primitive type.</param>
		public void Render(PrimitiveType type)
		{
			if (Vertices_v == null)
				return;

			GL.BindBuffer(BufferTarget.ArrayBuffer, verticesBufferId);
			GL.EnableClientState(ArrayCap.VertexArray);
			GL.VertexPointer(3, VertexPointerType.Double, 0, IntPtr.Zero);

			if (Normals_v != null)
			{
				GL.BindBuffer(BufferTarget.ArrayBuffer, normalsBufferId);
				GL.EnableClientState(ArrayCap.NormalArray);
				GL.NormalPointer(NormalPointerType.Double, 0, IntPtr.Zero);
			}

			if (Colors_v != null)
			{
				GL.BindBuffer(BufferTarget.ArrayBuffer, colorsBufferId);
				GL.EnableClientState(ArrayCap.ColorArray);
				GL.ColorPointer(3, ColorPointerType.Double, 0, IntPtr.Zero);
			}

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			GL.DrawArrays(type, 0, Vertices_v.Length / 3);


			GL.DisableClientState(ArrayCap.VertexArray);
			if (Normals_v != null)
				GL.DisableClientState(ArrayCap.NormalArray);

			if (Colors_v != null)
				GL.DisableClientState(ArrayCap.ColorArray);
		}

		/// <summary>
		/// Render.
		/// </summary>
		public void Render()
		{
			Render(PrimitiveType.Quads);
		}

		public void Dispose()
		{
			GL.DeleteBuffer(verticesBufferId);
			GL.DeleteBuffer(normalsBufferId);
			GL.DeleteBuffer(colorsBufferId);
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
}
