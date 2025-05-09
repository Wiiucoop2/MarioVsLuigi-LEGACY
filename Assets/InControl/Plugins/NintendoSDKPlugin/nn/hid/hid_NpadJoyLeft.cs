/*--------------------------------------------------------------------------------*
  Copyright (C)Nintendo All rights reserved.

  These coded instructions, statements, and computer programs contain proprietary
  information of Nintendo and/or its licensed developers and are protected by
  national and international copyright laws. They may not be disclosed to third
  parties or copied or duplicated in any form, in whole or in part, without the
  prior written consent of Nintendo.

  The content herein is highly confidential and should be handled accordingly.
 *--------------------------------------------------------------------------------*/

using System;
using System.Runtime.InteropServices;

namespace nn.hid
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NpadJoyLeftState
    {
        public long samplingNumber;
        public NpadButton buttons;
        public AnalogStickState analogStickL;
        public AnalogStickState analogStickR;
        public NpadAttribute attributes;

        public override string ToString()
        {
            return String.Format("L{0} R{1} [{2}] {3} {4}",
                this.analogStickL, this.analogStickR, this.buttons, this.attributes, this.samplingNumber);
        }
    }

    public static class NpadJoyLeft
    {
        [DllImport(Nn.DllName,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "nn_hid_GetNpadJoyLeftState")]
        public static extern void GetState(ref NpadJoyLeftState pOutValue, NpadId npadId);

        [DllImport(Nn.DllName,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "nn_hid_GetNpadJoyLeftState")]
        public static extern void GetState(ref NpadState pOutValue, NpadId npadId);

        [DllImport(Nn.DllName,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "nn_hid_GetNpadJoyLeftStates")]
        public static extern int GetStates([Out] NpadJoyLeftState[] pOutValues, int count, NpadId npadId);

        [DllImport(Nn.DllName,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "nn_hid_GetNpadJoyLeftStates")]
        public static extern int GetStates([Out] NpadStateArrayItem[] pOutValues, int count, NpadId npadId);
    }
}