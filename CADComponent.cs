using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kodo.Json;
using Kodo.Graphics;

namespace KodoCad
{
    struct CadComponentPin
    {
        public readonly Point Position;

        public readonly string Name;
        public readonly int Number;
    }

    class CadComponent
    {
        string referencePrefix;
        int referenceNumber;

        string name;
        Point position;
        PathGeometry geometry;
        List<CadComponentPin> pins;

        CadShapeText referenceText;
        CadShapeText nameText;

        public CadComponent(IEnumerable<CadShape> shapes)
        {

        }
    }

    abstract class CadShape
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

        public abstract void OnDraw(Context context, SolidColorBrush toolBrush);

        public abstract void Move(Point moveAmount);

        public abstract void ToSink(GeometrySink sink, Func<Point, Point> mapper);
        public abstract JsonNode ToOutput();
    }

    class CadShapeLine : CadShape
    {
        Point lineBegin;
        Point lineEnd;
        bool started;

        public override Point Origin => lineBegin;

        public CadShapeLine()
        {
        }

        public CadShapeLine(Line line)
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

    class CadShapeRectangle : CadShape
    {
        bool started;
        Point startPoint;
        Rectangle rectangle;

        public override Point Origin => startPoint;

        public CadShapeRectangle()
        {
        }

        public CadShapeRectangle(Rectangle rectangle)
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

        public override void OnDraw(Context context, SolidColorBrush toolBrush)
        {
            if (!started)
                return;

            context.DrawRectangle(rectangle, toolBrush, StrokeWidth, Stroke);
        }

        public override bool Contains(Point inReal, float threshold)
        {
            if (!rectangle.Contains(inReal))
                return false;

            return CadMath.LineContainsPoint(rectangle.TopLeft, rectangle.TopRight, inReal, threshold) ||
                   CadMath.LineContainsPoint(rectangle.TopRight, rectangle.BottomRight, inReal, threshold) ||
                   CadMath.LineContainsPoint(rectangle.BottomLeft, rectangle.BottomRight, inReal, threshold) ||
                   CadMath.LineContainsPoint(rectangle.TopLeft, rectangle.BottomLeft, inReal, threshold);
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

        public override JsonNode ToOutput()
        {
            var json = new JsonNode(JsonType.Object, "Rectangle");
            json.Append(new JsonNode(JsonType.Number, "l", rectangle.Left.ToString(CultureInfo.InvariantCulture)));
            json.Append(new JsonNode(JsonType.Number, "t", rectangle.Top.ToString(CultureInfo.InvariantCulture)));
            json.Append(new JsonNode(JsonType.Number, "r", rectangle.Right.ToString(CultureInfo.InvariantCulture)));
            json.Append(new JsonNode(JsonType.Number, "b", rectangle.Bottom.ToString(CultureInfo.InvariantCulture)));

            json.Append(new JsonNode(Filled ? JsonType.True : JsonType.False, "filled"));
            json.Append(new JsonNode(JsonType.Number, "stroke", StrokeWidth.ToString(CultureInfo.InvariantCulture)));
            json.Append(new JsonNode(JsonType.Number, "part", 0.ToString(CultureInfo.InvariantCulture)));
            return json;
        }
    }

    enum PinOrientation
    {
        Left,
        Up,
        Right,
        Down
    }

    class CadShapePin : CadShape
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

        public CadShapePin(string nameOfPin, int numberOfPin, float lenghtOfPin, PinOrientation orientationOfPin, TextFormat formatOfName, TextFormat formatOfNumber, Point initialPosition)
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

        public override void OnDraw(Context context, SolidColorBrush toolBrush)
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

        public override JsonNode ToOutput()
        {
            throw new NotImplementedException();
        }

        public override void ToSink(GeometrySink sink, Func<Point, Point> mapper)
        {
            throw new NotImplementedException();
        }
    }

    enum CadShapeAnchor
    {
        Left,
        Center,
        Right
    }

    class CadShapeText : CadShape
    {
        bool moving;
        string text;
        float size;
        Point origin;
        TextLayout layout;
        CadShapeAnchor anchor;

        Rectangle boundingBox;

        public string Text => text;
        public float Size => size;
        public CadShapeAnchor Anchor => anchor;

        public override Point Origin => origin;

        public CadShapeText(string text, TextFormat textFormat, Point initialPosition, CadShapeAnchor anchorPoint = CadShapeAnchor.Center)
        {
            this.text = text;
            size = textFormat.FontSize;
            layout = new TextLayout(text, textFormat, float.MaxValue, float.MaxValue);
            anchor = anchorPoint;
            origin = initialPosition;

            switch (anchor)
            {
                case CadShapeAnchor.Left:
                    boundingBox = Rectangle.FromXYWH(initialPosition.X, initialPosition.Y - layout.Metrics.Height / 2, layout.Metrics.Width, layout.Metrics.Height);
                    break;
                case CadShapeAnchor.Center:
                    boundingBox = Rectangle.FromXYWH(initialPosition.X - layout.Metrics.Width / 2, initialPosition.Y - layout.Metrics.Height / 2, layout.Metrics.Width, layout.Metrics.Height);
                    break;
                case CadShapeAnchor.Right:
                    break;
            }

            moving = true;
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
            origin = new Point(origin.X + moveAmount.X, origin.Y + moveAmount.Y);
            boundingBox = boundingBox.Move(moveAmount.X, moveAmount.Y);
        }

        public override void OnDraw(Context context, SolidColorBrush toolBrush)
        {
            var drawPoint = origin;

            switch (anchor)
            {
                case CadShapeAnchor.Left:
                    drawPoint = new Point(origin.X, origin.Y - layout.Metrics.Height / 2);
                    break;
                case CadShapeAnchor.Center:
                    drawPoint = new Point(origin.X - layout.Metrics.Width / 2, origin.Y - layout.Metrics.Height / 2);
                    break;
                case CadShapeAnchor.Right:
                    break;
            }

            context.DrawTextLayout(layout, drawPoint, toolBrush);

            toolBrush.Color = new Color(0xFF68768A);
            context.DrawRectangle(boundingBox, toolBrush, 0.05f);
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
                origin = mousePositionInReal;

                switch (anchor)
                {
                    case CadShapeAnchor.Left:
                        boundingBox = Rectangle.FromXYWH(origin.X, origin.Y - layout.Metrics.Height / 2, layout.Metrics.Width, layout.Metrics.Height);
                        break;
                    case CadShapeAnchor.Center:
                        boundingBox = Rectangle.FromXYWH(origin.X - layout.Metrics.Width / 2, origin.Y - layout.Metrics.Height / 2, layout.Metrics.Width, layout.Metrics.Height);
                        break;
                    case CadShapeAnchor.Right:
                        break;
                }
            }
        }

        public override JsonNode ToOutput()
        {
            throw new NotImplementedException();
        }

        public override void ToSink(GeometrySink sink, Func<Point, Point> mapper)
        {
            throw new NotImplementedException();
        }
    }

    class CadShapeCircle : CadShape
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

        public override void OnDraw(Context context, SolidColorBrush toolBrush)
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

        public override JsonNode ToOutput()
        {
            throw new NotImplementedException();
        }

        public override void ToSink(GeometrySink sink, Func<Point, Point> mapper)
        {
            throw new NotImplementedException();
        }
    }
}
