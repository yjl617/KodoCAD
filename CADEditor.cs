using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kodo.Graphics;
using Kodo.Graphics.Style;

namespace KodoCad
{
    enum EditorMode { Schematic, Component, Layout, Footprint }

    enum GridMode { None, Lines, Points }
    enum SnapMode { Fine, Coarse }

    enum ColorScheme { Light, Dark }

    enum ToolMode { Select, Move, Line, Rectangle, Circle, Text, Pin }
    enum ToolState { None, Armed, Editing }

    delegate void ShapeEditHandler(Set<CadShape> shape);

    class CadSchematicEditor : Control
    {
        public CadSchematicEditor(Window window)
            : base(window)
        {
        }
    }

    class CadEditor : Control
    {
        public event ShapeEditHandler OnShapeEdit;

        Color backgroundColor;
        Color gridOutlineColor;
        Color gridCoarseLineColor;
        Color gridFineLineColor;
        Color gridCoarseDotColor;
        Color gridFineDotColor;
        Color crosshairCursorColor;
        Color crosshairRelativeColor;
        Color statusColor;
        Color selectColor;
        Color geometryStrokeColor;
        Color geometryFillColor;

        void SetColorScheme(ColorScheme scheme)
        {
            Color foregroundColor;

            colorScheme = scheme;

            switch (scheme)
            {
                case ColorScheme.Light:
                    foregroundColor = Color.DimGray;
                    backgroundColor = Color.FloralWhite;

                    gridOutlineColor = Color.FromAColor(0.5f, foregroundColor);

                    gridCoarseLineColor = Color.FromAColor(0.15f, foregroundColor);
                    gridFineLineColor = Color.FromAColor(0.1f, foregroundColor);

                    gridCoarseDotColor = Color.FromAColor(0.4f, foregroundColor);
                    gridFineDotColor = Color.FromAColor(0.1f, foregroundColor);

                    crosshairCursorColor = Color.DimGray;
                    crosshairRelativeColor = Color.DimGray;

                    selectColor = Color.SkyBlue;

                    geometryStrokeColor = Color.Black;

                    statusColor = Color.DimGray;

                    break;
                case ColorScheme.Dark:
                    backgroundColor = new Color(0.1, 0.15, 0.2);
                    foregroundColor = new Color(0xFFECF1F3);

                    gridOutlineColor = Color.FromAColor(0.5f, foregroundColor);

                    gridCoarseLineColor = Color.FromAColor(0.15f, foregroundColor);
                    gridFineLineColor = Color.FromAColor(0.05f, foregroundColor);

                    gridCoarseDotColor = Color.FromAColor(0.4f, foregroundColor);
                    gridFineDotColor = Color.FromAColor(0.1f, foregroundColor);

                    crosshairCursorColor = Color.GhostWhite;
                    crosshairRelativeColor = Color.GhostWhite;

                    selectColor = Color.SkyBlue;

                    statusColor = Color.LightSkyBlue;

                    geometryStrokeColor = new Color(0xFFB4C0C3);
                    break;
            }
        }

        readonly float crosshairWidth = 2f;
        readonly float crosshairHeight = 20f;

        bool drawCenterAxes = true;
        bool drawRelativeCrosshair = true;

        bool multiSelected;
        CadShape shapeInEditing;
        List<CadShape> shapes = new List<CadShape>();
        Set<CadShape> shapesSelected = new Set<CadShape>();

        ColorScheme colorScheme = ColorScheme.Light;
        GridMode gridMode = GridMode.Lines;
        SnapMode snapMode = SnapMode.Fine;
        ToolMode toolMode = ToolMode.Select;
        ToolState toolState = ToolState.None;

        static readonly float[] zoomSettings = new float[] { 2.0f, 1.0f, 0.5f, 0.25f, 0.125f, 0.1f, 0.05f, 0.025f, 0.01f };
        int zoomSettingIndex = 1;
        float zoomSetting = 1.0f;

        static readonly float[] gridSettings = new float[] { 10f, 5.0f, 2.5f, 1.25f, 1f, 0.5f };
        int gridSettingIndex = 0;
        float gridSettingCoarse = 10.0f;
        float gridSettingFine = 10.0f / 5;

        Point cameraTranslation = new Point(-570, -400);

        Size worldSize = new Size(300, 210);

        Size viewportSize;
        Point viewportDPI;
        Point viewportCenterOnWorld;

        Rectangle selection;
        Point selectionStartPoint;
        Point movementStartPoint;

        Point worldOriginOnScreen;
        Point worldEndOnScreen;
        Point gridMajorSizeOnScreen;
        Point gridMinorSizeOnScreen;

        Point relativeCursorOnWorld;
        Point relativeCursorOnScreen;

        Point mousePositionWhenPressed;
        Point mousePositionOnScreen;
        Point mousePositionOnScreenWorldSnapped;
        Point mousePositionOnWorld;
        Point mousePositionOnWorldSnapped;

        Point mousePositionRelativeToOriginCenter;
        Point mousePositionRelativeToOriginCenterSnapped;
        Point mousePositionRelativeToOriginMovable;
        Point mousePositionRelativeToOriginMovableSnapped;

        TextFormat textFormatStatus;
        TextFormat textFormatBig;
        TextFormat textFormatSmall;

        Matrix3x2 worldTransform;
        Matrix3x2 worldTransformInverse;

        public Size WorldSize
        {
            get { return worldSize; }
            set
            {
                if (worldSize != value)
                {
                    worldSize = value;
                    Load();
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

            /*var testline = new CadLine(new Line(new Point(100,100), new Point(200,100)));
            var testoutput = testline.ToOutput();
            var testline2 = new CadLine(testoutput);
            shapes.Add(testline2);*/
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
                toolState = ToolState.Editing;
                selectionStartPoint = mousePositionOnWorldSnapped;
            }
            else if (mouse.Button == MouseButton.Left && toolMode == ToolMode.Move)
            {
                toolMode = ToolMode.Select;
                toolState = ToolState.Armed;

                if (shapeInEditing != null)
                {
                    shapeInEditing = null;
                }

            }
            else if (mouse.Button == MouseButton.Left && shapeInEditing != null)
            {
                toolState = ToolState.Editing;

                var stopEditing = shapeInEditing.OnMouseDown(mousePositionOnWorldSnapped);

                if (stopEditing)
                {
                    toolState = ToolState.None;

                    shapeInEditing = null;

                    switch (toolMode)
                    {
                        case ToolMode.Line:
                            shapeInEditing = new CadLine();
                            toolState = ToolState.Armed;
                            shapes.Add(shapeInEditing);

                            Update();
                            break;
                        case ToolMode.Rectangle:
                            shapeInEditing = new CadRectangle();
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
            var wheelDirection = Math.Sign(mouse.WheelDelta);

            //
            // Grid
            //
            if (Keyboard.IsDown(Key.CtrlLeft))
            {
                // Get a new grid setting value from the list.
                var gridSettingIndexOld = gridSettingIndex;
                gridSettingIndex = CadMath.Clamp(gridSettingIndex + wheelDirection, 0, gridSettings.Length - 1);
                gridSettingCoarse = gridSettings[gridSettingIndex];

                // Is this actually a new setting?
                if (gridSettingIndex != gridSettingIndexOld)
                {
                    gridSettingFine = CadMath.Round(gridSettingCoarse / 5);
                    Update();
                }
            }
            //
            // Zoom
            //
            else
            {
                // Get a new zoom setting value from the list.
                var zoomSettingIndexOld = zoomSettingIndex;
                zoomSettingIndex = CadMath.Clamp(zoomSettingIndex + wheelDirection, 0, zoomSettings.Length - 1);
                zoomSetting = zoomSettings[zoomSettingIndex];

                // Is this actually a new setting?
                if (zoomSettingIndex != zoomSettingIndexOld)
                {
                    var zoomOld = (viewportDPI.Y / 25.4f) / zoomSettings[zoomSettingIndexOld];
                    var zoomNew = (viewportDPI.Y / 25.4f) / zoomSettings[zoomSettingIndex];

                    var msx = mousePositionOnScreen.X - viewportSize.Width / 2;
                    var msy = mousePositionOnScreen.Y - viewportSize.Height / 2;
                    var dx = cameraTranslation.X - msx;
                    var dy = cameraTranslation.Y - msy;

                    cameraTranslation = new Point(cameraTranslation.X - (dx * (1 - zoomNew / zoomOld)),
                                                  cameraTranslation.Y - (dy * (1 - zoomNew / zoomOld)));
                    Update();
                }
            }
        }

        protected override void OnMouseMove(Mouse mouse)
        {
            CaptureKeyboard();

            mousePositionOnScreen = mouse.Position;

            // Calculate distance from the press event.
            var distanceX = mousePositionWhenPressed.X - mousePositionOnScreen.X;
            var distanceY = mousePositionWhenPressed.Y - mousePositionOnScreen.Y;

            // Move the camera?
            if (mouse.Button == MouseButton.Middle)
            {
                cameraTranslation = new Point(cameraTranslation.X - distanceX, cameraTranslation.Y - distanceY);

                // Update the press position.
                mousePositionWhenPressed = mousePositionOnScreen;
            }

            // Need to update to make sure all coordinates are up-to-date.
            Update();

            if (toolMode == ToolMode.Select)
            {
                if (toolState == ToolState.Editing)
                {
                    var start = selectionStartPoint;
                    var end = mousePositionOnWorldSnapped;

                    if (distanceX >= 0 && distanceY >= 0)
                    {
                        var lt = end;
                        var rb = start;
                        selection = Rectangle.FromLTRB(lt.X, lt.Y, rb.X, rb.Y);
                    }
                    else if (distanceX < 0 && distanceY >= 0)
                    {
                        var lt = new Point(start.X, end.Y);
                        var rb = new Point(end.X, start.Y);
                        selection = Rectangle.FromLTRB(lt.X, lt.Y, rb.X, rb.Y);
                    }
                    else if (distanceX >= 0 && distanceY < 0)
                    {
                        var lt = new Point(end.X, start.Y);
                        var rb = new Point(start.X, end.Y);
                        selection = Rectangle.FromLTRB(lt.X, lt.Y, rb.X, rb.Y);
                    }
                    else
                    {
                        var lt = start;
                        var rb = end;
                        selection = Rectangle.FromLTRB(lt.X, lt.Y, rb.X, rb.Y);
                    }

                    shapesSelected.Clear();
                    multiSelected = true;

                    foreach (var shape in shapes)
                    {
                        if (!shape.Locked && shape.Contained(selection))
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

                    foreach (var shape in shapes)
                    {
                        if (!shape.Locked && shape.Contains(mousePositionOnWorld, 0.2f * zoomSetting))
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
                var mousePos = mousePositionOnWorldSnapped;
                var moveAmount = new Point(mousePos.X - movementStartPoint.X, mousePos.Y - movementStartPoint.Y);

                movementStartPoint = mousePos;

                foreach (var shape in shapesSelected)
                {
                    shape.Move(moveAmount);
                }

                Update();
            }
            else if (shapeInEditing != null && toolState == ToolState.Editing)
            {
                shapeInEditing.OnMouseMove(mousePositionOnWorldSnapped);
            }
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

                        shapeInEditing = new CadLine();

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
                shapes.Remove(shapeInEditing);

                toolMode = ToolMode.Text;
                toolState = ToolState.Editing;

                shapeInEditing = new CadText("CadText", "Roboto Mono", 1, FontWeight.Normal, FontStyle.Normal, mousePositionOnWorldSnapped);
                shapes.Add(shapeInEditing);
                Refresh();
            }
            else if (key == Key.P && toolMode != ToolMode.Pin)
            {
                shapes.Remove(shapeInEditing);

                toolMode = ToolMode.Pin;
                var textFormat = new TextFormat("Roboto Mono", FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, 1, "en-US");
                shapeInEditing = new CadPin("CadPin", 1, 5, textFormat, textFormat, mousePositionOnWorldSnapped);
                toolState = ToolState.Armed;

                shapes.Add(shapeInEditing);
                shapesSelected.Clear();
                shapesSelected.Add(shapeInEditing);

                shapeInEditing.OnMouseDown(mousePositionOnWorldSnapped);

                toolMode = ToolMode.Move;
                toolState = ToolState.Editing;

                movementStartPoint = shapesSelected.First().Origin;
                mousePositionOnScreen = WorldToScreen(movementStartPoint);

                Mouse.SetPosition(FromWindowToScreen(FromControlToWindow(mousePositionOnScreen)));

                Update();
            }
            else if (key == Key.R)
            {
                if (Keyboard.IsDown(Key.CtrlLeft))
                {
                    foreach (var shape in shapesSelected)
                    {
                        shape.Rotate();
                    }

                    if (shapesSelected.Count == 1)
                    {
                        Mouse.SetPosition(
                            FromWindowToScreen(
                                FromControlToWindow(
                                    WorldToScreen(shapesSelected.First().Origin))));
                    }

                    Update();
                }
                else if (toolMode != ToolMode.Rectangle)
                {
                    toolMode = ToolMode.Rectangle;

                    shapes.Remove(shapeInEditing);
                    shapeInEditing = new CadRectangle();
                    toolState = ToolState.Armed;

                    shapes.Add(shapeInEditing);

                    Update();
                }
            }
            else if (key == Key.W && toolMode != ToolMode.Line)
            {
                toolMode = ToolMode.Line;

                shapes.Remove(shapeInEditing);
                shapeInEditing = new CadLine();
                toolState = ToolState.Armed;

                shapes.Add(shapeInEditing);

                Update();
            }
            else if (key == Key.M && shapesSelected.Count > 0)
            {
                toolMode = ToolMode.Move;
                toolState = ToolState.Editing;

                movementStartPoint = shapesSelected.First().Origin;
                mousePositionOnScreen = WorldToScreen(movementStartPoint);

                Mouse.SetPosition(FromWindowToScreen(FromControlToWindow(mousePositionOnScreen)));

                Update();
            }
            else if (key == Key.C)
            {
                if (Keyboard.IsDown(Key.AltLeft))
                {
                    SetColorScheme(colorScheme == ColorScheme.Light ? ColorScheme.Dark : ColorScheme.Light);
                    Update();
                }
            }
            else if (key == Key.Space && IsMouseInside == true)
            {
                relativeCursorOnWorld = mousePositionOnWorldSnapped;
                Update();
            }
            else if (key == Key.S)
            {
                if (Keyboard.IsDown(Key.CtrlLeft))
                {
                    shapes.Remove(shapeInEditing);

                    var component = new CadComponent(shapes, viewportCenterOnWorld);
                    shapes.Clear();
                    shapes.Add(component);

                    var str = component.ToOutput();
                }
            }
        }

        Point AbsoluteToCenter(Point abs)
        {
            return new Point(abs.X + viewportCenterOnWorld.X, abs.Y + viewportCenterOnWorld.Y);
        }

        Point CenterToAbsolute(Point center)
        {
            return new Point(center.X - viewportCenterOnWorld.X, center.Y - viewportCenterOnWorld.Y);
        }

        Point WorldToScreen(Point worldPoint)
        {
            return CadMath.Transform(worldPoint, worldTransform);
        }

        Point ScreenToWorld(Point screenPoint)
        {
            return CadMath.Transform(screenPoint, worldTransformInverse);
        }

        protected override void OnLoad(Context context)
        {
            SetColorScheme(colorScheme);

            textFormatStatus = new TextFormat("Roboto Mono", 12, FontWeight.Normal);
            textFormatStatus.TextAlignment = TextAlignment.Trailing;
            textFormatStatus.ParagraphAlignment = ParagraphAlignment.Far;
            textFormatBig = new TextFormat("Roboto Mono", 16);
            textFormatSmall = new TextFormat("Roboto Mono", 12);

            viewportDPI = context.GetDPI();

            viewportCenterOnWorld = new Point(worldSize.Width / 2, worldSize.Height / 2);

            /*var ss = new List<CadShape>() {
                new CadText("Resistor", "Roboto Mono", 1, FontWeight.Normal, FontStyle.Normal, new Point(viewportCenterOnWorld.X, viewportCenterOnWorld.Y - 5)) { Type = CadShapeType.ComponentName },
                new CadText("R", "Roboto Mono", 1, FontWeight.Normal, FontStyle.Normal, new Point(viewportCenterOnWorld.X, viewportCenterOnWorld.Y + 5)) { Type = CadShapeType.ComponentReference }
            };

            shapes.AddRange(ss);*/

            //var component = new CadComponent(ss, centerOnWorld);
            //shapes.Add(component);
        }

        protected override void OnUpdate(Context context)
        {
            viewportSize = Area.Dimensions;

            //
            // Calculate world transformation matrix.
            //
            worldTransform = Matrix3x2.Identity;
            worldTransform *= Matrix3x2.Translation(cameraTranslation);
            worldTransform *= Matrix3x2.Scale((viewportDPI.X / 25.4f) / zoomSetting, (viewportDPI.Y / 25.4f) / zoomSetting, cameraTranslation);
            worldTransform *= Matrix3x2.Translation(new Point(viewportSize.Width / 2, viewportSize.Height / 2));
            worldTransformInverse = Matrix3x2.Invert(worldTransform);

            mousePositionOnWorld = ScreenToWorld(mousePositionOnScreen);

            if (snapMode == SnapMode.Coarse)
            {
                mousePositionOnWorldSnapped = new Point((float)Math.Round(mousePositionOnWorld.X / gridSettingCoarse) * gridSettingCoarse, (float)Math.Round(mousePositionOnWorld.Y / gridSettingCoarse) * gridSettingCoarse);
            }
            else
            {
                mousePositionOnWorldSnapped = new Point((float)Math.Round(mousePositionOnWorld.X / gridSettingFine) * gridSettingFine, (float)Math.Round(mousePositionOnWorld.Y / gridSettingFine) * gridSettingFine);
            }

            mousePositionOnScreenWorldSnapped = WorldToScreen(mousePositionOnWorldSnapped);

            relativeCursorOnScreen = WorldToScreen(relativeCursorOnWorld);

            worldOriginOnScreen = WorldToScreen(new Point());
            worldEndOnScreen = WorldToScreen(new Point(worldSize.Width, worldSize.Height));
            gridMajorSizeOnScreen = WorldToScreen(new Point(gridSettingCoarse, gridSettingCoarse));
            gridMinorSizeOnScreen = WorldToScreen(new Point(gridSettingFine, gridSettingFine));

            viewportCenterOnWorld = new Point(worldSize.Width / 2, worldSize.Height / 2);

            mousePositionRelativeToOriginCenterSnapped = new Point(mousePositionOnWorldSnapped.X - viewportCenterOnWorld.X, mousePositionOnWorldSnapped.Y - viewportCenterOnWorld.Y);
            mousePositionRelativeToOriginMovableSnapped = new Point(mousePositionOnWorldSnapped.X - relativeCursorOnWorld.X, mousePositionOnWorldSnapped.Y - relativeCursorOnWorld.Y);
        }

        protected override void OnDraw(Context context)
        {
            var area = new Rectangle(Area.Dimensions);

            //
            // Draw background
            //

            SharedBrush.Color = backgroundColor;
            context.FillRectangle(area, SharedBrush);

            context.PushAxisAlignedClip(area, AntialiasMode.PerPrimitive);

            //
            // Draw grid
            //

            DrawGrid(context);

            //
            // Draw geometry
            //

            var transformStored = context.GetTransform();

            context.AntialiasMode = AntialiasMode.PerPrimitive;
            context.SetTransform(worldTransform * transformStored);

            SharedBrush.Color = geometryStrokeColor;

            for (var i = 0; i < shapes.Count; i++)
            {
                var shape = shapes[i];

                if (shapesSelected.Contains(shape))
                {
                    SharedBrush.Color = selectColor;
                    shape.OnDraw(context, SharedBrush);
                    SharedBrush.Color = geometryStrokeColor;
                }
                else
                {
                    shape.OnDraw(context, SharedBrush);
                }
            }

            context.SetTransform(transformStored);

            //
            // Draw selection rectangle
            //

            if (toolMode == ToolMode.Select && toolState == ToolState.Editing)
            {
                var tl = WorldToScreen(selection.TopLeft);
                var br = WorldToScreen(selection.BottomRight);
                var selectionOnScreen = Rectangle.FromLTRB(tl.X, tl.Y, br.X, br.Y);

                SharedBrush.Color = selectColor;

                context.AntialiasMode = AntialiasMode.Aliased;
                context.DrawRectangle(selectionOnScreen, SharedBrush, 1);
            }

            //
            // Draw cursor and relative crosshairs.
            //

            context.AntialiasMode = AntialiasMode.Aliased;

            var crossHalfW = crosshairWidth / 2;
            var crossHalfH = crosshairHeight / 2;

            SharedBrush.Color = crosshairCursorColor;
            context.FillRectangle(Rectangle.FromXYWH(mousePositionOnScreenWorldSnapped.X - crossHalfH, mousePositionOnScreenWorldSnapped.Y - crossHalfW, crosshairHeight, crosshairWidth), SharedBrush);
            context.FillRectangle(Rectangle.FromXYWH(mousePositionOnScreenWorldSnapped.X - crossHalfW, mousePositionOnScreenWorldSnapped.Y - crossHalfH, crosshairWidth, crosshairHeight), SharedBrush);

            if (drawRelativeCrosshair)
            {
                SharedBrush.Color = crosshairRelativeColor;
                context.FillRectangle(Rectangle.FromXYWH(relativeCursorOnScreen.X - crossHalfW, relativeCursorOnScreen.Y - crossHalfH, crosshairWidth, crosshairHeight), SharedBrush);
                context.FillRectangle(Rectangle.FromXYWH(relativeCursorOnScreen.X - crossHalfH, relativeCursorOnScreen.Y - crossHalfW, crosshairHeight, crosshairWidth), SharedBrush);
            }

            //
            // Draw the status texts
            //

            SharedBrush.Color = statusColor;

            context.AntialiasMode = AntialiasMode.PerPrimitive;
            context.DrawText(
                $"grid {CadMath.ToString3(gridSettingFine)} : {CadMath.ToString3(gridSettingCoarse)} | " +
                $"zoom {1 / zoomSetting}x | " +
                $"x {CadMath.ToString2(mousePositionOnWorldSnapped.X)} y {CadMath.ToString2(mousePositionOnWorldSnapped.Y)} | " +
                $"dx {CadMath.ToString2(mousePositionRelativeToOriginMovableSnapped.X)} dy {CadMath.ToString2(mousePositionRelativeToOriginMovableSnapped.Y)}",
                textFormatStatus,
                new Rectangle(area.Left, area.Top, area.Right - 5, area.Bottom - 5),
                SharedBrush);

            //
            // Draw the control outline
            //

            context.PopAxisAlignedClip();

            DrawOutline(context);
        }

        void DrawGrid(Context context)
        {
            context.AntialiasMode = AntialiasMode.Aliased;

            SharedBrush.Color = gridOutlineColor;
            context.DrawRectangle(Rectangle.FromLTRB(worldOriginOnScreen.X, worldOriginOnScreen.Y, worldEndOnScreen.X, worldEndOnScreen.Y), SharedBrush, 1);

            SharedBrush.Color = gridCoarseLineColor;

            var xGridMajorLinesInWorld = Math.Ceiling(worldSize.Width / gridSettingCoarse);
            var yGridMajorLinesInWorld = Math.Ceiling(worldSize.Height / gridSettingCoarse);

            var xGridMinorLinesInWorld = Math.Ceiling(worldSize.Width / (gridSettingFine));
            var yGridMinorLinesInWorld = Math.Ceiling(worldSize.Height / (gridSettingFine));

            if (gridMode == GridMode.Lines && (gridMajorSizeOnScreen.X - worldOriginOnScreen.X) > 5)
            {
                for (var i = 1; i < xGridMajorLinesInWorld; i++)
                {
                    var x = worldOriginOnScreen.X + (gridMajorSizeOnScreen.X - worldOriginOnScreen.X) * i;
                    var y1 = worldOriginOnScreen.Y;
                    var y2 = worldEndOnScreen.Y;

                    context.DrawLine(new Point(x, y1), new Point(x, y2), SharedBrush, 1f);
                }

                for (var i = 1; i < yGridMajorLinesInWorld; i++)
                {
                    var y = worldOriginOnScreen.Y + (gridMajorSizeOnScreen.Y - worldOriginOnScreen.Y) * i;
                    var x1 = worldOriginOnScreen.X;
                    var x2 = worldEndOnScreen.X;

                    context.DrawLine(new Point(x1, y), new Point(x2, y), SharedBrush, 1f);
                }

                if (snapMode == SnapMode.Fine && (gridMinorSizeOnScreen.X - worldOriginOnScreen.X) > 5)
                {
                    SharedBrush.Color = gridFineLineColor;

                    for (var i = 1; i < xGridMinorLinesInWorld; i++)
                    {
                        var x = worldOriginOnScreen.X + (gridMinorSizeOnScreen.X - worldOriginOnScreen.X) * i;
                        var y1 = worldOriginOnScreen.Y;
                        var y2 = worldEndOnScreen.Y;

                        context.DrawLine(new Point(x, y1), new Point(x, y2), SharedBrush, 1f);
                    }

                    for (var i = 1; i < yGridMinorLinesInWorld; i++)
                    {
                        var y = worldOriginOnScreen.Y + (gridMinorSizeOnScreen.Y - worldOriginOnScreen.Y) * i;
                        var x1 = worldOriginOnScreen.X;
                        var x2 = worldEndOnScreen.X;

                        context.DrawLine(new Point(x1, y), new Point(x2, y), SharedBrush, 1f);
                    }
                }
            }

            //
            // Draw the center axis
            //

            if (drawCenterAxes)
            {
                SharedBrush.Color = Color.FromAColor(1f, Color.DimGray);

                var centerX = worldOriginOnScreen.X + (worldEndOnScreen.X - worldOriginOnScreen.X) / 2;
                var centerY = worldOriginOnScreen.Y + (worldEndOnScreen.Y - worldOriginOnScreen.Y) / 2;

                context.AntialiasMode = AntialiasMode.Aliased;
                context.DrawLine(new Point(centerX, worldOriginOnScreen.Y), new Point(centerX, worldEndOnScreen.Y), SharedBrush, 1);
                context.DrawLine(new Point(worldOriginOnScreen.X, centerY), new Point(worldEndOnScreen.X, centerY), SharedBrush, 1);
            }
        }
    }
}
