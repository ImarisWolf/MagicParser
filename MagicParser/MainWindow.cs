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
            sw.Write(textBoxInput.Text);
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
                    textBoxInput.Text = sr.ReadToEnd();
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
                textBoxInput.Text = "";
            }
        }

        #endregion

        #region Events

        private void TextBoxInput_TextChanged(object sender, EventArgs e)
        {
            textBoxOutput.Text = textBoxInput.Text;
            if (changed == false) { changed = true; }
        }

        private void НовыйToolStripMenuItem_Click(object sender, EventArgs e)
        {
            New();
        }

        private void ОткрытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Open();
        }

        private void СохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void СохранитьКакToolStripMenuItem_Click(object sender, EventArgs e)
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

        #endregion

        private void ButtonGenerate_Click(object sender, EventArgs e)
        {
            CodeParser.text = textBoxInput.Text;
            ParseCodeResult result = CodeParser.ParseText();
            if (result.type == 2)
            {
                if (result.boo)
                {
                    MessageBox.Show(this, "Успешно сгенерировано", "Всё ок!", MessageBoxButtons.OK, MessageBoxIcon.Asterisk, MessageBoxDefaultButton.Button1);
                }
            } else if (result.type == 1)
            {
                if (result.boo == false)
                {
                    MessageBox.Show(this, result.error.Message, "Упс!", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                }
            } else
            {
                MessageBox.Show(this, "Произошла какая-то неведомая ошибка.", "Упс!", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
            }
        }
    }
}
