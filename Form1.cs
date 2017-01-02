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

        private static LameMP3FileWriter Mp3Writer;
        private static string dllPath;
        private static IntPtr mp3lib;

        private static int _bitrate = 256;

        private static int _rate = mx.WaveFormat.SampleRate;
        private static int _bits = 16;
        private static int _channels = mx.WaveFormat.Channels;
        private static int _mode = 1;

        private static Stream stdout;

        private bool _REC = false;

        internal RegistryKey registry;
        private readonly string _regKey = "DesktopRecorder";
        private readonly string _regVal = "file";

        private static SaveFileDialog dialog;
        private FileMode _FILEMODE = FileMode.Create;

        public Form1()
        {
            mx.DataAvailable += new EventHandler<WaveInEventArgs>(SoundChannel_DataAvailable);
            mx.RecordingStopped += new EventHandler<StoppedEventArgs>(SoundChannel_RecordingStopped);

            InitializeComponent();

            this.BackColor = System.Drawing.Color.LightGray;
            textBox2.ForeColor = System.Drawing.Color.DarkRed;
            textBox2.BackColor = System.Drawing.Color.LightGray;

            registry = Registry.CurrentUser.OpenSubKey(_regKey);
            if (registry != null)
            {
                textBox1.Text = registry.GetValue(_regVal, null).ToString();
                ModeSwap();
            }

            comboBox1.Text = "Overwrite";
            comboBox1.DisplayMember = "Name";
            comboBox1.ValueMember = "Id";

            comboBox1.Items.Add(new Item(comboBox1.Text, 0));
            comboBox1.Items.Add(new Item("Append", 1));

            comboBox3.Text = _bits.ToString();
            comboBox3.DisplayMember = comboBox1.DisplayMember;
            comboBox3.ValueMember = comboBox1.ValueMember;

            comboBox3.Items.Add(new Item("16", 16));
            comboBox3.Items.Add(new Item("32", 32));

            comboBox5.Text = _bitrate.ToString();
            comboBox5.DisplayMember = comboBox1.DisplayMember;
            comboBox5.ValueMember = comboBox1.ValueMember;

            comboBox5.Items.Add(new Item("32", 32));
            comboBox5.Items.Add(new Item("64", 64));
            comboBox5.Items.Add(new Item("128", 128));
            comboBox5.Items.Add(new Item("256", 256));

            comboBox6.Text = "Wav";
            comboBox6.DisplayMember = comboBox1.DisplayMember;
            comboBox6.ValueMember = comboBox1.ValueMember;

            comboBox6.Items.Add(new Item(comboBox6.Text, 1));
            comboBox6.Items.Add(new Item("Mp3", 2));

            string dirName = Path.GetTempPath();
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            dllPath = Path.Combine(dirName, "libmp3lame.dll");

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DesktopRecorder.libmp3lame.dll"))
            {
                try
                {
                    using (Stream outFile = File.Create(dllPath))
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

            mp3lib = NativeMethods.LoadLibrary(dllPath);

            if (mp3lib == IntPtr.Zero)
            {
                DialogResult result = MessageBox.Show("Cannot load Lame Mp3 Library (libmp3lame.dll)", "Error", MessageBoxButtons.OK);
            }
        }

        private void ErrorMessage(string msg)
        {
            textBox2.Text = string.Format("Error: {0}", msg);
        }

        private void ErrorClear()
        {
            textBox2.Text = string.Empty;
        }

        private void ModeSwap()
        {
            if (_mode == 1)
            {
                textBox1.Text = textBox1.Text.Replace(".mp3", ".wav");
            }
            else if (_mode == 2)
            {
                textBox1.Text = textBox1.Text.Replace(".wav", ".mp3");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_REC) // Stop
            {
                this.BackColor = System.Drawing.Color.LightGray;
                textBox2.ForeColor = System.Drawing.Color.DarkRed;
                textBox2.BackColor = System.Drawing.Color.LightGray;

                button1.Text = "Record";
                _REC = false;

                mx.StopRecording();

                Mp3Writer = null;

                stdout.Close();

                // set the length in the header
                stdout = File.Open(textBox1.Text, FileMode.Open);
                stdout.Position = 4;
                stdout.Write(Encoding.ASCII.GetBytes((stdout.Length - 8).ToString()), 0, 4);
                stdout.Position = 40;
                stdout.Write(Encoding.ASCII.GetBytes((stdout.Length - 44).ToString()), 0, 4);
                stdout.Close();
            }
            else // Start
            {
                registry = Registry.CurrentUser.CreateSubKey(_regKey);
                registry.SetValue(_regVal, textBox1.Text);
                registry.Close();

                if (textBox1.Text == string.Empty)
                {
                    ErrorMessage("File not specified");
                    return;
                }
                else
                {
                    ErrorClear();
                }

                try
                {
                    stdout = File.Open(textBox1.Text, _FILEMODE);
                }
                catch(Exception exc)
                {
                    DialogResult result = MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK);
                    return;
                }

                if (_mode == 2)
                {
                    try
                    {
                        Mp3Writer = new LameMP3FileWriter(stdout, new WaveFormat(_rate, _bits, _channels), _bitrate);
                    }
                    catch (ArgumentException exc)
                    {
                        DialogResult result = MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK);
                        stdout.Close();

                        return;
                    }
                }
                else if (_mode == 1 && _FILEMODE == FileMode.Create)
                {
                    try
                    {
                        WriteWavHeader(stdout, _bits == 32 ? true : false, (ushort)_channels, (ushort)_bits, _rate, 0);
                    }
                    catch (Exception exc)
                    {
                        DialogResult result = MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK);
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
                    DialogResult result = MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK);
                    stdout.Close();

                    return;
                }

                this.BackColor = System.Drawing.Color.DarkRed;
                textBox2.ForeColor = System.Drawing.Color.LightGray;
                textBox2.BackColor = System.Drawing.Color.DarkRed;

                button1.Text = "Stop";
                _REC = true;
            }
        }

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

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {        
            switch(comboBox1.SelectedIndex)
            {
                case 0:
                    _FILEMODE = System.IO.FileMode.Create;
                    break;
                case 1:
                    _FILEMODE = System.IO.FileMode.Append;
                    break;
                default:
                    break;
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            _bits = (comboBox3.SelectedItem as Item).Id;
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            _bitrate = (comboBox5.SelectedItem as Item).Id;
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            _mode = (comboBox6.SelectedItem as Item).Id;

            ModeSwap();
        }

        public static void SoundChannel_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (mx != null)
            {
                WaveBuffer sourceWaveBuffer = new WaveBuffer(e.Buffer);

                if (_bits == 16)
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

                    switch (_mode)
                    {
                        case 1: // Wav
                            stdout.Write(to16, 0, destOffset * 2);
                            break;
                        case 2: // Mp3
                            if (Mp3Writer != null)
                            {
                                Mp3Writer.Write(to16, 0, destOffset * 2);
                            }
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (_mode)
                    {
                        case 1: // Wav
                            stdout.Write(sourceWaveBuffer.ByteBuffer, 0, e.BytesRecorded);
                            break;
                        case 2: // Mp3
                            if (Mp3Writer != null)
                            {
                                Mp3Writer.Write(sourceWaveBuffer.ByteBuffer, 0, e.BytesRecorded);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

        }

        private static void SoundChannel_RecordingStopped(object sender, StoppedEventArgs e)
        {
            //if (mx != null)
            //{
            //    mx.Dispose();
            //}
            //Console.Error.WriteLine(e.Exception);
            //System.Environment.Exit(0);
        }

        public static short[] ConvertToShort(byte[] input, int count)
        {
            short[] output = new short[count / 2];
            for (int n = 0; n < output.Length; n++)
            {
                output[n] = BitConverter.ToInt16(input, n * 2);
            }
            return output;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            NativeMethods.FreeLibrary(mp3lib);
        }

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
    }
}
