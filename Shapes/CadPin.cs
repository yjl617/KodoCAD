using System;

using Kodo.Json;
using Kodo.Graphics;

namespace KodoCad
{
    enum PinOrientation
    {
        Left,
        Up,
        Right,
        Down
    }

    class CadPin : CadShape
    {
        float nameOffset = 1;

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

        Point actualOrigin;

        TextFormat nameFormat;
        TextFormat numberFormat;

        public float OffsetOfName => nameOffset;

        public PinOrientation Orientation => orientation;

        public string Name => name;
        public int Number => number;
        public float Length => lenght;

        public float SizeOfName => nameFormat.FontSize;
        public float SizeOfNumber => numberFormat.FontSize;

        public override Point Origin => lineBegin;

        public CadPin(string nameOfPin, int numberOfPin, float lenghtOfPin, TextFormat formatOfName, TextFormat formatOfNumber, Point initialPosition, CadPin original = null)
        {
            nameFormat = formatOfName;
            numberFormat = formatOfNumber;

            name = nameOfPin;
            number = numberOfPin;

            layoutOfName = new TextLayout(name.ToString(), formatOfName, float.MaxValue, float.MaxValue);
            layoutOfNumber = new TextLayout(number.ToString(), formatOfNumber, float.MaxValue, float.MaxValue);

            lenght = lenghtOfPin;

            orientation = PinOrientation.Left;

            if (original == null)
            {
                actualOrigin = initialPosition;
                lineBegin = initialPosition;
                lineEnd = new Point(initialPosition.X + lenghtOfPin, initialPosition.Y);
                layoutOfNameOrigin = new Point(lineEnd.X + nameOffset, lineEnd.Y - (layoutOfName.Metrics.Height / 2));
                layoutOfNumberOrigin = new Point(lineBegin.X + (lenghtOfPin / 2 - layoutOfNumber.Metrics.Width / 2), lineBegin.Y - (layoutOfName.Metrics.Height));
                boundingBox = Rectangle.FromLTRB(
                    lineBegin.X,
                    lineBegin.Y - layoutOfName.Metrics.Height / 2,
                    lineBegin.X + lenght + nameOffset + layoutOfName.Metrics.Width,
                    lineBegin.Y + layoutOfName.Metrics.Height / 2);

                boundingBox = boundingBox.Inflate(1.1f);
            }
            else
            {
                actualOrigin = original.actualOrigin;

                lineBegin = actualOrigin;
                lineEnd = new Point(actualOrigin.X + lenghtOfPin, actualOrigin.Y);
                layoutOfNameOrigin = new Point(lineEnd.X + nameOffset, lineEnd.Y - (layoutOfName.Metrics.Height / 2));
                layoutOfNumberOrigin = new Point(lineBegin.X + (lenghtOfPin / 2 - layoutOfNumber.Metrics.Width / 2), lineBegin.Y - (layoutOfName.Metrics.Height));
                boundingBox = Rectangle.FromLTRB(
                    lineBegin.X,
                    lineBegin.Y - layoutOfName.Metrics.Height / 2,
                    lineBegin.X + lenght + nameOffset + layoutOfName.Metrics.Width,
                    lineBegin.Y + layoutOfName.Metrics.Height / 2);

                boundingBox = boundingBox.Inflate(1.1f);

                switch (original.orientation)
                {
                    case PinOrientation.Left:
                        break;
                    case PinOrientation.Up:
                        Rotate();
                        break;
                    case PinOrientation.Right:
                        Rotate();
                        Rotate();
                        break;
                    case PinOrientation.Down:
                        Rotate();
                        Rotate();
                        Rotate();
                        break;
                }
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

        public override void Rotate()
        {
            var centerOfRotation = lineBegin;

            switch (orientation)
            {
                case PinOrientation.Left:
                    lineBegin = CadMath.Rotate(lineBegin, centerOfRotation);
                    lineEnd = CadMath.Rotate(lineEnd, centerOfRotation);
                    boundingBox = CadMath.Rotate(boundingBox, centerOfRotation);

                    orientation = PinOrientation.Up;
                    break;
                case PinOrientation.Up:
                    lineBegin = CadMath.Rotate(lineBegin, centerOfRotation);
                    lineEnd = CadMath.Rotate(lineEnd, centerOfRotation);
                    boundingBox = CadMath.Rotate(boundingBox, centerOfRotation);

                    orientation = PinOrientation.Right;
                    break;
                case PinOrientation.Right:
                    lineBegin = CadMath.Rotate(lineBegin, centerOfRotation);
                    lineEnd = CadMath.Rotate(lineEnd, centerOfRotation);
                    boundingBox = CadMath.Rotate(boundingBox, centerOfRotation);

                    orientation = PinOrientation.Down;
                    break;
                case PinOrientation.Down:
                    lineBegin = CadMath.Rotate(lineBegin, centerOfRotation);
                    lineEnd = CadMath.Rotate(lineEnd, centerOfRotation);
                    boundingBox = CadMath.Rotate(boundingBox, centerOfRotation);

                    orientation = PinOrientation.Left;
                    break;
            }
        }

        public override void Move(Point moveAmount)
        {
            actualOrigin = new Point(actualOrigin.X + moveAmount.X, actualOrigin.Y + moveAmount.Y);
            lineBegin = new Point(lineBegin.X + moveAmount.X, lineBegin.Y + moveAmount.Y);
            lineEnd = new Point(lineEnd.X + moveAmount.X, lineEnd.Y + moveAmount.Y);
            layoutOfNameOrigin = new Point(layoutOfNameOrigin.X + moveAmount.X, layoutOfNameOrigin.Y + moveAmount.Y);
            layoutOfNumberOrigin = new Point(layoutOfNumberOrigin.X + moveAmount.X, layoutOfNumberOrigin.Y + moveAmount.Y);
            boundingBox = boundingBox.Move(moveAmount.X, moveAmount.Y);
        }

        public override void OnDraw(Context context, SolidColorBrush toolBrush)
        {
            context.DrawLine(lineBegin, lineEnd, toolBrush, StrokeWidth, Stroke);

            var storedTransform = context.GetTransform();
            var nameOrigin = layoutOfNameOrigin;
            var numberOrigin = layoutOfNumberOrigin;

            switch (orientation)
            {
                case PinOrientation.Left:
                    break;
                case PinOrientation.Right:
                    nameOrigin = new Point(lineEnd.X - nameOffset - layoutOfName.Metrics.Width, lineEnd.Y - (layoutOfName.Metrics.Height / 2));
                    numberOrigin = new Point(lineEnd.X + (lenght / 2 - layoutOfNumber.Metrics.Width / 2), lineBegin.Y - (layoutOfName.Metrics.Height));
                    break;
                case PinOrientation.Up:
                    context.SetTransform(Matrix3x2.Rotation(90, lineBegin) * storedTransform);
                    break;
                case PinOrientation.Down:
                    context.SetTransform(Matrix3x2.Rotation(-90, lineBegin) * storedTransform);
                    break;
            }

            var stored = toolBrush.Color;
            //toolBrush.Color = Color.Black;
            context.DrawTextLayout(layoutOfName, nameOrigin, toolBrush);
            context.DrawTextLayout(layoutOfNumber, numberOrigin, toolBrush);
            toolBrush.Color = stored;

            context.SetTransform(storedTransform);
        }

        public override bool OnMouseDown(Point mousePositionInReal)
        {
            return true;
        }

        public override void OnMouseMove(Point mousePositionInReal)
        {
        }

        public override JsonNode ToOutput()
        {
            throw new NotImplementedException();
        }
    }
}
