using System;

using Kodo.Json;
using Kodo.Graphics;

namespace KodoCad
{
    class CadText : CadShape
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

        public CadText(string text, TextFormat textFormat, Point initialPosition, CadShapeAnchor anchorPoint = CadShapeAnchor.Center)
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

        public override void Rotate()
        {
            throw new NotImplementedException();
        }
    }
}
