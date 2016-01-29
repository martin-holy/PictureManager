using System;
using System.Runtime.InteropServices;

namespace PictureManager.ShellStuff {
  [Flags]
  internal enum ESFGAO {
    /// <summary>
    /// 
    /// </summary>
    SFGAO_CANCOPY = 1,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_CANMOVE = 2,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_CANLINK = 4,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_CANRENAME = 16,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_CANDELETE = 32,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_HASPROPSHEET = 64,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_DROPTARGET = 256,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_CAPABILITYMASK = 375,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_LINK = 65536,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_SHARE = 131072,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_READONLY = 262144,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_GHOSTED = 524288,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_DISPLAYATTRMASK = 983040,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_FILESYSANCESTOR = 268435456,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_FOLDER = 536870912,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_FILESYSTEM = 1073741824,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_HASSUBFOLDER = -2147483648,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_CONTENTSMASK = -2147483648,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_VALIDATE = 16777216,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_REMOVABLE = 33554432,
    /// <summary>
    /// 
    /// </summary>
    SFGAO_COMPRESSED = 67108864
  }

  [Flags]
  internal enum ESHCONTF {
    /// <summary>
    /// 
    /// </summary>
    SHCONTF_FOLDERS = 32,
    /// <summary>
    /// 
    /// </summary>
    SHCONTF_NONFOLDERS = 64,
    /// <summary>
    /// 
    /// </summary>
    SHCONTF_INCLUDEHIDDEN = 128
  }

  [Flags]
  internal enum ESHGDN {
    /// <summary>
    /// 
    /// </summary>
    SHGDN_NORMAL = 0,
    /// <summary>
    /// 
    /// </summary>
    SHGDN_INFOLDER = 1,
    /// <summary>
    /// 
    /// </summary>
    SHGDN_FORADDRESSBAR = 16384,
    /// <summary>
    /// 
    /// </summary>
    SHGDN_FORPARSING = 32768
  }

  [Flags]
  internal enum ESTRRET {
    /// <summary>
    /// 
    /// </summary>
    STRRET_WSTR = 0x0000,         // Use STRRET.pOleStr
                                  /// <summary>
                                  /// 
                                  /// </summary>
    STRRET_OFFSET = 0x0001,         // Use STRRET.uOffset to Ansi
                                    /// <summary>
                                    /// 
                                    /// </summary>
    STRRET_CSTR = 0x0002         // Use STRRET.cStr
  }

  [Flags]
  public enum IEIFLAG {
    /// <summary>
    /// ask the extractor if it supports ASYNC extract (free threaded)
    /// </summary>
    IEIFLAG_ASYNC = 0x0001,
    /// <summary>
    /// returned from the extractor if it does NOT cache the thumbnail
    /// </summary>
    IEIFLAG_CACHE = 0x0002,
    /// <summary>
    /// passed to the extractor to beg it to render to the aspect ratio of the supplied rect
    /// </summary>
    IEIFLAG_ASPECT = 0x0004,
    /// <summary>
    /// if the extractor shouldn't hit the net to get any content neede for the rendering
    /// </summary>
    IEIFLAG_OFFLINE = 0x0008,
    /// <summary>
    /// does the image have a gleam ? this will be returned if it does
    /// </summary>
    IEIFLAG_GLEAM = 0x0010,
    /// <summary>
    /// render as if for the screen  (this is exlusive with IEIFLAG_ASPECT )
    /// </summary>
    IEIFLAG_SCREEN = 0x0020,
    /// <summary>
    /// render to the approx size passed, but crop if neccessary
    /// </summary>
    IEIFLAG_ORIGSIZE = 0x0040,
    /// <summary>
    /// returned from the extractor if it does NOT want an icon stamp on the thumbnail
    /// </summary>
    IEIFLAG_NOSTAMP = 0x0080,
    /// <summary>
    /// returned from the extractor if it does NOT want an a border around the thumbnail
    /// </summary>
    IEIFLAG_NOBORDER = 0x0100,
    /// <summary>
    /// passed to the Extract method to indicate that a slower, higher quality image is desired, re-compute the thumbnail
    /// </summary>
    IEIFLAG_QUALITY = 0x0200
  }

  [Flags]
  public enum SIGDN : uint {
    SIGDN_NORMALDISPLAY = 0x00000000,
    SIGDN_PARENTRELATIVEPARSING = 0x80018001,
    SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8001c001,
    SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000,
    SIGDN_PARENTRELATIVEEDITING = 0x80031001,
    SIGDN_DESKTOPABSOLUTEEDITING = 0x8004c000,
    SIGDN_FILESYSPATH = 0x80058000,
    SIGDN_URL = 0x80068000
  }

  [StructLayout(LayoutKind.Sequential)]
  public struct SIZE {
    /// <summary>
    /// 
    /// </summary>
    public int cx;
    /// <summary>
    /// 
    /// </summary>
    public int cy;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0, CharSet = CharSet.Auto)]
  internal struct STRRET_CSTR {
    public ESTRRET uType;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 520)]
    public byte[] cStr;
  }
}
