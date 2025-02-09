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
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace DesktopRecorder
{
    public partial class Form1 : Form
    {
        #region Constructor

        private WasapiLoopbackCapture mx;
        private WaveBuffer sourceWaveBuffer;
        private WaveBuffer destWaveBuffer;

        private LameMP3FileWriter Mp3Writer;
        private IntPtr mp3lib;
        private readonly string lame = "libmp3lame.dll";
        private string libdir;

        private readonly int _minwidth = 277;
        private int _width;
        private int _height;

        private int _rate;
        private int _bits;
        private int _bitrate;
        private int _channels;
        
        private int _mode = 1;
        private readonly string[] _modes = { "16-bit wav","32-bit wav","32 kbps mp3","64 kbps mp3","128 kbps mp3","256 kbps mp3","320 kbps mp3" };
        private int _output = 0;
        private readonly string[] _outputs = { "File", "Stream" };

        private TcpClient tcp;
        private Stream stdout;

        private FileMode _FILEMODE = FileMode.Create;
        private string filename;

        private bool _REC = false;
        private bool restart;
        private bool exit;

        private Stopwatch Timer;
        private readonly string RECORD = "Record";
        private readonly string STOP = "Stop";
        private string VERB;

        private RegistryKey registry;
        private readonly string _regKey = "Software\\DesktopRecorder";
        private readonly string _regOutput = "output";
        private readonly string _regFile = "file";
        private readonly string _regStream = "stream";
        private readonly string _regMode = "mode";
        private readonly string _regDate = "date";
        private readonly string _regOverwrite = "overwrite";
        private readonly string _regAppend = "append";
        private readonly string _regVerb = "verb";
        private readonly string _regWidth = "width";
        private readonly string _regHeight = "height";
        private readonly string DisplayMember = "Name";
        private readonly string ValueMember = "Id";

        private SaveFileDialog dialog;

        private MethodInvoker update;
        private MethodInvoker reset;
        
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
                MessageBox.Show(exc.Message, "Device Error", MessageBoxButtons.OK);
                Environment.Exit(exc.HResult);
            }

            InitializeComponent();
            update = UpdateTimer;
            reset = ResetView;            
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

            #region load registry
            
            //settings
            registry = Registry.CurrentUser.OpenSubKey(_regKey);
            if (registry == null) registry = Registry.CurrentUser.CreateSubKey(_regKey);
            
            _output = (int)registry.GetValue(_regOutput, _output);                
            _mode = (int)registry.GetValue(_regMode, _mode);
            checkBox1.Checked = bool.Parse((string)registry.GetValue(_regDate, "False"));
            checkBox2.Checked = bool.Parse((string)registry.GetValue(_regOverwrite, "False"));
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
            _width = (int)registry.GetValue(_regWidth, 408); //277
            _height =(int)registry.GetValue(_regHeight, 121); //105
            OutputSwap(); //close registry
            ModeSwap();
            
            #endregion

            //set button text
            button1.Text = RECORD;

            //set timer
            Timer = new Stopwatch();
            ResetTimer();            

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
            comboBox2.Text = _modes[_mode];
            comboBox2.DisplayMember = DisplayMember;
            comboBox2.ValueMember = ValueMember;
            for (m = 0; m < _modes.Length; m++)
            {
                comboBox2.Items.Add(new Item(_modes[m], m));
            }

            #region libmp3lame.dll

            //extract embedded resource to the Temp folder and load
            try
            {
                libdir = Path.GetTempPath();
                if (!Directory.Exists(libdir))
                {
                    Directory.CreateDirectory(libdir);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "File System Error", MessageBoxButtons.OK);
                Environment.Exit(exc.HResult);
            }
            libdir = Path.Combine(libdir, lame);
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
                    MessageBox.Show(exc.Message, "File System Error", MessageBoxButtons.OK);
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
            Form1.ActiveForm.Width = _width;
            Form1.ActiveForm.Height = _height;
        }

        /// <summary>
        /// Called after resizing window
        /// </summary>
        private void Form1_Resize(object sender, EventArgs e)
        {
            _width = Form1.ActiveForm.Width;
            _height = Form1.ActiveForm.Height;
            
            #region save registry

            registry = Registry.CurrentUser.CreateSubKey(_regKey);
            registry.SetValue(_regWidth, _width);
            registry.SetValue(_regHeight, _height);
            registry.Close();

            #endregion
        }

        /// <summary>
        /// Called when quiting
        /// </summary>
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
        /// Reset the recording time
        /// </summary>
        private void ResetTimer()
        {
            Timer.Reset();
            label1.Text = "00:00:00.0000000";
        }

        /// <summary>
        /// Called after Stopped Recording
        /// </summary>
        private void ResetView()
        {
            Timer.Stop();
            BackColor = Color.WhiteSmoke;
            button1.FlatAppearance.BorderColor = Color.WhiteSmoke;
            comboBox1.Enabled = true;
            textBox1.Enabled = true;
            comboBox2.Enabled = true;
            button1.Text = RECORD;
        }

        #endregion

        #region Actions

        /// <summary>
        /// Record
        /// </summary>
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

            #region File

            if (_output == 0)
            {
                if (string.IsNullOrEmpty(textBox1.Text))
                {
                    button2_Click(sender, e);
                }

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

                    #region send header

                    byte[] header = Encoding.ASCII.GetBytes(VERB + " " + uri.PathAndQuery + " HTTP/1.1\r\n"

                    + (uri.UserInfo == null ? string.Empty : "Authorization: Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(uri.UserInfo)) + "\r\n")
                    + "Host: " + uri.DnsSafeHost + "\r\n"
                    + "UserAgent: desktoprecorder/1.8\r\n\r\n");

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
                                uri = new Uri(DeSerializeHeader(Header, "Location"));
                                textBox1.Text = uri.AbsoluteUri;
                                break;
                            case 4: //Authentication
                            case 5: //Error
                                break;
                        }
                        MessageBox.Show(Header, "Server Response", MessageBoxButtons.OK);
                        return;
                    end:
                        ;
                    }

                    #endregion
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Stream Error", MessageBoxButtons.OK);
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
                    Mp3Writer = new LameMP3FileWriter(stdout, new WaveFormat(_rate, _bits, _channels), _bitrate);
                }
                catch (ArgumentException exc)
                {
                    MessageBox.Show(exc.Message, "Mp3 Error", MessageBoxButtons.OK);
                    stdout.Close();
                    return;
                }
            }

            #endregion

            #region Wav

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

            #endregion

            if (!checkBox3.Checked) ResetTimer(); //don't reset timer if write mode is Append

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
            comboBox2.Enabled = false;

            BackColor = Color.DarkRed;
            button1.FlatAppearance.BorderColor = Color.DarkRed;
            button1.Text = STOP;
            _REC = true;            
        }

        /// <summary>
        /// File selector
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            dialog = new SaveFileDialog()
            {
                Title = "Save As",
                OverwritePrompt = false,
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
            VERB = "GET";
        }

        /// <summary>
        /// Set the verb to POST
        /// </summary>
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            VERB = "POST";
        }

        /// <summary>
        /// Set the verb to PUT
        /// </summary>
        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            VERB = "PUT";
        }

        /// <summary>
        /// Uncheck overwrite if append was toggled
        /// </summary>
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked == true) checkBox2.Checked = false;
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

            //update timer if not hidden
            if (_width > _minwidth) Invoke(update);
        }

        /// <summary>
        /// Stopped Recording
        /// </summary>
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
                if (e.Exception != null)
                {
                    Exception exc = e.Exception as IOException;
                    if (exc != null)
                    {
                        MessageBox.Show(exc.Message, "Socket Error", MessageBoxButtons.OK);
                    }
                    else
                    {
                        MessageBox.Show(e.Exception.Message, "Streaming Error", MessageBoxButtons.OK);
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
            }
            else if (!restart)
            {
                Invoke(reset);
            }
            else
            {
                restart = false;
            }

            if (e.Exception != null && _output == 0) MessageBox.Show(e.Exception.Message, "Recording Error", MessageBoxButtons.OK);
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
            stream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);
            // Chunk size.
            stream.Write(BitConverter.GetBytes(((bitDepth / 8) * totalSampleCount) + 36), 0, 4);
            // Format.
            stream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);
            #endregion

            #region Sub-chunk 1.
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
            #endregion

            #region Sub-chunk 2.
            // Sub-chunk 2 ID.
            stream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);
            // Sub-chunk 2 size.
            stream.Write(BitConverter.GetBytes((bitDepth / 8) * totalSampleCount), 0, 4);
            #endregion
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

                DialogResult result = MessageBox.Show(status, "Ignore this certificate name error?", MessageBoxButtons.YesNo);
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
                int i = Header.IndexOf("\r\n");
                if (i > -1) return Header.Substring(Offset, i - Offset);
                return Header.Substring(Offset);
            }
            return null;
        }

        #endregion
    }
}
