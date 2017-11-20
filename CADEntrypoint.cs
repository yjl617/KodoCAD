using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kodo.Json;
using Kodo.Graphics;
using Kodo.Graphics.Style;

namespace KodoCad
{
    class CadEntrypoint
    {
        static void OnUnhandledException(Exception exp)
        {
        }

        static void Main(string[] args)
        {
            /*var document = new JsonDocument();
            
            var shape = new CADShapeRectangle(Rectangle.FromXYWH(100, 100, 10, 10));
            document.Append(shape.ToOutput());

            document.Write("e:/KodoCAD.js", false, true);*/


            using (var manager = new WindowManager(OnUnhandledException))
            {
                var windowSettings = new WindowSettings();
                windowSettings.Area = Rectangle.FromXYWH(100, 100, 1280, 720);
                windowSettings.Name = "KodoCAD";
                var window = new CadWindow(manager, windowSettings);

                window.StyleInformation = new StyleInformation(
                    accent: Color.LightSteelBlue,
                    background: new Color(0xFF151A22),
                    foreground: new Color(0xFFECF1F3),
                    outline: Color.Black,
                    overlay: new Color(0.2, 0.2, 0.2, 0.3),
                    overlayHover: new Color(0.2, 0.6, 0.65, 0.7),
                    overlayPress: new Color(0.2, 0.8, 0.5, 0.5));


                manager.Run(window);
            }
        }
    }

    class CadWindowQuickOptions : Window
    {
        Button buttonOK;
        Button buttonCancel;

        Textbox textboxText;
        Textbox textboxSize;

        Set<CadShape> shapesOriginal;
        Set<CadShape> shapesNew;

        CadShapeText shapeOriginal;
        CadShapeText shapeNew;

        public event DefaultEventHandler OnClosed;

        public CadShapeText Shape => shapeNew;
        public CadShapeText ShapeOriginal => shapeOriginal;

        public CadWindowQuickOptions(WindowManager manager, WindowSettings settings) : base(manager, settings)
        {
            buttonOK = new Button(this);
            buttonOK.Text = "OK";
            buttonOK.OnClick += OnClickOK;

            buttonCancel = new Button(this);
            buttonCancel.Text = "Cancel";
            buttonCancel.OnClick += OnClickCancel;

            textboxText = new Textbox(this);
            textboxText.Subtle = true;
            textboxSize = new Textbox(this);
            textboxText.Subtle = true;
        }

        protected override void OnUpdate(Context context)
        {
            base.OnUpdate(context);

            var clientArea = Client;

            textboxText.Area = Rectangle.FromXYWH(3, 3, 250, 30);
            textboxSize.Area = Rectangle.FromXYWH(3, 30 + 10, 250, 30);

            buttonOK.Area = Rectangle.FromLTRB(clientArea.Left, textboxSize.Area.Bottom, clientArea.Left + clientArea.Width / 2, clientArea.Bottom);
            buttonCancel.Area = Rectangle.FromLTRB(clientArea.Right - clientArea.Width / 2, textboxSize.Area.Bottom, clientArea.Right, clientArea.Bottom);
        }

        public void Show(CadShapeText textShape)
        {
            shapeOriginal = textShape;

            textboxText.Text = shapeOriginal.Text;
            textboxSize.Text = shapeOriginal.Size.ToString();

            Visible = true;
        }

        public void Hide()
        {
            Visible = false;
            OnClosed?.Invoke();
        }

        void OnClickOK()
        {
            var textFormat = new TextFormat("Nunito", FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, float.Parse(textboxSize.Text, CultureInfo.InvariantCulture), "en-US");
            shapeNew = new CadShapeText(textboxText.Text, textFormat, shapeOriginal.Origin);
            Hide();
        }

        void OnClickCancel()
        {
            shapeNew = shapeOriginal;
            Hide();
        }
    }

    class CadWindow : Window
    {
        CadWindowQuickOptions quickOptions;

        CadEditor editor;

        public CadWindow(WindowManager manager, WindowSettings settings)
            : base(manager, settings)
        {
            var textEditorSettings = new WindowSettings();
            textEditorSettings.Area = Rectangle.FromXYWH(0, 0, 16f * 20, 9f * 20).CenterTo(Area);
            textEditorSettings.MinimumSize = new Size(0, 0);
            textEditorSettings.Margings = new WindowMargings(3);
            textEditorSettings.NoTitle = true;
            textEditorSettings.ToolWindow = true;

            quickOptions = new CadWindowQuickOptions(manager, textEditorSettings);
            quickOptions.OnClosed += TextEditor_OnClosed;
            quickOptions.Visible = false;

            quickOptions.StyleInformation = new StyleInformation(
                accent: Color.LightSteelBlue,
                background: new Color(0xFF151A22),
                foreground: new Color(0xFFECF1F3),
                outline: Color.Black,
                overlay: new Color(0.2, 0.2, 0.2, 0.3),
                overlayHover: new Color(0.2, 0.6, 0.65, 0.7),
                overlayPress: new Color(0.2, 0.8, 0.5, 0.5));

            quickOptions.Create();

            editor = new CadEditor(this);
            editor.OnShapeEdit += Editor_OnShapeEdit;
        }

        void TextEditor_OnClosed()
        {
            editor.ReplaceShape(quickOptions.ShapeOriginal, quickOptions.Shape);

            Locked = false;
        }

        void Editor_OnShapeEdit(Set<CadShape> shapes)
        {
            if (shapes.Count == 1)
            {
                var shape = shapes.First();

                if (shape is CadShapeText)
                {
                    Locked = true;
                    quickOptions.Show(shape as CadShapeText);
                }
            }
            else
            {

            }
        }

        protected override void OnUpdate(Context context)
        {
            base.OnUpdate(context);

            var clientArea = Client;

            editor.Area = Rectangle.FromLTRB(clientArea.Left, clientArea.Top, clientArea.Right, clientArea.Bottom);
        }
    }
}
