using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kodo.Graphics;
using Kodo.Graphics.Style;

namespace KodoCad
{
    enum GridMode { None, Lines, Points }
    enum SnapMode { Fine, Coarse }

    enum ToolMode { Select, Move, Line, Rectangle, Circle, Text, Pin }
    enum ToolState { None, Armed, Editing }

    delegate void ShapeEditHandler(Set<CadShape> shape);

    interface ICadEditorEditComponent
    {
        Rectangle BoundingBox { get; }
        void OnKey(Key key);
        void OnCharacter(char c);
        void OnMouseDown();
        void OnMouseContained(bool contained);
    }

    class CadEditorTextBox : ICadEditorEditComponent
    {
        bool focused;
        string text = string.Empty;
        Rectangle box;
        bool allowLetters = true;

        TextFormat fmt;

        int textCursor;

        public bool Focused => focused;

        public bool AllowLetters { get => allowLetters; set => allowLetters = value; }

        public Rectangle Box { get => box; set => box = value; }

        public string Text
        {
            get { return text; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    value = string.Empty;

                if (text != value)
                {
                    text = value;
                    textCursor = text.Length;
                }
            }
        }

        public Rectangle BoundingBox => box;

        public CadEditorTextBox()
        {
            fmt = new TextFormat("Nunito", 14);
        }

        public void Focus(bool focus)
        {
            if (focus)
            {
                focused = true;
            }
            else
            {
                focused = false;
            }
        }

        public void OnKey(Key key)
        {
            switch (key)
            {
                case Key.Home:
                    textCursor = 0;
                    break;
                case Key.End:
                    textCursor = text.Length;
                    break;
                case Key.Back:
                case Key.Delete:
                    if (text.Length > 0)
                    {
                        int toRemove = textCursor - 1;

                        if (key == Key.Delete)
                            toRemove++;

                        if (toRemove < 0)
                            break;
                        if (toRemove >= text.Length)
                            toRemove = text.Length - 1;

                        text = text.Remove(toRemove, 1);

                        if (key == Key.Back)
                            textCursor--;

                        textCursor = CadMath.Clamp(textCursor, 0, text.Length);
                    }
                    break;
                case Key.ArrowLeft:
                    textCursor = CadMath.Clamp(textCursor - 1, 0, text.Length);
                    break;
                case Key.ArrowRight:
                    textCursor = CadMath.Clamp(textCursor + 1, 0, text.Length);
                    break;
            }
        }

        public void OnCharacter(char c)
        {
            if (AllowLetters)
            {
                if ((char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c)) && !char.IsControl(c))
                {
                    text = text.Insert(textCursor, c.ToString());
                    textCursor = CadMath.Clamp(textCursor + 1, 0, text.Length);
                }
            }
            else
            {
                if (char.IsDigit(c) || c == '.')
                {
                    text = text.Insert(textCursor, c.ToString());
                    textCursor = CadMath.Clamp(textCursor + 1, 0, text.Length);
                }
            }
        }

        public void OnDraw(Context context, SolidColorBrush brush)
        {
            brush.Color = focused ? new Color(0xFFEAF4FC) : new Color(0xFFFFFFFF);
            context.FillRectangle(box, brush);
            brush.Color = new Color(0xFFEAEAEA);
            context.DrawRectangle(box, brush, 1);

            brush.Color = new Color(0xFF151A22);

            using (var layout = new TextLayout(text, fmt, box.Dimensions))
            {
                context.DrawTextLayout(layout, box.TopLeft, brush);

                if (focused)
                {
                    brush.Color = new Color(0xFF3e7cbf);

                    var textCursorPosition = layout.Metrics.WidthIncludingTrailingWhitespace;

                    if (textCursor < text.Length)
                    {
                        using (var testLayout = new TextLayout(text.Substring(0, textCursor), fmt, box.Dimensions))
                        {
                            textCursorPosition = testLayout.Metrics.WidthIncludingTrailingWhitespace;
                        }
                    }

                    context.DrawLine(new Point(box.Left + textCursorPosition, box.Top + (box.Height + 5 - layout.Metrics.Height / 2)), new Point(box.Left + textCursorPosition, box.Bottom - (box.Height + 5 - layout.Metrics.Height / 2)), brush, 1);
                }
            }
        }

        public void OnMouseDown()
        {
            Focus(true);
        }

        public void OnMouseContained(bool contained)
        {
            if (!contained)
            {
                Focus(false);
            }
        }
    }

    class CadEditorEditPin : CadEditorEdit
    {
        protected override Size BoxSize => throw new NotImplementedException();

        protected override List<ICadEditorEditComponent> Components => throw new NotImplementedException();
    }

    class CadEditorEditText : CadEditorEdit
    {
        CadEditorTextBox boxOfText;
        CadEditorTextBox boxOfSize;

        List<ICadEditorEditComponent> components = new List<ICadEditorEditComponent>();

        protected override Size BoxSize => new Size(300, 120);

        protected override List<ICadEditorEditComponent> Components => components;

        public CadEditorEditText(CadShapeText textShape)
        {
            boxOfText = new CadEditorTextBox();
            boxOfSize = new CadEditorTextBox();

            boxOfText.Box = Rectangle.FromXYWH(0, 0, 250, 30);
            boxOfSize.Box = Rectangle.FromXYWH(0, 30, 250, 30);

            components.Add(boxOfText);
            components.Add(boxOfSize);

            boxOfText.Text = textShape.Text;
            boxOfSize.Text = textShape.Size.ToString();
        }

        public CadEditorEditText(Set<CadShapeText> texts)
        {
            boxOfText = new CadEditorTextBox();
            boxOfSize = new CadEditorTextBox();

            boxOfText.Box = Rectangle.FromXYWH(0, 0, 250, 30);
            boxOfSize.Box = Rectangle.FromXYWH(0, 30, 250, 30);

            components.Add(boxOfText);
            components.Add(boxOfSize);

            if (texts.Count == 1)
            {
                var first = texts.First();
                boxOfText.Text = first.Text;
                boxOfSize.Text = first.Size.ToString();
            }
            else
            {

            }
        }

        public override void Draw(Point location, Context context, SolidColorBrush brush, Color background, Color outline)
        {
            base.Draw(location, context, brush, background, outline);

            boxOfText.Box = Rectangle.FromXYWH(location.X, location.Y, 250, 30);
            boxOfSize.Box = Rectangle.FromXYWH(location.X, location.Y + 30, 250, 30);

            boxOfText.OnDraw(context, brush);
            boxOfSize.OnDraw(context, brush);
        }
    }

    abstract class CadEditorEdit
    {
        abstract protected List<ICadEditorEditComponent> Components { get; }
        abstract protected Size BoxSize { get; }

        Rectangle boundingBox;

        public virtual void OnKey(Key key)
        {

        }

        public virtual void OnCharacter(char c)
        {

        }

        public virtual void OnMouseDown()
        {

        }

        public virtual void OnMouseMove()
        {

        }

        public void Show()
        {

        }

        public virtual void Draw(Point location, Context context, SolidColorBrush brush, Color background, Color outline)
        {
            var boxSize = BoxSize;

            var geometry = new PathGeometry();

            using (var sink = geometry.Open())
            {
                sink.BeginFigure(location, FigureBegin.Filled);
                var lastPoint = location;
                sink.AddLine(lastPoint = new Point(lastPoint.X - 5, lastPoint.Y + 5));
                sink.AddLine(lastPoint = new Point(lastPoint.X - 10, lastPoint.Y + 0));
                sink.AddLine(lastPoint = new Point(lastPoint.X - 0, lastPoint.Y + boxSize.Height));
                sink.AddLine(lastPoint = new Point(lastPoint.X + boxSize.Width, lastPoint.Y + 0));
                sink.AddLine(lastPoint = new Point(lastPoint.X + 0, lastPoint.Y - boxSize.Height));

                sink.AddLine(lastPoint = new Point(lastPoint.X - (boxSize.Width - 10 - 5 - 5), lastPoint.Y - 0));

                sink.AddLine(lastPoint = new Point(location.X, location.Y));

                sink.EndFigure(FigureEnd.Closed);
                sink.Close();
            }

            brush.Color = new Color(0xFFC0CBCE);
            context.FillGeometry(geometry, brush);
            brush.Color = new Color(0xFF68768A);
            context.DrawGeometry(geometry, brush, 2);
        }
    }

    class CadEditor : Control
    {
        CadEditorEdit editor;

        public event ShapeEditHandler OnShapeEdit;

        static readonly Color basicForegroundColor = new Color(0xFFECF1F3);

        Color gridOutlineColor = Color.FromAColor(0.5f, basicForegroundColor);

        Color gridlineMajorColor = Color.FromAColor(0.15f, basicForegroundColor);
        Color gridlineMinorColor = Color.FromAColor(0.05f, basicForegroundColor);

        Color griddotMajorColor = Color.FromAColor(0.4f, basicForegroundColor);
        Color griddotMinorColor = Color.FromAColor(0.1f, basicForegroundColor);

        Color crosshairCursorColor = Color.GhostWhite;
        Color crosshairRelativeColor = Color.GhostWhite;

        Color geometryColor = new Color(0xFFB4C0C3);

        float crosshairWidth = 1f;
        float crosshairHeight = 20f;

        bool drawCenterAxes;

        Set<CadShape> shapesSelected = new Set<CadShape>();
        bool multiSelected;
        List<CadShape> shapes = new List<CadShape>();
        CadShape shapeInEditing;

        GridMode gridMode = GridMode.Lines;
        SnapMode snapMode = SnapMode.Fine;
        ToolMode toolMode = ToolMode.Select;
        ToolState toolState = ToolState.None;

        int gridStepUnitsMinor = 2;
        int gridStepUnitsMajor = 10;

        float zoomFactor = 1.0f;
        int zoomFactorIndex = 0;
        float[] zoomFactors = new float[] { 1.0f, 0.5f, 0.25f, 0.125f, 0.1f, 0.05f, 0.025f, 0.01f, 0.005f, 0.0025f, 0.001f, 0.0005f };

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

        public void ReplaceShape(CadShape shapeOriginal, CadShape shapeNew)
        {
            shapes.Remove(shapeOriginal);
            shapes.Add(shapeNew);
            Update();
        }

        public CadEditor(Window window)
            : base(window)
        {
            /*using (var file = new System.IO.StreamReader(@"C:\Users\Jay\SkyDrive\AMK\Huhtinen\PSU+AMP\PSU+AMP.lib"))
            {
                while (true)
                {
                    var line = file.ReadLine();

                    if (string.IsNullOrEmpty(line))
                        break;
                    if (!line.StartsWith("DEF"))
                        continue;

                    line = file.ReadLine();
                    line = file.ReadLine();

                    var parts = line.Split(' ');

                    var txtFormat = new TextFormat("Montserrat", FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, CADMath.MilsToMillimeters(int.Parse(parts[4])), "en-US");

                    shapes.Add(new CADShapeText(parts[1].Trim('"'), txtFormat, new Point(CADMath.MilsToMillimeters(int.Parse(parts[2])), -CADMath.MilsToMillimeters(int.Parse(parts[3])))));

                    line = file.ReadLine();
                    line = file.ReadLine();
                    line = file.ReadLine();
                    line = file.ReadLine();

                    parts = line.Split(' ');

                    shapes.Add(new CADShapeRectangle(new Rectangle(
                        CADMath.MilsToMillimeters(int.Parse(parts[1])),
                        CADMath.MilsToMillimeters(int.Parse(parts[2])),
                        CADMath.MilsToMillimeters(int.Parse(parts[3])),
                        CADMath.MilsToMillimeters(int.Parse(parts[4])))));

                    break;
                }
            }*/
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

            mousePositionWhenPressed = mouse.Position;

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
                            shapeInEditing = new CadShapeLine();
                            toolState = ToolState.Armed;
                            shapes.Add(shapeInEditing);

                            Update();
                            break;
                        case ToolMode.Rectangle:
                            shapeInEditing = new CadShapeRectangle();
                            toolState = ToolState.Armed;
                            shapes.Add(shapeInEditing);

                            Update();
                            break;
                        case ToolMode.Pin:
                            toolMode = ToolMode.Select;
                            toolState = ToolState.Armed;
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
            zoomFactorIndex = CadMath.Clamp(zoomFactorIndex + (mouse.WheelDelta > 0 ? 1 : -1), 0, zoomFactors.Length - 1);
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

                gridSize = new Size(CadMath.Round(gridSize.Width * zoomRatio, 0), CadMath.Round(gridSize.Height * zoomRatio, 0));

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
                        if (shape.Contains(rel, 0.2f * zoomFactor))
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
        }

        protected override void OnCharacter(char c)
        {
            if (editor != null)
            {
                editor.OnCharacter(c);
                return;
            }
        }

        protected override void OnKeyDown(Key key)
        {
            if (editor != null)
            {
                editor.OnKey(key);
                return;
            }

            if (key == Key.Escape)
            {
                if (shapeInEditing != null)
                {
                    if (toolState != ToolState.Armed)
                    {
                        shapes.Remove(shapeInEditing);
                        shapeInEditing = null;

                        shapeInEditing = new CadShapeLine();

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
                /*if (shapesSelected.Count == 1)
                {
                    var shape = shapes.First();

                    if (shape is CadShapeText textShape)
                    {
                        editor = new CadEditorEditText(textShape);
                        Update();
                    }
                }
                else
                {

                }*/

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
            else if (key == Key.C && Keyboard.IsDown(Key.ShiftLeft))
            {
                drawCenterAxes = !drawCenterAxes;
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

                var textFormat = new TextFormat("Nunito", FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, 1, "en-US");
                shapeInEditing = new CadShapeText("CadShapeText", textFormat, rel);
                shapes.Add(shapeInEditing);
                Refresh();
            }
            else if (key == Key.P && toolMode != ToolMode.Pin)
            {
                toolMode = ToolMode.Pin;
                var rel = SelectRelative(mousePositionRelativeToOriginRealSnapped, mousePositionRelativeToOriginCenterRealSnapped);
                var textFormat = new TextFormat("Nunito", FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, 1, "en-US");
                shapeInEditing = new CadShapePin("CadShapePin", 1, 5, PinOrientation.Left, textFormat, textFormat, rel);
                toolState = ToolState.Armed;

                shapes.Add(shapeInEditing);

                Update();
            }
            else if (key == Key.R && toolMode != ToolMode.Rectangle)
            {
                toolMode = ToolMode.Rectangle;

                shapeInEditing = new CadShapeRectangle();
                toolState = ToolState.Armed;

                shapes.Add(shapeInEditing);

                Update();
            }
            else if (key == Key.W && toolMode != ToolMode.Line)
            {
                toolMode = ToolMode.Line;

                shapeInEditing = new CadShapeLine();
                toolState = ToolState.Armed;

                shapes.Add(shapeInEditing);

                Update();
            }
            else if (key == Key.M && shapesSelected.Count > 0)
            {
                toolMode = ToolMode.Move;
                toolState = ToolState.Editing;

                movementStartPoint = shapesSelected.First().Origin;
                mousePositionOnScreen = RealToScreen(movementStartPoint);

                Mouse.SetPosition(FromWindowToScreen(FromControlToWindow(mousePositionOnScreen)));

                Update();
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
            return absolute;
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
            return CadMath.Transform(real, matrixRealToGrid);
        }

        Point GridToReal(Point grid)
        {
            return CadMath.Transform(grid, matrixGridToReal);
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
            textFormatTest = new TextFormat("Roboto Mono", 12);
            textFormatBig = new TextFormat("Roboto Mono", 16);
            textFormatSmall = new TextFormat("Roboto Mono", 12);

            dpi = context.GetDPI();

            gridStepMinor = new Point(gridStepUnitsMinor / 25.4f * dpi.X, gridStepUnitsMinor / 25.4f * dpi.Y);
            gridStepMajor = new Point(gridStepUnitsMajor / 25.4f * dpi.X, gridStepUnitsMajor / 25.4f * dpi.Y);
        }

        protected override void OnUpdate(Context context)
        {
            var area = new Rectangle(Area.Dimensions);

            origin = CadMath.Clamp(
                origin,
                new Point(-(origin.X + gridStepMinor.X * gridSize.Width) + (area.Width * 6 / 4), -(origin.Y + gridStepMinor.Y * gridSize.Height) + (area.Height * 6 / 4)),
                new Point(area.Width / 4, area.Height / 4));
            originOnReality = CadMath.Transform(origin, matrixGridToReal);

            //
            // Calculate tranformation matrices.
            //
            matrixRealToGrid = Matrix3x2.Scale((dpi.X / 25.4f) / zoomFactor, (dpi.Y / 25.4f) / zoomFactor);
            matrixGridToReal = Matrix3x2.Scale((zoomFactor * 25.4f) / dpi.X, (zoomFactor * 25.4f) / dpi.Y);

            originCenter = RealToScreen(originCenterOnReality);
            originMovable = RealToScreen(originMovableOnReality);

            // Must happen after origin calculation.
            matrixOriginTranslation = Matrix3x2.Translation(origin);

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
            mousePositionRelativeToOriginRealSnapped = new Point(CadMath.Round(linesFromOrigin.X * snapUnitStep * zoomFactor), CadMath.Round(linesFromOrigin.Y * snapUnitStep * zoomFactor));
            mousePositionRelativeToOriginCenterRealSnapped = new Point(CadMath.Round(linesFromOriginCenter.X * snapUnitStep * zoomFactor), CadMath.Round(linesFromOriginCenter.Y * snapUnitStep * zoomFactor));
            mousePositionRelativeToOriginMovableRealSnapped = new Point(CadMath.Round(linesFromOriginMovable.X * snapUnitStep * zoomFactor), CadMath.Round(linesFromOriginMovable.Y * snapUnitStep * zoomFactor));
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

                var upperBound = CadMath.Clamp((int)Math.Floor((area.Right - origin.X) / gridStepMajor.X), 0, gridLinesMajorX);
                var lowerBound = CadMath.Clamp((int)Math.Floor(-(origin.X / gridStepMajor.X)), 0, upperBound);

                for (var i = lowerBound + 1; i <= upperBound; i++)
                {
                    var left = origin.X + gridStepMajor.X * i;
                    var top = Math.Max(area.Top, origin.Y);
                    var bottom = Math.Min(area.Bottom, origin.Y + gridStepMajor.Y * gridLinesMajorY);
                    context.FillRectangle(Rectangle.FromLTRB(left - 0.5f, top, left + 0.5f, bottom), SharedBrush);
                }

                upperBound = CadMath.Clamp((int)Math.Floor((area.Bottom - origin.Y) / gridStepMajor.Y), 0, gridLinesMajorY);
                lowerBound = CadMath.Clamp((int)Math.Floor(-(origin.Y / gridStepMajor.Y)), 0, upperBound);

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

                    upperBound = CadMath.Clamp((int)Math.Ceiling((area.Right - origin.X) / gridStepMinor.X), 0, gridLinesMinorX);
                    lowerBound = CadMath.Clamp((int)Math.Ceiling(-(origin.X / gridStepMinor.X)), 0, upperBound);

                    for (var i = lowerBound + 1; i < upperBound; i++)
                    {
                        var left = origin.X + gridStepMinor.X * i;
                        var top = Math.Max(area.Top, origin.Y);
                        var bottom = Math.Min(area.Bottom, origin.Y + gridStepMinor.Y * gridLinesMinorY);
                        context.FillRectangle(Rectangle.FromLTRB(left - 0.5f, top, left + 0.5f, bottom), SharedBrush);
                    }

                    upperBound = CadMath.Clamp((int)Math.Ceiling((area.Bottom - origin.Y) / gridStepMinor.Y), 0, gridLinesMinorY);
                    lowerBound = CadMath.Clamp((int)Math.Ceiling(-(origin.Y / gridStepMinor.Y)), 0, upperBound);

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

                var upperBoundX = CadMath.Clamp((int)Math.Floor((area.Right - origin.X) / gridStepMajor.X), 0, gridLinesMajorX);
                var lowerBoundX = CadMath.Clamp((int)Math.Floor(-(origin.X / gridStepMajor.X)), 0, upperBoundX);
                var upperBoundY = CadMath.Clamp((int)Math.Floor((area.Bottom - origin.Y) / gridStepMajor.Y), 0, gridLinesMajorY);
                var lowerBoundY = CadMath.Clamp((int)Math.Floor(-(origin.Y / gridStepMajor.Y)), 0, upperBoundY);

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

                    upperBoundX = CadMath.Clamp((int)Math.Ceiling((area.Right - origin.X) / gridStepMinor.X), 0, gridLinesMinorX);
                    lowerBoundX = CadMath.Clamp((int)Math.Ceiling(-(origin.X / gridStepMinor.X)), 0, upperBoundX);
                    upperBoundY = CadMath.Clamp((int)Math.Ceiling((area.Bottom - origin.Y) / gridStepMinor.Y), 0, gridLinesMinorY);
                    lowerBoundY = CadMath.Clamp((int)Math.Ceiling(-(origin.Y / gridStepMinor.Y)), 0, upperBoundY);

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
                $"grid {CadMath.ToString(CadMath.Round(zoomFactor * gridStepUnitsMinor))} : {CadMath.ToString(CadMath.Round(zoomFactor * gridStepUnitsMajor))}\n" +
                $"abs  x:{CadMath.ToString(mousePositionRelativeToOriginRealSnapped.X)} y:{CadMath.ToString(mousePositionRelativeToOriginRealSnapped.Y)}\n" +
                $"cen  x:{CadMath.ToString(mousePositionRelativeToOriginCenterRealSnapped.X)} y:{CadMath.ToString(mousePositionRelativeToOriginCenterRealSnapped.Y)}\n" +
                $"rel  x:{CadMath.ToString(mousePositionRelativeToOriginMovableRealSnapped.X)} y:{CadMath.ToString(mousePositionRelativeToOriginMovableRealSnapped.Y)}\n" +
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
                    SharedBrush.Color = new Color(0xFFF3F3F4);
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

            if (editor != null)
            {
                editor.Draw(originCenter, context, SharedBrush, new Color(), new Color());
            }

            if (drawCenterAxes)
            {
                SharedBrush.Opacity = 1;
                SharedBrush.Color = Color.FromAColor(0.5f, basicForegroundColor);

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

                /*context.AntialiasMode = AntialiasMode.PerPrimitive;
                new CadEditorEdit().Draw(originCenter, context, SharedBrush, Style.Colors.Background, new Color(0xFF6AA332));
                context.AntialiasMode = AntialiasMode.Aliased;*/
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

            var infoRect = Rectangle.FromLTRB(area.Right - 300, area.Bottom - 30, area.Right, area.Bottom);

            Style.Align(infoRect);
            context.FillRectangle(infoRect, Style.Background);
            context.FillRectangle(infoRect, Style.Foreground);

            SharedBrush.Color = Color.GhostWhite;

            var relativeStr = "";
            textFormatBig.TextAlignment = TextAlignment.Center;
            textFormatBig.ParagraphAlignment = ParagraphAlignment.Center;
            var p = mousePositionRelativeToOriginMovableRealSnapped;
            var layoutStr =
                $"{relativeStr} " +
                $"dX {CadMath.ToString2(p.X)}" +
                $" dY {CadMath.ToString2(p.Y)}";
            using (var layout = new TextLayout(layoutStr, textFormatBig, infoRect.Dimensions))
            {
                //layout.SetFontSize(10, new TextRange(layoutStr.IndexOf(relativeStr, StringComparison.Ordinal), relativeStr.Length));
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
