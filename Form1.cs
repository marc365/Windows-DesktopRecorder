#region MIT license
// 
// MIT license
//
// Copyright (c) 2017-2025 Marc Williams
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
using NAudio;
using NAudio.Lame;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace DesktopRecorder
{
    public partial class Form1 : Form
    {
        #region Constructor

        private RegistryKey registry;
        private WasapiLoopbackCapture mx;
        private WaveBuffer source;
        private WaveBuffer dest;
        private LameMP3FileWriter mp3writer;
        private IntPtr mp3lib;
        private Stream stdout;
        private TcpClient tcp;
        private Stopwatch Timer;
        private SaveFileDialog dialog;
        private MethodInvoker update;
        private MethodInvoker reset;
        private MethodInvoker click;
        private MethodInvoker output;
        private MethodInvoker mode;
        private MethodInvoker load;
        private Thread pipe;

        private int _rate;
        private int _bits;
        private int _bitrate;
        private int _channels;
        private int _mode = 1;
        private readonly string[] _modes = { Title.Mode0, Title.Mode1, Title.Mode2, Title.Mode3, Title.Mode4, Title.Mode5, Title.Mode6 };
        private int _output = 0;
        private readonly string[] _outputs = { Title.File, Title.Stream };
        private string _verb;
        private string _filename;
        private int _width;
        private int _height;
        private int _left;
        private int _top;
        private int _update;
        private string _time;
        private string _button_start;
        private string _button_stop;
        private FileMode _filemode = FileMode.Create;

        private bool recording;
        private bool restart;
        private bool exit;
        private bool remote;
        private bool minimized;
        private bool small;

        /// <summary>
        /// Initializer
        /// </summary>
        public Form1()
        {
            //associate with the active audio device
            try
            {
                mx = new WasapiLoopbackCapture();
                _rate = mx.WaveFormat.SampleRate;
                _bits = 32;
                _channels = mx.WaveFormat.Channels;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, Error.Device, MessageBoxButtons.OK);
                Environment.Exit(exc.HResult);
            }

            //remote control
            using (NamedPipeClientStream instance = new NamedPipeClientStream(Def.PipeName))
            {
                try
                {
                    instance.Connect(0);
                    instance.ReadByte();
                    pipe = null;
                }
                catch (TimeoutException) //nothing found 
                {
                    //only first instance running can be remote controlled
                    remote = true;
                    pipe = new Thread(PipeServer);
                }
            }

            //load the settings
            LoadRegistry();

            //set before open
            Left = _left;
            Top = _top;

            InitializeComponent();
            
            //method invokers
            update = UpdateTimer;
            reset = ResetView;
            click = button1_Invoke;
            output = OutputSwap;
            mode = ModeSwap;
            load = LoadRegistry;
        }

        #endregion

        #region UI

        /// <summary>
        /// Called when starting
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
            //set the callbacks
            mx.DataAvailable += new EventHandler<WaveInEventArgs>(SoundChannel_DataAvailable);
            mx.RecordingStopped += new EventHandler<StoppedEventArgs>(SoundChannel_RecordingStopped);

            //set timer
            Timer = new Stopwatch();

            //load the UI settings
            LoadRegistryUI();

            //set when starting
            Width = _width;
            Height = _height;

            //view modes
            OutputSwap(); //close registry
            ModeSwap();
            ResetView();
            ButtonSwap();

            //output modes
            comboBox1.DisplayMember = Def.Name;
            comboBox1.ValueMember = Def.Id;
            int m = 0;
            for (; m < _outputs.Length; m++)
            {
                comboBox1.Items.Add(new Item(_outputs[m], m));
            }

            //recording modes
            comboBox2.DisplayMember = Def.Name;
            comboBox2.ValueMember = Def.Id;
            for (m = 0; m < _modes.Length; m++)
            {
                comboBox2.Items.Add(new Item(_modes[m], m));
            }
            
            #region libmp3lame.dll

            //extract embedded resource to the Temp folder and load
            string libdir = Path.GetTempPath();
            try
            {                
                if (!Directory.Exists(libdir))
                {
                    Directory.CreateDirectory(libdir);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, Error.Directory, MessageBoxButtons.OK);
                Environment.Exit(exc.HResult);
            }
            libdir = Path.Combine(libdir, Def.LameDll);
            if (!File.Exists(libdir)) using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DesktopRecorder.libmp3lame.dll"))
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
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, Error.FileSystem, MessageBoxButtons.OK);
                    Environment.Exit(exc.HResult);
                }
            }
            mp3lib = NativeMethods.LoadLibrary(libdir);

            #endregion
        }

        /// <summary>
        /// Called after starting
        /// </summary>
        private void Form1_Shown(object sender, EventArgs e)
        {
            if (Form.ActiveForm != null)
            {
                ButtonSwap();
            }
            if (pipe != null) pipe.Start();
            if (Program.AutoStart) Invoke(click);
            ResetTimer();
        }

        /// <summary>
        /// Called after resizing window
        /// </summary>
        private void Form1_Resized(object sender, EventArgs e)
        {
            if (Form.ActiveForm != null)
            {
                _width = Width;
                _height = Height;
                _left = Left;
                _top = Top;

                ButtonSwap();

                SaveRegistry();
                UpdateTimer();
            }
        }

        /// <summary>
        /// Called when quiting
        /// </summary>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (pipe != null)
            {
                remote = false;                
                using (NamedPipeClientStream instance = new NamedPipeClientStream(Def.PipeName)) //connecting to close 
                {
                    instance.Connect(0);
                    instance.ReadByte();
                }
            }
            if (recording)
            {
                recording = false;
                exit = true;
                mx.StopRecording();
            }
            else
            {
                NativeMethods.FreeLibrary(mp3lib);
            }
            SaveRegistry();
        }

        /// <summary>
        /// Change output
        /// </summary>
        private void OutputSwap()
        {
            registry = Registry.CurrentUser.OpenSubKey(Reg.KEY);
            switch (_output)
            {
                case 0:
                    textBox1.Text = (string)registry.GetValue(Reg.File, Def.EmptyString);
                    button2.Show();
                    checkBox1.Show();
                    checkBox2.Show();
                    checkBox3.Show();
                    radioButton1.Hide();
                    radioButton2.Hide();
                    radioButton3.Hide();
                    break;
                case 1:
                    textBox1.Text = (string)registry.GetValue(Reg.Stream, Def.Https);
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

            comboBox1.SelectedItem = _output;
            comboBox1.Text = _outputs[_output];
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
                textBox1.Text = textBox1.Text.Replace(Def.Mp3, Def.Wav);
            }
            else
            {
                textBox1.Text = textBox1.Text.Replace(Def.Wav, Def.Mp3);
            }

            comboBox2.SelectedItem = _mode;
            comboBox2.Text = _modes[_mode];
        }

        /// <summary>
        /// Resize the button
        /// </summary>
        private void ButtonSwap()
        {
            if (_height < Def.ToggleHeight && !small)
            {
                small = true;
                button1.Image = Properties.Resources.rec_but;
                tableLayoutPanel1.ColumnStyles.Clear();
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
                button1.Font = new System.Drawing.Font(Def.Font, 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
                checkBox4.Hide();
                checkBox5.Hide();
            }
            else if (_height >= Def.ToggleHeight)
            {
                if (small || button1.Image == null)
                {
                    button1.Image = Properties.Resources.rec_large;
                    tableLayoutPanel1.ColumnStyles.Clear();
                    tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124F));
                    button1.Font = new System.Drawing.Font(Def.Font, 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
                    checkBox4.Show();
                    checkBox5.Show();
                }
                small = false;
            }
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        }

        /// <summary>
        /// Format the recording time and
        /// change the form border style 
        /// based on the width of the window
        /// </summary>
        private void UpdateTimer()
        {
            int nudge = 0;
            if (_height < Def.ToggleHeight) nudge = 54;
            if (_width < Def.ToolWidth - nudge) { _time = Def.EmptyString; FormBorderStyle = FormBorderStyle.SizableToolWindow; }
            else if (_width < Def.ShortWidth - nudge) { _time = Def.EmptyString; FormBorderStyle = FormBorderStyle.Sizable; }
            else if (_width < Def.ShortTimeWidth - nudge) { _time = Timer.Elapsed.ToString(Def.ShortStamp); FormBorderStyle = FormBorderStyle.Sizable; }
            else { _time = Timer.Elapsed.ToString(Def.TimeStamp); FormBorderStyle = FormBorderStyle.Sizable; }
            if (_time != label1.Text) label1.Text = _time;
        }

        /// <summary>
        /// Run the timer update from a background thread
        /// </summary>
        private void BackgroundTimer()
        {
            while (recording)
            {
                if ( _width > Def.ShortWidth)
                {
                    try
                    {
                        Invoke(update);
                    }
                    catch (InvalidOperationException)
                    {
                        //Object disposed exception
                    }
                }
                Thread.Sleep(_update);                
            }
        }

        /// <summary>
        /// Reset the recording time
        /// </summary>
        private void ResetTimer()
        {
            Timer.Reset();
            Invoke(update);
        }

        /// <summary>
        /// Called after Stopped Recording
        /// </summary>
        private void ResetView()
        {
            Timer.Stop();
            BackColor = Color.WhiteSmoke;
            button1.FlatAppearance.BorderColor = BackColor;
            comboBox1.Enabled = true;
            textBox1.Enabled = true;
            comboBox2.Enabled = true;
            button1.Text = _button_start;
        }

        #endregion

        #region Actions

        /// <summary>
        /// Record
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            //toggle stop
            if (recording)
            {
                mx.StopRecording();
                return;
            }

            SaveRegistry();

            #region File

            if (_output == 0)
            {
                if (string.IsNullOrEmpty(textBox1.Text))
                {
                    button2_Click(sender, e);
                }

                _filename = textBox1.Text;

                if (checkBox1.Checked)
                {
                    _filename = _filename.Substring(0, _filename.Length - 4) + DateTime.Now.ToString(Def.DateStamp) + _filename.Substring(_filename.Length - 4, 4);
                }

                if (File.Exists(_filename))
                {
                    if (checkBox3.Checked)
                    {
                        _filemode = FileMode.Append;
                    }
                    else
                    {
                        _filemode = FileMode.Create;

                        if (!checkBox2.Checked)
                        {
                            FileInfo file = new FileInfo(_filename);

                            _filename = file.Name.Substring(0, file.Name.Length - file.Extension.Length);
                            _filename = Path.Combine(file.DirectoryName, string.Concat(_filename, Def.Dot, Directory.GetFiles(file.DirectoryName, _filename + Def.DotStar).Length, file.Extension));
                        }
                    }
                }

                try
                {
                    stdout = File.Open(_filename, _filemode);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, Error.File, MessageBoxButtons.OK);
                    return;
                }
            }

            #endregion

            #region Stream

            else if (_output == 1)
            {
                try
                {
                    Uri uri = new Uri(textBox1.Text);
                    tcp = new TcpClient();
                    IPAddress[] a = Dns.GetHostAddresses(uri.DnsSafeHost);

                    tcp.Connect(a[0], uri.Port);

                    if (uri.Scheme == Def.https)
                    {
                        SslStream ssl = new SslStream(tcp.GetStream(), true, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);

                        ssl.AuthenticateAsClient(uri.DnsSafeHost, null, System.Security.Authentication.SslProtocols.Tls12, false);

                        stdout = ssl;
                    }
                    else
                    {
                        stdout = tcp.GetStream();
                    }

                    #region send header

                    byte[] header = Encoding.ASCII.GetBytes(new StringBuilder()
                    .Append(_verb).Append(Def.Space).Append(uri.PathAndQuery).Append(Def.Space).Append(Def.Proto).Append(Def.NewLine)
                    .Append((uri.UserInfo == null) ? Def.EmptyString : string.Concat(Def.Authorization, Def.Basic, Def.Space, Convert.ToBase64String(Encoding.ASCII.GetBytes(uri.UserInfo)), Def.NewLine))
                    .Append(Def.Host).Append(uri.DnsSafeHost).Append(Def.NewLine)
                    .Append(Def.UserAgent).Append(Def.AppName).Append(Def.Slash).Append(Application.ProductVersion).Append(Def.NewLine).Append(Def.NewLine).ToString());

                    stdout.Write(header, 0, header.Length);

                    #endregion

                    #region parse response

                    Thread.Sleep(500); //pause for network (single thread)

                    //the server will return an header if there is an error 
                    if (tcp.Available > 0)
                    {
                        byte[] buf = new byte[65535];
                        int l = stdout.Read(buf, 0, buf.Length);
                        string Header = Encoding.ASCII.GetString(buf, 0, l);
                        switch (buf[9] - 0x30) //error class 100, 200, 300, 400, 500 from the first ASCII character 
                        {
                            case 1: //Continue
                            case 2: //OK
                                goto end;
                            case 3: //Redirect
                                uri = new Uri(DeSerializeHeader(Header, Def.Location));
                                textBox1.Text = uri.AbsoluteUri;
                                break;
                            case 4: //Authentication
                            case 5: //Error
                                break;
                        }
                        MessageBox.Show(Header, Title.ServerResponse, MessageBoxButtons.OK);
                        return;
                    end:
                        ;
                    }

                    #endregion
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, Error.Stream, MessageBoxButtons.OK);
                    return;
                }
            }

            #endregion

            #region Mp3

            //encode with libmp3lame.dll
            if (_mode > 1)
            {
                try
                {
                    mp3writer = new LameMP3FileWriter(stdout, new WaveFormat(_rate, _bits, _channels), _bitrate);
                }
                catch (ArgumentException exc)
                {
                    MessageBox.Show(exc.Message, Error.Mp3, MessageBoxButtons.OK);
                    stdout.Close();
                    return;
                }
            }

            #endregion

            #region Wav

            else if (_filemode == FileMode.Create && _output == 0)
            {
                try
                {
                    WriteWavHeader(stdout, (_bits == 32) ? true : false, (ushort)_channels, (ushort)_bits, _rate, 0);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, Error.Header, MessageBoxButtons.OK);
                    stdout.Close();
                    return;
                }
            }

            #endregion

            if (!checkBox3.Checked) ResetTimer(); //don't reset timer if write mode is Append

            try
            {
                mx.StartRecording();
            }
            catch (Exception exc)
            {
                Timer.Stop();
                MessageBox.Show(exc.Message, Error.Startup, MessageBoxButtons.OK);
                stdout.Close();
                return;
            }

            comboBox1.Enabled = false;
            textBox1.Enabled = false;
            comboBox2.Enabled = false;

            BackColor = Color.DarkRed;
            button1.FlatAppearance.BorderColor = BackColor;
            button1.Text = _button_stop;
            recording = true;

            new Thread(BackgroundTimer).Start();
        }

        /// <summary>
        /// STAThread invoke
        /// </summary>
        private void button1_Invoke()
        {
            button1_Click(null, null);
        }

        /// <summary>
        /// File selector
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            dialog = new SaveFileDialog()
            {
                Title = Title.SaveAs,
                OverwritePrompt = false,
                Filter =  string.Format(Title.FileSelector, (_mode < 2) ? Def.wav : Def.mp3 )
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = dialog.FileName;
                if (recording)
                {
                    HotSwap();
                }
            }
        }

        /// <summary>
        /// Change output
        /// </summary>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            _output = (comboBox1.SelectedItem as Item).Id;
            OutputSwap();
        }

        /// <summary>
        /// Change mode
        /// </summary>
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            _mode = (comboBox2.SelectedItem as Item).Id;
            ModeSwap();
        }

        /// <summary>
        /// Click the timer to reset it
        /// </summary>
        private void label1_Click(object sender, EventArgs e)
        {
            ResetTimer();
        }

        /// <summary>
        /// Set the verb to GET
        /// </summary>
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            _verb = Def.GET;
        }

        /// <summary>
        /// Set the verb to POST
        /// </summary>
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            _verb = Def.POST;
        }

        /// <summary>
        /// Set the verb to PUT
        /// </summary>
        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            _verb = Def.PUT;
        }

        /// <summary>
        /// Uncheck overwrite if append was toggled
        /// </summary>
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked == true) checkBox2.Checked = false;
        }

        /// <summary>
        /// Window always on top 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            TopMost = checkBox4.Checked;
        }

        #endregion

        #region Sound Channel

        /// <summary>
        /// Recording CallBack
        /// </summary>
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

                source = new WaveBuffer(e.Buffer);
                dest = new WaveBuffer(to16);

                for (int sample = 0; sample < sourceSamples; sample++)
                {
                    float sample32 = source.FloatBuffer[sample];
                    dest.ShortBuffer[destOffset++] = (short)(sample32 * short.MaxValue);
                }

                if (_mode == 0) //16-bit wav
                {
                    stdout.Write(to16, 0, destOffset * 2);
                }
                else if (mp3writer != null)
                {
                    mp3writer.Write(to16, 0, destOffset * 2);
                }
            }
        }

        /// <summary>
        /// Stopped Recording
        /// </summary>
        private void SoundChannel_RecordingStopped(object sender, StoppedEventArgs e)
        {
            recording = false;

            mp3writer = null;

            stdout.Close();

            if (_mode < 2 && _output == 0) //wav file
            {
                //set the time duration in the Wav header now that we're complete
                stdout = File.Open(_filename, FileMode.Open);
                stdout.Position = 4;
                stdout.Write(BitConverter.GetBytes((uint)stdout.Length - 8), 0, 4);
                stdout.Position = 40;
                stdout.Write(BitConverter.GetBytes((uint)stdout.Length - 44), 0, 4);
                stdout.Close();
            }
            else if (_output == 1)
            {
                if (e.Exception != null)
                {
                    Exception exc = e.Exception as IOException;
                    if (exc != null)
                    {
                        MessageBox.Show(exc.Message, Error.Socket, MessageBoxButtons.OK);
                    }
                    else
                    {
                        MessageBox.Show(e.Exception.Message, Error.Streaming, MessageBoxButtons.OK);
                    }
                }
                tcp.Close();
            }

            if (restart)
            {
                button1_Click(sender, e);
            }

            if (exit)
            {
                NativeMethods.FreeLibrary(mp3lib);
                remote = false;
            }
            else if (!restart)
            {
                Invoke(load);
                Invoke(output); //close registry
                Invoke(reset);
            }
            else
            {
                restart = false;
            }

            if (e.Exception != null && _output == 0) MessageBox.Show(e.Exception.Message, Error.Recording, MessageBoxButtons.OK);
        }

        #endregion

        #region File

        /// <summary>
        /// Creates a Wav file header
        /// </summary>
        private void WriteWavHeader(Stream stream, bool isFloatingPoint, ushort channelCount, ushort bitDepth, int sampleRate, int totalSampleCount)
        {
            stream.Position = 0;

            #region RIFF header.
            // Chunk ID.
            stream.Write(Encoding.ASCII.GetBytes(Def.RIFF), 0, 4);
            // Chunk size.
            stream.Write(BitConverter.GetBytes((((int)bitDepth / 8) * totalSampleCount) + 36), 0, 4);
            // Format.
            stream.Write(Encoding.ASCII.GetBytes(Def.WAVE), 0, 4);
            #endregion

            #region Sub-chunk 1.
            // Sub-chunk 1 ID.
            stream.Write(Encoding.ASCII.GetBytes(Def.fmt), 0, 4);
            // Sub-chunk 1 size.
            stream.Write(BitConverter.GetBytes(16), 0, 4);
            // Audio format (floating point (3) or PCM (1)). Any other format indicates compression.
            stream.Write(BitConverter.GetBytes((ushort)((!isFloatingPoint) ? 1u : 3u)), 0, 2);
            // Channels.
            stream.Write(BitConverter.GetBytes(channelCount), 0, 2);
            // Sample rate.
            stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);
            // Bytes rate.
            stream.Write(BitConverter.GetBytes(sampleRate * channelCount * ((int)bitDepth / 8)), 0, 4);
            // Block align.
            stream.Write(BitConverter.GetBytes(channelCount * ((int)bitDepth / 8)), 0, 2);
            // Bits per sample.
            stream.Write(BitConverter.GetBytes(bitDepth), 0, 2);
            #endregion

            #region Sub-chunk 2.
            // Sub-chunk 2 ID.
            stream.Write(Encoding.ASCII.GetBytes(Def.data), 0, 4);
            // Sub-chunk 2 size.
            stream.Write(BitConverter.GetBytes(((int)bitDepth / 8) * totalSampleCount), 0, 4);
            #endregion
        }

        /// <summary>
        /// Trigger the RecordingStopped method to restart recording
        /// </summary>
        private void HotSwap()
        {
            restart = true;
            mx.StopRecording();
            ResetTimer();
        }

        #endregion

        #region Stream

        /// <summary>
        /// Validate certificates
        /// </summary>
        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
            {
                string status = certificate.Subject + " : " + certificate.Issuer + Environment.NewLine;
                if (chain != null && chain.ChainStatus != null)
                {
                    for (int i = chain.ChainStatus.Length - 1; i > -1; i--)
                    {

                        status += chain.ChainStatus[i].Status.ToString() + Environment.NewLine;

                    }
                }

                DialogResult result = MessageBox.Show(status, Title.IgnoreCertificateError, MessageBoxButtons.YesNo);
                return result == DialogResult.Yes;
            }

            // If the certificate is a valid, signed certificate, return true.
            if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
            {
                return true;
            }

            // If there are errors in the certificate chain, look at each error to determine the cause.
            if ((sslPolicyErrors & System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors) != 0)
            {
                if (chain != null && chain.ChainStatus != null)
                {
                    for (int i = chain.ChainStatus.Length - 1; i > -1; i--)
                    {
                        if ((certificate.Subject == certificate.Issuer) && (chain.ChainStatus[i].Status == System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.UntrustedRoot))
                        {
                            // Self-signed certificates with an untrusted root are valid. 
                            continue;
                        }
                        else
                        {
                            if (chain.ChainStatus[i].Status != System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.NoError)
                            {
                                // If there are any other errors in the certificate chain, the certificate is invalid,
                                // so the method returns false.
                                return false;
                            }
                        }
                    }
                }

                // When processing reaches this line, the only errors in the certificate chain are untrusted
                // root errors for self-signed certificates. These certificates are valid, so return true.
                return true;
            }

            // In all other cases, return false.
            return false;
        }

        private static string DeSerializeHeader(string Header, string Parameter, int Offset = 0)
        {
            Offset = Header.IndexOf(Parameter, Offset, StringComparison.OrdinalIgnoreCase);
            if (Offset > -1)
            {
                Offset += Parameter.Length;
                int i = Header.IndexOf(Def.NewLine);
                if (i > -1) return Header.Substring(Offset, i - Offset);
                return Header.Substring(Offset);
            }
            return null;
        }

        #endregion

        #region Registry

        //Computer\\HKEY_CURRENT_USER\\Software\\DesktopRecorder

        /// <summary>
        /// Load the settings from the registry
        /// </summary>
        private void LoadRegistry()
        {
            registry = Registry.CurrentUser.OpenSubKey(Reg.KEY);
            if (registry == null) registry = Registry.CurrentUser.CreateSubKey(Reg.KEY);
            
            _output = (int)registry.GetValue(Reg.Output, _output);
            _mode = (int)registry.GetValue(Reg.Mode, _mode);
            if (checkBox1 != null)
            {
                LoadRegistryUI();
            }
            _width = (int)registry.GetValue(Reg.Width, 408); //legacy size
            _height = (int)registry.GetValue(Reg.Height, 121); //legacy size
            _left = (int)registry.GetValue(Reg.Left, 0);
            _top = (int)registry.GetValue(Reg.Top, 0);
            _update = (int)registry.GetValue(Reg.UpdateFrequency, 83); //12 fps
            while (true)
            {
                _button_start = Def.Space + (string)registry.GetValue(Reg.ButtonStart); //space character for alignment
                if (_button_start != Def.Space)
                {
                    break;
                }
                registry = Registry.CurrentUser.CreateSubKey(Reg.KEY);
                registry.SetValue(Reg.ButtonStart, Title.Record);
            }
            while (true)
            {
                _button_stop = Def.Space + (string)registry.GetValue(Reg.ButtonStop); //space character for alignment
                if (_button_stop != Def.Space)
                {
                    break;
                }
                registry = Registry.CurrentUser.CreateSubKey(Reg.KEY);
                registry.SetValue(Reg.ButtonStop, Title.Stop);
            }
            string ontop;
            while (true)
            {
                ontop = (string)registry.GetValue(Reg.AlwaysOnTop);
                if (ontop != null)
                {
                    break;
                }
                registry = Registry.CurrentUser.CreateSubKey(Reg.KEY);
                registry.SetValue(Reg.AlwaysOnTop, Def.False);
            }
            base.TopMost = bool.Parse(ontop);
        }

        /// <summary>
        /// Load UI state from the registry and set minimized
        /// </summary>
        private void LoadRegistryUI()
        {
            checkBox1.Checked = bool.Parse((string)registry.GetValue(Reg.Date, Def.False));
            checkBox2.Checked = bool.Parse((string)registry.GetValue(Reg.Overwrite, Def.False));
            checkBox3.Checked = bool.Parse((string)registry.GetValue(Reg.Append, Def.False));
            checkBox4.Checked = bool.Parse((string)registry.GetValue(Reg.AlwaysOnTop, Def.False));
            checkBox5.Checked = bool.Parse((string)registry.GetValue(Reg.StartMinimized, Def.False));
            _verb = (string)registry.GetValue(Reg.Verb, Def.PUT);
            switch (_verb)
            {
                case Def.GET:
                    radioButton1.Checked = true;
                    break;
                case Def.POST:
                    radioButton2.Checked = true;
                    break;
                case Def.PUT:
                    radioButton3.Checked = true;
                    break;
            }
            string min;
            while (true)
            {
                min = (string)registry.GetValue(Reg.StartMinimized);
                if (min != null)
                {
                    break;
                }
                registry = Registry.CurrentUser.CreateSubKey(Reg.KEY);
                registry.SetValue(Reg.StartMinimized, Def.False);
            }
            if (!minimized)
            {
                base.WindowState = (bool.Parse(min) ? FormWindowState.Minimized : FormWindowState.Normal);
                minimized = true;
            }
        }

        /// <summary>
        /// Save all settings to the registry
        /// </summary>
        private void SaveRegistry()
        {
            registry = Registry.CurrentUser.CreateSubKey(Reg.KEY);
            registry.SetValue(Reg.Output, _output);
            switch (_output)
            {
                case 0:
                    registry.SetValue(Reg.File, textBox1.Text);
                    break;
                case 1:
                    registry.SetValue(Reg.Stream, textBox1.Text);
                    break;
            }
            registry.SetValue(Reg.Mode, _mode);
            registry.SetValue(Reg.Date, checkBox1.Checked);
            registry.SetValue(Reg.Overwrite, checkBox2.Checked);
            registry.SetValue(Reg.Append, checkBox3.Checked);
            registry.SetValue(Reg.Verb, _verb);
            registry.SetValue(Reg.AlwaysOnTop, checkBox4.Checked);
            registry.SetValue(Reg.StartMinimized, checkBox5.Checked);
            registry.SetValue(Reg.Width, _width);
            registry.SetValue(Reg.Height, _height);
            registry.SetValue(Reg.Left, _left);
            registry.SetValue(Reg.Top, _top);
            registry.SetValue(Reg.UpdateFrequency, _update);
            registry.Close();
        }

        #endregion

        #region IPC
        
        /// <summary>
        /// Inter-process communication server
        /// </summary>
        private void PipeServer()
        {
            while (remote)
            {
                using (NamedPipeServerStream server = new NamedPipeServerStream(Def.PipeName))
                {

                    server.WaitForConnection();
                    server.WriteByte(1);
                    if (remote)
                    {
                        int msg = server.ReadByte();
                        Invoke(load);
                        if (recording && bool.Parse((string)registry.GetValue(Reg.HotSwap, Def.False)))
                        {
                            registry = Registry.CurrentUser.CreateSubKey(Reg.KEY);
                            registry.DeleteValue(Reg.HotSwap);
                            HotSwap();
                        }
                        Invoke(output); //close registry
                        Invoke(mode);
                        //command line remote control
                        switch (msg)
                        {
                            case 1: //handshake
                                break;
                            case 2: //start
                                if (!recording) Invoke(click);
                                break;
                            case 3: //stop
                                if (recording) Invoke(click);
                                break;
                            case 4: //quit
                                Environment.Exit(0);
                                break;
                        }
                    }
                }
            }
        }
        
        #endregion
    }
}
