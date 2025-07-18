﻿using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BencodeNET.Torrents;
using BencodeNET.Parsing;

namespace FileRepairer
{
    public partial class Form1 : Form
    {
        private int bufferlen = 131072;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog()==DialogResult.OK)
            {
                textBox2.Text = openFileDialog2.FileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            String text = "";
            Task.Run(() =>
            {
                while (text != "-1")
                {
                    System.Threading.Thread.Sleep(10);
                    this.Invoke((Action)(() =>
                    {
                         this.Text = text;
                    }));
                }
            });
            Task.Run(() => {
                System.IO.FileStream smallFile = System.IO.File.Open(textBox1.Text, System.IO.FileMode.Open);
                System.IO.FileStream largeFile = System.IO.File.Open(textBox2.Text, System.IO.FileMode.Open);
                if (smallFile.Length != largeFile.Length)
                {
                    long offset = 0;
                    for (long i = 0; i < smallFile.Length; i++)
                    {
                        int b1 = smallFile.ReadByte();
                        int b2 = largeFile.ReadByte();
                        if (b1!=b2)
                        {
                            offset = i;
                            break;
                        }
                        text = i.ToString();
                    }
                    largeFile.Seek(offset + (largeFile.Length - smallFile.Length), System.IO.SeekOrigin.Begin);
                    smallFile.Seek(offset, System.IO.SeekOrigin.Begin);
                    byte[] buffer=new byte[bufferlen];
                    for (long i=smallFile.Length-offset; i>0;)
                    {
                        int rlen = smallFile.Read(buffer, 0, buffer.Length);
                        largeFile.Write(buffer,0,rlen);
                        i -= rlen;
                        text = $"{offset.ToString()} - {i.ToString()}";
                    }
                }
                smallFile.Close();
                largeFile.Close();
                MessageBox.Show("Fin");
                text = "-1";
             });
            
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog()== DialogResult.OK )
            {
                textBox3.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string tempFile = $"temp_{DateTime.Now.ToString("yyyyMMdd_HHmmss.fffffff")}.tmp";
            if (File.Exists(tempFile)) File.Delete(tempFile);
            string path = textBox3.Text;
            string[] s = textBox4.Text.Split(new string[] {"\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
            int skipbytes = (int)numericUpDown1.Value;
            Task.Run(() =>
            {
                for (int i = 0; i < s.Length; i++)
                {
                    string[] t = s[i].Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                    if (t.Length < 2) continue;
                    long len = long.Parse(t[0]);
                    string name = t[1];
                    string fname = Path.Combine(path, name);
                    if (File.Exists(fname))
                    {
                        FileInfo finfo = new FileInfo(fname);
                        if (len > finfo.Length && finfo.Length > skipbytes)
                        {
                            int bytecount = (int)(len - finfo.Length);
                            File.Copy(fname, tempFile);
                            FileStream fs0 = new FileStream(tempFile, FileMode.Open);
                            FileStream fs = new FileStream(fname, FileMode.Open);
                            fs0.Seek(skipbytes, SeekOrigin.Begin);
                            fs.Seek(skipbytes, SeekOrigin.Begin);
                            byte[] buffer = new byte[bufferlen];
                            for (long j = bytecount; j > 0;)
                            {
                                fs.Write(buffer, 0, (int)Math.Min(buffer.Length, j));
                                j -= buffer.Length;
                            }
                            for (long j = fs0.Length - skipbytes; j > 0;)
                            {
                                int rlen = fs0.Read(buffer, 0, buffer.Length);
                                fs.Write(buffer, 0, (int)Math.Min(rlen, j));
                                j -= rlen;
                            }
                            fs.Close();
                            fs0.Close();
                            File.Delete(tempFile);
                        }
                    }
                    this.Invoke((Action)(()=>{
                        Text = $"[{i.ToString()}/{s.Length}]{fname}";
                    }));
                }
                MessageBox.Show("Fin");
            });
            
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int len0 = textBox4.Text.Length;
            textBox4.Text = textBox4.Text.Replace(",\r\n    \"path\" : [\r\n     \"", "\t").Replace("\"\r\n    ]\r\n   },\r\n   {\r\n    \"length\" : ","\r\n");
            textBox4.Text = textBox4.Text.Replace("   {\r\n    \"length\" : ", "").Replace("\"\r\n    ]\r\n   }","");
            int h1 = textBox4.Text.IndexOf("\"files\" : [") + "\"files\" : [".Length;
            if (h1>10) textBox4.Text = textBox4.Text.Substring(h1);
            if (textBox4.Text.Contains("]")) textBox4.Text = textBox4.Text.Substring(0, textBox4.Text.IndexOf("]"));
            if (len0 == textBox4.Text.Length)
            {
                textBox4.Text = textBox4.Text.Replace(".zip", ".zip.!qB");
                textBox4.Text = textBox4.Text.Replace(".cbz", ".cbz.!qB");
                textBox4.Text = textBox4.Text.Replace(".cbr", ".cbr.!qB");
                textBox4.Text = textBox4.Text.Replace(".pdf", ".pdf.!qB");
                textBox4.Text = textBox4.Text.Replace(".epub", ".epub.!qB");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else e.Effect = DragDropEffects.None; 
        }
        public string dirMask = "";
        public int startpos, len;
        private void LoadTorrent(string pathTorr)
        {
            if (textBox3.Text.Contains("#"))
            {
                if (textBox3.Text.Length == 1)
                {
                    dirMask = "";
                }
                else {
                    string[] masks = textBox3.Text.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                    if (masks.Length >= 3)
                    {
                        dirMask = masks[0];
                        startpos = int.Parse(masks[1]);
                        len = int.Parse(masks[2]);
                    }
                }
            }
            if (dirMask.Length > 0)
            {
                FileInfo finfo = new FileInfo(pathTorr);
                textBox3.Text = dirMask.Replace("#", finfo.Name.Substring(startpos,len));
            }
            Torrent infoTorr = new BencodeParser(System.Text.Encoding.Default).Parse<Torrent>(pathTorr);
            StringBuilder sb = new StringBuilder();
            foreach(MultiFileInfo f in infoTorr.Files)
            {
                sb.AppendLine($"{f.FileSize.ToString()}\t{f.FileName}");
            }
            textBox4.Text = sb.ToString();
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string Path = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            LoadTorrent(Path);
        }
    }
}
