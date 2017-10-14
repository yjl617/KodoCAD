using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kodo.Graphics;

namespace KodoCAD
{
    abstract class CADShape
    {
        public bool Filled { get; set; }

        public abstract bool Contains(Point inReal, float threshold);
        public abstract bool Contained(Rectangle inReal);

        public abstract void OnMouseMove(Point mousePositionInReal);
        public abstract bool OnMouseDown(Point mousePositionInReal);

        public abstract void OnDraw(Context context, Brush toolBrush);

        public abstract void Move(Point moveAmount);

        public abstract void ToSink(GeometrySink sink, Func<Point, Point> mapper);
        public abstract string ToOutput();
    }

    class CADShapeLine : CADShape
    {
        Point lineBegin;
        Point lineEnd;
        bool started;

        StrokeStyle stroke;

        public CADShapeLine(Line line) : base()
        {
            lineBegin = line.Begin;
            lineEnd = line.end;
            started = true;
        }
        public CADShapeLine()
        {
            var prop = new StrokeStyleProperties(
                startCap: CapStyle.Round,
                endCap: CapStyle.Round,
                dashCap: CapStyle.Round,
                lineJoin: LineJoin.Round,
                miterLimit: 1,
                dashStyle: DashStyle.Solid,
                dashOffset: 0);
            stroke = new StrokeStyle(prop);
        }

        public override bool Contains(Point inReal, float threshold)
        {
            var distanceAC = CADMath.Distance(lineBegin, inReal);
            var distanceBC = CADMath.Distance(lineEnd, inReal);
            var distanceAB = CADMath.Distance(lineBegin, lineEnd);

            return distanceAC + distanceBC - distanceAB < threshold;
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

        public override void OnDraw(Context context, Brush toolBrush)
        {
            if (!started)
                return;

            context.DrawLine(lineBegin, lineEnd, toolBrush, 0.1f, stroke);
        }

        public override string ToOutput()
        {
            throw new NotImplementedException();
        }

        public override void ToSink(GeometrySink sink, Func<Point, Point> mapper)
        {
            throw new NotImplementedException();
        }

        public override void Move(Point moveAmount)
        {
            lineBegin = new Point(lineBegin.X + moveAmount.X, lineBegin.Y + moveAmount.Y);
            lineEnd = new Point(lineEnd.X + moveAmount.X, lineEnd.Y + moveAmount.Y);
        }
    }

    class CADShapeRectangle : CADShape
    {
        Rectangle rectangle;

        public CADShapeRectangle(Rectangle rectangle)
        {
            this.rectangle = rectangle;
        }

        public override void OnMouseMove(Point mousePositionInReal)
        {
            throw new NotImplementedException();
        }

        public override bool OnMouseDown(Point mousePositionInReal)
        {
            throw new NotImplementedException();
        }

        public override void OnDraw(Context context, Brush toolBrush)
        {
            context.DrawRectangle(rectangle, toolBrush, 0.1f);
        }

        public override bool Contains(Point inReal, float threshold)
        {
            return rectangle.Contains(inReal);
        }

        public override bool Contained(Rectangle inReal)
        {
            throw new NotImplementedException();
        }

        public override void Move(Point moveAmount)
        {
            rectangle.Move(moveAmount.X, moveAmount.Y);
        }

        public override void ToSink(GeometrySink sink, Func<Point, Point> mapper)
        {
            throw new NotImplementedException();
        }

        public override string ToOutput()
        {
            throw new NotImplementedException();
        }
    }

    class CADShapeText : CADShape
    {
        bool moving;
        string text;
        float size;
        Point origin;
        TextLayout layout;

        public string Text => text;
        public float Size => size;
        public Point Origin => origin;

        public CADShapeText(string text, TextFormat textFormat, Point initialPosition)
        {
            this.text = text;
            size = textFormat.FontSize;
            origin = initialPosition;
            layout = new TextLayout(text, textFormat, float.MaxValue, float.MaxValue);
            moving = true;
        }

        public override bool Contained(Rectangle inReal)
        {
            var metrics = layout.Metrics;
            return inReal.Contains(origin) && inReal.Contains(new Point(origin.X + metrics.Width, origin.Y + metrics.Height));
        }

        public override bool Contains(Point inReal, float threshold)
        {
            var testPoint = new Point(inReal.X - origin.X, inReal.Y - origin.Y);
            var hitMetrics = layout.HitTestPoint(testPoint.X, testPoint.Y, out bool isTrailingHit, out bool isInside);
            return isInside;
        }

        public override void Move(Point moveAmount)
        {
            origin = new Point(origin.X + moveAmount.X, origin.Y + moveAmount.Y);
        }

        public override void OnDraw(Context context, Brush toolBrush)
        {
            context.DrawTextLayout(layout, origin, toolBrush);
        }

        public override bool OnMouseDown(Point mousePositionInReal)
        {
            moving = false;
            return true;
        }

        public override void OnMouseMove(Point mousePositionInReal)
        {
            if (moving)
                origin = mousePositionInReal;
        }

        public override string ToOutput()
        {
            throw new NotImplementedException();
        }

        public override void ToSink(GeometrySink sink, Func<Point, Point> mapper)
        {
            throw new NotImplementedException();
        }
    }

    class CADShapeCircle : CADShape
    {
        public override bool Contained(Rectangle inReal)
        {
            throw new NotImplementedException();
        }

        public override bool Contains(Point inReal, float threshold)
        {
            throw new NotImplementedException();
        }

        public override void Move(Point moveAmount)
        {
            throw new NotImplementedException();
        }

        public override void OnDraw(Context context, Brush toolBrush)
        {
            throw new NotImplementedException();
        }

        public override bool OnMouseDown(Point mousePositionInReal)
        {
            throw new NotImplementedException();
        }

        public override void OnMouseMove(Point mousePositionInReal)
        {
            throw new NotImplementedException();
        }

        public override string ToOutput()
        {
            throw new NotImplementedException();
        }

        public override void ToSink(GeometrySink sink, Func<Point, Point> mapper)
        {
            throw new NotImplementedException();
        }
    }
}
