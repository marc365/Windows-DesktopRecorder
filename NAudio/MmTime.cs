/*
  from the NAudio library by Mark Heath © 2001-2013 Mark Heath
 * 
 * modified for use in NSox
 */

using System;
using System.Runtime.InteropServices;

namespace NAudio
{
    // http://msdn.microsoft.com/en-us/library/dd757347(v=VS.85).aspx
    [StructLayout(LayoutKind.Explicit)]
    struct MmTime
    {
        internal const int TIME_MS = 0x0001;
        internal const int TIME_SAMPLES = 0x0002;
        internal const int TIME_BYTES = 0x0004;

        [FieldOffset(0)]
        internal UInt32 wType;
        [FieldOffset(4)]
        internal UInt32 ms;
        [FieldOffset(4)]
        internal UInt32 sample;
        [FieldOffset(4)]
        internal UInt32 cb;
        [FieldOffset(4)]
        internal UInt32 ticks;
        [FieldOffset(4)]
        internal Byte smpteHour;
        [FieldOffset(5)]
        internal Byte smpteMin;
        [FieldOffset(6)]
        internal Byte smpteSec;
        [FieldOffset(7)]
        internal Byte smpteFrame;
        [FieldOffset(8)]
        internal Byte smpteFps;
        [FieldOffset(9)]
        internal Byte smpteDummy;
        [FieldOffset(10)]
        internal Byte smptePad0;
        [FieldOffset(11)]
        internal Byte smptePad1;
        [FieldOffset(4)]
        internal UInt32 midiSongPtrPos;
    }
}
