﻿/*
  from the NAudio library by Mark Heath © 2001-2013
 * 
 * modified for use in NSox
 */

using System;

namespace NAudio
{
    /// <summary>
    /// Provides a buffered store of samples
    /// Read method will return queued samples or fill buffer with zeroes
    /// Now backed by a circular buffer
    /// </summary>
    public class BufferedWaveProvider : IWaveProvider
    {
        private CircularBuffer circularBuffer;
        private readonly WaveFormat waveFormat;

        /// <summary>
        /// Creates a new buffered WaveProvider
        /// </summary>
        /// <param name="waveFormat">WaveFormat</param>
        internal BufferedWaveProvider(WaveFormat waveFormat)
        {
            this.waveFormat = waveFormat;
            this.BufferLength = waveFormat.AverageBytesPerSecond * 2;
        }

        /// <summary>
        /// Buffer length in bytes
        /// </summary>
        internal int BufferLength { get; set; }

        /// <summary>
        /// Buffer duration
        /// </summary>
        internal TimeSpan BufferDuration
        {
            get
            {
                return TimeSpan.FromSeconds((double)BufferLength / WaveFormat.AverageBytesPerSecond);
            }
            set
            {
                BufferLength = (int)(value.TotalSeconds * WaveFormat.AverageBytesPerSecond);
            }
        }

        /// <summary>
        /// If true, when the buffer is full, start throwing away data
        /// if false, AddSamples will throw an exception when buffer is full
        /// </summary>
        internal bool DiscardOnBufferOverflow { get; set; }

        /// <summary>
        /// The number of buffered bytes
        /// </summary>
        internal int BufferedBytes
        {
            get
            {
                return circularBuffer == null ? 0 : circularBuffer.Count;
            }
        }

        /// <summary>
        /// Buffered Duration
        /// </summary>
        internal TimeSpan BufferedDuration
        {
            get { return TimeSpan.FromSeconds((double)BufferedBytes / WaveFormat.AverageBytesPerSecond); }
        }

        /// <summary>
        /// Gets the WaveFormat
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        /// <summary>
        /// Adds samples. Takes a copy of buffer, so that buffer can be reused if necessary
        /// </summary>
        internal void AddSamples(byte[] buffer, int offset, int count)
        {
            // create buffer here to allow user to customise buffer length
            if (circularBuffer == null)
            {
                circularBuffer = new CircularBuffer(this.BufferLength);
            }

            var written = circularBuffer.Write(buffer, offset, count);
            if (written < count && !DiscardOnBufferOverflow)
            {
                throw new InvalidOperationException("Buffer full");
            }
        }

        /// <summary>
        /// Reads from this WaveProvider
        /// Will always return count bytes, since we will zero-fill the buffer if not enough available
        /// </summary>
        public int Read(byte[] buffer, int offset, int count)
        {
            int read = 0;
            if (circularBuffer != null) // not yet created
            {
                read = this.circularBuffer.Read(buffer, offset, count);
            }
            if (read < count)
            {
                // zero the end of the buffer
                Array.Clear(buffer, offset + read, count - read);
            }
            return count;
        }

        /// <summary>
        /// Discards all audio from the buffer
        /// </summary>
        internal void ClearBuffer()
        {
            if (circularBuffer != null)
            {
                circularBuffer.Reset();
            }
        }
    }
}
