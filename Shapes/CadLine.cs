using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kodo.Json;
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

        public override JsonNode ToOutput()
        {
            var json = new JsonNode(JsonType.Object, "line");
            json.Append(new JsonNode(JsonType.Number, "x0", Stringify(lineBegin.X)));
            json.Append(new JsonNode(JsonType.Number, "y0", Stringify(lineBegin.Y)));
            json.Append(new JsonNode(JsonType.Number, "x1", Stringify(lineEnd.X)));
            json.Append(new JsonNode(JsonType.Number, "y1", Stringify(lineEnd.Y)));

            json.Append(new JsonNode(Filled ? JsonType.True : JsonType.False, "filled"));
            json.Append(new JsonNode(JsonType.Number, "stroke", Stringify(StrokeWidth)));
            json.Append(new JsonNode(JsonType.String, "type", Stringify(Type)));
            json.Append(new JsonNode(JsonType.Number, "part", Stringify(0)));
            return json;
        }
    }
}
