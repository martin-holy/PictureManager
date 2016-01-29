using System;
using System.Runtime.InteropServices;

namespace PictureManager.ShellStuff.Interfaces {
  /// <summary>
  /// 
  /// </summary>
  [ComImport, Guid("00000000-0000-0000-C000-000000000046")]
  [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  internal interface IUnknown {
    [PreserveSig]
    IntPtr QueryInterface(ref Guid riid, out IntPtr pVoid);

    [PreserveSig]
    IntPtr AddRef();

    [PreserveSig]
    IntPtr Release();
  }
}