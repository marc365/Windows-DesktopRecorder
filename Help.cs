using System;
using System.Text;
using NAudio.Wave;
using NAudio.Wave.Compression;

namespace NAudio
{
	class Help
	{
        public static void Run()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("   NSox -help (-h)             show this text");
            Console.WriteLine("   NSox -drivers (-d)          list available codecs");
            Console.WriteLine("   NXox -rec                   record to stdout");
            Console.WriteLine("   NSox -play                  play from stdin");
            Console.WriteLine("   NSox <filename>             play .wav .aif .aiff .mp3 file to stdout"); //todo .flac .mp2 .raw 
            Console.WriteLine("   NSox -tone                  play a sin wave");
            Console.WriteLine("   NSox -pink                  play pink noise");
            Console.WriteLine("Options:");
            Console.WriteLine("   -g722                       apply g722 codec");
            Console.WriteLine("   -mp3                        apply mp3 codec");
            Console.WriteLine("   -b                          bits, 8, 16 or 32");
            Console.WriteLine("   -c                          channels, 1 or 2");
            Console.WriteLine("   -r                          hz rate, 4000 - 48000");
            Console.WriteLine("   -br                         kbit/s bitrate, 8 - 320 or 48000, 56000, 64000");
            Console.WriteLine("   -t                          timed output for files");
            Console.WriteLine("   -ms                         (-rec)audio record milliseconds, 10 - 3000");
            Console.WriteLine("   --buffer                    (-play)block encode buffer size 256 - 16384 * 4");
            Console.WriteLine("   -l                          loop play the file");
            Console.WriteLine("   -f                          frequency for the tone generator 1 - 20000");
            Console.WriteLine("Tricks:");
            Console.WriteLine("   NSox -mixer | NSox -play -c 2 -b 32 -r 44100");
            Console.WriteLine("   NSox -rec -mp3 -c 2 -b 16 -r 44100 -br 128 > recording.mp3");
            Console.WriteLine("   NSox -rec -g722 | NSox -play -g722");
        }

        public static void Drivers()
        {

            foreach (var driver in AcmDriver.EnumerateAcmDrivers())
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendFormat("Long Name: {0}\r\n", driver.LongName);
                builder.AppendFormat("Short Name: {0}\r\n", driver.ShortName);
                builder.AppendFormat("Driver ID: {0}\r\n", driver.DriverId);
                driver.Open();
                builder.AppendFormat("FormatTags:\r\n");
                foreach (AcmFormatTag formatTag in driver.FormatTags)
                {
                    builder.AppendFormat("===========================================\r\n");
                    builder.AppendFormat("Format Tag {0}: {1}\r\n", formatTag.FormatTagIndex, formatTag.FormatDescription);
                    builder.AppendFormat("   Standard Format Count: {0}\r\n", formatTag.StandardFormatsCount);
                    builder.AppendFormat("   Support Flags: {0}\r\n", formatTag.SupportFlags);
                    builder.AppendFormat("   Format Tag: {0}, Format Size: {1}\r\n", formatTag.FormatTag, formatTag.FormatSize);
                    builder.AppendFormat("   Formats:\r\n");
                    foreach (AcmFormat format in driver.GetFormats(formatTag))
                    {
                        builder.AppendFormat("   ===========================================\r\n");
                        builder.AppendFormat("   Format {0}: {1}\r\n", format.FormatIndex, format.FormatDescription);
                        builder.AppendFormat("      FormatTag: {0}, Support Flags: {1}\r\n", format.FormatTag, format.SupportFlags);
                        builder.AppendFormat("      WaveFormat: {0} {1}Hz Channels: {2} Bits: {3} Block Align: {4}, AverageBytesPerSecond: {5} ({6:0.0} kbps), Extra Size: {7}\r\n",
                            format.WaveFormat.Encoding, format.WaveFormat.SampleRate, format.WaveFormat.Channels,
                            format.WaveFormat.BitsPerSample, format.WaveFormat.BlockAlign,
                             format.WaveFormat.AverageBytesPerSecond,
                            (format.WaveFormat.AverageBytesPerSecond * 8) / 1000.0,
                            format.WaveFormat.ExtraSize);
                        if (format.WaveFormat is WaveFormatExtraData && format.WaveFormat.ExtraSize > 0)
                        {
                            WaveFormatExtraData wfed = (WaveFormatExtraData)format.WaveFormat;
                            builder.Append("      Extra Bytes:\r\n      ");
                            for (int n = 0; n < format.WaveFormat.ExtraSize; n++)
                            {
                                builder.AppendFormat("{0:X2} ", wfed.ExtraData[n]);
                            }
                            builder.Append("\r\n");
                        }
                    }
                }
                driver.Close();
                Console.WriteLine(builder.ToString());
            }
            System.Environment.Exit(0);
        }


    }
}
