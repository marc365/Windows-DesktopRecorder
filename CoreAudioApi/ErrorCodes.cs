/*
  LICENSE
  -------
  Copyright (C) 2007 Ray Molenkamp

  This source code is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this source code or the software it produces.

  Permission is granted to anyone to use this source code for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this source code must not be misrepresented; you must not
     claim that you wrote the original source code.  If you use this source code
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original source code.
  3. This notice may not be removed or altered from any source distribution.
*/
// modified for NAudio

using NAudio;

namespace CoreAudioApi
{
    enum AudioClientErrors
    {
        /// <summary>
        /// AUDCLNT_E_NOT_INITIALIZED
        /// </summary>
        NotInitialized = unchecked((int)0x88890001),
        /// <summary>
        /// AUDCLNT_E_UNSUPPORTED_FORMAT
        /// </summary>
        UnsupportedFormat = unchecked((int)0x88890008),
        /// <summary>
        /// AUDCLNT_E_DEVICE_IN_USE
        /// </summary>
        DeviceInUse = unchecked((int)0x8889000A),

    }

    static class ErrorCodes
    {
        // AUDCLNT_ERR(n) MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, n)
        // AUDCLNT_SUCCESS(n) MAKE_SCODE(SEVERITY_SUCCESS, FACILITY_AUDCLNT, n)
        const int SEVERITY_ERROR = 1;
        const int FACILITY_AUDCLNT = 0x889;
        static readonly int AUDCLNT_E_NOT_INITIALIZED = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x001);
        static readonly int AUDCLNT_E_ALREADY_INITIALIZED = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x002);
        static readonly int AUDCLNT_E_WRONG_ENDPOINT_TYPE = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x003);
        static readonly int AUDCLNT_E_DEVICE_INVALIDATED = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x004);
        static readonly int AUDCLNT_E_NOT_STOPPED = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x005);
        static readonly int AUDCLNT_E_BUFFER_TOO_LARGE = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x006);
        static readonly int AUDCLNT_E_OUT_OF_ORDER = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x007);
        static readonly int AUDCLNT_E_UNSUPPORTED_FORMAT = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x008);
        static readonly int AUDCLNT_E_INVALID_SIZE = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x009);
        static readonly int AUDCLNT_E_DEVICE_IN_USE = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x00A);
        static readonly int AUDCLNT_E_BUFFER_OPERATION_PENDING = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x00B);
        static readonly int AUDCLNT_E_THREAD_NOT_REGISTERED = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x00C);
        static readonly int AUDCLNT_E_EXCLUSIVE_MODE_NOT_ALLOWED = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x00E);
        static readonly int AUDCLNT_E_ENDPOINT_CREATE_FAILED = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x00F);
        static readonly int AUDCLNT_E_SERVICE_NOT_RUNNING = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x010);
        static readonly int AUDCLNT_E_EVENTHANDLE_NOT_EXPECTED = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x011);
        static readonly int AUDCLNT_E_EXCLUSIVE_MODE_ONLY = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x0012);
        static readonly int AUDCLNT_E_BUFDURATION_PERIOD_NOT_EQUAL = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x013);
        static readonly int AUDCLNT_E_EVENTHANDLE_NOT_SET = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x014);
        static readonly int AUDCLNT_E_INCORRECT_BUFFER_SIZE = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x015);
        static readonly int AUDCLNT_E_BUFFER_SIZE_ERROR = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x016);
        static readonly int AUDCLNT_E_CPUUSAGE_EXCEEDED = HResult.MAKE_HRESULT(SEVERITY_ERROR, FACILITY_AUDCLNT, 0x017);
        /*static readonly int AUDCLNT_S_BUFFER_EMPTY              AUDCLNT_SUCCESS(0x001)
        static readonly int AUDCLNT_S_THREAD_ALREADY_REGISTERED AUDCLNT_SUCCESS(0x002)
        static readonly int AUDCLNT_S_POSITION_STALLED		   AUDCLNT_SUCCESS(0x003)*/
    }
}
