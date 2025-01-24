/*
  from the NAudio library by Mark Heath © 2001-2013 Mark Heath
 * 
 * modified for use in NSox
 */

namespace NAudio
{
    /// <summary>
    /// Wave Callback Strategy
    /// </summary>
    internal enum WaveCallbackStrategy
    {
        /// <summary>
        /// Use a function
        /// </summary>
        FunctionCallback,
        /// <summary>
        /// Create a new window (should only be done if on GUI thread)
        /// </summary>
        NewWindow,
        /// <summary>
        /// Use an existing window handle
        /// </summary>
        ExistingWindow,
        /// <summary>
        /// Use an event handle
        /// </summary>
        Event,
    }
}
