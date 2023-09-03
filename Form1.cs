#region MIT license
// 
// MIT license
//
// Copyright (c) 2017-2023 Marc Williams
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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;

namespace DesktopRecorder
{
    public partial class Form1 : Form
    {    
        private static WasapiLoopbackCapture mx = new WasapiLoopbackCapture();
        private static WaveBuffer sourceWaveBuffer;
        private static WaveBuffer destWaveBuffer;

        private static LameMP3FileWriter Mp3Writer;
        private static IntPtr mp3lib;
        private readonly string lame = "libmp3lame.dll";
        private static string libdir;
        private static int _bitrate = 256;

        private static int _rate = mx.WaveFormat.SampleRate;
        private static int _bits = 32;
        private static int _channels = mx.WaveFormat.Channels;
        private static int _mode = 1;
        private readonly string[] _modes = { "16-bit wav","32-bit wav","32 kbps mp3","64 kbps mp3","128 kbps mp3","256 kbps mp3","320 kbps mp3" };
        private static int _output = 0;
        private readonly string[] _outputs = { "File", "Stream" };
        private static TcpClient tcp;
        private static Stream stdout;
        private FileMode _FILEMODE = FileMode.Create;
        private static string filename;

        private static bool _REC = false;
        private static bool restart;
        private static bool exit;

        private static Stopwatch Timer;
        private readonly string RECORD = "Record";
        private readonly string STOP = "Stop";
        private static string VERB;

        private RegistryKey registry;
        private readonly string _regKey = "DesktopRecorder";
        private readonly string _regOutput = "output";
        private readonly string _regFile = "file";
        private readonly string _regStream = "stream";
        private readonly string _regMode = "mode";
        private readonly string _regDate = "date";
        private readonly string _regOverwrite = "overwrite";
        private readonly string _regAppend = "append";
        private readonly string _regVerb = "verb";
        private readonly string DisplayMember = "Name";
        private readonly string ValueMember = "Id";

        private static SaveFileDialog dialog;

        private static MethodInvoker update;
        private static MethodInvoker reset;
        
        /// <summary>
        /// Initializer
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            update = UpdateTimer;
            reset = ResetView;            
        }

        /// <summary>
        /// Always validate certificates
        /// </summary>
        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        /// <summary>
        /// Called when starting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            //set the callbacks
            mx.DataAvailable += new EventHandler<WaveInEventArgs>(SoundChannel_DataAvailable);
            mx.RecordingStopped += new EventHandler<StoppedEventArgs>(SoundChannel_RecordingStopped);

            //load the settings from the registry
            registry = Registry.CurrentUser.OpenSubKey(_regKey);
            if (registry != null)
            {
                _output = (int)registry.GetValue(_regOutput, _output);                
                _mode = (int)registry.GetValue(_regMode, _mode);
                checkBox1.Checked = bool.Parse((string)registry.GetValue(_regDate, "False"));
                checkBox2.Checked = bool.Parse((string)registry.GetValue(_regOverwrite, "True"));
                checkBox3.Checked = bool.Parse((string)registry.GetValue(_regAppend, "False"));
                VERB = (string)registry.GetValue(_regVerb, "PUT");
                switch (VERB)
                {
                    case "GET":
                        radioButton1.Checked = true;
                        break;
                    case "POST":
                        radioButton2.Checked = true;
                        break;
                    case "PUT":
                        radioButton3.Checked = true;
                        break;
                }
                OutputSwap(); //close registry
                ModeSwap();
            }

            //set button text
            button1.Text = RECORD;

            //set timer
            Timer = new Stopwatch();
            label1.Text = Timer.Elapsed.ToString();

            //output modes
            comboBox1.Text = _outputs[_output];
            comboBox1.DisplayMember = DisplayMember;
            comboBox1.ValueMember = ValueMember;
            int m = 0;
            for (; m < _outputs.Length; m++)
            {
                comboBox1.Items.Add(new Item(_outputs[m], m));
            }

            //recording modes
            comboBox.Text = _modes[_mode];
            comboBox.DisplayMember = DisplayMember;
            comboBox.ValueMember = ValueMember;
            for (m = 0; m < _modes.Length; m++)
            {
                comboBox.Items.Add(new Item(_modes[m], m));
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
        }

        /// <summary>
        /// Called when quiting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_REC)
            {
                exit = true;
                mx.StopRecording();
            }
            else
            {
                NativeMethods.FreeLibrary(mp3lib);
            }
        }

        /// <summary>
        /// Change output
        /// </summary>
        private void OutputSwap()
        {
            if (_REC) return;

            registry = Registry.CurrentUser.OpenSubKey(_regKey);
            switch (_output)
            {
                case 0:
                    textBox1.Text = (string)registry.GetValue(_regFile, null);
                    textBox1.Width = 126;
                    button2.Show();
                    checkBox1.Show();
                    checkBox2.Show();
                    checkBox3.Show();
                    radioButton1.Hide();
                    radioButton2.Hide();
                    radioButton3.Hide();
                    break;
                case 1:
                    textBox1.Text = (string)registry.GetValue(_regStream, "https://");
                    textBox1.Width = 150;
                    button2.Hide();
                    checkBox1.Hide();
                    checkBox2.Hide();
                    checkBox3.Hide();
                    radioButton1.Show();
                    radioButton2.Show();
                    radioButton3.Show();
                    break;
            }
            registry.Close();
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
                case 6:
                    _bits = 16;
                    _bitrate = 320;
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
        /// Show the recording time
        /// </summary>
        private void UpdateTimer()
        {
            label1.Text = Timer.Elapsed.ToString();
        }

        /// <summary>
        /// Called after Stopped Recording
        /// </summary>
        private void ResetView()
        {
            Timer.Stop();
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            button1.FlatAppearance.BorderColor = System.Drawing.Color.WhiteSmoke;
            comboBox1.Enabled = true;
            textBox1.Enabled = true;
            comboBox.Enabled = true;
            button1.Text = RECORD;
        }

        /// <summary>
        /// Record
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            //toggle stop
            if (_REC)
            {
                mx.StopRecording();
                return;
            }

            #region save registry

            registry = Registry.CurrentUser.CreateSubKey(_regKey);
            registry.SetValue(_regOutput, _output);
            switch (_output)
            {
                case 0:
                    registry.SetValue(_regFile, textBox1.Text);
                    break;
                case 1:
                    registry.SetValue(_regStream, textBox1.Text);
                    break;
            }
            registry.SetValue(_regMode, _mode);
            registry.SetValue(_regDate, checkBox1.Checked);
            registry.SetValue(_regOverwrite, checkBox2.Checked);
            registry.SetValue(_regAppend, checkBox3.Checked);
            registry.SetValue(_regVerb, VERB);
            registry.Close();

            #endregion

            if (string.IsNullOrEmpty(textBox1.Text))
            {
                button2_Click(sender, e);
            }

            if (_output == 0) //file
            {
                filename = textBox1.Text;

                if (checkBox1.Checked)
                {
                    filename = filename.Substring(0, filename.Length - 4) + DateTime.Now.ToString(" yyyy-MM-dd") + filename.Substring(filename.Length - 4, 4);
                }

                if (File.Exists(filename))
                {
                    if (checkBox3.Checked)
                    {
                        _FILEMODE = FileMode.Append;
                    }
                    else
                    {
                        _FILEMODE = FileMode.Create;

                        if (!checkBox2.Checked)
                        {
                            FileInfo file = new FileInfo(filename);

                            filename = file.Name.Substring(0, file.Name.Length - file.Extension.Length);
                            filename = Path.Combine(file.DirectoryName, filename + "." + Directory.GetFiles(file.DirectoryName, filename + ".*").Length + file.Extension);
                        }
                    }
                }

                try
                {
                    stdout = File.Open(filename, _FILEMODE);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "File Error", MessageBoxButtons.OK);
                    return;
                }
            }
            else if (_output == 1) //stream
            {
                try
                {
                    Uri uri = new Uri(textBox1.Text);
                    tcp = new TcpClient();
                    IPAddress[] a = Dns.GetHostAddresses(uri.DnsSafeHost);

                    tcp.Connect(a[0], uri.Port);

                    if (uri.Scheme == "https")
                    {
                        SslStream ssl = new SslStream(tcp.GetStream(), true, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);

                        ssl.AuthenticateAsClient(uri.DnsSafeHost, null, System.Security.Authentication.SslProtocols.Tls12, false);

                        stdout = ssl;
                    }
                    else
                    {
                        stdout = tcp.GetStream();
                    }

                    byte[] header = Encoding.ASCII.GetBytes(VERB + " " + uri.PathAndQuery + " HTTP/1.1\r\n"

                    + (uri.UserInfo == null ? string.Empty : "Authorization: Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(uri.UserInfo)) + "\r\n")
                    + "Host: " + uri.DnsSafeHost + "\r\n"
                    + "UserAgent: desktoprecorder/1.6\r\n\r\n");

                    stdout.Write(header, 0, header.Length); 
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Stream Error", MessageBoxButtons.OK);
                    return;
                }
            }

            //mp3 encoding with libmp3lame.dll
            if (_mode > 1)
            {
                try
                {
                    Mp3Writer = new LameMP3FileWriter(stdout, new WaveFormat(_rate, _bits, _channels), _bitrate);
                }
                catch (ArgumentException exc)
                {
                    MessageBox.Show(exc.Message, "Mp3 Error", MessageBoxButtons.OK);
                    stdout.Close();
                    return;
                }
            }
            else if (_FILEMODE == FileMode.Create && _output == 0)
            {
                try
                {
                    WriteWavHeader(stdout, _bits == 32 ? true : false, (ushort)_channels, (ushort)_bits, _rate, 0);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Header Error", MessageBoxButtons.OK);
                    stdout.Close();
                    return;
                }
            }

            if (!checkBox2.Checked) Timer.Reset();

            try
            {
                mx.StartRecording();
            }
            catch (Exception exc)
            {
                Timer.Stop();
                MessageBox.Show(exc.Message, "Startup Error", MessageBoxButtons.OK);
                stdout.Close();
                return;
            }

            comboBox1.Enabled = false;
            textBox1.Enabled = false;
            comboBox.Enabled = false;

            this.BackColor = System.Drawing.Color.DarkRed;
            button1.FlatAppearance.BorderColor = System.Drawing.Color.DarkRed;
            button1.Text = STOP;
            _REC = true;            
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
                Filter = _mode <2 ? "wav files (*.wav)|*.wav|All files (*.*)|*.*" : "mp3 files (*.mp3)|*.mp3|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = dialog.FileName;
                if (_REC)
                {
                    restart = true;
                    mx.StopRecording();
                    Timer.Restart();
                }
            }
        }

        /// <summary>
        /// Change output
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            _output = (comboBox1.SelectedItem as Item).Id;
            OutputSwap();
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
        public void SoundChannel_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded == 0)
            {
                if (Timer.IsRunning) Timer.Stop();
                return;
            }

            if (!Timer.IsRunning)
            {
                Timer.Start();
            }

            if (_mode == 1) //32-bit float
            {
                stdout.Write(e.Buffer, 0, e.BytesRecorded);
            }
            else
            {
                byte[] to16 = new byte[e.BytesRecorded / 2];
                int destOffset = 0;
                int sourceSamples = e.BytesRecorded / 4;

                sourceWaveBuffer = new WaveBuffer(e.Buffer);
                destWaveBuffer = new WaveBuffer(to16);

                for (int sample = 0; sample < sourceSamples; sample++)
                {
                    float sample32 = sourceWaveBuffer.FloatBuffer[sample];
                    destWaveBuffer.ShortBuffer[destOffset++] = (short)(sample32 * 32767);
                }

                if (_mode == 0) //16-bit wav
                {
                    stdout.Write(to16, 0, destOffset * 2);
                }
                else if (Mp3Writer != null)
                {
                    Mp3Writer.Write(to16, 0, destOffset * 2);
                }
            }
            
            try
            {
                Invoke(update);
            }
            catch
            { }
        }

        /// <summary>
        /// Stopped Recording
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SoundChannel_RecordingStopped(object sender, StoppedEventArgs e)
        {
            _REC = false;

            Mp3Writer = null;

            stdout.Close();

            if (_mode < 2 && _output == 0) //wav file
            {
                //set the time duration in the Wav header now that we're complete
                stdout = File.Open(filename, FileMode.Open);
                stdout.Position = 4;
                stdout.Write(BitConverter.GetBytes((uint)stdout.Length - 8), 0, 4);
                stdout.Position = 40;
                stdout.Write(BitConverter.GetBytes((uint)stdout.Length - 44), 0, 4);
                stdout.Close();
            }
            else if (_output == 1)
            {
                tcp.Close();
            }

            if (restart)
            {                
                button1_Click(sender, e);
            }

            if (exit)
            {
                NativeMethods.FreeLibrary(mp3lib);
            }
            else if (!restart)
            {
                Invoke(reset);
            }
            else
            {
                restart = false;
            }
            
            if (e.Exception != null) MessageBox.Show(e.Exception.Message, "Recording Error", MessageBoxButtons.OK);
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


        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            VERB = "GET";
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            VERB = "POST";
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            VERB = "PUT";
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked == true) checkBox2.Checked = false;
        }
    }
}
