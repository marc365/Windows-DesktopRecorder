/*
  from the NAudio library by Mark Heath © 2001-2013 Mark Heath
 * 
 * modified for use in NSox
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio
{
    /// <summary>
    /// Wave Callback Info
    /// </summary>
    internal class WaveCallbackInfo
    {
        /// <summary>
        /// Callback Strategy
        /// </summary>
        internal WaveCallbackStrategy Strategy { get; private set; }
        /// <summary>
        /// Window Handle (if applicable)
        /// </summary>
        internal IntPtr Handle { get; private set; }

        /// <summary>
        /// Sets up a new WaveCallbackInfo for function callbacks
        /// </summary>
        internal static WaveCallbackInfo FunctionCallback()
        {
            return new WaveCallbackInfo(WaveCallbackStrategy.FunctionCallback, IntPtr.Zero);
        }

        /// <summary>
        /// Sets up a new WaveCallbackInfo to use a New Window
        /// IMPORTANT: only use this on the GUI thread
        /// </summary>
        internal static WaveCallbackInfo NewWindow()
        {
            return new WaveCallbackInfo(WaveCallbackStrategy.NewWindow, IntPtr.Zero);
        }

        /// <summary>
        /// Sets up a new WaveCallbackInfo to use an existing window
        /// IMPORTANT: only use this on the GUI thread
        /// </summary>
        internal static WaveCallbackInfo ExistingWindow(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new ArgumentException("Handle cannot be zero");
            }
            return new WaveCallbackInfo(WaveCallbackStrategy.ExistingWindow, handle);
        }

        private WaveCallbackInfo(WaveCallbackStrategy strategy, IntPtr handle)
        {
            this.Strategy = strategy;
            this.Handle = handle;
        }

        internal MmResult WaveOutOpen(out IntPtr waveOutHandle, int deviceNumber, WaveFormat waveFormat, WaveInterop.WaveCallback callback)
        {
            MmResult result;
            if (Strategy == WaveCallbackStrategy.FunctionCallback)
            {
                result = WaveInterop.waveOutOpen(out waveOutHandle, (IntPtr)deviceNumber, waveFormat, callback, IntPtr.Zero, WaveInterop.WaveInOutOpenFlags.CallbackFunction);
            }
            else
            {
                result = WaveInterop.waveOutOpenWindow(out waveOutHandle, (IntPtr)deviceNumber, waveFormat, this.Handle, IntPtr.Zero, WaveInterop.WaveInOutOpenFlags.CallbackWindow);
            }
            return result;
        }

        internal MmResult WaveInOpen(out IntPtr waveInHandle, int deviceNumber, WaveFormat waveFormat, WaveInterop.WaveCallback callback)
        {
            MmResult result;
            if (Strategy == WaveCallbackStrategy.FunctionCallback)
            {
                result = WaveInterop.waveInOpen(out waveInHandle, (IntPtr)deviceNumber, waveFormat, callback, IntPtr.Zero, WaveInterop.WaveInOutOpenFlags.CallbackFunction);
            }
            else
            {
                result = WaveInterop.waveInOpenWindow(out waveInHandle, (IntPtr)deviceNumber, waveFormat, this.Handle, IntPtr.Zero, WaveInterop.WaveInOutOpenFlags.CallbackWindow);
            }
            return result;
        }
    }
}
