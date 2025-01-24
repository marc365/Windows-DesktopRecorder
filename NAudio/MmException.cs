/*
  from the NAudio library by Mark Heath © 2001-2013 Mark Heath
 * 
 * modified for use in NSox
 */

using System;

namespace NAudio
{
    /// <summary>
    /// Summary description for MmException.
    /// </summary>
    internal class MmException : Exception
    {
        private MmResult result;
        private string function;

        /// <summary>
        /// Creates a new MmException
        /// </summary>
        /// <param name="result">The result returned by the Windows API call</param>
        /// <param name="function">The name of the Windows API that failed</param>
        internal MmException(MmResult result, string function)
            : base(MmException.ErrorMessage(result, function))
        {
            this.result = result;
            this.function = function;
        }


        private static string ErrorMessage(MmResult result, string function)
        {
            return String.Format("{0} calling {1}", result, function);
        }

        /// <summary>
        /// Helper function to automatically raise an exception on failure
        /// </summary>
        /// <param name="result">The result of the API call</param>
        /// <param name="function">The API function name</param>
        internal static void Try(MmResult result, string function)
        {
            if (result != MmResult.NoError)
                throw new MmException(result, function);
        }

        /// <summary>
        /// Returns the Windows API result
        /// </summary>
        internal MmResult Result
        {
            get
            {
                return result;
            }
        }
    }
}
