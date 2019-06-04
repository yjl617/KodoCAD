using System;
using System.Collections.Generic;
using System.Text;

using Kodo.Graphics;

namespace KodoCad
{
    class CadComponent : CadShape
    {
        string componentName;
        string referencePrefix;
        int referenceNumber;

        public override Point Origin => origin;

        public override Rectangle BoundingBox => boundingBox;

        Point origin;
        Rectangle boundingBox;

        List<CadShape> shapes;

        Matrix3x2 shapeTransform;

        CadText nameText;
        CadText referenceText;

        public CadComponent(IEnumerable<CadShape> shapesOfComponent, Point componentOrigin)
        {
            origin = componentOrigin;

            shapeTransform = Matrix3x2.Translation(origin);

            shapes = new List<CadShape>();

            var xMin = componentOrigin.X;
            var yMin = componentOrigin.Y;
            var xMax = componentOrigin.X;
            var yMax = componentOrigin.Y;
            var shapeOriginDiff = new Point(-origin.X, -origin.Y);

            foreach (var shape in shapesOfComponent)
            {
                var bounds = shape.BoundingBox;

                xMin = Math.Min(xMin, bounds.Left);
                yMin = Math.Min(yMin, bounds.Top);
                xMax = Math.Max(xMax, bounds.Right);
                yMax = Math.Max(yMax, bounds.Bottom);

                shape.Move(shapeOriginDiff);
                shapes.Add(shape);

                if (shape.Type == CadShapeType.ComponentName)
                {
                    nameText = shape as CadText;
                    componentName = nameText.Text;

                }
                if (shape.Type == CadShapeType.ComponentReference)
                {
                    referenceText = shape as CadText;
                    referencePrefix = referenceText.Text;
                }
            }

            boundingBox = Rectangle.FromLTRB(xMin, yMin, xMax, yMax);
        }

        public override bool Contained(Rectangle rectangleOnWorld)
        {
            return rectangleOnWorld.Contains(boundingBox.TopLeft) && rectangleOnWorld.Contains(boundingBox.BottomRight);
        }

        public override bool Contains(Point pointOnWorld, float threshold)
        {
            return boundingBox.Contains(pointOnWorld);
        }

        public override void Move(Point moveAmount)
        {
            boundingBox = boundingBox.Move(moveAmount.X, moveAmount.Y);

            foreach (var shape in shapes)
            {
                shape.Move(moveAmount);
            }
        }

        public override void OnDraw(Context context, SolidColorBrush toolBrush)
        {
            var storedTransform = context.GetTransform();
            context.SetTransform(shapeTransform * storedTransform);

            foreach (var shape in shapes)
            {
                shape.OnDraw(context, toolBrush);
            }

            context.SetTransform(storedTransform);

            toolBrush.Color = Color.IndianRed;
            context.DrawRectangle(boundingBox, toolBrush, 0.1f);
        }

        public override bool OnMouseDown(Point mousePositionInReal)
        {
            throw new NotImplementedException();
        }

        public override void OnMouseMove(Point mousePositionInReal)
        {
            throw new NotImplementedException();
        }

        public override void Rotate()
        {
            throw new NotImplementedException();
        }

        public override string ToOutput()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("component");

            foreach (var shape in shapes)
            {
                stringBuilder.Append("    ");
                stringBuilder.AppendLine(shape.ToOutput());
            }

            stringBuilder.AppendLine("end");

            return stringBuilder.ToString();
        }

        public override void FromOutput(string output)
        {
            throw new NotImplementedException();
        }
    }
}
