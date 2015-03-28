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

namespace Saigong // TODO: Add plan mode
{
    public enum StyleName
    {
        NormalText, TitleTile, LesserTitleText, MetaText
    }

    public partial class MainWindow : Window
    {
        delegate int intDelegate();

        static string saveLocation = "saves/";
        static string backLocation = "saves/back/";

        bool ListenToStyleChanges;

        int findIndexStart;

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
            Directory.CreateDirectory("saves/back/");
            FindInitialise(true);
            TitleTextArea.Focus();
            WindowTitle.Text = Lang.title;
            WindowClose.Text = Lang.batsu;
            HideHandle();
            ListenToStyleChanges = false;
            SetStatus(Lang.startupFinished);
        }

        private void EditStart()
        {
            StatusDisplay.Visibility = Visibility.Hidden;
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

        private void ToWindow()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.Style = (Style)App.Current.FindResource("WindowWin");
                WindowBorder.Visibility = Visibility.Visible;
                ShowHandle();
            }
            else
            {
                this.Style = (Style)App.Current.FindResource("WindowMax");
                WindowBorder.Visibility = Visibility.Hidden;
                HideHandle();
            }
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
            File.WriteAllText(location, text);
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
                return true;
            }
            else
            {
                return false;
            }
        }

        private void SetStatus(string text)
        {
            StatusDisplay.Visibility = Visibility.Visible;
            StatusDisplay.Text = text;
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
            findIndexStart = 0;
        }

        private void FindText() // TODO: Fix search
        {
            if (OperationTextArea.Visibility == Visibility.Hidden)
            {
                FindInitialise(false);
                Keyboard.Focus(OperationTextArea);
            }
            else
            {
                if (OperationTextArea.Text == "")
                {
                    SetStatus(Lang.nullOperation);
                }
                else
                {
                    int findIndexCurrent;
                    findIndexCurrent = mainTextRange.Text.IndexOf
                        (
                        OperationTextArea.Text,
                        findIndexStart
                        );
                    if (findIndexCurrent == -1)
                    {
                        if (findIndexStart == 0)
                        {
                            SetStatus(Lang.notFound);
                        }
                        else
                        {
                            findIndexStart = 0;
                            FindText();
                        }
                    }
                    else
                    {
                        int blockCountCurrent = 1;
                        int blockCountIndexCurrent = 0;
                        int blockCountIndex = 0;
                        blockCountIndex = mainTextRange.Text.IndexOf
                            (
                            "\r\n",
                            blockCountIndexCurrent
                            );
                        while (blockCountIndex < findIndexCurrent)
                        {
                            blockCountCurrent++;
                            blockCountIndexCurrent = blockCountIndex + 1;
                            blockCountIndex = mainTextRange.Text.IndexOf
                                (
                                "\r\n",
                                blockCountIndexCurrent
                                );
                        }
                        SetStatus(Lang.found);
                        MainTextArea.Selection.Select
                            (
                            MainTextArea.Document.ContentStart.GetPositionAtOffset
                                (
                                findIndexCurrent + (blockCountCurrent * 2)
                                ),
                            MainTextArea.Document.ContentStart.GetPositionAtOffset
                                (
                                findIndexCurrent + (blockCountCurrent * 2) + OperationTextArea.Text.Length
                                )
                            );
                        findIndexStart = findIndexCurrent + 1;
                    }
                }
            }
        }

        private void CharCount()
        {
            SetStatus
                (
                (mainTextRange.Text.Length - blockCount).ToString() + Lang.chara
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
            }
            SetStatus(Lang.saved);
        }

        private void LoadFile()
        {
            if (LoadText(TitleTextArea.Text))
            {
                SetStatus(Lang.loaded);
                BackupCurrent();
            }
            else
            {
                SetStatus(Lang.loadFail);
            }
        }

        private void ShutProgram()
        {
            SetStatus(Lang.shutdown);
            Application.Current.Shutdown();
        }

        private void ShowTime()
        {
            SetStatus(DateTime.Now.TimeOfDay.ToString(@"hh\:mm"));
        }

        private void BackupCurrent()
        {
            if (!File.Exists
                (string.Format("saves/back/{0} {1}.txt", TitleTextArea.Text, DateTime.Now.Date.ToShortDateString().Replace("/", "-"))
                ))
            {
                SaveFile(true);
                SetStatus(Lang.backupDone);
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
                        SetStatus(Lang.nullTitle);
                    }
                }
                switch (e.Key)
                {
                    case Key.LeftAlt: ListenToStyleChanges = true; break;
                    case Key.Q: ShutProgram(); break;
                    case Key.M: CharCount(); break;
                    //case Key.F: FindText(); break; YOU ARE NOT GOING TO FIND ANYTHING
                    case Key.W: ToWindow(); break;
                    case Key.N: ShowTime(); break;
                    case Key.LWin: this.WindowState = WindowState.Minimized; break;
                }
                e.Handled = true;
                //IsolateElements(true);
            }
        }

        private void MainTextArea_TextChanged(object sender, TextChangedEventArgs e)
        {
            EditStart();
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
            ;
        }
    }
}
