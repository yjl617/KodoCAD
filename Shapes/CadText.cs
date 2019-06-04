using System;

using Kodo.Graphics;

using static KodoCad.CadMath;
using static KodoCad.CadUtilities;

namespace KodoCad
{
    class CadText : CadShape
    {
        bool moving;
        readonly string text;
        readonly float size;
        Point origin;
        TextFormat format;
        TextLayout layout;
        readonly CadShapeAnchor anchor;

        Rectangle boundingBox;

        public string Text => text;
        public float Size => size;
        public CadShapeAnchor Anchor => anchor;

        public override Point Origin => origin;

        public override Rectangle BoundingBox => boundingBox;

        public CadText(string text, string fontFamily, float fontSize, FontWeight fontWeight, FontStyle fontStyle, Point initialPosition, CadShapeAnchor anchorPoint = CadShapeAnchor.Left)
        {
            this.text = text;
            size = fontSize;
            format = new TextFormat(fontFamily, fontWeight, fontStyle, FontStretch.Normal, fontSize, "en-US");
            layout = new TextLayout(text, format, float.MaxValue, float.MaxValue);
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

        public override void Rotate()
        {
            throw new NotImplementedException();
        }

        public override string ToOutput()
        {
            return $"text " +
                $"{Stringify(origin.X)}," +
                $"{Stringify(origin.Y)}," +
                $"{format.FontFamilyName}," +
                $"{Stringify((int)format.FontWeight)}," +
                $"{Stringify((int)format.FontStyle)}," +
                $"{Stringify(size)}," +
                $"{Stringify((int)Anchor)}," +
                $"{Stringify(Part)}," +
                $"{text}";
        }

        public override void FromOutput(string output)
        {
            throw new NotImplementedException();
        }
    }
}
