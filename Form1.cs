using Microsoft.Win32;
using NAudio.Codecs;
using NAudio.Lame;
using NAudio.Wave;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DesktopRecorder
{
    static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr hModule);
    }

    public partial class Form1 : Form
    {

        private static WasapiLoopbackCapture mx;

        private static bool G722Audio = false;
        private static G722CodecState _state;
        private static G722Codec _codec = new G722Codec();

        private static bool Mp3Audio = true;
        private static LameMP3FileWriter writer;
        private static IntPtr mp3lib;

        private static int _bitrate = 256;

        private static int _rate = 48000;
        private static int _bits = 16;
        private static int _channels = 2;

        private static NAudio.Wave.WaveFormat frm;

        private static Stream stdout;

        private static SaveFileDialog dialog;

        private readonly string _regKey = "DesktopRecorder";
        private readonly string _regVal = "file";

        private bool _REC = false;
        private System.IO.FileMode _FILEMODE = System.IO.FileMode.Create;

        private static int count;

        private string dllPath;

        internal RegistryKey registry;

        public Form1()
        {
            InitializeComponent();
            this.BackColor = System.Drawing.Color.LightGray;
            textBox2.ForeColor = System.Drawing.Color.DarkRed;
            textBox2.BackColor = System.Drawing.Color.LightGray;

            registry = Registry.CurrentUser.OpenSubKey(_regKey);
            if (registry != null)
            {
                textBox1.Text = registry.GetValue(_regVal, null).ToString();
            }

            comboBox1.Text = "Overwrite";
            comboBox1.DisplayMember = "Name";
            comboBox1.ValueMember = "Id";

            comboBox1.Items.Add(new Item(comboBox1.Text, 0));
            comboBox1.Items.Add(new Item("Append", 1));

            comboBox2.Text = _channels.ToString();
            comboBox2.DisplayMember = "Name";
            comboBox2.ValueMember = "Id";

            comboBox2.Items.Add(new Item("1", 1));
            comboBox2.Items.Add(new Item("2", 2));
            comboBox2.Items.Add(new Item("4", 4));

            comboBox3.Text = _bits.ToString();
            comboBox3.DisplayMember = "Name";
            comboBox3.ValueMember = "Id";

            comboBox3.Items.Add(new Item("8", 8));
            comboBox3.Items.Add(new Item("16", 16));
            comboBox3.Items.Add(new Item("32", 32));

            comboBox4.Text = _rate.ToString();
            comboBox4.DisplayMember = "Name";
            comboBox4.ValueMember = "Id";

            comboBox4.Items.Add(new Item("8000", 8000));
            comboBox4.Items.Add(new Item("11025", 11025));
            comboBox4.Items.Add(new Item("16000", 16000));
            comboBox4.Items.Add(new Item("22050", 22050));
            comboBox4.Items.Add(new Item("32000", 32000));
            comboBox4.Items.Add(new Item("44100", 44100));
            comboBox4.Items.Add(new Item("48000", 48000));
            comboBox4.Items.Add(new Item("96000", 96000));
            comboBox4.Items.Add(new Item("192000", 192000));

            comboBox5.Text = _bitrate.ToString();
            comboBox5.DisplayMember = "Name";
            comboBox5.ValueMember = "Id";

            comboBox5.Items.Add(new Item("32", 32));
            comboBox5.Items.Add(new Item("64", 64));
            comboBox5.Items.Add(new Item("128", 128));
            comboBox5.Items.Add(new Item("256", 256));

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

                writer = null;
                //mx.Dispose();

                stdout.Close();
            }
            else // Start
            {
                if (textBox1.Text == string.Empty)
                {
                    ErrorMessage("File not specified");
                    return;
                }
                else
                {
                    ErrorClear();
                }

                frm = new WaveFormat(_rate, _bits, _channels);

                registry = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_regKey);
                registry.SetValue(_regVal, textBox1.Text);
                registry.Close();

                this.BackColor = System.Drawing.Color.DarkRed;
                textBox2.ForeColor = System.Drawing.Color.LightGray;
                textBox2.BackColor = System.Drawing.Color.DarkRed;

                button1.Text = "Stop";
                _REC = true;

                stdout = System.IO.File.Open(textBox1.Text, _FILEMODE);

                if (Mp3Audio)
                {
                    writer = new LameMP3FileWriter(stdout, frm, _bitrate);
                }

                OpenMixerChannel();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            dialog = new SaveFileDialog()
            {
                Title = "Save As",
                Filter = "mp3 files (*.mp3)|*.mp3|All files (*.*)|*.*",
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

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            _channels = (comboBox2.SelectedItem as Item).Id;
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            _bits = (comboBox3.SelectedItem as Item).Id;
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            _rate = (comboBox4.SelectedItem as Item).Id;
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            _bitrate = (comboBox5.SelectedItem as Item).Id;
        }

        private static void OpenMixerChannel()
        {
            //todo frm = new WaveFormat(44100, 32, 2); //probably

            if (mx == null)
            {
                mx = new WasapiLoopbackCapture();
                mx.DataAvailable += new EventHandler<WaveInEventArgs>(SoundChannel_DataAvailable);
                mx.RecordingStopped += new EventHandler<StoppedEventArgs>(SoundChannel_RecordingStopped);
            }

            try
            {
                mx.StartRecording();
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine(exc.ToString());
            }
        }

        public static void SoundChannel_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (mx != null)
            {
                byte[] to16 = new byte[e.BytesRecorded / 2];
                int destOffset = 0;
                try
                {
                    //todo Convert32To16 crashes here reporting 'corrupt memory' my own ConvertIeeeTo16 gives no output?
                    WaveBuffer sourceWaveBuffer = new WaveBuffer(e.Buffer);
                    WaveBuffer destWaveBuffer = new WaveBuffer(to16);
                    int sourceSamples = e.BytesRecorded / 4;

                    for (int sample = 0; sample < sourceSamples; sample++)
                    {
                        // adjust volume
                        float sample32 = sourceWaveBuffer.FloatBuffer[sample];
                        // clip
                        if (sample32 > 1.0f)
                            sample32 = 1.0f;
                        if (sample32 < -1.0f)
                            sample32 = -1.0f;
                        destWaveBuffer.ShortBuffer[destOffset++] = (short)(sample32 * 32767);
                    }
                }
                catch
                {
                }
                if (G722Audio)
                {
                    //todo resample to 4000Hz
                    if (_state == null)
                    {
                        _state = new G722CodecState(_bitrate, G722Flags.Packed);
                    }
                    var wb = new byte[e.BytesRecorded];
                    //todo is this the way to use the codec?
                    count = _codec.Encode(_state, wb, ConvertToShort(to16, to16.Length), to16.Length / 2);
                    stdout.Write(wb, 0, count);
                }
                else if (Mp3Audio)
                {
                    //if (writer == null)
                    //{
                    //    writer = new LameMP3FileWriter(stdout, frm, _bitrate);
                    //}

                    if (writer != null)
                    {
                        writer.Write(to16, 0, destOffset * 2);
                    }

                }
                else
                {
                    //todo convert format
                    stdout.Write(to16, 0, destOffset * 2);
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
    }
}
