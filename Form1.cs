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
    public partial class Form1 : Form
    {
        private static WasapiLoopbackCapture mx = new WasapiLoopbackCapture();
        private static WaveBuffer sourceWaveBuffer;

        private static LameMP3FileWriter Mp3Writer;
        private static IntPtr mp3lib;
        private static string lame = "libmp3lame.dll";
        private static string libdir;
        private static int _bitrate = 256;

        private static int _rate = mx.WaveFormat.SampleRate;
        private static int _bits = 32;
        private static int _channels = mx.WaveFormat.Channels;
        private static int _mode = 1;
        private static string[] _modes = { "16-bit wav","32-bit float wav","Mp3@32 kbps","Mp3@64 kbps","Mp3@128 kbps","Mp3@256 kbps" };
        private static Stream stdout;

        private bool _REC = false;

        internal RegistryKey registry;
        private readonly string _regKey = "DesktopRecorder";
        private readonly string _regFile = "file";
        private readonly string _regMode = "mode";
        private readonly string DisplayMember = "Name";
        private readonly string ValueMember = "Id";
        private static SaveFileDialog dialog;
        private static DialogResult result;
        private FileMode _FILEMODE = FileMode.Create;

        /// <summary>
        /// Initializer
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            //set the callbacks
            mx.DataAvailable += new EventHandler<WaveInEventArgs>(SoundChannel_DataAvailable);
            mx.RecordingStopped += new EventHandler<StoppedEventArgs>(SoundChannel_RecordingStopped);

            this.BackColor = System.Drawing.Color.WhiteSmoke;

            //load the settings from the registry
            registry = Registry.CurrentUser.OpenSubKey(_regKey);
            if (registry != null)
            {
                textBox1.Text = registry.GetValue(_regFile, null).ToString();
                _mode = int.Parse(registry.GetValue(_regMode, _mode).ToString());
                ModeSwap();
            }

            //recording modes
            comboBox.Text = _modes[_mode];
            comboBox.DisplayMember = DisplayMember;
            comboBox.ValueMember = ValueMember;
            int m = 0;
            foreach (string mode in _modes)
            {
                comboBox.Items.Add(new Item(mode, m));
                m++;
            }

            //extract embedded resource to the Temp folder and load the mp3 library
            try
            {
                libdir = Path.GetTempPath();
                if (!Directory.Exists(libdir))
                {
                    Directory.CreateDirectory(libdir);
                }
            }
            catch
            { }
            libdir = Path.Combine(libdir, lame);
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DesktopRecorder.libmp3lame.dll"))
            {
                try
                {
                    using (Stream outFile = File.Create(libdir))
                    {
                        const int bufs = 4096;
                        byte[] buf = new byte[bufs];
                        while (true)
                        {
                            int read = stream.Read(buf, 0, bufs);
                            if (read < 1)
                                break;
                            outFile.Write(buf, 0, read);
                        }
                    }
                }
                catch
                { }
            }
            mp3lib = NativeMethods.LoadLibrary(libdir);
            if (mp3lib == IntPtr.Zero)
            {
                result = MessageBox.Show("Cannot load Lame Mp3 Library (libmp3lame.dll)", "Error", MessageBoxButtons.OK);
            }
        }

        /// <summary>
        /// Change modes
        /// </summary>
        private void ModeSwap()
        {
            switch (_mode)
            {
                case 0:
                    _bits = 16;
                    break;
                case 1:
                    _bits = 32;
                    break;
                case 2:
                    _bits = 16;
                    _bitrate = 32;
                    break;
                case 3:
                    _bits = 16;
                    _bitrate = 64;
                    break;
                case 4:
                    _bits = 16;
                    _bitrate = 128;
                    break;
                case 5:
                    _bits = 16;
                    _bitrate = 256;
                    break;
                default:
                    break;
            }

            if (_mode<2)
            {
                textBox1.Text = textBox1.Text.Replace(".mp3", ".wav");
            }
            else
            {
                textBox1.Text = textBox1.Text.Replace(".wav", ".mp3");
            }
        }

        /// <summary>
        /// Record
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            // Toggle Stop
            if (_REC)
            {
                mx.StopRecording();

                Mp3Writer = null;

                stdout.Close();

                if (_mode < 1)
                {
                    // set the time duration in the Wav header now that we're complete
                    stdout = File.Open(textBox1.Text, FileMode.Open);
                    stdout.Position = 4;
                    stdout.Write(Encoding.ASCII.GetBytes((stdout.Length - 8).ToString()), 0, 4);
                    stdout.Position = 40;
                    stdout.Write(Encoding.ASCII.GetBytes((stdout.Length - 44).ToString()), 0, 4);
                    stdout.Close();
                }

                this.BackColor = System.Drawing.Color.WhiteSmoke;

                // Reset to opposite
                button1.Text = "Record";
                // Not recording
                _REC = false;
            }
            // Toggle Start
            else
            {
                // save the settings in the registry
                registry = Registry.CurrentUser.CreateSubKey(_regKey);
                registry.SetValue(_regFile, textBox1.Text);
                registry.SetValue(_regMode, _mode);
                registry.Close();

                if (textBox1.Text == string.Empty)
                {
                    result = MessageBox.Show("File not specified", "Error", MessageBoxButtons.OK);
                    return;
                }

                if (File.Exists(textBox1.Text))
                {
                    result = MessageBox.Show(string.Format("Yes to Overwrite the file.{0}No to carry on recording at the end of the file{0}Cancel to not start recording", Environment.NewLine), "Overwrite the existing file?", MessageBoxButtons.YesNoCancel);

                    if (result.ToString() == "Yes")
                    {
                        _FILEMODE = FileMode.Create;
                    }
                    else if (result.ToString() == "No")
                    {
                        _FILEMODE = FileMode.Append;
                    }
                    else if (result.ToString() == "Cancel")
                    {
                        return;
                    }
                }
                try
                {
                    stdout = File.Open(textBox1.Text, _FILEMODE);
                }
                catch(Exception exc)
                {
                    result = MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK);
                    return;
                }

                //Mp3 encoding with libmp3lame.dll
                if (_mode>1)
                {
                    if (mp3lib == IntPtr.Zero)
                    {
                        result = MessageBox.Show("Cannot load Lame Mp3 Library (libmp3lame.dll)", "Error", MessageBoxButtons.OK);
                        return;
                    }

                    try
                    {
                        Mp3Writer = new LameMP3FileWriter(stdout, new WaveFormat(_rate, _bits, _channels), _bitrate);
                    }
                    catch (ArgumentException exc)
                    {
                        result = MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK);
                        stdout.Close();

                        return;
                    }
                }
                else if (_FILEMODE == FileMode.Create)
                {
                    try
                    {
                        WriteWavHeader(stdout, _bits == 32 ? true : false, (ushort)_channels, (ushort)_bits, _rate, 0);
                    }
                    catch (Exception exc)
                    {
                        result = MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK);
                        stdout.Close();
                        return;
                    }
                }

                try
                {
                    mx.StartRecording();
                }
                catch (Exception exc)
                {
                    result = MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK);
                    stdout.Close();
                    return;
                }

                this.BackColor = System.Drawing.Color.DarkRed;

                button1.Text = "Stop";
                _REC = true;
            }
        }

        /// <summary>
        /// File selector
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            dialog = new SaveFileDialog()
            {
                Title = "Save As",
                Filter = _mode == 1 ? "wav files (*.wav)|*.wav|All files (*.*)|*.*" : "mp3 files (*.mp3)|*.mp3|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = dialog.FileName;
            }
        }

        /// <summary>
        /// Quit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }

        /// <summary>
        /// Change mode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _mode = (comboBox.SelectedItem as Item).Id;
            ModeSwap();
        }

        /// <summary>
        /// Recording CallBack
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void SoundChannel_DataAvailable(object sender, WaveInEventArgs e)
        {
            sourceWaveBuffer = new WaveBuffer(e.Buffer);

            if (_mode == 1) //32-bit float
            {
                stdout.Write(sourceWaveBuffer.ByteBuffer, 0, e.BytesRecorded);
            }
            else
            {
                byte[] to16 = new byte[e.BytesRecorded / 2];
                int destOffset = 0;

                WaveBuffer destWaveBuffer = new WaveBuffer(to16);
                int sourceSamples = e.BytesRecorded / 4;

                for (int sample = 0; sample < sourceSamples; sample++)
                {
                    float sample32 = sourceWaveBuffer.FloatBuffer[sample];
                    destWaveBuffer.ShortBuffer[destOffset++] = (short)(sample32 * 32767);
                }

                if (_mode == 0) // 16-bit Wav
                {
                    stdout.Write(to16, 0, destOffset * 2);
                }
                else if (Mp3Writer != null)
                {
                    Mp3Writer.Write(to16, 0, destOffset * 2);
                }
            }
        }

        /// <summary>
        /// Stopped Recording
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void SoundChannel_RecordingStopped(object sender, StoppedEventArgs e)
        {
            //if (mx != null)
            //{
            //    mx.Dispose();
            //}
            //Console.Error.WriteLine(e.Exception);
            //System.Environment.Exit(0);
        }

        /// <summary>
        /// Called when quiting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            NativeMethods.FreeLibrary(mp3lib);
        }

        /// <summary>
        /// Creates a Wav file header
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="isFloatingPoint"></param>
        /// <param name="channelCount"></param>
        /// <param name="bitDepth"></param>
        /// <param name="sampleRate"></param>
        /// <param name="totalSampleCount"></param>
        private void WriteWavHeader(Stream stream, bool isFloatingPoint, ushort channelCount, ushort bitDepth, int sampleRate, int totalSampleCount)
        {
            stream.Position = 0;

            // RIFF header.
            // Chunk ID.
            stream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);

            // Chunk size.
            stream.Write(BitConverter.GetBytes(((bitDepth / 8) * totalSampleCount) + 36), 0, 4);

            // Format.
            stream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);



            // Sub-chunk 1.
            // Sub-chunk 1 ID.
            stream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);

            // Sub-chunk 1 size.
            stream.Write(BitConverter.GetBytes(16), 0, 4);

            // Audio format (floating point (3) or PCM (1)). Any other format indicates compression.
            stream.Write(BitConverter.GetBytes((ushort)(isFloatingPoint ? 3 : 1)), 0, 2);

            // Channels.
            stream.Write(BitConverter.GetBytes(channelCount), 0, 2);

            // Sample rate.
            stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);

            // Bytes rate.
            stream.Write(BitConverter.GetBytes(sampleRate * channelCount * (bitDepth / 8)), 0, 4);

            // Block align.
            stream.Write(BitConverter.GetBytes((ushort)channelCount * (bitDepth / 8)), 0, 2);

            // Bits per sample.
            stream.Write(BitConverter.GetBytes(bitDepth), 0, 2);



            // Sub-chunk 2.
            // Sub-chunk 2 ID.
            stream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);

            // Sub-chunk 2 size.
            stream.Write(BitConverter.GetBytes((bitDepth / 8) * totalSampleCount), 0, 4);
        }

        /// <summary>
        /// Link to authors website
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void label1_Click(object sender, EventArgs e)
        {
            result = MessageBox.Show("https://github.com/marc365/Windows-DesktopRecorder", "For updates visit:", MessageBoxButtons.OK);
        }

        /// <summary>
        /// Magic functions into the Windows Gui
        /// enables draging the boarderless window and creates a drop shadow effect
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST)
                m.Result = (IntPtr)(HT_CAPTION);
        }
        private const int WM_NCHITTEST = 0x84;
        private const int HT_CLIENT = 0x1;
        private const int HT_CAPTION = 0x2;
        private const int CS_DROPSHADOW = 0x00020000;
        protected override CreateParams CreateParams
        {
            get
            {
                // add the drop shadow flag for automatically drawing
                // a drop shadow around the form
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }
    }
}
