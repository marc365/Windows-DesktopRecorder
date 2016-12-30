// created on 10/12/2002 at 23:58
using System;
using System.Runtime.InteropServices;

namespace NAudio.Mixer 
{
	/// <summary>
	/// Boolean mixer control
	/// </summary>
	public class BooleanMixerControl : MixerControl 
	{
		private MixerInterop.MIXERCONTROLDETAILS_BOOLEAN boolDetails;

        internal BooleanMixerControl(MixerInterop.MIXERCONTROL mixerControl, IntPtr mixerHandle, MixerFlags mixerHandleType, int nChannels) 
		{
			this.mixerControl = mixerControl;
            this.mixerHandle = mixerHandle;
            this.mixerHandleType = mixerHandleType;
			this.nChannels = nChannels;
			this.mixerControlDetails = new MixerInterop.MIXERCONTROLDETAILS();
			
			GetControlDetails();

		}
		
		/// <summary>
		/// Gets the details for this control
		/// </summary>
		/// <param name="pDetails">memory pointer</param>
		protected override void GetDetails(IntPtr pDetails) 
		{
			boolDetails = (MixerInterop.MIXERCONTROLDETAILS_BOOLEAN) Marshal.PtrToStructure(pDetails,typeof(MixerInterop.MIXERCONTROLDETAILS_BOOLEAN));
		}
		
		/// <summary>
		/// The current value of the control
		/// </summary>
		public bool Value 
		{
			get 
			{
				GetControlDetails(); // make sure we have the latest value
				return (boolDetails.fValue == 1);
			}
			set 
			{
				//GetControlDetails();
				//MixerInterop.MIXERCONTROLDETAILS_BOOLEAN boolDetails = (MixerInterop.MIXERCONTROLDETAILS_BOOLEAN) Marshal.PtrToStructure(mixerControlDetails.paDetails,typeof(MixerInterop.MIXERCONTROLDETAILS_BOOLEAN));
				//boolDetails.fValue = (value) ? 1 : 0;
				// TODO: pin the memory
                MmException.Try(MixerInterop.mixerSetControlDetails(mixerHandle, ref mixerControlDetails, MixerFlags.Value | mixerHandleType), "mixerSetControlDetails");
			}
		}		
	}
}
