using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kodo.Graphics;
using Kodo.Graphics.Style;

namespace KodoCAD
{
    enum GridMode { None, Lines, Points }
    enum OriginMode { Absolute, Center }
    enum SnapMode { Fine, Coarse }

    enum ToolMode { Select, Move, Line, Rectangle, Circle, Text }
    enum ToolState { None, Armed, Editing }



    delegate void ShapeEditHandler(Set<CADShape> shape);

    class CADEditor : Control
    {
        public event ShapeEditHandler OnShapeEdit;

        Color gridOutlineColor = Color.FromAColor(0.5f, Color.GhostWhite);

        Color gridlineMajorColor = Color.FromAColor(0.2f, Color.GhostWhite);
        Color gridlineMinorColor = Color.FromAColor(0.05f, Color.GhostWhite);

        //Color gridlineMajorColor = Color.FromAColor(0.2f, new Color(0xFF748496));
        //Color gridlineMinorColor = Color.FromAColor(0.05f, new Color(0xFF748496));

        Color griddotMajorColor = Color.FromAColor(0.4f, Color.GhostWhite);
        Color griddotMinorColor = Color.FromAColor(0.1f, Color.GhostWhite);

        Color crosshairCursorColor = Color.GhostWhite;
        Color crosshairRelativeColor = Color.GhostWhite;

        Color geometryColor = Color.IndianRed;

        float crosshairWidth = 1f;
        float crosshairHeight = 20f;

        Set<CADShape> shapesSelected = new Set<CADShape>();
        bool multiSelected;
        List<CADShape> shapes = new List<CADShape>();
        CADShape shapeInEditing;

        GridMode gridMode = GridMode.Lines;
        OriginMode originMode = OriginMode.Absolute;
        SnapMode snapMode = SnapMode.Coarse;
        ToolMode toolMode = ToolMode.Select;
        ToolState toolState = ToolState.None;

        int gridStepUnitsMinor = 2;
        int gridStepUnitsMajor = 10;

        float zoomFactor = 1.0f;
        int zoomFactorIndex = 0;
        float[] zoomFactors = new float[] { 1.0f, 0.5f, 0.25f, 0.1f, 0.05f, 0.025f, 0.01f, 0.005f, 0.0025f, 0.001f, 0.0005f };

        Point dpi;

        Size gridSize = new Size(300, 210);

        Rectangle selection;
        Point selectionStartPoint;
        Point movementStartPoint;

        Point origin;
        Point originOnReality;
        Point originCenter;
        Point originCenterOnReality = new Point(150, 105);
        Point originMovable;
        Point originMovableOnReality;

        Point gridStepMinor;
        Point gridStepMajor;

        Point mousePositionOnScreen;
        Point mousePositionOnScreenSnapped;
        Point mousePositionWhenPressed;
        Point mousePositionRelativeToOrigin;
        Point mousePositionRelativeToOriginReal;
        Point mousePositionRelativeToOriginRealSnapped;
        Point mousePositionRelativeToOriginCenter;
        Point mousePositionRelativeToOriginCenterRealSnapped;
        Point mousePositionRelativeToOriginMovable;
        Point mousePositionRelativeToOriginMovableReal;
        Point mousePositionRelativeToOriginMovableRealSnapped;

        Point linesFromOrigin;
        Point linesFromOriginCenter;
        Point linesFromOriginMovable;

        TextFormat textFormatTest;

        TextFormat textFormatBig;
        TextFormat textFormatSmall;

        Matrix3x2 matrixGridToReal;
        Matrix3x2 matrixRealToGrid;
        Matrix3x2 matrixOriginTranslation;

        public OriginMode OriginMode
        {
            get { return originMode; }
            set
            {
                if (originMode != value)
                {
                    originMode = value;
                    Update();
                }
            }
        }

        public Size GridSize
        {
            get { return gridSize; }
            set
            {
                if (gridSize != value)
                {
                    gridSize = value;
                    originCenterOnReality = new Point(gridSize.Width / 2, gridSize.Height / 2);
                    originMovableOnReality = new Point(0, 0);
                    Update();
                }
            }
        }

        public void ReplaceShape(CADShape shapeOriginal, CADShape shapeNew)
        {
            shapes.Remove(shapeOriginal);
            shapes.Add(shapeNew);
            Update();
        }

        public CADEditor(Window window)
            : base(window)
        {
        }

        protected override void OnMouseUp(Mouse mouse)
        {
            base.OnMouseUp(mouse);

            if (toolMode == ToolMode.Select && toolState == ToolState.Editing)
            {
                selection = new Rectangle();
                toolState = ToolState.Armed;
                Update();
            }
        }

        protected override void OnMouseDown(Mouse mouse)
        {
            base.OnMouseDown(mouse);

            //if (mouse.Button == MouseButton.Middle)
            {
                mousePositionWhenPressed = mouse.Position;
            }

            if (mouse.Button == MouseButton.Left && toolMode == ToolMode.Select)
            {
                var rel = SelectRelative(mousePositionRelativeToOriginRealSnapped, mousePositionRelativeToOriginCenterRealSnapped);
                toolState = ToolState.Editing;
                selectionStartPoint = rel;
            }
            else if (mouse.Button == MouseButton.Left && toolMode == ToolMode.Move)
            {
                toolMode = ToolMode.Select;
                toolState = ToolState.Armed;

            }
            else if (mouse.Button == MouseButton.Left && shapeInEditing != null)
            {
                toolState = ToolState.Editing;

                var rel = SelectRelative(mousePositionRelativeToOriginRealSnapped, mousePositionRelativeToOriginCenterRealSnapped);

                var stopEditing = shapeInEditing.OnMouseDown(rel);

                if (stopEditing)
                {
                    toolState = ToolState.None;

                    shapeInEditing = null;

                    switch (toolMode)
                    {
                        case ToolMode.Line:
                            shapeInEditing = new CADShapeLine();
                            toolState = ToolState.Armed;
                            shapes.Add(shapeInEditing);

                            Update();
                            break;
                        case ToolMode.Text:
                            toolMode = ToolMode.Select;
                            toolState = ToolState.Armed;
                            Update();

                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }

        protected override void OnMouseWheel(Mouse mouse)
        {
            mousePositionOnScreen = mouse.Position;

            var zoomFactorIndexOld = zoomFactorIndex;
            zoomFactorIndex = CADMath.Clamp(zoomFactorIndex + (mouse.WheelDelta > 0 ? 1 : -1), 0, zoomFactors.Length - 1);
            zoomFactor = zoomFactors[zoomFactorIndex];

            if (zoomFactorIndex != zoomFactorIndexOld)
            {
                var snapStep = snapMode == SnapMode.Fine ? gridStepMinor : gridStepMajor;
                var snapUnitStep = snapMode == SnapMode.Fine ? gridStepUnitsMinor : gridStepUnitsMajor;

                var zoomOld = zoomFactors[zoomFactorIndexOld];
                var zoomNew = zoomFactors[zoomFactorIndex];
                var zoomRatio = zoomOld / zoomNew;

                var unitsOldX = linesFromOrigin.X * snapUnitStep;
                var unitsNewX = unitsOldX * zoomRatio;
                var unitsDiffX = (unitsOldX - unitsNewX) / snapUnitStep;

                var unitsOldY = linesFromOrigin.Y * snapUnitStep;
                var unitsNewY = unitsOldY * zoomRatio;
                var unitsDiffY = (unitsOldY - unitsNewY) / snapUnitStep;

                gridSize = new Size(CADMath.Round(gridSize.Width * zoomRatio, 0), CADMath.Round(gridSize.Height * zoomRatio, 0));

                origin = new Point(origin.X + snapStep.X * unitsDiffX, origin.Y + snapStep.Y * unitsDiffY);
            }

            Update();
        }

        protected override void OnMouseMove(Mouse mouse)
        {
            CaptureKeyboard();

            mousePositionOnScreen = mouse.Position;

            var distanceX = -(mousePositionOnScreen.X - mousePositionWhenPressed.X);
            var distanceY = -(mousePositionOnScreen.Y - mousePositionWhenPressed.Y);

            if (mouse.Button == MouseButton.Middle)
            {
                mousePositionWhenPressed = mousePositionOnScreen;
                origin = new Point(origin.X - distanceX, origin.Y - distanceY);
            }

            Update();

            if (toolMode == ToolMode.Select)
            {
                if (toolState == ToolState.Editing)
                {
                    var sta = selectionStartPoint;
                    var rel = SelectRelative(mousePositionRelativeToOriginRealSnapped, mousePositionRelativeToOriginCenterRealSnapped);

                    if (distanceX >= 0 && distanceY >= 0)
                    {
                        var lt = rel;
                        var rb = sta;
                        selection = Rectangle.FromLTRB(lt.X, lt.Y, rb.X, rb.Y);
                    }
                    else if (distanceX < 0 && distanceY >= 0)
                    {
                        var lt = new Point(sta.X, rel.Y);
                        var rb = new Point(rel.X, sta.Y);
                        selection = Rectangle.FromLTRB(lt.X, lt.Y, rb.X, rb.Y);
                    }
                    else if (distanceX >= 0 && distanceY < 0)
                    {
                        var lt = new Point(rel.X, sta.Y);
                        var rb = new Point(sta.X, rel.Y);
                        selection = Rectangle.FromLTRB(lt.X, lt.Y, rb.X, rb.Y);
                    }
                    else
                    {
                        var lt = sta;
                        var rb = rel;
                        selection = Rectangle.FromLTRB(lt.X, lt.Y, rb.X, rb.Y);
                    }

                    shapesSelected.Clear();
                    multiSelected = true;

                    foreach (var shape in shapes)
                    {
                        if (shape.Contained(selection))
                        {
                            shapesSelected.Add(shape);
                        }
                    }
                }
                else
                {
                    if (shapesSelected.Count > 0 && multiSelected)
                        return;

                    shapesSelected.Clear();

                    multiSelected = false;

                    var rel = SelectRelative(mousePositionRelativeToOriginReal, mousePositionRelativeToOriginCenterRealSnapped);

                    foreach (var shape in shapes)
                    {
                        if (shape.Contains(rel, 0.1f * zoomFactor))
                        {
                            shapesSelected.Add(shape);
                            break;
                        }
                    }
                }

                Refresh();
            }
            else if (toolMode == ToolMode.Move && toolState == ToolState.Editing)
            {
                var rel = SelectRelative(mousePositionRelativeToOriginRealSnapped, mousePositionRelativeToOriginCenterRealSnapped);

                var moveAmount = new Point(rel.X - movementStartPoint.X, rel.Y - movementStartPoint.Y);

                movementStartPoint = rel;

                foreach (var shape in shapesSelected)
                {
                    shape.Move(moveAmount);
                }

                Update();
            }
            else if (shapeInEditing != null)
            {
                var rel = SelectRelative(mousePositionRelativeToOriginRealSnapped, mousePositionRelativeToOriginCenterRealSnapped);
                shapeInEditing.OnMouseMove(rel);
            }
            /*else
            {
                if (shapesSelected.Count > 1)
                    return;

                shapesSelected.Clear();

                foreach (var shape in shapes)
                {
                    if (shape.Contains(mousePositionRelativeToOriginRealSnapped, 0.1f * zoomFactor))
                    {
                        shapesSelected.Add(shape);
                        break;
                    }
                }
            }*/
        }

        protected override void OnKeyDown(Key key)
        {
            if (key == Key.Escape)
            {
                if (shapeInEditing != null)
                {
                    if (toolState != ToolState.Armed)
                    {
                        shapes.Remove(shapeInEditing);
                        shapeInEditing = null;

                        shapeInEditing = new CADShapeLine();

                        toolState = ToolState.Armed;

                        shapes.Add(shapeInEditing);
                    }
                    else
                    {
                        shapes.Remove(shapeInEditing);
                        shapeInEditing = null;
                        toolMode = ToolMode.Select;
                        toolState = ToolState.Armed;
                    }
                }
                else if (toolMode == ToolMode.Move)
                {
                    toolMode = ToolMode.Select;
                    toolState = ToolState.Armed;
                }
                else
                {
                    if (toolState == ToolState.Armed)
                    {
                        if (multiSelected)
                        {
                            shapesSelected.Clear();
                            multiSelected = false;
                        }
                    }
                }

                Refresh();
            }
            else if (key == Key.E)
            {
                if (shapesSelected.Count > 0)
                {
                    OnShapeEdit?.Invoke(shapesSelected);
                }
            }
            else if (key == Key.S && Keyboard.IsDown(Key.ShiftLeft))
            {
                snapMode = snapMode == SnapMode.Fine ? SnapMode.Coarse : SnapMode.Fine;
                Update();
            }
            else if (key == Key.O && Keyboard.IsDown(Key.ShiftLeft))
            {
                originMode = originMode == OriginMode.Absolute ? OriginMode.Center : OriginMode.Absolute;
                Update();
            }
            else if (key == Key.G && Keyboard.IsDown(Key.ShiftLeft))
            {
                switch (gridMode)
                {
                    case GridMode.None:
                        gridMode = GridMode.Lines;
                        break;
                    case GridMode.Lines:
                        gridMode = GridMode.Points;
                        break;
                    case GridMode.Points:
                        gridMode = GridMode.None;
                        break;
                }

                Update();
            }
            else if (key == Key.Delete)
            {
                foreach (var shape in shapesSelected)
                {
                    shapes.Remove(shape);
                }

                shapesSelected.Clear();
                Update();
            }
            else if (key == Key.T && toolMode != ToolMode.Text)
            {
                toolMode = ToolMode.Text;
                toolState = ToolState.Editing;

                var rel = SelectRelative(mousePositionRelativeToOriginRealSnapped, mousePositionRelativeToOriginCenterRealSnapped);

                var textFormat = new TextFormat("Montserrat", FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, 2, "en-US");
                shapeInEditing = new CADShapeText("CADShapeText", textFormat, rel);
                shapes.Add(shapeInEditing);
                Refresh();
            }
            else if (key == Key.W && toolMode != ToolMode.Line)
            {
                toolMode = ToolMode.Line;

                shapeInEditing = new CADShapeLine();
                toolState = ToolState.Armed;

                shapes.Add(shapeInEditing);

                Update();
            }
            else if (key == Key.M)
            {
                toolMode = ToolMode.Move;
                toolState = ToolState.Armed;

                var rel = SelectRelative(mousePositionRelativeToOriginRealSnapped, mousePositionRelativeToOriginCenterRealSnapped);

                if (shapesSelected.Count > 0)
                {
                    toolState = ToolState.Editing;
                    movementStartPoint = rel;
                }

                Refresh();
            }
            else if (key == Key.Space && IsMouseInside == true)
            {
                originMovable = mousePositionOnScreenSnapped;
                originMovableOnReality = ScreenToReal(originMovable);
                Update();
            }
        }

        Point SelectRelative(Point absolute, Point center)
        {
            return originMode == OriginMode.Absolute ? absolute : center;
        }

        /// <summary>
        /// In real coordinates.
        /// </summary>
        /// <param name="abs"></param>
        Point AbsoluteToCenter(Point abs)
        {
            return new Point(abs.X + originCenterOnReality.X, abs.Y + originCenterOnReality.Y);
        }

        Point CenterToAbsolute(Point center)
        {
            return new Point(center.X - originCenterOnReality.X, center.Y - originCenterOnReality.Y);
        }

        Point RealToGrid(Point real)
        {
            return CADMath.Transform(real, matrixRealToGrid);
        }

        Point GridToReal(Point grid)
        {
            return CADMath.Transform(grid, matrixGridToReal);
        }

        Point RealToScreen(Point real)
        {
            return GridToScreen(RealToGrid(real));
        }

        Point GridToScreen(Point grid)
        {
            return new Point(origin.X + grid.X, origin.Y + grid.Y);
        }

        Point ScreenToGrid(Point screen)
        {
            return new Point(screen.X - origin.X, screen.Y - origin.Y);
        }

        Point ScreenToReal(Point screen)
        {
            return GridToReal(ScreenToGrid(screen));
        }

        protected override void OnLoad(Context context)
        {
            textFormatTest = new TextFormat("Source Code Pro", FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, 12, "en-US");
            textFormatTest.TextAlignment = TextAlignment.Leading;
            textFormatTest.ParagraphAlignment = ParagraphAlignment.Near;

            textFormatBig = new TextFormat("Source Code Pro", FontWeight.Bold, FontStyle.Normal, FontStretch.Normal, 18, "en-US");
            textFormatBig.TextAlignment = TextAlignment.Leading;
            textFormatBig.ParagraphAlignment = ParagraphAlignment.Near;

            textFormatSmall = new TextFormat("Source Code Pro", FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, 12, "en-US");
            textFormatSmall.TextAlignment = TextAlignment.Leading;
            textFormatSmall.ParagraphAlignment = ParagraphAlignment.Near;

            dpi = context.GetDPI();

            gridStepMinor = new Point(gridStepUnitsMinor / 25.4f * dpi.X, gridStepUnitsMinor / 25.4f * dpi.Y);
            gridStepMajor = new Point(gridStepUnitsMajor / 25.4f * dpi.X, gridStepUnitsMajor / 25.4f * dpi.Y);
        }

        protected override void OnUpdate(Context context)
        {
            var area = new Rectangle(Area.Dimensions);

            origin = CADMath.Clamp(
                origin,
                new Point(-(origin.X + gridStepMinor.X * gridSize.Width) + (area.Width * 6 / 4), -(origin.Y + gridStepMinor.Y * gridSize.Height) + (area.Height * 6 / 4)),
                new Point(area.Width / 4, area.Height / 4));
            originOnReality = CADMath.Transform(origin, matrixGridToReal);

            //
            // Calculate tranformation matrices.
            //
            matrixRealToGrid = Matrix3x2.Scale((dpi.X / 25.4f) / zoomFactor, (dpi.Y / 25.4f) / zoomFactor);
            matrixGridToReal = Matrix3x2.Scale((zoomFactor * 25.4f) / dpi.X, (zoomFactor * 25.4f) / dpi.Y);

            originCenter = RealToScreen(originCenterOnReality);
            originMovable = RealToScreen(originMovableOnReality);

            // Must happen after origin calculation.
            matrixOriginTranslation = Matrix3x2.Translation(originMode == OriginMode.Absolute ? origin : originCenter);

            mousePositionRelativeToOrigin = ScreenToGrid(mousePositionOnScreen);
            mousePositionRelativeToOriginCenter = new Point(mousePositionOnScreen.X - originCenter.X, mousePositionOnScreen.Y - originCenter.Y);
            mousePositionRelativeToOriginMovable = new Point(mousePositionOnScreen.X - originMovable.X, mousePositionOnScreen.Y - originMovable.Y);

            var snapStep = snapMode == SnapMode.Fine ? gridStepMinor : gridStepMajor;
            var snapUnitStep = snapMode == SnapMode.Fine ? gridStepUnitsMinor : gridStepUnitsMajor;

            linesFromOrigin = new Point((float)Math.Round(mousePositionRelativeToOrigin.X / snapStep.X),
                                        (float)Math.Round(mousePositionRelativeToOrigin.Y / snapStep.Y));

            mousePositionOnScreenSnapped = new Point(origin.X + (snapStep.X * linesFromOrigin.X), origin.Y + (snapStep.Y * linesFromOrigin.Y));

            linesFromOriginCenter = new Point((float)Math.Round(mousePositionRelativeToOriginCenter.X / snapStep.X),
                                               (float)Math.Round(mousePositionRelativeToOriginCenter.Y / snapStep.Y));

            linesFromOriginMovable = new Point((float)Math.Round(mousePositionRelativeToOriginMovable.X / snapStep.X),
                                               (float)Math.Round(mousePositionRelativeToOriginMovable.Y / snapStep.Y));

            mousePositionRelativeToOriginReal = GridToReal(mousePositionRelativeToOrigin);
            mousePositionRelativeToOriginRealSnapped = new Point(CADMath.Round(linesFromOrigin.X * snapUnitStep * zoomFactor), CADMath.Round(linesFromOrigin.Y * snapUnitStep * zoomFactor));
            mousePositionRelativeToOriginCenterRealSnapped = new Point(CADMath.Round(linesFromOriginCenter.X * snapUnitStep * zoomFactor), CADMath.Round(linesFromOriginCenter.Y * snapUnitStep * zoomFactor));
            mousePositionRelativeToOriginMovableRealSnapped = new Point(CADMath.Round(linesFromOriginMovable.X * snapUnitStep * zoomFactor), CADMath.Round(linesFromOriginMovable.Y * snapUnitStep * zoomFactor));
        }

        protected override void OnDraw(Context context)
        {
            var area = new Rectangle(Area.Dimensions);

            context.PushAxisAlignedClip(area, AntialiasMode.PerPrimitive);

            DrawBackground(context);

            var gridLinesMajorX = (int)Math.Floor(gridSize.Width / gridStepUnitsMajor);
            var gridLinesMajorY = (int)Math.Floor(gridSize.Height / gridStepUnitsMajor);
            var gridLinesMinorX = (int)Math.Floor(gridSize.Width / gridStepUnitsMinor);
            var gridLinesMinorY = (int)Math.Floor(gridSize.Height / gridStepUnitsMinor);

            context.AntialiasMode = AntialiasMode.Aliased;

            SharedBrush.Opacity = 1;

            if (gridMode == GridMode.Lines)
            {
                SharedBrush.Color = gridlineMajorColor;

                var upperBound = CADMath.Clamp((int)Math.Floor((area.Right - origin.X) / gridStepMajor.X), 0, gridLinesMajorX);
                var lowerBound = CADMath.Clamp((int)Math.Floor(-(origin.X / gridStepMajor.X)), 0, upperBound);

                for (var i = lowerBound + 1; i <= upperBound; i++)
                {
                    var left = origin.X + gridStepMajor.X * i;
                    var top = Math.Max(area.Top, origin.Y);
                    var bottom = Math.Min(area.Bottom, origin.Y + gridStepMajor.Y * gridLinesMajorY);
                    context.FillRectangle(Rectangle.FromLTRB(left - 0.5f, top, left + 0.5f, bottom), SharedBrush);
                }

                upperBound = CADMath.Clamp((int)Math.Floor((area.Bottom - origin.Y) / gridStepMajor.Y), 0, gridLinesMajorY);
                lowerBound = CADMath.Clamp((int)Math.Floor(-(origin.Y / gridStepMajor.Y)), 0, upperBound);

                for (var i = lowerBound + 1; i <= upperBound; i++)
                {
                    var top = origin.Y + gridStepMajor.Y * i;
                    var left = Math.Max(area.Left, origin.X);
                    var right = Math.Min(area.Right, origin.X + gridStepMajor.X * gridLinesMajorX);
                    context.FillRectangle(Rectangle.FromLTRB(left, top - 0.5f, right, top + 0.5f), SharedBrush);
                }

                if (snapMode == SnapMode.Fine)
                {
                    SharedBrush.Color = gridlineMinorColor;

                    upperBound = CADMath.Clamp((int)Math.Ceiling((area.Right - origin.X) / gridStepMinor.X), 0, gridLinesMinorX);
                    lowerBound = CADMath.Clamp((int)Math.Ceiling(-(origin.X / gridStepMinor.X)), 0, upperBound);

                    for (var i = lowerBound + 1; i < upperBound; i++)
                    {
                        var left = origin.X + gridStepMinor.X * i;
                        var top = Math.Max(area.Top, origin.Y);
                        var bottom = Math.Min(area.Bottom, origin.Y + gridStepMinor.Y * gridLinesMinorY);
                        context.FillRectangle(Rectangle.FromLTRB(left - 0.5f, top, left + 0.5f, bottom), SharedBrush);
                    }

                    upperBound = CADMath.Clamp((int)Math.Ceiling((area.Bottom - origin.Y) / gridStepMinor.Y), 0, gridLinesMinorY);
                    lowerBound = CADMath.Clamp((int)Math.Ceiling(-(origin.Y / gridStepMinor.Y)), 0, upperBound);

                    for (var i = lowerBound + 1; i < upperBound; i++)
                    {
                        var top = origin.Y + gridStepMinor.Y * i;
                        var left = Math.Max(area.Left, origin.X);
                        var right = Math.Min(area.Right, origin.X + gridStepMinor.X * gridLinesMinorX);
                        context.FillRectangle(Rectangle.FromLTRB(left, top - 0.5f, right, top + 0.5f), SharedBrush);
                    }
                }
            }
            else if (gridMode == GridMode.Points)
            {
                SharedBrush.Color = griddotMajorColor;

                var upperBoundX = CADMath.Clamp((int)Math.Floor((area.Right - origin.X) / gridStepMajor.X), 0, gridLinesMajorX);
                var lowerBoundX = CADMath.Clamp((int)Math.Floor(-(origin.X / gridStepMajor.X)), 0, upperBoundX);
                var upperBoundY = CADMath.Clamp((int)Math.Floor((area.Bottom - origin.Y) / gridStepMajor.Y), 0, gridLinesMajorY);
                var lowerBoundY = CADMath.Clamp((int)Math.Floor(-(origin.Y / gridStepMajor.Y)), 0, upperBoundY);

                for (var i = lowerBoundX + 1; i <= upperBoundX; i++)
                {
                    var left = origin.X + gridStepMajor.X * i;

                    for (var j = lowerBoundY + 1; j <= upperBoundY; j++)
                    {
                        var top = origin.Y + gridStepMajor.Y * j;

                        context.FillRectangle(Rectangle.FromLTRB(left - 0.5f, top - 0.5f, left + 0.5f, top + 0.5f), SharedBrush);
                    }
                }

                if (snapMode == SnapMode.Fine)
                {
                    SharedBrush.Color = griddotMinorColor;

                    upperBoundX = CADMath.Clamp((int)Math.Ceiling((area.Right - origin.X) / gridStepMinor.X), 0, gridLinesMinorX);
                    lowerBoundX = CADMath.Clamp((int)Math.Ceiling(-(origin.X / gridStepMinor.X)), 0, upperBoundX);
                    upperBoundY = CADMath.Clamp((int)Math.Ceiling((area.Bottom - origin.Y) / gridStepMinor.Y), 0, gridLinesMinorY);
                    lowerBoundY = CADMath.Clamp((int)Math.Ceiling(-(origin.Y / gridStepMinor.Y)), 0, upperBoundY);

                    for (var i = lowerBoundX + 1; i < upperBoundX; i++)
                    {
                        var left = origin.X + gridStepMinor.X * i;

                        for (var j = lowerBoundY + 1; j < upperBoundY; j++)
                        {
                            var top = origin.Y + gridStepMinor.Y * j;

                            context.FillRectangle(Rectangle.FromLTRB(left - 0.5f, top - 0.5f, left + 0.5f, top + 0.5f), SharedBrush);
                        }
                    }
                }
            }

            context.AntialiasMode = AntialiasMode.PerPrimitive;

            SharedBrush.Opacity = 1;
            SharedBrush.Color = Color.LightSkyBlue;
            context.DrawText(
                $"grid {CADMath.ToString(CADMath.Round(zoomFactor * gridStepUnitsMinor))} : {CADMath.ToString(CADMath.Round(zoomFactor * gridStepUnitsMajor))}\n" +
                $"abs  x:{CADMath.ToString(mousePositionRelativeToOriginRealSnapped.X)} y:{CADMath.ToString(mousePositionRelativeToOriginRealSnapped.Y)}\n" +
                $"cen  x:{CADMath.ToString(mousePositionRelativeToOriginCenterRealSnapped.X)} y:{CADMath.ToString(mousePositionRelativeToOriginCenterRealSnapped.Y)}\n" +
                $"rel  x:{CADMath.ToString(mousePositionRelativeToOriginMovableRealSnapped.X)} y:{CADMath.ToString(mousePositionRelativeToOriginMovableRealSnapped.Y)}\n" +
                $"tool {toolMode} : {toolState}",
                textFormatTest,
                area,
                SharedBrush);

            //
            // Draw geometry.
            //
            context.AntialiasMode = AntialiasMode.PerPrimitive;

            var transformStored = context.GetTransform();
            context.SetTransform(matrixRealToGrid * transformStored * matrixOriginTranslation);

            SharedBrush.Opacity = 1;
            SharedBrush.Color = geometryColor;

            for (var i = 0; i < shapes.Count; i++)
            {
                var shape = shapes[i];

                if (shapesSelected.Contains(shape))
                {
                    SharedBrush.Color = Color.SpringGreen;
                    shape.OnDraw(context, SharedBrush);
                    SharedBrush.Color = geometryColor;
                }
                else
                {
                    shape.OnDraw(context, SharedBrush);
                }
            }

            if (toolMode == ToolMode.Select && toolState == ToolState.Editing)
            {
                SharedBrush.Opacity = 1;
                SharedBrush.Color = Color.LightSkyBlue;

                context.DrawRectangle(selection, SharedBrush, 0.5f * zoomFactor);
            }

            context.SetTransform(transformStored);

            //
            // Draw the cursor and relative crosshairs.
            //

            context.AntialiasMode = AntialiasMode.Aliased;

            SharedBrush.Opacity = 1;

            var crossHalfW = crosshairWidth / 2;
            var crossHalfH = crosshairHeight / 2;
            var crossLeft = mousePositionOnScreenSnapped.X;
            var crossTop = mousePositionOnScreenSnapped.Y;

            SharedBrush.Color = crosshairCursorColor;
            context.FillRectangle(Rectangle.FromXYWH(crossLeft - crossHalfH, crossTop - crossHalfW, crosshairHeight, crosshairWidth), SharedBrush);
            context.FillRectangle(Rectangle.FromXYWH(crossLeft - crossHalfW, crossTop - crossHalfH, crosshairWidth, crosshairHeight), SharedBrush);

            crossLeft = originMovable.X;
            crossTop = originMovable.Y;

            SharedBrush.Color = crosshairRelativeColor;
            context.FillRectangle(Rectangle.FromXYWH(crossLeft - crossHalfW, crossTop - crossHalfH, crosshairWidth, crosshairHeight), SharedBrush);
            context.FillRectangle(Rectangle.FromXYWH(crossLeft - crossHalfH, crossTop - crossHalfW, crosshairHeight, crosshairWidth), SharedBrush);

            //
            // Draw the center axis
            //

            if (originMode == OriginMode.Center)
            {
                SharedBrush.Opacity = 1;
                SharedBrush.Color = Color.FromAColor(0.5f, Color.LightSkyBlue);

                context.FillRectangle(Rectangle.FromLTRB(
                    originCenter.X - 0.5f,
                    Math.Max(area.Top, origin.Y),
                    originCenter.X + 0.5f,
                    Math.Min(area.Bottom, origin.Y + gridStepMajor.Y * gridLinesMajorY)), SharedBrush);

                context.FillRectangle(Rectangle.FromLTRB(
                     Math.Max(area.Left, origin.X),
                     originCenter.Y - 0.5f,
                     Math.Min(area.Right, origin.X + gridStepMajor.X * gridLinesMajorX),
                     originCenter.Y + 0.5f), SharedBrush);
            }

            //
            // Draw the grid outline
            //
            var gridOutline = Rectangle.FromLTRB(
                Math.Max(area.Left, origin.X),
                Math.Max(area.Top, origin.Y),
                Math.Min(area.Right, origin.X + gridStepMajor.X * gridLinesMajorX),
                Math.Min(area.Bottom, origin.Y + gridStepMajor.Y * gridLinesMajorY));

            SharedBrush.Opacity = 1;
            SharedBrush.Color = gridOutlineColor;

            context.DrawRectangle(gridOutline, SharedBrush, 1);

            var infoRect = Rectangle.FromLTRB(area.Right - 200, area.Bottom - 80, area.Right, area.Bottom);

            Style.Align(infoRect);
            context.FillRectangle(infoRect, Style.Background);
            context.FillRectangle(infoRect, Style.Foreground);

            SharedBrush.Color = Color.GhostWhite;

            var relativeStr = "Relative";
            textFormatBig.TextAlignment = TextAlignment.Center;
            textFormatBig.ParagraphAlignment = ParagraphAlignment.Center;
            var p = mousePositionRelativeToOriginMovableRealSnapped;
            var layoutStr =
                $"{relativeStr}" +
                $"\nX {CADMath.ToString2(p.X)}" +
                $"\nY {CADMath.ToString2(p.Y)}";
            using (var layout = new TextLayout(layoutStr, textFormatBig, infoRect.Dimensions))
            {
                layout.SetFontSize(10, new TextRange(layoutStr.IndexOf(relativeStr, StringComparison.Ordinal), relativeStr.Length));
                context.DrawTextLayout(layout, infoRect.TopLeft, SharedBrush);
            }

            SharedBrush.Color = Color.Black;
            context.DrawRectangle(infoRect, SharedBrush, 2);

            //
            // Draw the control outline.
            //
            DrawOutline(context);

            context.PopAxisAlignedClip();
        }
    }
}
