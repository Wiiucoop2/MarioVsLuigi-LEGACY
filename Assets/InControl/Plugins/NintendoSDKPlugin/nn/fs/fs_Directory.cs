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

namespace nn.fs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DirectoryHandle
    {
        public IntPtr handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DirectoryEntry
    {
        public EntryType entryType
        {
            get { return (EntryType)_entryType; }
            set { _entryType = (sbyte)value; }
        }

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Directory.EntryNameLengthMax + 1)]
        public string name;
        byte _reserved0;
        byte _reserved1;
        byte _reserved2;
        sbyte _entryType;
        byte _reserved3;
        byte _reserved4;
        byte _reserved5;
        long fileSize;

        public override string ToString()
        {
            if (entryType == EntryType.Directory)
            {
                return String.Format("{0}/", name);
            }
            else
            {
                return String.Format("{0} : {1}", name, fileSize);
            }
        }
    }

    public static partial class Directory
    {
        public const int EntryNameLengthMax = 768;

        [DllImport(Nn.DllName,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "nn_fs_ReadDirectory")]
        public static extern nn.Result Read(
            ref long outValue, [Out] DirectoryEntry[] entryBuffer, DirectoryHandle handle, long entryBufferCount);

        [DllImport(Nn.DllName,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "nn_fs_GetDirectoryEntryCount")]
        public static extern nn.Result GetEntryCount(ref long outValue, DirectoryHandle handle);

        [DllImport(Nn.DllName,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "nn_fs_CloseDirectory")]
        public static extern void Close(DirectoryHandle handle);
    }
}