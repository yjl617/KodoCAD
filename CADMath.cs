using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kodo.Graphics;

namespace KodoCAD
{
    public class Set<T> : HashSet<T>
    {
    }

    public static class CADMath
    {
        public static float MillimetersToMils(float millimeters)
        {
            return (millimeters * 1000f) / 25.4f;
        }

        public static float MilsToMillimeters(float mils)
        {
            return (mils * 25.4f) / 1000f;
        }

        public static float Distance(Point p1, Point p2)
        {
            return (float)(Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow((p2.Y - p1.Y), 2)));
        }

        public static Point Transform(Point point, Matrix3x2 matrix)
        {
            return Matrix3x2.TransformPoint(matrix, point);
        }

        public static float Round(float value, int decimals = 3)
        {
            return (float)Math.Round(value, decimals);
        }

        public static float Clamp(float value, float min, float max)
        {
            return Math.Max(Math.Min(value, max), min);
        }

        public static int Clamp(int value, int min, int max)
        {
            return Math.Max(Math.Min(value, max), min);
        }

        public static Point Clamp(Point value, Point min, Point max)
        {
            return new Point(Clamp(value.X, min.X, max.X), Clamp(value.Y, min.Y, max.Y));
        }

        public static string ToString(float number)
        {
            return number.ToString("0.000 'mm'", CultureInfo.InvariantCulture);
        }

        public static string ToString2(double number) => ToString2((float)number);
        public static string ToString2(float number)
        {
            return number.ToString("'+'0.000 'mm';'-'0.000 'mm'", CultureInfo.InvariantCulture);
        }
    }
}
