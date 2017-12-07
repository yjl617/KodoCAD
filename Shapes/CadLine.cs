using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kodo.Json;
using Kodo.Graphics;

namespace KodoCad
{
    class CadLine : CadShape
    {
        Point lineBegin;
        Point lineEnd;
        bool started;

        public override Point Origin => lineBegin;

        public CadLine()
        {
        }

        public CadLine(Line line)
        {
            lineBegin = line.Begin;
            lineEnd = line.end;
            started = true;
        }

        public override bool Contains(Point inReal, float threshold)
        {
            return CadMath.LineContainsPoint(lineBegin, lineEnd, inReal, threshold);
        }

        public override bool Contained(Rectangle inReal)
        {
            return inReal.Contains(lineBegin) && inReal.Contains(lineEnd);
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
            if (!started)
                return;

            context.DrawLine(lineBegin, lineEnd, toolBrush, StrokeWidth, Stroke);
        }

        public override JsonNode ToOutput()
        {
            throw new NotImplementedException();
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
    }
}
