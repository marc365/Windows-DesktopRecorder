﻿using System;
using System.Collections.Generic;
using System.Text;

namespace NAudio.Dmo
{
    [Flags]
    enum OutputStreamInfoFlags
    {
        DMO_OUTPUT_STREAMF_WHOLE_SAMPLES = 0x00000001,
        DMO_OUTPUT_STREAMF_SINGLE_SAMPLE_PER_BUFFER = 0x00000002,
        DMO_OUTPUT_STREAMF_FIXED_SAMPLE_SIZE = 0x00000004,
        DMO_OUTPUT_STREAMF_DISCARDABLE = 0x00000008,
        DMO_OUTPUT_STREAMF_OPTIONAL = 0x00000010
    }
}
