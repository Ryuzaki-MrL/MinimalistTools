﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using GameAssetsManager.Properties;

namespace GameAssetsManager
{
    public partial class FormScriptView : Form
    {
        private Dictionary<string, string> scripts = new Dictionary<string, string>();
        private Dictionary<string, byte> mnemonics = new Dictionary<string, byte>();
        private BindingList<string> scrnames = new BindingList<string>();
        private TextContainer msgref = new TextContainer();
        private string fname = "";

        public FormScriptView()
        {
            InitializeComponent();
            listBoxScr.DataSource = scrnames;
            string[] s = Resources.Mnemonics.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            for (byte i = 0; i < s.Length; ++i)
                mnemonics[s[i]] = i;
            mnemonics["L"] = mnemonics["LITERAL"];
            mnemonics["V"] = mnemonics["VAR"];
            if (!Settings.Default.rootpath.Equals(String.Empty))
                msgref.Load(Settings.Default.rootpath + "messages.rzdb");
        }

        private void listBoxScr_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxScr.SelectedIndex >= 0)
                textBoxScr.Text = scripts[listBoxScr.SelectedItem.ToString()];
        }

        private void insertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InputBox input = new InputBox("Add Script");
            if (input.ShowDialog() == DialogResult.OK)
            {
                scripts[input.GetResult()] = "";
                scrnames.Add(input.GetResult());
            }
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listBoxScr.SelectedIndex >= 0)
                scrnames.RemoveAt(listBoxScr.SelectedIndex);
        }

        private void textBoxScr_TextChanged(object sender, EventArgs e)
        {
            if (listBoxScr.SelectedIndex >= 0)
                scripts[listBoxScr.SelectedItem.ToString()] = textBoxScr.Text;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                fname = openFileDialog1.FileName;
                scrnames.Clear();
                BinaryReader br = new BinaryReader(File.OpenRead(openFileDialog1.FileName));
                byte scrCount = br.ReadByte();
                for (int i = 0; i < scrCount; ++i)
                    scrnames.Add(br.ReadString());
                for (int i = 0; i < scrCount; ++i)
                    scripts[scrnames[i]] = br.ReadString();
                br.Close();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = fname;
            if (!saveFileDialog1.FileName.Equals(String.Empty) || saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                fname = saveFileDialog1.FileName;
                BinaryWriter bw = new BinaryWriter(File.Open(saveFileDialog1.FileName, FileMode.Create));
                bw.Write((Byte)scrnames.Count);
                foreach (string s in scrnames) bw.Write(s);
                foreach (string s in scrnames) bw.Write(scripts[s]);
                bw.Close();
                MessageBox.Show("OK");
            }
        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InputBox input = new InputBox("Rename Script", scrnames[listBoxScr.SelectedIndex]);
            if (input.ShowDialog() == DialogResult.OK)
            {
                scripts[input.GetResult()] = scripts[scrnames[listBoxScr.SelectedIndex]];
                scrnames[listBoxScr.SelectedIndex] = input.GetResult();
            }
        }

        private void compileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string destfile = "";
            if (!Settings.Default.rootpath.Equals(String.Empty))
                destfile = Settings.Default.rootpath + "scripts.rzdb";
            else if (saveFileDialog2.ShowDialog() == DialogResult.OK)
                destfile = saveFileDialog2.FileName;
            if (destfile.Equals(String.Empty))
                return;
            RZDBWriter bw = new RZDBWriter(File.Open(destfile, FileMode.Create));
            bw.WriteSize(listBoxScr.Items.Count);
            foreach (string sname in listBoxScr.Items)
            {
                List<string> tokens = new List<string>();
                Dictionary<string, int> labels = new Dictionary<string, int>();
                string[] lines = scripts[sname].Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                int tkcount = 0;
                foreach (string s in lines)
                {
                    if (s.StartsWith("_"))
                    {
                        labels[s] = tkcount;
                        continue;
                    }
                    int slen = tokens.Count;
                    tokens.AddRange(s.Split(' '));
                    tkcount += tokens.Count - slen;
                }
                bw.WriteSize(tokens.Count);
                foreach (string token in tokens)
                {
                    int entid = Array.IndexOf(FormEntityView.GetEntityNames(), token);
                    int sprid = Array.IndexOf(FormEntityView.GetSpriteNames(), token);
                    int msgid = msgref.entries.IndexOf(token);
                    sbyte literal = 0;
                    if (mnemonics.ContainsKey(token))
                        bw.Write(mnemonics[token]);
                    else if (SByte.TryParse(token, out literal))
                        bw.Write(literal);
                    else
                    {
                        if (entid != -1)
                            bw.Write((Byte)entid);
                        else if (sprid != -1)
                            bw.Write((Byte)sprid);
                        else if (msgid != -1)
                            bw.Write((Byte)msgid);
                        else if (labels.ContainsKey(token))
                            bw.Write((Byte)labels[token]);
                        else
                            bw.Write((Byte)0xFF);
                    }
                }
            }
            bw.Close();
            MessageBox.Show("OK");
        }
    }
}
