using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

using System.Windows.Controls;

using System.IO; // IOSYS
using System.Timers; // Sakuya! Sakuya! Pad-chou de ite dareda
using System.Windows.Media;
using System.Windows.Media.Animation;

// Just started using power mode to write my code
// I am no programmer
// I am the magician of digital age

namespace Saigong
{
    public enum EditMode
    {
        Main, Plan
    }

    public partial class MainWindow : Window
    {
        Lang lang;
        Dictionary<string, string> configs;

        delegate int intDelegate();

        bool ListenToStyleChanges;
        bool Searching;
        EditMode currentMode;
        string nameWhenLoad;

        TextPointer findStart;

        TextPointer mainCaretPosition;
        TextPointer planCaretPosition;

        List<TextBlock> messageTextBlocks;

        string TextTitle
        {
            get
            {
                return this.TitleTextArea.Text;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            Initialise();
        }

        private void Initialise()
        {
            TextClasses.LoadStyles();
            configs = ConfigLoader.LoadConfigFile();
            if (configs.Keys.Contains("lang"))
            {
                lang = new Lang(configs["lang"]);
            }
            else
            {
                lang = new Lang();
            }
            ApplyConfig();
            Directory.CreateDirectory(DirectoryManager.SaveDir);
            Directory.CreateDirectory(DirectoryManager.BackDir);
            Directory.CreateDirectory(DirectoryManager.PlanDir);
            TitleTextArea.Focus();
            WindowTitle.Text = lang["title"];
            HideHandle();
            ListenToStyleChanges = false;
            Searching = false;
            mainCaretPosition = MainTextArea.Document.ContentStart;
            messageTextBlocks = new List<TextBlock>();
            AddMessage(lang["startupFinished"]);
            findStart = MainTextArea.Document.ContentStart;
            currentMode = EditMode.Main;
            nameWhenLoad = null;

            MainTextArea.Document.Blocks.Add
                (
                new Paragraph()
                );
            MainTextArea.Document.Blocks.FirstBlock.Style
                = TextClasses.GetTextClassStyleByName("NormalText").DisplayStyle;
        }

        // Helper doing what its name says
        private double PtToPx(double pt)
        {
            return (pt * 96) / 72;
        }

        // Helper function to add default value to config without replacing
        private void SetConfigDefault(string key, string value)
        {
            if (!configs.Keys.Contains(key))
                configs[key] = value;
        }

        private void ApplyConfig()
        {
            // First make default values
            SetConfigDefault
                ("main-font-family", "Baskerville, Georgia, STSong, SimSun, serif");
            SetConfigDefault("line-width", "24"); // In Hanzi
            SetConfigDefault("text-rendering-mode", "Aliased"); // Hack

            MainTextArea.Width =
                PtToPx
                (
                TextClasses.NormalTextWidth
                * Convert.ToInt32(configs["line-width"])
                + 2
                );

            App.Current.Resources["MainTextFamily"] =
                new FontFamily(configs["main-font-family"]);

            // Set text rendering mode
            TextOptions.SetTextRenderingMode
                (
                this,
                (TextRenderingMode)
                    Enum.Parse(typeof(TextRenderingMode), configs["text-rendering-mode"])
                );
        }

        private void EditStart()
        {
            OperationTextArea.Visibility = Visibility.Hidden;
            HideHandle();
        }

        private void ShowHandle()
        {
            if (this.WindowState == WindowState.Normal)
            {
                WindowTitle.Visibility = Visibility.Visible;
                ExitButton.Visibility = Visibility.Visible;
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
            }
        }

        private void HideHandle()
        {
            WindowTitle.Visibility = Visibility.Hidden;
            ExitButton.Visibility = Visibility.Hidden;
            this.ResizeMode = ResizeMode.NoResize;
        }

        private void SaveFile()
        {
            SaveText
                (
                DirectoryManager.GetDir(TextTitle, DirType.Save),
                MainTextArea
                );
            AddMessage(lang["saved"]);
            SaveText
                (
                DirectoryManager.GetDir(TextTitle, DirType.Plan),
                PlanTextArea
                );
            AddMessage(lang["planSaved"]);
        }
        
        private void Backup(bool manual)
        {
            var dir =
                string.Format
                (
                "{0} {1}",
                TextTitle,
                manual?
                TimeStringManager.GetCurrentDateTimeString()
                :
                TimeStringManager.GetCurrentDateString()
                );
            if (!File.Exists(dir))
            {
                SaveText
                    (
                    DirectoryManager.GetDir(dir, DirType.Back),
                    MainTextArea
                    );
                if (manual) AddMessage(lang["backupDone"]);
                else AddMessage(lang["autoBackupDone"]);
            }
        }

        private void SaveText(string dir, RichTextBox from)
        {
            IEnumerator<Block> blocks;
            Block b;

            StringBuilder sb = new StringBuilder();

            blocks = from.Document.Blocks.GetEnumerator();
            for (int i = 0; i < from.Document.Blocks.Count; i++)
            {
                blocks.MoveNext();
                b = blocks.Current;
                if (b.Tag != null)
                {
                    sb.Append((string)b.Tag);
                }
                sb.Append(new TextRange(b.ContentStart, b.ContentEnd).Text);
                sb.Append("\r\n");
            }
            File.WriteAllText(dir, sb.ToString(), Encoding.UTF8);
        }

        private bool LoadText(string title)
        {
            FileStream fs;
            FlowDocument doc = new FlowDocument();
            MainTextArea.Document.Blocks.Clear();
            var location = DirectoryManager.GetDir(title, DirType.Save);
            if (File.Exists(location))
            {
                fs = new FileStream
                    (
                    location,
                    FileMode.Open,
                    FileAccess.Read
                    );
                using (fs)
                {
                    TextRange tr = new TextRange
                        (
                        doc.ContentStart,
                        doc.ContentEnd
                        );
                    tr.Load(fs, DataFormats.Text);
                }
                // Now apply styles to the new text
                var sc = TextClasses.GetSymbolCollection();
                foreach (var b in doc.Blocks)
                {
                    var newp = new Paragraph();
                    var text = new TextRange(b.ContentStart, b.ContentEnd).Text;
                    string sym = "";

                    foreach (var s in sc)
                    {
                        if (text.StartsWith(s) && s.Length > sym.Length)
                        {
                            sym = s;
                        }
                    }
                    newp.Style = TextClasses.GetTextClassStyle(sym).DisplayStyle;
                    newp.Tag = sym;
                    newp.Inlines.Add(new Run(text.Substring(sym.Length)));
                    MainTextArea.Document.Blocks.Add(newp);
                }
                MainTextArea.Focus();
                MainTextArea.CaretPosition = MainTextArea.Document.ContentEnd;
                mainCaretPosition = MainTextArea.CaretPosition;
                return true;
            }
            else
            {
                mainCaretPosition = MainTextArea.Document.ContentEnd;
                return false;
            }
        }

        private void AddMessage(string text)
        {
            if (messageTextBlocks.Count<TextBlock>(t => t.Opacity == 0.0) == messageTextBlocks.Count)
            {
                messageTextBlocks.Clear();
                MessageContainerGrid.Children.Clear();
            }
            TextBlock tb;
            tb = new TextBlock();
            tb.Style = (Style)App.Current.Resources["MessageTextBlock"];
            tb.Margin = new Thickness(0, 0, 20, 20 + (messageTextBlocks.Count * 50));
            tb.Text = text;
            MessageContainerGrid.Children.Add(tb);
            messageTextBlocks.Add(tb);
            tb.BeginStoryboard((Storyboard)App.Current.Resources["FadeOut"]);
            return;
        }

        private void IsolateElements(bool enable)
        {
            if (enable)
            {
                MainTextArea.IsReadOnly = false;
                TitleTextArea.IsReadOnly = false;
            }
            else
            {
                MainTextArea.IsReadOnly = true;
                TitleTextArea.IsReadOnly = true;
                Keyboard.Focus(MainTextArea);
            }
        }

        private void FindInitialise()
        {
            mainCaretPosition = MainTextArea.CaretPosition;
            findStart = MainTextArea.CaretPosition;
            OperationTextArea.Visibility = Visibility.Visible;
            OperationTextArea.Text = "";
        }

        private void FindEnd()
        {
            OperationTextArea.Visibility = System.Windows.Visibility.Hidden;
            switch (currentMode)
            {
                case EditMode.Main:
                    MainTextArea.Focus();
                    MainTextArea.CaretPosition = mainCaretPosition;
                    break;
                case EditMode.Plan:
                    PlanTextArea.Focus();
                    break;
            }
        }

        private void FindText()
        {
            String text = OperationTextArea.Text;

            if (OperationTextArea.Visibility == Visibility.Hidden)
            {
                FindInitialise();
                Keyboard.Focus(OperationTextArea);
                return;
            }

            if (OperationTextArea.Text == "")
            {
                AddMessage(lang["nullOperation"]);
                return;
            }

            // First check if the text really exist
            {
                var tr =
                    new TextRange(MainTextArea.Document.ContentStart, MainTextArea.Document.ContentEnd);
                if (tr.Text.IndexOf(text) == -1)
                {
                    AddMessage(lang["notFound"]);
                    return;
                }
            }

            BEGIN:

            {
                var tr =
                    new TextRange(findStart, MainTextArea.Document.ContentEnd);
                for
                    (
                    var tp = tr.Start;
                    tp.GetOffsetToPosition(tr.End) > text.Length;
                    tp = tp.GetPositionAtOffset(1)
                    )
                {
                    var sr =
                        new TextRange(tp, tp.GetPositionAtOffset(text.Length));
                    if (sr.Text == text) // Found
                    {
                        Searching = true;
                        findStart = tp.GetPositionAtOffset(1);
                        MainTextArea.Focus();
                        MainTextArea.Selection.Select
                            (tp, tp.GetPositionAtOffset(text.Length));
                        AddMessage(lang["found"]);
                        Searching = false;
                        return;
                    }
                }
            }

            // This part is reached if not found
            if (findStart != MainTextArea.Document.ContentStart)
            {
                findStart = MainTextArea.Document.ContentStart;
                // Show From Beginning Sign Here
                goto BEGIN;
            }
        }

        private void CharCount()
        {
            int count = 0;
            foreach (var b in MainTextArea.Document.Blocks)
            {
                var tr =
                    new TextRange(b.ContentStart, b.ContentEnd);
                count += tr.Text.Length;
            }
            AddMessage(count.ToString() + lang["chara"]);
        }

        private void LoadFile()
        {
            if (LoadText(TextTitle))
            {
                AddMessage(lang["loaded"]);
                nameWhenLoad = TitleTextArea.Text;
                Backup(false);
            }
            else
            {
                AddMessage(lang["loadFail"]);
            }
            if (LoadPlan(TextTitle))
            {
                AddMessage(lang["planLoaded"]);
            }
            else
            {
                AddMessage(lang["planLoadFail"]);
            }
        }

        private void ShutProgram()
        {
            AddMessage(lang["shutdown"]);
            Application.Current.Shutdown();
        }

        private void ShowTime()
        {
            AddMessage(DateTime.Now.TimeOfDay.ToString(@"hh\:mm"));
        }

        private void ChangeParagraphStyle(Paragraph p, TextClass tc)
        {
            p.Style = tc.DisplayStyle;
            p.Tag = tc.Symbol;
        }

        private void ApplyStyle(KeyEventArgs e)
        {
            if (MainTextArea.IsFocused == false)
            {
                return;
            }
            var p = MainTextArea.CaretPosition.Paragraph;
            var tc = TextClasses.GetTextClassStyle(e.Key);
            if (tc == null)
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
                ChangeParagraphStyle(p, tc);
            }
            ListenToStyleChanges = false;
        }

        private bool LoadPlan(string title)
        {
            FileStream fs;
            var location = DirectoryManager.GetDir(title, DirType.Plan);
            if (File.Exists(location))
            {
                fs = new FileStream
                    (
                    location,
                    FileMode.Open,
                    FileAccess.Read
                    );
                using (fs)
                {
                    TextRange tr = new TextRange
                        (
                        PlanTextArea.Document.ContentStart,
                        PlanTextArea.Document.ContentEnd
                        );
                    tr.Load(fs, DataFormats.Text);
                }
                PlanTextArea.Focus();
                PlanTextArea.CaretPosition = PlanTextArea.Document.ContentEnd;
                planCaretPosition = PlanTextArea.CaretPosition;
                return true;
            }
            else
            {
                planCaretPosition = PlanTextArea.Document.ContentEnd;
                return false;
            }
        }

        private void TogglePlan()
        {
            if (PlanTextArea.Visibility == Visibility.Hidden)
            {
                mainCaretPosition = MainTextArea.CaretPosition;
                MainTextArea.Visibility = Visibility.Hidden;
                PlanTextArea.Visibility = Visibility.Visible;
                PlanTextArea.Focus();
                PlanTextArea.CaretPosition = planCaretPosition;
                currentMode = EditMode.Plan;
            }
            else
            {
                planCaretPosition = PlanTextArea.CaretPosition;
                PlanTextArea.Visibility = Visibility.Hidden;
                MainTextArea.Visibility = Visibility.Visible;
                MainTextArea.Focus();
                MainTextArea.CaretPosition = mainCaretPosition;
                currentMode = EditMode.Main;
            }
        }

        private void ChangeWindowState()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (ListenToStyleChanges)
            {
                ApplyStyle(e);
                return;
            }
            if (e.Key == Key.Escape) // Stop searching
            {
                FindEnd();
            }
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                //IsolateElements(false);
                if ((e.Key == Key.S) || (e.Key == Key.O))
                {
                    if (TitleTextArea.Text != "")
                    {
                        if (e.Key == Key.S)
                        {
                            SaveFile();
                        }
                        if (e.Key == Key.O)
                        {
                            LoadFile();
                        }
                    }
                    else
                    {
                        AddMessage(lang["nullTitle"]);
                    }
                }
                switch (e.Key)
                {
                    case Key.LeftAlt: ListenToStyleChanges = true; break;
                    case Key.Q: ShutProgram(); break;
                    case Key.M: if (currentMode == EditMode.Main) CharCount(); break;
                    case Key.F: if (currentMode == EditMode.Main) FindText(); break;
                    case Key.W: ChangeWindowState(); break;
                    case Key.N: ShowTime(); break;
                    case Key.P: Backup(true); break;
                    case Key.LWin: this.WindowState = WindowState.Minimized; break;
                    case Key.Tab: if (!OperationTextArea.IsVisible) TogglePlan(); break;
                }
                e.Handled = true;
            }
        }

        private void WindowTitle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.Style = (Style)App.Current.FindResource("WindowWin");
                WindowBorder.Visibility = Visibility.Visible;
                ShowHandle();
            }
            else if (this.WindowState == WindowState.Maximized)
            {
                this.Style = (Style)App.Current.FindResource("WindowMax");
                WindowBorder.Visibility = Visibility.Hidden;
                HideHandle();
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            ShutProgram();
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
            e.Handled = true;
        }

        private void MainTextArea_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (!Searching)
            {
                mainCaretPosition = MainTextArea.CaretPosition;
            }
        }
    }
}