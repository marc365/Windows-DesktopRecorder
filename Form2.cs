#region MIT license
// 
// MIT license
//
// Copyright (c) 2017 Marc Williams
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
#endregion
using Microsoft.Win32;
using NAudio.Lame;
using NAudio.Wave;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace DesktopRecorder
{
    public partial class Form2 : Form
    {
        /// <summary>
        /// Initializer
        /// </summary>
        public Form2()
        {
            InitializeComponent();
            
            button1.Text = "Overwrite";
            button2.Text = "Append";
            button3.Text = "Cancel";

            label1.Text = string.Format("Overwrite the file.{0}Append recording at the end of the file{0}Cancel starting the recording", Environment.NewLine);
        }

        private void button_Click(object sender, EventArgs e)
        {
            Result = ((Button)sender).Text;
            Close();
        }

        public string Result { get; set;}
    }
}
