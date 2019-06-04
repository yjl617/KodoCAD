using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kodo.Graphics;

using static KodoCad.CadMath;
using static KodoCad.CadUtilities;

namespace KodoCad
{
    class CadLine : CadShape
    {
        Point lineBegin;
        Point lineEnd;
        bool started;

        public override Point Origin => lineBegin;

        public override Rectangle BoundingBox => Rectangle.FromLTRB(lineBegin.X, lineBegin.Y, lineEnd.X, lineEnd.Y);

        public CadLine()
        {
        }

        public CadLine(string output)
        {
            FromOutput(output);
        }

        public CadLine(Line line)
        {
            lineBegin = line.Begin;
            lineEnd = line.end;
            started = true;
        }

        public override bool Contains(Point inReal, float threshold)
        {
            return LineContainsPoint(lineBegin, lineEnd, inReal, threshold);
        }

        public override bool Contained(Rectangle inReal)
        {
            return inReal.Contains(lineBegin) && inReal.Contains(lineEnd);
        }

        public override void Move(Point moveAmount)
        {
            lineBegin = new Point(lineBegin.X + moveAmount.X, lineBegin.Y + moveAmount.Y);
            lineEnd = new Point(lineEnd.X + moveAmount.X, lineEnd.Y + moveAmount.Y);
        }

        public override void Rotate()
        {
            throw new NotImplementedException();
        }

        public override void OnMouseMove(Point mousePositionInReal)
        {
            lineEnd = mousePositionInReal;
        }

        public override bool OnMouseDown(Point mousePositionInReal)
        {
            if (started)
            {
                lineEnd = mousePositionInReal;

                if (lineEnd.X - lineBegin.X < 0 || lineEnd.Y - lineBegin.Y < 0)
                {
                    var temp = lineBegin;
                    lineBegin = lineEnd;
                    lineEnd = temp;
                }

                return true;
            }
            else
            {
                started = true;
                lineBegin = mousePositionInReal;
                lineEnd = mousePositionInReal;
                return false;
            }
        }

        public override void OnDraw(Context context, SolidColorBrush toolBrush)
        {
            context.DrawLine(lineBegin, lineEnd, toolBrush, StrokeWidth, Stroke);
        }

        public override string ToOutput()
        {
            return $"line " +
                $"{Stringify(lineBegin.X)}," +
                $"{Stringify(lineBegin.Y)}," +
                $"{Stringify(lineEnd.X)}," +
                $"{Stringify(lineEnd.Y)}," +
                $"{(Filled ? "y" : "n")}," +
                $"{Stringify(StrokeWidth)}," +
                $"{Stringify((int)Type)}," +
                $"{Stringify(Part)}";
        }

        public override void FromOutput(string output)
        {
            var str = output;
            str = str.Trim();
            str = str.Remove(0, 5);

            var parts = str.Split(',');

            var x1 = int.Parse(parts[0]);
            var y1 = int.Parse(parts[1]);
            var x2 = int.Parse(parts[2]);
            var y2 = int.Parse(parts[3]);

            lineBegin = new Point(MicrometersToMillimeters(x1), MicrometersToMillimeters(y1));
            lineEnd = new Point(MicrometersToMillimeters(x2), MicrometersToMillimeters(y2));

            Filled = parts[4] == "y" ? true : false;
            StrokeWidth = MicrometersToMillimeters(int.Parse(parts[5]));
            Type = (CadShapeType)int.Parse(parts[6]);
            Part = int.Parse(parts[7]);
        }
    }
}
