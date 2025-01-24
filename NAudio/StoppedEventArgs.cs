/*
  from the NAudio library by Mark Heath © 2001-2013 Mark Heath
 * 
 * modified for use in NSox
 */

using System;

namespace NAudio
{
    /// <summary>
    /// Stopped Event Args
    /// </summary>
    public class StoppedEventArgs : EventArgs
    {
        private readonly Exception exception;

        /// <summary>
        /// Initializes a new instance of StoppedEventArgs
        /// </summary>
        /// <param name="exception">An exception to report (null if no exception)</param>
        internal StoppedEventArgs(Exception exception = null)
        {
            this.exception = exception;
        }

        /// <summary>
        /// An exception. Will be null if the playback or record operation stopped
        /// </summary>
        internal Exception Exception { get { return exception; } }
    }
}
