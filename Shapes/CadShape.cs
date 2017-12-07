using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kodo.Json;
using Kodo.Graphics;

namespace KodoCad
{
    enum CadRotation
    {
        CW,
        CCW
    }

    enum CadShapeAnchor
    {
        Left,
        Center,
        Right
    }

    enum CadShapeType
    {
        Normal,
        ComponentReference,
        ComponentName
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
        public bool Permanent { get; set; }
        public bool Locked { get; set; }

        public CadShapeType Type { get; set; }

        public abstract Point Origin { get; }

        public abstract bool Contains(Point inReal, float threshold);
        public abstract bool Contained(Rectangle inReal);

        public abstract void OnMouseMove(Point mousePositionInReal);
        public abstract bool OnMouseDown(Point mousePositionInReal);

        public abstract void OnDraw(Context context, SolidColorBrush toolBrush);

        public abstract void Rotate();
        public abstract void Move(Point moveAmount);

        public abstract JsonNode ToOutput();
    }
}
