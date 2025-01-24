/*
  from the NAudio library by Mark Heath © 2001-2013 Mark Heath
 * 
 * modified for use in NSox
 */

using System;
using System.Runtime.InteropServices;

namespace NAudio
{
    /// <summary>
    /// WaveHeader interop structure (WAVEHDR)
    /// http://msdn.microsoft.com/en-us/library/dd743837%28VS.85%29.aspx
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    class WaveHeader
    {
        /// <summary>pointer to locked data buffer (lpData)</summary>
        internal IntPtr dataBuffer;
        /// <summary>length of data buffer (dwBufferLength)</summary>
        internal int bufferLength;
        /// <summary>used for input only (dwBytesRecorded)</summary>
        internal int bytesRecorded;
        /// <summary>for client's use (dwUser)</summary>
        internal IntPtr userData;
        /// <summary>assorted flags (dwFlags)</summary>
        internal WaveHeaderFlags flags;
        /// <summary>loop control counter (dwLoops)</summary>
        internal int loops;
        /// <summary>PWaveHdr, reserved for driver (lpNext)</summary>
        internal IntPtr next;
        /// <summary>reserved for driver</summary>
        internal IntPtr reserved;
    }
}
