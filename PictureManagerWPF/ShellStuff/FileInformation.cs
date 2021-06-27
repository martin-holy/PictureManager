using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace PictureManager.ShellStuff {
  public static class FileInformation {
    internal static object[] GetVideoMetadata(string dirPath, string fileName) {
      var shl = new Shell32.Shell();
      var fldr = shl.NameSpace(dirPath);
      var itm = fldr.ParseName(fileName);
      // INFO I am not sure, but it looks like that the iColumn numbers are not the same all the time
      string[] data = {
        fldr.GetDetailsOf(itm, 314), // height
        fldr.GetDetailsOf(itm, 316), // width
        fldr.GetDetailsOf(itm, 319), // orientation
        fldr.GetDetailsOf(itm, 315)  // FPS
      };
      int.TryParse(data[0], out var h);
      int.TryParse(data[1], out var w);
      int.TryParse(data[2], out var o);
      data[3].TryParseDoubleUniversal(out var fps);

      return new object[] { h, w, o, fps };
    }

    internal static List<string> GetAllVideoMetadata(string filePath) {
      var shl = new Shell32.Shell();
      var fldr = shl.NameSpace(Path.GetDirectoryName(filePath));
      var itm = fldr.ParseName(Path.GetFileName(filePath));
      var headers = new Dictionary<short, string>();
      var output = new List<string>();

      for (short i = 0; i < short.MaxValue; i++) {
        var header = fldr.GetDetailsOf(null, i);
        if (string.IsNullOrEmpty(header)) continue;
        headers.Add(i, header);
      }

      foreach (var header in headers) {
        output.Add($"{header.Key} => {header.Value}: >{fldr.GetDetailsOf(itm, header.Key)}<");
      }

      return output;
    }

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
