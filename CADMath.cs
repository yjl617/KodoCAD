using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kodo.Graphics;

namespace KodoCad
{
    public class Set<T> : HashSet<T>
    {
    }

    public static class CadUtilities
    {
        public static string Stringify(float value)
            => MillimetersToMicrometers(value).ToString(CultureInfo.InvariantCulture);

        public static string Stringify(int value)
            => value.ToString(CultureInfo.InvariantCulture);

        public static float MicrometersToMillimeters(int micrometers)
            => micrometers / 1000f;

        public static int MillimetersToMicrometers(float millimeters)
            => (int)Math.Round(millimeters * 1000f);

        public static float MillimetersToMils(float millimeters)
            => (millimeters * 1000f) / 25.4f;

        public static float MilsToMillimeters(float mils)
            => (mils * 25.4f) / 1000f;
    }

    public static class CadMath
    {
        public static bool LineContainsPoint(Point p1, Point p2, Point p, float threshold)
        {
            var distanceAC = Distance(p1, p);
            var distanceBC = Distance(p2, p);
            var distanceAB = Distance(p1, p2);

            return distanceAC + distanceBC - distanceAB < threshold;
        }

        public static float Distance(Point p1, Point p2)
        {
            return (float)(Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow((p2.Y - p1.Y), 2)));
        }

        public static Rectangle Rotate(Rectangle rectangle, Point center)
        {
            var centerOfRotation = center;
            var tl = rectangle.TopLeft;
            var br = rectangle.BottomRight;

            tl = Rotate(tl, centerOfRotation);
            br = Rotate(br, centerOfRotation);

            return Rectangle.FromLTRB(br.X, tl.Y, tl.X, br.Y);
        }

        public static Point Rotate(Point point, Point center)
        {
            return Matrix3x2.TransformPoint(Matrix3x2.Rotation(90, center), point);
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

        /// <summary>
        /// 0.000 mm
        /// </summary>
        public static string ToString(float number)
        {
            return number.ToString("0.000 'mm'", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 0.000
        /// </summary>
        public static string ToString3(float number)
        {
            return number.ToString("0.000", CultureInfo.InvariantCulture);
        }

        public static string ToString2(double number) => ToString2((float)number);
        /// <summary>
        /// +000.000;-000.000
        /// </summary>
        public static string ToString2(float number)
        {
            return number.ToString("'+'000.000;'-'000.000", CultureInfo.InvariantCulture);
        }
    }
}
