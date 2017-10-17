using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kodo.Graphics;

namespace KodoCAD
{
    struct CADComponentPin
    {
        public readonly Point Position;

        public readonly string Name;
        public readonly int Number;
    }

    class CADComponent
    {
        string referencePrefix;
        int referenceNumber;

        string name;
        Point position;
        PathGeometry geometry;
        List<CADComponentPin> pins;

        CADShapeText referenceText;
        CADShapeText nameText;

        public CADComponent(IEnumerable<CADShape> shapes)
        {

        }
    }

    abstract class CADShape
    {
        public float StrokeWidth = 0.1f;
        public StrokeStyle Stroke { get; set; } = new StrokeStyle(new StrokeStyleProperties(
                startCap: CapStyle.Round,
                endCap: CapStyle.Round,
                dashCap: CapStyle.Round,
                lineJoin: LineJoin.Round,
                miterLimit: 1,
                dashStyle: DashStyle.Solid,
                dashOffset: 0));

        public bool Filled { get; set; }

        public abstract Point Origin { get; }

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

        public override Point Origin => lineBegin;

        public CADShapeLine()
        {
        }

        public CADShapeLine(Line line)
        {
            lineBegin = line.Begin;
            lineEnd = line.end;
            started = true;
        }

        public override bool Contains(Point inReal, float threshold)
        {
            return CADMath.LineContainsPoint(lineBegin, lineEnd, inReal, threshold);
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

            context.DrawLine(lineBegin, lineEnd, toolBrush, StrokeWidth, Stroke);
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
        bool started;
        Point startPoint;
        Rectangle rectangle;

        public override Point Origin => startPoint;

        public CADShapeRectangle()
        {
        }

        public CADShapeRectangle(Rectangle rectangle)
        {
            started = true;
            this.rectangle = rectangle;
        }

        public override void OnMouseMove(Point mousePositionInReal)
        {
            if (!started)
                return;

            if (mousePositionInReal.X < startPoint.X && mousePositionInReal.Y > startPoint.Y)
            {
                rectangle = Rectangle.FromLTRB(mousePositionInReal.X, mousePositionInReal.Y, startPoint.X, startPoint.Y);
            }
            else if (mousePositionInReal.X < startPoint.X && mousePositionInReal.Y < startPoint.Y)
            {
                rectangle = Rectangle.FromLTRB(mousePositionInReal.X, startPoint.Y, startPoint.X, mousePositionInReal.Y);
            }
            else if (mousePositionInReal.X > startPoint.X && mousePositionInReal.Y > startPoint.Y)
            {
                rectangle = Rectangle.FromLTRB(startPoint.X, startPoint.Y, mousePositionInReal.X, mousePositionInReal.Y);
            }
            else
            {
                rectangle = Rectangle.FromLTRB(startPoint.X, mousePositionInReal.Y, mousePositionInReal.X, startPoint.Y);
            }
        }

        public override bool OnMouseDown(Point mousePositionInReal)
        {
            if (!started)
            {
                started = true;
                startPoint = mousePositionInReal;
                rectangle = Rectangle.FromLTRB(mousePositionInReal.X, mousePositionInReal.Y, mousePositionInReal.X, mousePositionInReal.Y);
                return false;
            }
            else
            {
                return true;
            }
        }

        public override void OnDraw(Context context, Brush toolBrush)
        {
            if (!started)
                return;

            context.DrawRectangle(rectangle, toolBrush, StrokeWidth, Stroke);
        }

        public override bool Contains(Point inReal, float threshold)
        {
            if (!rectangle.Contains(inReal))
                return false;

            return CADMath.LineContainsPoint(rectangle.TopLeft, rectangle.TopRight, inReal, threshold) ||
                   CADMath.LineContainsPoint(rectangle.TopRight, rectangle.BottomRight, inReal, threshold) ||
                   CADMath.LineContainsPoint(rectangle.BottomLeft, rectangle.BottomRight, inReal, threshold) ||
                   CADMath.LineContainsPoint(rectangle.TopLeft, rectangle.BottomLeft, inReal, threshold);
        }

        public override bool Contained(Rectangle inReal)
        {
            return inReal.Contains(rectangle.TopLeft) && inReal.Contains(rectangle.BottomRight);
        }

        public override void Move(Point moveAmount)
        {
            rectangle = rectangle.Move(moveAmount.X, moveAmount.Y);
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

    enum PinOrientation
    {
        Left,
        Up,
        Right,
        Down
    }

    class CADShapePin : CADShape
    {
        float nameOffset = 1;

        bool moving;
        PinOrientation orientation;
        Point lineBegin;
        Point lineEnd;
        Point layoutOfNameOrigin;
        Point layoutOfNumberOrigin;
        TextLayout layoutOfName;
        TextLayout layoutOfNumber;
        Rectangle boundingBox;
        string name;
        int number;
        float lenght;

        public override Point Origin => lineBegin;

        public CADShapePin(string nameOfPin, int numberOfPin, float lenghtOfPin, PinOrientation orientationOfPin, TextFormat formatOfName, TextFormat formatOfNumber, Point initialPosition)
        {
            lineBegin = initialPosition;
            lineEnd = initialPosition;
            lenght = lenghtOfPin;
            orientation = orientationOfPin;

            moving = true;

            name = nameOfPin;
            number = numberOfPin;

            layoutOfName = new TextLayout(name.ToString(), formatOfName, float.MaxValue, float.MaxValue);
            layoutOfNumber = new TextLayout(number.ToString(), formatOfNumber, float.MaxValue, float.MaxValue);

            switch (orientationOfPin)
            {
                case PinOrientation.Left:
                    lineEnd = new Point(lineBegin.X + lenghtOfPin, lineBegin.Y);
                    layoutOfNameOrigin = new Point(lineEnd.X + nameOffset, lineEnd.Y - (layoutOfName.Metrics.Height / 2));

                    boundingBox = Rectangle.FromLTRB(lineBegin.X, lineBegin.Y - layoutOfName.Metrics.Height / 2, lineBegin.X + lenght + nameOffset + layoutOfName.Metrics.Width, lineBegin.Y + layoutOfName.Metrics.Height / 2);
                    break;
                case PinOrientation.Up:
                    lineEnd = new Point(lineBegin.X, lineBegin.Y - lenghtOfPin);
                    break;
                case PinOrientation.Right:
                    lineEnd = new Point(lineBegin.X - lenghtOfPin, lineBegin.Y);
                    layoutOfNameOrigin = new Point(lineEnd.X - nameOffset - layoutOfName.Metrics.Width, lineEnd.Y - (layoutOfName.Metrics.Height / 2));

                    boundingBox = Rectangle.FromLTRB(0, 0, 0, 0);
                    break;
                case PinOrientation.Down:
                    lineEnd = new Point(lineBegin.X, lineBegin.Y + lenghtOfPin);
                    break;
            }
        }

        public override bool Contained(Rectangle inReal)
        {
            return inReal.Contains(boundingBox.TopLeft) && inReal.Contains(boundingBox.BottomRight);
        }

        public override bool Contains(Point inReal, float threshold)
        {
            return boundingBox.Contains(inReal);
        }

        public override void Move(Point moveAmount)
        {
            lineBegin = new Point(lineBegin.X + moveAmount.X, lineBegin.Y + moveAmount.Y);
            lineEnd = new Point(lineEnd.X + moveAmount.X, lineEnd.Y + moveAmount.Y);
            layoutOfNameOrigin = new Point(layoutOfNameOrigin.X + moveAmount.X, layoutOfNameOrigin.Y + moveAmount.Y);
            boundingBox = boundingBox.Move(moveAmount.X, moveAmount.Y);
        }

        public override void OnDraw(Context context, Brush toolBrush)
        {
            context.DrawLine(lineBegin, lineEnd, toolBrush, StrokeWidth, Stroke);

            context.DrawTextLayout(layoutOfName, layoutOfNameOrigin, toolBrush);
        }

        public override bool OnMouseDown(Point mousePositionInReal)
        {
            moving = false;
            return true;
        }

        public override void OnMouseMove(Point mousePositionInReal)
        {
            if (moving)
            {
                lineBegin = mousePositionInReal;
                lineEnd = mousePositionInReal;

                switch (orientation)
                {
                    case PinOrientation.Left:
                        lineEnd = new Point(lineBegin.X + lenght, lineBegin.Y);
                        layoutOfNameOrigin = new Point(lineEnd.X + nameOffset, lineEnd.Y - (layoutOfName.Metrics.Height / 2));

                        boundingBox = Rectangle.FromLTRB(lineBegin.X, lineBegin.Y - layoutOfName.Metrics.Height / 2, lineBegin.X + lenght + nameOffset + layoutOfName.Metrics.Width, lineBegin.Y + layoutOfName.Metrics.Height / 2);
                        break;
                    case PinOrientation.Up:
                        lineEnd = new Point(lineBegin.X, lineBegin.Y - lenght);
                        break;
                    case PinOrientation.Right:
                        lineEnd = new Point(lineBegin.X - lenght, lineBegin.Y);
                        layoutOfNameOrigin = new Point(lineEnd.X - layoutOfName.Metrics.Width, lineEnd.Y - (layoutOfName.Metrics.Height / 2));
                        break;
                    case PinOrientation.Down:
                        lineEnd = new Point(lineBegin.X, lineBegin.Y + lenght);
                        break;
                }
            }
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

    class CADShapeText : CADShape
    {
        bool moving;
        string text;
        float size;
        Point origin;
        TextLayout layout;

        public string Text => text;
        public float Size => size;

        public override Point Origin => origin;

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
        public override Point Origin => throw new NotImplementedException();

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
