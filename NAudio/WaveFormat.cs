/*
  from the NAudio library by Mark Heath © 2001-2013 Mark Heath
 * 
 * modified for use in NSox
 */

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NAudio
{
    /// <summary>
    /// Represents a Wave file format
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
    public class WaveFormat
    {
        /// <summary>format type</summary>
        protected WaveFormatEncoding waveFormatTag;
        /// <summary>number of channels</summary>
        protected short channels;
        /// <summary>sample rate</summary>
        protected int sampleRate;
        /// <summary>for buffer estimation</summary>
        protected int averageBytesPerSecond;
        /// <summary>block size of data</summary>
        protected short blockAlign;
        /// <summary>number of bits per sample of mono data</summary>
        protected short bitsPerSample;
        /// <summary>number of following bytes</summary>
        protected short extraSize;

        /// <summary>
        /// Creates a new PCM 44.1Khz stereo 16 bit format
        /// </summary>
        internal WaveFormat()
            : this(44100, 16, 2)
        {

        }

        /// <summary>
        /// Gets the size of a wave buffer equivalent to the latency in milliseconds.
        /// </summary>
        /// <param name="milliseconds">The milliseconds.</param>
        /// <returns></returns>
        internal int ConvertLatencyToByteSize(int milliseconds)
        {
            int bytes = (int)((AverageBytesPerSecond / 1000.0) * milliseconds);
            if ((bytes % BlockAlign) != 0)
            {
                // Return the upper BlockAligned
                bytes = bytes + BlockAlign - (bytes % BlockAlign);
            }
            return bytes;
        }

        /// <summary>
        /// Creates a new PCM format with the specified sample rate, bit depth and channels
        /// </summary>
        internal WaveFormat(int rate, int bits, int channels)
        {
            if (channels < 1)
            {
                throw new ArgumentOutOfRangeException("Channels must be 1 or greater", "channels");
            }            
            if (bits == 32) this.waveFormatTag = WaveFormatEncoding.IeeeFloat;
            // minimum 16 bytes, sometimes 18 for PCM
            else this.waveFormatTag = WaveFormatEncoding.Pcm;
            this.channels = (short)channels;
            this.sampleRate = rate;
            this.bitsPerSample = (short)bits;
            this.extraSize = 0;

            this.blockAlign = (short)(channels * (bits / 8));
            this.averageBytesPerSecond = this.sampleRate * this.blockAlign;
        }

        /// <summary>
        /// Creates a new 32 bit IEEE floating point wave format
        /// </summary>
        /// <param name="sampleRate">sample rate</param>
        /// <param name="channels">number of channels</param>
        public static WaveFormat CreateIeeeFloatWaveFormat(int sampleRate, int channels)
        {
            WaveFormat wf = new WaveFormat();
            wf.waveFormatTag = WaveFormatEncoding.IeeeFloat;
            wf.channels = (short)channels;
            wf.bitsPerSample = 32;
            wf.sampleRate = sampleRate;
            wf.blockAlign = (short)(4 * channels);
            wf.averageBytesPerSecond = sampleRate * wf.blockAlign;
            wf.extraSize = 0;
            return wf;
        }

        /// <summary>
        /// Helper function to retrieve a WaveFormat structure from a pointer
        /// </summary>
        /// <param name="pointer">WaveFormat structure</param>
        /// <returns></returns>
        public static WaveFormat MarshalFromPtr(IntPtr pointer)
        {
            WaveFormat waveFormat = (WaveFormat)Marshal.PtrToStructure(pointer, typeof(WaveFormat));
            switch (waveFormat.Encoding)
            {
                case WaveFormatEncoding.Pcm:
                    // can't rely on extra size even being there for PCM so blank it to avoid reading
                    // corrupt data
                    waveFormat.extraSize = 0;
                    break;
                case WaveFormatEncoding.Extensible:
                    waveFormat = (WaveFormatExtensible)Marshal.PtrToStructure(pointer, typeof(WaveFormatExtensible));
                    break;
                //case WaveFormatEncoding.Adpcm:
                //    waveFormat = (AdpcmWaveFormat)Marshal.PtrToStructure(pointer, typeof(AdpcmWaveFormat));
                //    break;
                //case WaveFormatEncoding.Gsm610:
                //    waveFormat = (Gsm610WaveFormat)Marshal.PtrToStructure(pointer, typeof(Gsm610WaveFormat));
                //    break;
                default:
                    if (waveFormat.ExtraSize > 0)
                    {
                        waveFormat = (WaveFormatExtraData)Marshal.PtrToStructure(pointer, typeof(WaveFormatExtraData));
                    }
                    break;
            }
            return waveFormat;
        }

        ///// <summary>
        ///// Reads in a WaveFormat (with extra data) from a fmt chunk (chunk identifier and
        ///// length should already have been read)
        ///// </summary>
        ///// <param name="br">Binary reader</param>
        ///// <param name="formatChunkLength">Format chunk length</param>
        ///// <returns>A WaveFormatExtraData</returns>
        //public static WaveFormat FromFormatChunk(BinaryReader br, int formatChunkLength)
        //{
        //    WaveFormatExtraData waveFormat = new WaveFormatExtraData();
        //    waveFormat.ReadWaveFormat(br, formatChunkLength);
        //    waveFormat.ReadExtraData(br);
        //    return waveFormat;
        //}

        //private void ReadWaveFormat(BinaryReader br, int formatChunkLength)
        //{
        //    if (formatChunkLength < 16)
        //        throw new InvalidDataException("Invalid WaveFormat Structure");
        //    this.waveFormatTag = (WaveFormatEncoding)br.ReadUInt16();
        //    this.channels = br.ReadInt16();
        //    this.sampleRate = br.ReadInt32();
        //    this.averageBytesPerSecond = br.ReadInt32();
        //    this.blockAlign = br.ReadInt16();
        //    this.bitsPerSample = br.ReadInt16();
        //    if (formatChunkLength > 16)
        //    {
        //        this.extraSize = br.ReadInt16();
        //        if (this.extraSize != formatChunkLength - 18)
        //        {
        //            Help.WriteLine("Format chunk mismatch");
        //            this.extraSize = (short)(formatChunkLength - 18);
        //        }
        //    }
        //}

        ///// <summary>
        ///// Reads a new WaveFormat object from a stream
        ///// </summary>
        ///// <param name="br">A binary reader that wraps the stream</param>
        //public WaveFormat(BinaryReader br)
        //{
        //    int formatChunkLength = br.ReadInt32();
        //    this.ReadWaveFormat(br, formatChunkLength);
        //}

        /// <summary>
        /// Reports this WaveFormat as a string
        /// </summary>
        /// <returns>String describing the wave format</returns>
        public override string ToString()
        {
            switch (this.waveFormatTag)
            {
                case WaveFormatEncoding.Pcm:
                    return String.Format("{0} bit PCM: {1}kHz {2} channels",
                        bitsPerSample, sampleRate / 1000, channels);
                case WaveFormatEncoding.IeeeFloat:
                    return String.Format("{0} bit Float: {1}kHz {2} channels",
                        bitsPerSample, sampleRate / 1000, channels);
                default:
                    return this.waveFormatTag.ToString();
            }
        }
        
        /// <summary>
        /// Returns the encoding type used
        /// </summary>
        internal WaveFormatEncoding Encoding
        {
            get
            {
                return waveFormatTag;
            }
        }

        /// <summary>
        /// Returns the number of channels (1=mono,2=stereo etc)
        /// </summary>
        internal int Channels
        {
            get
            {
                return channels;
            }
        }

        /// <summary>
        /// Returns the sample rate (samples per second)
        /// </summary>
        internal int SampleRate
        {
            get
            {
                return sampleRate;
            }
        }

        /// <summary>
        /// Returns the average number of bytes used per second
        /// </summary>
        internal int AverageBytesPerSecond
        {
            get
            {
                return averageBytesPerSecond;
            }
        }

        /// <summary>
        /// Returns the block alignment
        /// </summary>
        internal virtual int BlockAlign
        {
            get
            {
                return blockAlign;
            }
        }

        /// <summary>
        /// Returns the number of bits per sample (usually 16 or 32, sometimes 24 or 8)
        /// Can be 0 for some codecs
        /// </summary>
        internal int BitsPerSample
        {
            get
            {
                return bitsPerSample;
            }
        }

        /// <summary>
        /// Returns the number of extra bytes used by this waveformat. Often 0,
        /// except for compressed formats which store extra data after the WAVEFORMATEX header
        /// </summary>
        internal int ExtraSize
        {
            get
            {
                return extraSize;
            }
        }
    }
}
