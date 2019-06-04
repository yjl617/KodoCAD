using System;

using Kodo.Json;
using Kodo.Graphics;

namespace KodoCad
{
    class CadCircle : CadShape
    {
        public override Point Origin => throw new NotImplementedException();

        public override Rectangle BoundingBox => throw new NotImplementedException();

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
        }

        public override void FromOutput(string output)
        {
            throw new NotImplementedException();
        }

        public override string ToOutput()
        {
            throw new NotImplementedException();
        }
    }
}
