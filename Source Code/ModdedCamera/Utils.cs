using System;
using System.Drawing;
using GTA;
using GTA.Math;

namespace ModdedCamera
{

	public static class Utils
	{

		public static double ToRadians(this float val)
		{
			return 0.017453292519943295 * (double)val;
		}


		public static Quaternion GetLookRotation(Vector3 lookPosition, Vector3 up)
		{
			Utils.OrthoNormalize(ref lookPosition, ref up);
			Vector3 vector = Vector3.Cross(up, lookPosition);
			double num = Math.Sqrt((double)(1f + vector.X + up.Y + lookPosition.Z)) * 0.5;
			double num2 = 1.0 / (4.0 * num);
			double num3 = (double)(up.Z - lookPosition.Y) * num2;
			double num4 = (double)(lookPosition.X - vector.Z) * num2;
			double num5 = (double)(vector.Y - up.X) * num2;
			return new Quaternion((float)num3, (float)num4, (float)num5, (float)num);
		}


		public static Vector3 RotationToDirection(Vector3 rotation)
		{
			double num = (double)(rotation.Z * 0.01745329f);
			double num2 = (double)(rotation.X * 0.01745329f);
			double num3 = Math.Abs(Math.Cos(num2));
			return new Vector3((float)(-(float)(Math.Sin(num) * num3)), (float)(Math.Cos(num) * num3), (float)Math.Sin(num2));
		}


		public static Vector3 RightVector(this Vector3 position, Vector3 up)
		{
			position.Normalize();
			up.Normalize();
			return Vector3.Cross(position, up);
		}


		public static Vector3 LeftVector(this Vector3 position, Vector3 up)
		{
			position.Normalize();
			up.Normalize();
			return -Vector3.Cross(position, up);
		}


		public static void OrthoNormalize(ref Vector3 normal, ref Vector3 tangent)
		{
			Vector3.Normalize(normal);
			Vector3 vector = Vector3.Multiply(normal, Vector3.Dot(tangent, normal));
			tangent = Vector3.Subtract(tangent, vector);
			tangent.Normalize();
		}


		public static SizeF GetScreenResolutionMaintainRatio()
		{
			int width = Game.ScreenResolution.Width;
			int height = Game.ScreenResolution.Height;
			float num = (float)width / (float)height;
			float width2 = 1080f * num;
			return new SizeF(width2, 1080f);
		}
	}
}
