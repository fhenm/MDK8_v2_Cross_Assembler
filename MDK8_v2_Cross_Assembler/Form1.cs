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


namespace MDK8_v2_Cross_Assembler
{
    public partial class Form1 : Form
    {
        byte[] data;
        bool text1_change = false;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            data = Cross_Assy.Program.op_Cross_Asse(richTextBox1.Text);

            string str = "";
            for (int i = 0; i < data.Length; i++)
            {
                str += string.Format("{0:X2}", data[i]);
                if (i != 0 && i % 2 != 0 && i < data.Length - 1)
                {
                    str += " ";
                }
            }

            if (System.Text.Encoding.UTF8.GetString(data).ToUpper().Contains("ERROR"))
            {
                textBox3.BackColor = Color.Red;
                textBox3.Text = System.Text.Encoding.UTF8.GetString(data);
            }
            else
            {
                textBox3.BackColor = Color.White;
                textBox3.Text = "";
                textBox2.Text = str;
            }

            textBox4.Text = Cross_Assy.Program.jump_N;
            textBox5.Text = Cross_Assy.Program.jump_A;
            textBox6.Text = Cross_Assy.Program.BY_NUM;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (text1_change)
            {
                // メッセージボックスを表示
                DialogResult result = MessageBox.Show(
                   "編集中のデータが保存されていません! ファイルを保存しますか？", "保存",
                   MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result == DialogResult.Cancel) { return; }
                else if(result == DialogResult.No)
                {
                    DialogResult FQ = openFileDialog1.ShowDialog();
                    if (FQ == System.Windows.Forms.DialogResult.OK)
                    {
                        StreamReader sr = new StreamReader(openFileDialog1.FileName);
                        richTextBox1.Text = sr.ReadToEnd();
                        sr.Close();
                    }
                    return;
                }

                SaveFileDialog dialog = new SaveFileDialog()
                {
                    Filter = "テキストファイル|*.txt|すべてのファイル|*.*", // フィルタ
                    OverwritePrompt = true, // 上書きの警告
                    FileName = this.Text,   // 初期ファイル名 (タイトルバーから)
                };

                // ファイルを保存するためのダイアログを表示
                if (dialog.ShowDialog() != DialogResult.OK)
                { return; }

                // ファイルを書き込む
                using (StreamWriter sw = new StreamWriter(dialog.OpenFile()))
                {
                    sw.Write(richTextBox1.Text);
                }
            }

            DialogResult dr = openFileDialog1.ShowDialog();
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                StreamReader sr = new StreamReader(openFileDialog1.FileName);
                richTextBox1.Text = sr.ReadToEnd();
                sr.Close();

                Form1.ActiveForm.Text = openFileDialog1.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult dr = saveFileDialog1.ShowDialog();
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                StreamWriter sr = new StreamWriter(saveFileDialog1.OpenFile());
                sr.Write(richTextBox1.Text);
                sr.Close();
                text1_change = false;
                Form1.ActiveForm.Text = saveFileDialog1.FileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DialogResult dr = saveFileDialog2.ShowDialog();
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                FileStream sr = new FileStream(saveFileDialog2.FileName , System.IO.FileMode.Open , System.IO.FileAccess.Write);
                sr.SetLength(0);
                BinaryWriter bw = new BinaryWriter(sr);
                bw.Write(data);
                bw.Close();
                sr.Close();
            }
        }

        private void RichTextBox1_TextChanged(object sender, EventArgs e)
        {
            text1_change = true;
            DrawLineNumber();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (text1_change)
            {
                // メッセージボックスを表示
                DialogResult result = MessageBox.Show(
                   "ファイルを保存しますか？", "保存",
                   MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                // [キャンセル] なら終了処理中断，[いいえ] なら保存せず終了
                if (result == DialogResult.Cancel) { e.Cancel = true; return; }
                else if (result != DialogResult.Yes) { return; }

                SaveFileDialog dialog = new SaveFileDialog()
                {
                    Filter = "テキストファイル|*.txt|すべてのファイル|*.*", // フィルタ
                    OverwritePrompt = true, // 上書きの警告
                    FileName = this.Text,   // 初期ファイル名 (タイトルバーから)
                };

                // ファイルを保存するためのダイアログを表示
                if (dialog.ShowDialog() != DialogResult.OK)
                { e.Cancel = true; return; }

                // ファイルを書き込む
                using (StreamWriter sw = new StreamWriter(dialog.OpenFile()))
                {
                    sw.Write(richTextBox1.Text);
                }
            }
        }
        void DrawLineNumber()
        {
            int lineNum = 0;
            int height = richTextBox1.Size.Height;
            Graphics g = this.CreateGraphics();
            g.Clear(Color.White);

            int charIndex = richTextBox1.GetCharIndexFromPosition(new Point(0, 0));
            lineNum = richTextBox1.GetLineFromCharIndex(charIndex);

            while (true)
            {
                charIndex = richTextBox1.GetFirstCharIndexFromLine(lineNum);
                if (charIndex == -1)
                    break;
                Point pt = richTextBox1.GetPositionFromCharIndex(charIndex);
                Font f = new Font("MS UI Gothic", 12, GraphicsUnit.Pixel);
                g.DrawString((lineNum + 1).ToString(), f, Brushes.Blue, new PointF(0, pt.Y+5));
                lineNum++;

                if (height < pt.Y+20)
                break;
            }
            g.Dispose();
        }

        private void richTextBox1_VScroll(object sender, EventArgs e)
        {
            DrawLineNumber();
        }
    }
}
