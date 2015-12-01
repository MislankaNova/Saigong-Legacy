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

namespace Saigong
{
    public enum StyleName
    {
        NormalText, TitleTile, LesserTitleText, MetaText
    }

    public partial class MainWindow : Window
    {
        Lang lang;

        delegate int intDelegate();

        static string saveLocation = "saves/";
        static string backLocation = "saves/back/";
        static string planLocation = "saves/plan/";

        bool ListenToStyleChanges;

        TextPointer findStart;

        TextPointer mainCaretPosition;
        TextPointer planCaretPosition;

        List<TextBlock> messageTextBlocks;

        static string[] StyleName = new string[4]
        {
            "LesserTitleText",
            "TitleText",
            "MetaText",
            "NormalText"
        };

        static string[] StyleSymbol = new string[4] 
        {
            "##",
            "#",
            "*",
            ""
        };

        static Key[] StyleKey = new Key[4]
        {
            Key.D2,
            Key.D1,
            Key.D3,
            Key.D0
        };

        int blockCount
        {
            get
            {
                return MainTextArea.Document.Blocks.Count;
            }
            set
            {
                ;
            }
        }

        TextRange mainTextRange
        {
            get
            {
                return new TextRange
                    (
                    MainTextArea.Document.ContentStart,
                    MainTextArea.Document.ContentEnd
                    );
            }
            set
            {
                ;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            Initialise();
        }

        private void Initialise()
        {
            lang = new Lang("hant"); // Use "hant" (Han traditional) for now. Use loaded prefrence in the future.
            Directory.CreateDirectory("saves/back/");
            Directory.CreateDirectory("saves/plan/");
            FindInitialise(true);
            TitleTextArea.Focus();
            WindowTitle.Text = lang["title"];
            WindowClose.Text = "X";
            HideHandle();
            ListenToStyleChanges = false;
            messageTextBlocks = new List<TextBlock>();
            AddMessage(lang["startupFinished"]);
            findStart = MainTextArea.Document.ContentStart;
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
                WindowClose.Visibility = Visibility.Visible;
                this.ResizeMode = ResizeMode.CanResizeWithGrip;
            }
        }

        private void HideHandle()
        {
            WindowTitle.Visibility = Visibility.Hidden;
            WindowClose.Visibility = Visibility.Hidden;
            this.ResizeMode = ResizeMode.NoResize;
        }

        private void SaveText(string location, bool back)
        {
            IEnumerator<Block> blocks;
            Block b;
            string text = "";
            if (back)
            {
                location = backLocation + location + ".txt";
            }
            else
            {
                location = saveLocation + location + ".txt";
            }
            //FormatExisting();
            blocks = MainTextArea.Document.Blocks.GetEnumerator();
            for (int i = 0; i < MainTextArea.Document.Blocks.Count; i++)
            {
                blocks.MoveNext();
                b = blocks.Current;
                if (b.Tag == null)
                {
                    text += StyleSymbol[Array.IndexOf(StyleName, "NormalText")];
                }
                else
                {
                    text += StyleSymbol[Array.IndexOf(StyleName, b.Tag.ToString())];
                }
                text += new TextRange(b.ContentStart, b.ContentEnd).Text;
                text += "\r\n";
            }
            File.WriteAllText(location, text, Encoding.UTF8);
        }

        private bool LoadText(string location)
        {
            FileStream fs;
            location = saveLocation + location + ".txt";
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
                        MainTextArea.Document.ContentStart,
                        MainTextArea.Document.ContentEnd
                        );
                    tr.Load(fs, DataFormats.Text);
                }
                FormatExisting();
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

        private void FindInitialise(bool startup)
        {
            if (!startup)
            {
                OperationTextArea.Visibility = Visibility.Visible;
            }
            OperationTextArea.Text = "";
        }

        private void FindText()
        {
            String text = OperationTextArea.Text;

            if (OperationTextArea.Visibility == Visibility.Hidden)
            {
                FindInitialise(false);
                Keyboard.Focus(OperationTextArea);
                return;
            }

            if (OperationTextArea.Text == "")
            {
                AddMessage(lang["nullOperation"]);
                return;
            }

            BEGIN:

            foreach (Block b in MainTextArea.Document.Blocks)
            {
                TextRange tr = new TextRange(b.ContentStart, b.ContentEnd);
                int index = 0;
                if (tr.Start.GetOffsetToPosition(findStart) > 0)
                {
                    index = tr.Start.GetOffsetToPosition(findStart);
                }
                if (index >= tr.Text.Length)
                {
                    goto NEXT;
                }
                index = tr.Text.IndexOf(text, index);
                if (index == -1)
                {
                    goto NEXT;
                }
                if (tr.Start.GetPositionAtOffset(index).GetOffsetToPosition(findStart) <= 0)
                {
                    MainTextArea.Focus();
                    MainTextArea.Selection.Select
                        (
                        tr.Start.GetPositionAtOffset(index),
                        tr.Start.GetPositionAtOffset(index + text.Length)
                        );
                    findStart = tr.Start.GetPositionAtOffset(index + 1);
                    AddMessage(lang["found"]);
                    return;
                }
            NEXT:
                ;
            }
            if (findStart != MainTextArea.Document.ContentStart)
            {
                findStart = MainTextArea.Document.ContentStart;
                goto BEGIN;
            }
            AddMessage(lang["notFound"]);
        }

        private void CharCount()
        {
            AddMessage
                (
                (mainTextRange.Text.Length - blockCount).ToString() + lang["chara"]
                );
        }

        private void SaveFile(bool back = false)
        {
            if (back)
            {
                SaveText(
                    TitleTextArea.Text + " " + DateTime.Now.Date.ToShortDateString().Replace("/", "-"),
                    back
                    );
            }
            else
            {
                SaveText(TitleTextArea.Text, back);
                SavePlan(TitleTextArea.Text);
            }
            AddMessage(lang["saved"]);
        }

        private void LoadFile()
        {
            if (LoadText(TitleTextArea.Text))
            {
                AddMessage(lang["loaded"]);
                BackupCurrent();
            }
            else
            {
                AddMessage(lang["loadFail"]);
            }
            if (LoadPlan(TitleTextArea.Text))
            {
                AddMessage(lang["planLoaded"]);
                BackupCurrent();
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

        private void BackupCurrent()
        {
            if (!File.Exists
                (string.Format("saves/back/{0} {1}.txt", TitleTextArea.Text, DateTime.Now.Date.ToShortDateString().Replace("/", "-"))
                ))
            {
                SaveFile(true);
                AddMessage(lang["autoBackupDone"]);
            }
        }

        private void ChangeParagraphStyle(Paragraph p, string key)
        {
            if (Array.IndexOf(StyleName, key) < 0)
            {
                key = "NormalText";
            }
            p.Style = (Style)FindResource(key);
            p.Tag = key;
        }

        private void ApplyStyle(KeyEventArgs e)
        {
            if (MainTextArea.IsFocused == false)
            {
                return;
            }
            Paragraph p = MainTextArea.CaretPosition.Paragraph;
            int styleNo = Array.IndexOf(StyleKey, e.Key);
            ListenToStyleChanges = false;
            if (styleNo > -1)
            {
                ChangeParagraphStyle(p, StyleName[styleNo]);
                e.Handled = true;
            }
            else
            {
                e.Handled = false;
            }
        }

        private void FormatExisting()
        {
            IEnumerator<Block> paras = MainTextArea.Document.Blocks.GetEnumerator();
            string currentText;
            Paragraph newPara;
            List<Block> newBlocks = new List<Block>();
            for (int i = 0; i < MainTextArea.Document.Blocks.Count; i++)
            {
                paras.MoveNext();
                if (paras.Current.Tag == null)
                {
                    currentText = new TextRange(paras.Current.ContentStart, paras.Current.ContentEnd).Text;
                    newPara = new Paragraph();
                    foreach (string symbol in StyleSymbol)
                    {
                        if (currentText.StartsWith(symbol))
                        {
                            currentText = currentText.Remove(0, symbol.Length);
                            newPara.Tag = StyleName[Array.IndexOf(StyleSymbol, symbol)];
                            break;
                        }
                    }
                    newPara.Inlines.Add(new Run(currentText));
                    ChangeParagraphStyle((Paragraph)newPara, newPara.Tag.ToString());
                }
                else
                {
                    newPara = (Paragraph)paras.Current;
                }
                newBlocks.Add(newPara);
            }
            MainTextArea.Document.Blocks.Clear();
            MainTextArea.Document.Blocks.AddRange(newBlocks);
        }

        private void SavePlan(string location)
        {
            IEnumerator<Block> paras = PlanTextArea.Document.Blocks.GetEnumerator();
            string toSave = "";
            location = planLocation + location + ".txt";
            for (int i = 0; i < PlanTextArea.Document.Blocks.Count; i++)
            {
                paras.MoveNext();
                toSave += new TextRange(paras.Current.ContentStart, paras.Current.ContentEnd).Text;
                toSave += "\r\n";
            }
            File.WriteAllText(location, toSave, Encoding.UTF8);
        }

        private bool LoadPlan(string location)
        {
            FileStream fs;
            location = planLocation + location + ".txt";
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
            }
            else
            {
                planCaretPosition = PlanTextArea.CaretPosition;
                PlanTextArea.Visibility = Visibility.Hidden;
                MainTextArea.Visibility = Visibility.Visible;
                MainTextArea.Focus();
                MainTextArea.CaretPosition = mainCaretPosition;
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
                    case Key.M: CharCount(); break;
                    case Key.F: FindText(); break;
                    case Key.W: ChangeWindowState(); break;
                    case Key.N: ShowTime(); break;
                    case Key.LWin: this.WindowState = WindowState.Minimized; break;
                    case Key.Tab: TogglePlan(); break;
                }
                e.Handled = true;
                //IsolateElements(true);
            }
        }
        
        private void MainTextArea_TextChanged(object sender, TextChangedEventArgs e)
        {
            EditStart();
            findStart = MainTextArea.Document.ContentStart;
        }

        private void OperationTextArea_TextChanged(object sender, TextChangedEventArgs e)
        {
            findStart = MainTextArea.Document.ContentStart;
        }

        private void WindowClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ShutProgram();
        }

        private void MainTextArea_MouseLeave(object sender, MouseEventArgs e)
        {
            ShowHandle();
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

        private void PlanTextArea_MouseLeave(object sender, MouseEventArgs e)
        {
            ShowHandle();
        }
    }
}
