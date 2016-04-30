using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace PictureManager.ShellStuff {
  public static class FileInformation {
    internal static object GetFileIdInfo(string filePath) {
      var handle = CreateFile(filePath, FileAccess.Read, FileShare.Read, IntPtr.Zero, FileMode.Open, 0x02000000 | 0x00000080, IntPtr.Zero);
      var fileStruct = new FILE_ID_INFO();
      GetFileInformationByHandleEx(handle, FILE_INFO_BY_HANDLE_CLASS.FileIdInfo, out fileStruct, (uint)Marshal.SizeOf(fileStruct));
      CloseHandle(handle);
      var win32Error = Marshal.GetLastWin32Error();
      if (win32Error != 0)
        throw new Win32Exception();

      return fileStruct;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandleEx(
        IntPtr hFile,
        FILE_INFO_BY_HANDLE_CLASS fileInformationClass,
        out FILE_ID_INFO lpFileInformation,
        uint dwBufferSize);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CloseHandle(
        IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CreateFile(
        string lpFileName,
        [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
        [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
        IntPtr lpSecurityAttributes,
        [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILE_ID_INFO {
      public ulong VolumeSerialNumber { get; }
      public FileId128 FileId { get; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FileId128 {
      /*public ulong HighPart { get; }
      public ulong LowPart { get; }*/
      public byte[] Identifier { get; }
    }

    private enum FILE_INFO_BY_HANDLE_CLASS {
      FileIdInfo = 18 // 0x12
    }

  }
}
