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
            //var line = new CadLine("	line 100,   100,110,110,y,10,0,0" + Environment.NewLine);
            //var o = line.ToOutput();
            /*var document = new JsonDocument();
            
            var shape = new CadRectangle(Rectangle.FromXYWH(100, 100, 10, 10));
            document.Append(shape.ToOutput());

            document.Write("e:/KodoCAD.js", false, true);*/

            using (var manager = new WindowManager(OnUnhandledException))
            {
                var windowSettings = new WindowSettings();
                windowSettings.Area = Rectangle.FromXYWH(100, 100, 1280, 880);
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

    class CadWindowPinOptions : Window
    {
        Button buttonOK;
        Button buttonCancel;

        Textbox textboxName;
        Textbox textboxNumber;
        Textbox textboxSizeOfName;
        Textbox textboxSizeOfNumber;
        Textbox textboxLength;
        Textbox textboxNameOffset;

        Set<CadShape> shapesOriginal;
        Set<CadShape> shapesNew;

        CadPin shapeOriginal;
        CadPin shapeNew;

        public event DefaultEventHandler OnClosed;

        public CadPin Shape => shapeNew;
        public CadPin ShapeOriginal => shapeOriginal;

        public CadWindowPinOptions(WindowManager manager, WindowSettings settings) : base(manager, settings)
        {
            buttonOK = new Button(this);
            buttonOK.Text = "OK";
            buttonOK.OnClick += OnClickOK;

            buttonCancel = new Button(this);
            buttonCancel.Text = "Cancel";
            buttonCancel.OnClick += OnClickCancel;

            textboxName = new Textbox(this);
            textboxName.Subtle = true;
            textboxNumber = new Textbox(this);
            textboxName.Subtle = true;

            textboxLength = new Textbox(this);
            textboxLength.Subtle = true;

            textboxSizeOfName = new Textbox(this);
            textboxSizeOfName.Subtle = true;
            textboxSizeOfNumber = new Textbox(this);
            textboxSizeOfNumber.Subtle = true;

            textboxNameOffset = new Textbox(this);
            textboxNameOffset.Subtle = true;
        }

        protected override void OnUpdate(Context context)
        {
            base.OnUpdate(context);

            var clientArea = Client;

            textboxName.Area = Rectangle.FromXYWH(3, 3, 250, 30);
            textboxNumber.Area = Rectangle.FromXYWH(3, textboxName.Area.Bottom + 10, 250, 30);

            textboxLength.Area = Rectangle.FromXYWH(3, textboxNumber.Area.Bottom + 10, 250, 30);

            textboxSizeOfName.Area = Rectangle.FromXYWH(3, textboxLength.Area.Bottom + 10, 250, 30);
            textboxSizeOfNumber.Area = Rectangle.FromXYWH(3, textboxSizeOfName.Area.Bottom + 10, 250, 30);

            textboxNameOffset.Area = Rectangle.FromXYWH(3, textboxSizeOfNumber.Area.Bottom + 10, 250, 30);

            buttonOK.Area = Rectangle.FromLTRB(clientArea.Left, textboxNameOffset.Area.Bottom, clientArea.Left + clientArea.Width / 2, clientArea.Bottom);
            buttonCancel.Area = Rectangle.FromLTRB(clientArea.Right - clientArea.Width / 2, textboxNameOffset.Area.Bottom, clientArea.Right, clientArea.Bottom);
        }

        public void Show(CadPin textShape)
        {
            shapeOriginal = textShape;

            textboxName.Text = shapeOriginal.Name;
            textboxNumber.Text = shapeOriginal.Number.ToString();
            textboxLength.Text = shapeOriginal.Length.ToString();
            textboxSizeOfName.Text = shapeOriginal.SizeOfName.ToString();
            textboxSizeOfNumber.Text = shapeOriginal.SizeOfNumber.ToString();
            textboxNameOffset.Text = shapeOriginal.OffsetOfName.ToString();

            Visible = true;
        }

        public void Hide()
        {
            Visible = false;
            OnClosed?.Invoke();
        }

        void OnClickOK()
        {
            var formatOfName = new TextFormat("Nunito", float.Parse(textboxSizeOfName.Text, CultureInfo.InvariantCulture));
            var formatOfNumber = new TextFormat("Nunito", float.Parse(textboxSizeOfNumber.Text, CultureInfo.InvariantCulture));
            shapeNew = new CadPin(textboxName.Text, int.Parse(textboxNumber.Text), float.Parse(textboxLength.Text), formatOfName, formatOfNumber, shapeOriginal.Origin, shapeOriginal);
            Hide();
        }

        void OnClickCancel()
        {
            shapeNew = shapeOriginal;
            Hide();
        }
    }

    class CadWindowTextOptions : Window
    {
        Button buttonOK;
        Button buttonCancel;

        Textbox textboxText;
        Textbox textboxSize;

        Set<CadShape> shapesOriginal;
        Set<CadShape> shapesNew;

        CadText shapeOriginal;
        CadText shapeNew;

        public event DefaultEventHandler OnClosed;

        public CadText Shape => shapeNew;
        public CadText ShapeOriginal => shapeOriginal;

        public CadWindowTextOptions(WindowManager manager, WindowSettings settings) : base(manager, settings)
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

        public void Show(CadText textShape)
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
            shapeNew = new CadText(textboxText.Text, "Roboto Mono", float.Parse(textboxSize.Text, CultureInfo.InvariantCulture), FontWeight.Normal, FontStyle.Normal, shapeOriginal.Origin);
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
        CadWindowPinOptions pinOptions;
        CadWindowTextOptions textOptions;

        CadEditor editor;

        public CadWindow(WindowManager manager, WindowSettings settings)
            : base(manager, settings)
        {
            var textEditorSettings = new WindowSettings();
            textEditorSettings.Area = Rectangle.FromXYWH(0, 0, 16f * 20, 9f * 30).CenterTo(Area);
            textEditorSettings.MinimumSize = new Size(0, 0);
            textEditorSettings.Margings = new WindowMargings(3);
            textEditorSettings.NoTitle = true;
            textEditorSettings.ToolWindow = true;

            pinOptions = new CadWindowPinOptions(manager, textEditorSettings);
            pinOptions.OnClosed += PinEditor_OnClosed;
            pinOptions.Visible = false;
            pinOptions.StyleInformation = StyleInformation;
            pinOptions.Create();

            textOptions = new CadWindowTextOptions(manager, textEditorSettings);
            textOptions.OnClosed += TextEditor_OnClosed;
            textOptions.Visible = false;
            textOptions.StyleInformation = StyleInformation;
            textOptions.Create();

            editor = new CadEditor(this);
            editor.OnShapeEdit += Editor_OnShapeEdit;
        }

        void TextEditor_OnClosed()
        {
            editor.ReplaceShape(textOptions.ShapeOriginal, textOptions.Shape);

            Locked = false;
        }

        void PinEditor_OnClosed()
        {
            editor.ReplaceShape(pinOptions.ShapeOriginal, pinOptions.Shape);

            Locked = false;
        }

        void Editor_OnShapeEdit(Set<CadShape> shapes)
        {
            if (shapes.Count == 1)
            {
                var shape = shapes.First();

                if (shape is CadText textShape)
                {
                    Locked = true;
                    textOptions.Show(textShape);
                }
                else if (shape is CadPin pinShape)
                {
                    Locked = true;
                    pinOptions.Show(pinShape);
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
