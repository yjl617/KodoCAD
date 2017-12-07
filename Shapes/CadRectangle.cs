using System;
using System.Globalization;
using System.Linq;

using Kodo.Json;
using Kodo.Graphics;

using static KodoCad.CadMath;
using static KodoCad.CadUtil;

namespace KodoCad
{
    class CadRectangle : CadShape
    {
        Rectangle rectangle;
        Rectangle boundingBox;

        public override Point Origin => rectangle.Center;

        public CadRectangle()
        {
        }

        public CadRectangle(Rectangle r)
        {
            rectangle = r;
            boundingBox = rectangle.Inflate(1.1f);
        }

        public override void OnMouseMove(Point mousePositionInReal)
        {
            var startPoint = boundingBox.TopLeft;

            if (mousePositionInReal.X >= startPoint.X && mousePositionInReal.Y >= startPoint.Y)
            {
                rectangle = Rectangle.FromLTRB(startPoint.X, startPoint.Y, mousePositionInReal.X, mousePositionInReal.Y);
            }
            else if (mousePositionInReal.X < startPoint.X && mousePositionInReal.Y >= startPoint.Y)
            {
                rectangle = Rectangle.FromLTRB(mousePositionInReal.X, startPoint.Y, startPoint.X, mousePositionInReal.Y);
            }
            else if (mousePositionInReal.X < startPoint.X && mousePositionInReal.Y < startPoint.Y)
            {
                rectangle = Rectangle.FromLTRB(mousePositionInReal.X, mousePositionInReal.Y, startPoint.X, startPoint.Y);
            }
            else if (mousePositionInReal.X >= startPoint.X && mousePositionInReal.Y < startPoint.Y)
            {
                rectangle = Rectangle.FromLTRB(startPoint.X, mousePositionInReal.Y, mousePositionInReal.X, startPoint.Y);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public override bool OnMouseDown(Point mousePositionInReal)
        {
            if (boundingBox.Right == -1)
            {
                boundingBox = rectangle.Inflate(1.1f);
                return true;
            }

            rectangle = Rectangle.FromLTRB(mousePositionInReal.X, mousePositionInReal.Y, mousePositionInReal.X, mousePositionInReal.Y);
            boundingBox = Rectangle.FromLTRB(mousePositionInReal.X, mousePositionInReal.Y, -1, -1);

            return false;
        }

        public override void OnDraw(Context context, SolidColorBrush toolBrush)
        {
            context.DrawRectangle(rectangle, toolBrush, StrokeWidth, Stroke);

            var stored = toolBrush.Color;

            toolBrush.Color = new Color(toolBrush.Color.Alpha, toolBrush.Color.Red - 0.1f, toolBrush.Color.Green - 0.1f, toolBrush.Color.Blue - 0.1f);

            context.FillRectangle(rectangle, toolBrush);

            toolBrush.Color = stored;
        }

        public override bool Contains(Point inReal, float threshold)
        {
            threshold += 0.1f;

            if (!boundingBox.Contains(inReal))
                return false;

            return LineContainsPoint(rectangle.TopLeft, rectangle.TopRight, inReal, threshold) ||
                   LineContainsPoint(rectangle.TopRight, rectangle.BottomRight, inReal, threshold) ||
                   LineContainsPoint(rectangle.BottomLeft, rectangle.BottomRight, inReal, threshold) ||
                   LineContainsPoint(rectangle.TopLeft, rectangle.BottomLeft, inReal, threshold);
        }

        public override bool Contained(Rectangle inReal)
        {
            return inReal.Contains(rectangle.TopLeft) && inReal.Contains(rectangle.BottomRight);
        }

        public override void Move(Point moveAmount)
        {
            rectangle = rectangle.Move(moveAmount.X, moveAmount.Y);
            boundingBox = boundingBox.Move(moveAmount.X, moveAmount.Y);
        }

        public override JsonNode ToOutput()
        {
            var json = new JsonNode(JsonType.Object, "rect");
            json.Append(new JsonNode(JsonType.Number, "l", Stringify(rectangle.Left)));
            json.Append(new JsonNode(JsonType.Number, "t", Stringify(rectangle.Top)));
            json.Append(new JsonNode(JsonType.Number, "r", Stringify(rectangle.Right)));
            json.Append(new JsonNode(JsonType.Number, "b", Stringify(rectangle.Bottom)));
            json.Append(new JsonNode(Filled ? JsonType.True : JsonType.False, "filled"));
            json.Append(new JsonNode(JsonType.Number, "stroke", Stringify(StrokeWidth)));
            json.Append(new JsonNode(JsonType.Number, "part", Stringify(0)));
            return json;
        }

        public override void Rotate()
        {
            CadMath.Rotate(rectangle, rectangle.Center);
        }
    }
}
