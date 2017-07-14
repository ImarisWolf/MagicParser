using MagicParser.CodeParsing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MagicParser
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Private variables

        private string fileName = "";
        private bool changed = false;

        private Size startSize;
        private Size leftStartSize;
        private Size rightStartSize;
        private int rightStartPos;

        private Font font;

        private bool selectChanged = true;

        #endregion

        #region Private methods

        private bool CheckChanges() // в случае наличия изменений предлагает сохранить файл и возвращает true; если была нажата "отмена", возвращает false
        {
            if (changed)
            {
                DialogResult answer = MessageBox.Show(this, "Желаете ли вы сохранить изменения?", "Сохранение", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

                switch (answer)
                {
                    case DialogResult.Yes:
                        Save();
                        return true;
                    case DialogResult.Cancel:
                        return false;
                }
            }
            return true;
        }
        
        private void Write()
        {
            StreamWriter sw = new StreamWriter(fileName, false);
            sw.Write(InputTextBox.Text);
            changed = false;
            sw.Close();
        }

        private void SaveAs()
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                fileName = saveFileDialog.FileName;
                Write();
            }
        }

        private void Save()
        {
            if (fileName == "")
            {
                SaveAs();
            }
            else
            {
                Write();
            }
        }

        private void Open()
        {
            if (CheckChanges())
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    fileName = openFileDialog.FileName;
                    StreamReader sr = new StreamReader(fileName);
                    InputTextBox.Text = sr.ReadToEnd();
                    changed = false;
                    sr.Close();
                }
            }
        }

        private void New()
        {
            if (CheckChanges())
            {
                fileName = "";
                InputTextBox.Text = "";
            }
        }

        #endregion


        #region Events

        private void InputTextBox_TextChanged(object sender, EventArgs e)
        {
            if (selectChanged)
            {
                if (changed == false) { changed = true; }
                Analizer a = new Analizer(InputTextBox.Text);
                selectChanged = false;
                string parsedText = a.Parse();
                OutputTextBox.Text = parsedText;
                if (a.errorDescription != null)
                {
                    OutputTextBox.ForeColor = Color.Gray;
                    ErrorLogTextBox.Text = a.errorDescription + "\r\nDouble click to set position onto the error.";
                }
                else
                {
                    OutputTextBox.ForeColor = Color.Black;
                    ErrorLogTextBox.Text = "";
                }
                a.Paint(InputTextBox);
                selectChanged = true;
            }
            
        }

        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            New();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Open();
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!CheckChanges())
            {
                e.Cancel = true;
            }
        }

        private void MainWindow_Resize(object sender, EventArgs e)
        {
            InputTextBox.Height = leftStartSize.Height + (Size.Height - startSize.Height);
            InputTextBox.Width = leftStartSize.Width + (Size.Width - startSize.Width) / 2;
            OutputTextBox.Left = rightStartPos + (Size.Width - startSize.Width) / 2;
            OutputTextBox.Width = rightStartSize.Width + (Size.Width - startSize.Width) / 2;
            OutputTextBox.Height = rightStartSize.Height + (Size.Height - startSize.Height);
        }

        private void MainWindow_Shown(object sender, EventArgs e)
        {
            startSize = Size;
            leftStartSize = InputTextBox.Size;
            rightStartSize = OutputTextBox.Size;
            rightStartPos = OutputTextBox.Left;
            font = InputTextBox.Font;
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.KeyCode == Keys.Enter)
            //{
            //    int startPos = InputTextBox.SelectionStart;
            //    string trimmed = InputTextBox.Text.Substring(0, startPos).TrimEnd();
            //    string token = "";

            //    for (int i = trimmed.Length - 1; i >= 0; i--)
            //    {
            //        if (!Char.IsWhiteSpace(trimmed[i]))
            //        {
            //            token = token.Insert(0, trimmed[i].ToString());
            //        }
            //        else if (token != "") break;
            //    }
            //    if (token.ToLower() == Analizer.declarationBeginToken.ToLower())
            //    {
            //        InputTextBox.Text = InputTextBox.Text + "\r\n    \r\n" + Analizer.declarationEndToken;
            //        InputTextBox.SelectionStart = startPos + 6 + Analizer.declarationEndToken.Length;
            //        SendKeys.Send("{BACKSPACE}");
            //        InputTextBox.SelectionStart = startPos + 5;
            //    }
            //}
        }

        private void ErrorLogTextBox_DoubleClick(object sender, EventArgs e)
        {
            if (Analizer.tokenizerLastErrorPos != 0)
            {
                InputTextBox.Focus();
                InputTextBox.Select(Analizer.tokenizerLastErrorPos, 1);
            }
        }


        #endregion

        private void CopyRightPartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Analizer.tokenizerLastErrorPos == 0) Clipboard.SetText(OutputTextBox.Text);
            else MessageBox.Show("There are errors in the code. Fix them first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
        }


        private void InputTextBox_SelectionChanged(object sender, EventArgs e)
        {
            if (selectChanged)
            {
                selectChanged = false;
                int view = InputTextBox.SelectionStart - 1;
                int initLength = InputTextBox.SelectionLength;
                InputTextBox.SelectAll();
                InputTextBox.SelectionBackColor = Color.White;
                InputTextBox.BackColor = Color.White;

                Color col = Color.LightGray;

                string text = InputTextBox.Text;

                int singleQuote = -1; //Позиция текущей открывающей одинарной кавычки. Если -1, то открывающей кавычки нет.
                int doubleQuote = -1; //Позиция текущей открывающей двойной   кавычки. Если -1, то открывающей кавычки нет.
                int openBrackets = 0; //Количество открытых скобок.
                int highlightedBracket = 0; //Номер подсвеченной открытой скобки (на которой стоит указатель).
                Stack<int> bracketsPos = new Stack<int>(); //Стек позиций текущих открытых скобок.

                string highlight = null; //Указатель на то, какой символ следует подсветить.

                for (int i = 0; i < text.Length; i++)
                {
                    //Если одинарная кавычка...
                    if (text[i] == '\'')
                    {
                        //...и двойная кавычка не открыта...
                        if (doubleQuote == -1)
                        {
                            //...и одинарная кавычка не открыта...
                            if (singleQuote == -1)
                            {
                                //...то это открывающая одинарная кавычка. Запоминаем номер.
                                singleQuote = i;
                                //Если курсор на открывающей одинарной кавычке...
                                if (view == i)
                                {
                                    //То следует искать следующую одинарную кавычку.
                                    highlight = "singleQuote";
                                    //Подсветим текущую кавычку.
                                    InputTextBox.Select(i, 1);
                                    InputTextBox.SelectionBackColor = col;
                                }
                                //...иначе ничего не происходит.
                            }
                            //...но одинарная кавычка уже открыта...
                            else
                            {
                                //...то это закрывающая одинарная кавычка.
                                //Если подсвеченная одиночная кавычка уже была...
                                if (highlight == "singleQuote")
                                {
                                    //...подсвечиваем вторую и уходим отсюда.
                                    InputTextBox.Select(i, 1);
                                    InputTextBox.SelectionBackColor = col;
                                    break;
                                }
                                //...если её не было, но нужно подсветить эту...
                                else if (view == i)
                                {
                                    //...вспоминаем номер предыдущей одинарной кавычки и подсвечиваем обе.
                                    InputTextBox.Select(singleQuote, 1);
                                    InputTextBox.SelectionBackColor = col;
                                    InputTextBox.Select(i, 1);
                                    InputTextBox.SelectionBackColor = col;
                                    break;
                                }
                                //...иначе просто закрываем одинарную кавычку.
                                singleQuote = -1;
                            }
                        }
                        //...а если двойная кавычка открыта, то одинарная кавычка - это текст.
                        else continue;
                    }
                    //Если двойная кавычка...
                    else if (text[i] == '"')
                    {
                        //...и одинарная кавычка уже открыта...
                        if (singleQuote >= 0)
                        {
                            //...и двойная кавычка не открыта...
                            if (doubleQuote == -1)
                            {
                                //...то это открывающая двойная кавычка. Запоминаем номер.
                                doubleQuote = i;
                                //Если курсор на открывающей двойной кавычке...
                                if (view == i)
                                {
                                    //То следует искать следующую двойную кавычку.
                                    highlight = "doubleQuote";
                                    //Подсветим текущую кавычку.
                                    InputTextBox.Select(i, 1);
                                    InputTextBox.SelectionBackColor = col;
                                }
                                //...иначе ничего не происходит.
                            }
                            //...но двойная кавычка уже открыта...
                            else
                            {
                                //...то это закрывающая двойная кавычка.
                                //Если подсвеченная двойная кавычка уже была...
                                if (highlight == "doubleQuote")
                                {
                                    //...подсвечиваем вторую и уходим отсюда.
                                    InputTextBox.Select(i, 1);
                                    InputTextBox.SelectionBackColor = col;
                                    break;
                                }
                                //...если её не было, но нужно подсветить эту...
                                else if (view == i)
                                {
                                    //...вспоминаем номер предыдущей одинарной кавычки и подсвечиваем обе.
                                    InputTextBox.Select(doubleQuote, 1);
                                    InputTextBox.SelectionBackColor = col;
                                    InputTextBox.Select(i, 1);
                                    InputTextBox.SelectionBackColor = col;
                                    break;
                                }
                                //...иначе просто закрываем двойную кавычку.
                                doubleQuote = -1;
                            }
                        }
                        //...а если одинарная кавычка не открыта, то двойной быть просто не может.
                        else break;
                    }
                    //Если открывающая скобка...
                    else if (text[i] == '(')
                    {
                        //...если одиночная кавычка открыта...
                        if (singleQuote >= 0)
                        {
                            //... а двойная кавычка - не открыта...
                            if (doubleQuote == -1)
                            {
                                //...открываем скобку. Стало на одну открытую скобку больше.
                                openBrackets++;
                                //Записываем положение открытой скобки в стек.
                                bracketsPos.Push(i);
                                //Если курсор на открывающей скобке...
                                if (view == i)
                                {
                                    //То следует искать соответствующую ей закрывающую скобку (после закрытия (!) которой openBrackets станет равен highlightedBracket - 1).
                                    highlight = "openBracket";
                                    highlightedBracket = openBrackets;
                                    //Подсветим текущую скобку.
                                    InputTextBox.Select(i, 1);
                                    InputTextBox.SelectionBackColor = col;
                                }
                                //...иначе ничего не происходит.
                            }
                            //...а если двойная кавычка открыта, то скобка - это текст.
                            else continue;
                        }
                        //...иначе скобки быть просто не может.
                        else break;
                    }
                    //Если закрывающая скобка...
                    else if (text[i] == ')')
                    {
                        //...если одиночная кавычка открыта...
                        if (singleQuote >= 0)
                        {
                            //... а двойная кавычка - не открыта...
                            if (doubleQuote == -1)
                            {
                                //...и если уже была подсвечена открывающая скобка, и текущий номер открытой скобки - номер подсвеченной скобки...
                                if (highlight == "openBracket" && highlightedBracket == openBrackets)
                                {
                                    //...то подсвечиваем текущую закрывающую скобку (она как раз намеривается закрыть подсвеченную открытую скобку)
                                    InputTextBox.Select(i, 1);
                                    InputTextBox.SelectionBackColor = col;
                                    break;
                                }
                                //...если ещё не было подсвеченной скобки, но указатель как раз на текущей закрывающей скобке...
                                else if (view == i)
                                {
                                    //...то необходимо подсветить соответствующую ей открывающую скобку. Также подсвечиваем текущую закрывающую скобку.
                                    InputTextBox.Select(bracketsPos.Pop(), 1);
                                    InputTextBox.SelectionBackColor = col;
                                    InputTextBox.Select(i, 1);
                                    InputTextBox.SelectionBackColor = col;
                                    break;
                                }
                                //...иначе просто закрываем скобку (удаляем из стека более неактуальное положение последней открытой скобки и уменьшаем количество открытых скобок).
                                bracketsPos.Pop();
                                openBrackets--;
                            }
                            //...а если двойная кавычка открыта, то скобка - это текст.
                            else continue;
                        }
                        //...иначе скобки быть просто не может.
                        else break;
                    }
                    //Иначе это просто какой-то символ, который никак не нужно выделять или запоминать. Пропускаем.
                }

                InputTextBox.SelectionStart = view + 1;
                InputTextBox.SelectionLength = initLength;
                InputTextBox.SelectionBackColor = Color.White;
                selectChanged = true;
            }
        }
    }
}
