/*--------------------------------------------------------------------------------*
  Copyright (C)Nintendo All rights reserved.

  These coded instructions, statements, and computer programs contain proprietary
  information of Nintendo and/or its licensed developers and are protected by
  national and international copyright laws. They may not be disclosed to third
  parties or copied or duplicated in any form, in whole or in part, without the
  prior written consent of Nintendo.

  The content herein is highly confidential and should be handled accordingly.
 *--------------------------------------------------------------------------------*/

using System.Runtime.InteropServices;

namespace nn.fs
{
    public static partial class SaveData
    {
#if DEVELOPMENT_BUILD || NN_FS_SAVE_DATA_FOR_DEBUG_ENABLE
        [DllImport(Nn.DllName,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "nn_fs_SetSaveDataRootPath")]
        public static extern void SetRootPath(string rootPath);

        [DllImport(Nn.DllName,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "nn_fs_MountSaveDataForDebug")]
        public static extern nn.Result MountForDebug(string name);
#else
        public static void SetRootPath(string rootPath)
        {
        }

        public static nn.Result MountForDebug(string name)
        {
            return new nn.Result();
        }
#endif
    }
}