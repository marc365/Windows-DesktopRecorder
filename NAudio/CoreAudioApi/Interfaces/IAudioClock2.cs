﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace NAudio.CoreAudioApi.Interfaces
{
    /// <summary>
    /// Defined in AudioClient.h
    /// </summary>
    [Guid("6f49ff73-6727-49AC-A008-D98CF5E70048"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioClock
    {
        [PreserveSig]
        int GetCharacteristics(out uint characteristics);

        [PreserveSig]
        int GetFrequency(out ulong frequency);

        [PreserveSig]
        int GetPosition(out ulong devicePosition, out ulong qpcPosition);
    }

    /// <summary>
    /// Defined in AudioClient.h
    /// </summary>
    [Guid("6f49ff73-6727-49AC-A008-D98CF5E70048"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioClock2 : IAudioClock
    {
        [PreserveSig]
        int GetDevicePosition(out ulong devicePosition, out ulong qpcPosition);
    }
}
