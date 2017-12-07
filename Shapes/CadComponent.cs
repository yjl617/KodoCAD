using System;
using System.Collections.Generic;
using System.Text;

using Kodo.Json;
using Kodo.Graphics;

namespace KodoCad
{
    class CadComponent : CadShape
    {
        string referencePrefix;
        int referenceNumber;

        string name;

        Point origin;
        Rectangle boundingBox;

        List<CadShape> shapes;

        CadText nameText;
        CadText referenceText;

        public CadComponent(IEnumerable<CadShape> shapesOfComponent)
        {
            shapes = new List<CadShape>();

            foreach (var shape in shapesOfComponent)
            {
                shapes.Add(shape);

                if (shape.Type == CadShapeType.ComponentName)
                {
                    nameText = shape as CadText;

                }
                if (shape.Type == CadShapeType.ComponentReference)
                {
                    referenceText = shape as CadText;
                    referencePrefix = referenceText.Text;
                }
            }
        }

        public override Point Origin => origin;

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

        public override void Rotate()
        {
            throw new NotImplementedException();
        }

        public override JsonNode ToOutput()
        {
            throw new NotImplementedException();
        }
    }
}
