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
    class CADEntrypoint
    {
        static void OnUnhandledException(Exception exp)
        {
        }

        static void Main(string[] args)
        {
            var mm = CADMath.MilsToMillimeters(300);
            var mils = CADMath.MillimetersToMils(mm);
            mm = CADMath.MilsToMillimeters(1);

            using (var manager = new WindowManager(OnUnhandledException))
            {
                var windowSettings = new WindowSettings();
                windowSettings.Area = Rectangle.FromXYWH(100, 100, 1280, 720);
                windowSettings.Name = "KodoCAD";
                var window = new CADWindow(manager, windowSettings);

                manager.Run(window);
            }
        }
    }

    class CADWindowTextOptions : Window
    {


        Button buttonOK;
        Button buttonCancel;
        Textbox textboxText;
        Textbox textboxSize;

        CADShapeText shapeOriginal;
        CADShapeText shapeNew;

        public event DefaultEventHandler OnClosed;

        public CADShapeText Shape => shapeNew;
        public CADShapeText ShapeOriginal => shapeOriginal;

        public CADWindowTextOptions(WindowManager manager, WindowSettings settings) : base(manager, settings)
        {
            buttonOK = new Button(this);
            buttonOK.Text = "OK";
            buttonOK.OnClick += OnClickOK;

            buttonCancel = new Button(this);
            buttonCancel.Text = "Cancel";
            buttonCancel.OnClick += OnClickCancel;

            textboxText = new Textbox(this);
            textboxSize = new Textbox(this);
        }

        protected override void OnUpdate(Context context)
        {
            base.OnUpdate(context);

            var clientArea = Client;

            textboxText.Area = Rectangle.FromLTRB(clientArea.Left, clientArea.Top, clientArea.Right, clientArea.Top + (clientArea.Height * (5f / 8f)));
            textboxSize.Area = Rectangle.FromLTRB(clientArea.Left, clientArea.Top + (clientArea.Height * (5f / 8f)), clientArea.Right, clientArea.Top + (clientArea.Height * (5f / 8f)) + clientArea.Top + (clientArea.Height * (2f / 8f)));

            buttonOK.Area = Rectangle.FromLTRB(clientArea.Left, textboxSize.Area.Bottom, clientArea.Left + clientArea.Width / 2, clientArea.Bottom);
            buttonCancel.Area = Rectangle.FromLTRB(clientArea.Right - clientArea.Width / 2, textboxSize.Area.Bottom, clientArea.Right, clientArea.Bottom);
        }

        public void Show(CADShapeText textShape)
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
            var textFormat = new TextFormat("Montserrat", FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, float.Parse(textboxSize.Text, CultureInfo.InvariantCulture), "en-US");
            shapeNew = new CADShapeText(textboxText.Text, textFormat, shapeOriginal.Origin);
            Hide();
        }

        void OnClickCancel()
        {
            shapeNew = shapeOriginal;
            Hide();
        }
    }

    class CADWindow : Window
    {
        CADWindowTextOptions textEditor;

        CADEditor editor;

        public CADWindow(WindowManager manager, WindowSettings settings)
            : base(manager, settings)
        {
            
            var textEditorSettings = new WindowSettings();
            textEditorSettings.Area = Rectangle.FromXYWH(0, 0, 16f * 20, 9f * 20).CenterTo(this.Area);
            textEditorSettings.MinimumSize = new Size(0, 0);
            textEditorSettings.Margings = new WindowMargings(3);
            textEditorSettings.NoTitle = true;
            textEditorSettings.ToolWindow = true;

            textEditor = new CADWindowTextOptions(manager, textEditorSettings);
            textEditor.OnClosed += TextEditor_OnClosed;
            textEditor.Visible = false;
            textEditor.Create();

            editor = new CADEditor(this);
            editor.OnShapeEdit += Editor_OnShapeEdit;
        }

        void TextEditor_OnClosed()
        {
            editor.ReplaceShape(textEditor.ShapeOriginal, textEditor.Shape);

            Locked = false;
        }

        void Editor_OnShapeEdit(Set<CADShape> shapes)
        {
            Locked = true;

            if (shapes.Count == 1)
            {
                var shape = shapes.First();

                if (shape is CADShapeText)
                {
                    textEditor.Show(shape as CADShapeText);
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
