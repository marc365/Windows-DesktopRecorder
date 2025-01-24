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
    /// WaveOutCapabilities structure (based on WAVEOUTCAPS2 from mmsystem.h)
    /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/multimed/htm/_win32_waveoutcaps_str.asp
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct WaveOutCapabilities
    {
        /// <summary>
        /// wMid
        /// </summary>
        private short manufacturerId;
        /// <summary>
        /// wPid
        /// </summary>
        private short productId;
        /// <summary>
        /// vDriverVersion
        /// </summary>
        private int driverVersion;
        /// <summary>
        /// Product Name (szPname)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxProductNameLength)]
        private string productName;
        /// <summary>
        /// Supported formats (bit flags) dwFormats 
        /// </summary>
        private int supportedFormats;
        /// <summary>
        /// Supported channels (1 for mono 2 for stereo) (wChannels)
        /// Seems to be set to -1 on a lot of devices
        /// </summary>
        private short channels;
        /// <summary>
        /// wReserved1
        /// </summary>
        private short reserved;
        /// <summary>
        /// Optional functionality supported by the device
        /// </summary>
        private WaveOutSupport support;

        // extra WAVEOUTCAPS2 members
        private Guid manufacturerGuid;
        private Guid productGuid;
        private Guid nameGuid;

        private const int MaxProductNameLength = 32;

        /// <summary>
        /// Number of channels supported
        /// </summary>
        internal int Channels
        {
            get
            {
                return channels;
            }
        }

        /// <summary>
        /// Whether playback control is supported
        /// </summary>
        internal bool SupportsPlaybackRateControl
        {
            get
            {
                return (support & WaveOutSupport.PlaybackRate) == WaveOutSupport.PlaybackRate;
            }
        }

        /// <summary>
        /// The product name
        /// </summary>
        internal string ProductName
        {
            get
            {
                return productName;
            }
        }

        /// <summary>
        /// Checks to see if a given SupportedWaveFormat is supported
        /// </summary>
        /// <param name="waveFormat">The SupportedWaveFormat</param>
        /// <returns>true if supported</returns>
        //internal bool SupportsWaveFormat(SupportedWaveFormat waveFormat)
        //{
        //    return (supportedFormats & waveFormat) == waveFormat;
        //}

        /// <summary>
        /// The device name Guid (if provided)
        /// </summary>
        internal Guid NameGuid { get { return nameGuid; } }
        /// <summary>
        /// The product name Guid (if provided)
        /// </summary>
        internal Guid ProductGuid { get { return productGuid; } }
        /// <summary>
        /// The manufacturer guid (if provided)
        /// </summary>
        internal Guid ManufacturerGuid { get { return manufacturerGuid; } }
    }

    /// <summary>
    /// Supported wave formats for WaveOutCapabilities
    /// </summary>
    [Flags]
    internal enum SupportedWaveFormat
    {
        UNKNOWN = 0x00000000        

    }
}
