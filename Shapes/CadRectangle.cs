﻿using System;

using Kodo.Json;
using Kodo.Graphics;

using static KodoCad.CadMath;
using static KodoCad.CadUtilities;

namespace KodoCad
{
    class CadRectangle : CadShape
    {
        Rectangle rect;
        Rectangle bound;

        public override Point Origin => rect.TopLeft;

        public CadRectangle()
        {
            Filled = true;
        }

        public CadRectangle(Rectangle r)
        {
            Filled = true;

            rect = r;
            bound = rect.Inflate(1.05f);
        }

        public override void OnMouseMove(Point mousePositionInReal)
        {
            var p0 = bound.TopLeft;
            var p1 = mousePositionInReal;

            if (p1.X >= p0.X && p1.Y >= p0.Y)
            {
                rect = Rectangle.FromLTRB(p0.X, p0.Y, p1.X, p1.Y);
            }
            else if (p1.X < p0.X && p1.Y >= p0.Y)
            {
                rect = Rectangle.FromLTRB(p1.X, p0.Y, p0.X, p1.Y);
            }
            else if (p1.X < p0.X && p1.Y < p0.Y)
            {
                rect = Rectangle.FromLTRB(p1.X, p1.Y, p0.X, p0.Y);
            }
            else if (p1.X >= p0.X && p1.Y < p0.Y)
            {
                rect = Rectangle.FromLTRB(p0.X, p1.Y, p1.X, p0.Y);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public override bool OnMouseDown(Point mousePositionInReal)
        {
            if (bound.Right == -1)
            {
                bound = rect.Inflate(1.05f);
                return true;
            }

            rect = Rectangle.FromLTRB(mousePositionInReal.X, mousePositionInReal.Y, mousePositionInReal.X, mousePositionInReal.Y);
            bound = Rectangle.FromLTRB(mousePositionInReal.X, mousePositionInReal.Y, -1, -1);
            return false;
        }

        public override void OnDraw(Context context, SolidColorBrush brush)
        {
            var stored = brush.Color;

            brush.Color = new Color(brush.Color.Alpha, brush.Color.Red - 0.5f, brush.Color.Green - 0.5f, brush.Color.Blue - 0.5f);

            context.FillRectangle(rect, brush);

            brush.Color = stored;

            context.DrawRectangle(rect, brush, StrokeWidth, Stroke);
        }

        public override bool Contains(Point inReal, float threshold)
        {
            threshold += 0.1f;

            if (!bound.Contains(inReal))
                return false;

            return LineContainsPoint(rect.TopLeft, rect.TopRight, inReal, threshold) ||
                   LineContainsPoint(rect.TopRight, rect.BottomRight, inReal, threshold) ||
                   LineContainsPoint(rect.BottomLeft, rect.BottomRight, inReal, threshold) ||
                   LineContainsPoint(rect.TopLeft, rect.BottomLeft, inReal, threshold);
        }

        public override bool Contained(Rectangle inReal)
        {
            return inReal.Contains(rect.TopLeft) && inReal.Contains(rect.BottomRight);
        }

        public override void Move(Point moveAmount)
        {
            rect = rect.Move(moveAmount.X, moveAmount.Y);
            bound = bound.Move(moveAmount.X, moveAmount.Y);
        }

        public override void Rotate()
        {
            CadMath.Rotate(rect, rect.Center);
        }

        public override JsonNode ToOutput()
        {
            var json = new JsonNode(JsonType.Object, "rect");
            json.Append(new JsonNode(JsonType.Number, "l", Stringify(rect.Left)));
            json.Append(new JsonNode(JsonType.Number, "t", Stringify(rect.Top)));
            json.Append(new JsonNode(JsonType.Number, "r", Stringify(rect.Right)));
            json.Append(new JsonNode(JsonType.Number, "b", Stringify(rect.Bottom)));

            json.Append(new JsonNode(Filled ? JsonType.True : JsonType.False, "filled"));
            json.Append(new JsonNode(JsonType.Number, "stroke", Stringify(StrokeWidth)));
            json.Append(new JsonNode(JsonType.String, "type", Stringify(Type)));
            json.Append(new JsonNode(JsonType.Number, "part", Stringify(0)));
            return json;
        }
    }
}
