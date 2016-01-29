using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PictureManager.ShellStuff.Interfaces {
  /// <summary>
  /// 
  /// </summary>
  [ComImport()]
  [GuidAttribute("BB2E617C-0920-11d1-9A0B-00C04FC2D6C1")]
  [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
  internal interface IExtractImage {
    /// <summary>
    /// Gets the location.
    /// </summary>
    /// <param name="pszPathBuffer">The PSZ path buffer.</param>
    /// <param name="cchMax">The CCH max.</param>
    /// <param name="pdwPriority">The PDW priority.</param>
    /// <param name="prgSize">Size of the PRG.</param>
    /// <param name="dwRecClrDepth">The dw rec CLR depth.</param>
    /// <param name="pdwFlags">The PDW flags.</param>
    /// <returns></returns>
    int GetLocation([Out, MarshalAs(UnmanagedType.LPWStr)]StringBuilder pszPathBuffer, //The buffer used to return the path description. This value identifies the image so you can avoid loading the same one more than once.
                    int cchMax, // The size of pszPathBuffer in characters.
                    ref int pdwPriority, // Not used
                    ref SIZE prgSize, //A pointer to a SIZE structure with the desired width and height of the image. Must not be NULL.
                    int dwRecClrDepth, // The recommended color depth in units of bits per pixel. Must not be NULL.
                    ref IEIFLAG pdwFlags); //Flags that specify how the image is to be handled

    /// <summary>
    /// Extracts the specified Image thumbnail.
    /// </summary>
    /// <param name="phBmpThumbnail">The Image thumbnail pointer.</param>
    /// <returns></returns>
    int Extract(out IntPtr phBmpThumbnail);
  }
}