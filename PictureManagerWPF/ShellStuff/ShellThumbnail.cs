using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using PictureManager.ShellStuff.Interfaces;

namespace PictureManager.ShellStuff {
  /// <summary>
  /// Extract a thumbnail image from a specified file
  /// </summary>
  [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
  public class ShellThumbnail : IDisposable {
    private bool _disposed;
    //private const IEIFLAG DEFAULTFLAGS = IEIFLAG.IEIFLAG_ASPECT | IEIFLAG.IEIFLAG_SCREEN | IEIFLAG.IEIFLAG_QUALITY | IEIFLAG.IEIFLAG_OFFLINE;
    private const IEIFLAG DEFAULTFLAGS = IEIFLAG.IEIFLAG_QUALITY;
    private const int S_OK = 0;

    private int _hResult;
    private IntPtr _folderPidl = IntPtr.Zero;
    private IntPtr _filePidl = IntPtr.Zero;
    private IShellFolder _desktopFolder;
    private IShellFolder _folder;
    private Guid _iidShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
    private Guid _iidExtractImage = new Guid("BB2E617C-0920-11d1-9A0B-00C04FC2D6C1");
    private int _reserved = 0;
    private IExtractImage _extractImage;

    [DllImport("shell32.dll")]
    private static extern int SHGetDesktopFolder(out IShellFolder ppshf);

    public Image Thumbnail;

    public ShellThumbnail() {
    }

    ~ShellThumbnail() {
      Dispose();
    }

    public void CreateThumbnail(string srcPath, string destPath, int size, long quality) {
      GenerateIExtractImage(srcPath);
      Extract(size, size, DEFAULTFLAGS);
      SaveAsJpeg(destPath, quality);
    }

    private void SaveAsJpeg(string path, long quality) {
      ImageCodecInfo ici = ImageCodecInfo.GetImageDecoders().FirstOrDefault(x => x.FormatID == ImageFormat.Jpeg.Guid);
      System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;
      EncoderParameters encoderParameters = new EncoderParameters(1);
      EncoderParameter encoderParameter = new EncoderParameter(encoder, quality);
      encoderParameters.Param[0] = encoderParameter;
      Thumbnail.Save(path, ici, encoderParameters);
    }

    private void GenerateIExtractImage(string path) {
      try {
        // we get the desktop shell then the PIDL for the file's folder.
        // Once we have the PIDL then get a reference to the folder (BindToObject) then we can get the PIDL for the file.
        //Now get the IExtractImage interface (GETUIObjectOf) from which we can cast to the IExtractImage object
        SHGetDesktopFolder(out _desktopFolder);

        int pdwAttributes = 0; // not required
        int pchEaten = 0; // not required
        _hResult = _desktopFolder.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, System.IO.Path.GetDirectoryName(path), out pchEaten, out _folderPidl, out pdwAttributes);
        if (_hResult != S_OK) Marshal.ThrowExceptionForHR(_hResult);

        _hResult = _desktopFolder.BindToObject(_folderPidl, IntPtr.Zero, ref _iidShellFolder, ref _folder);
        if (_hResult != S_OK) Marshal.ThrowExceptionForHR(_hResult);

        _hResult = _folder.ParseDisplayName(IntPtr.Zero, IntPtr.Zero, System.IO.Path.GetFileName(path), out pchEaten, out _filePidl, out pdwAttributes);
        if (_hResult != S_OK) Marshal.ThrowExceptionForHR(_hResult);

        IUnknown unk = null;
        _hResult = _folder.GetUIObjectOf(IntPtr.Zero, 1, ref _filePidl, ref _iidExtractImage, out _reserved, ref unk);
        if (_hResult != S_OK) Marshal.ThrowExceptionForHR(_hResult);

        // Now cast the unknown as the extractImage object
        _extractImage = (IExtractImage) unk;
      } catch (FileNotFoundException) {
      } catch (COMException) {
      } catch (Exception) {
      }
    }

    private void Extract(int width, int height, IEIFLAG flags) {
      IntPtr bitmap = IntPtr.Zero;
      SIZE size = new SIZE {
        cx = width,
        cy = height
      };

      try {
        if (_extractImage != null) {
          StringBuilder location = new StringBuilder(260);
          int priority = 0;
          const int requestedColorDepth = 32;

          _hResult = _extractImage.GetLocation(location, location.Capacity, ref priority, ref size, requestedColorDepth, ref flags);
          if (_hResult != S_OK) Marshal.ThrowExceptionForHR(_hResult);

          _hResult = _extractImage.Extract(out bitmap);
          if (_hResult != S_OK) Marshal.ThrowExceptionForHR(_hResult);

          Thumbnail = Image.FromHbitmap(bitmap);
        }
      } catch (COMException) {
      } catch (Exception) {
      } finally {
        if (bitmap != IntPtr.Zero)
          Marshal.Release(bitmap);
      }
    }

    public void Dispose() {
      if (!_disposed) {
        if (IntPtr.Zero != _filePidl)
          Marshal.Release(_filePidl);
        if (IntPtr.Zero != _folderPidl)
          Marshal.Release(_folderPidl);
        if (null != _extractImage)
          Marshal.ReleaseComObject(_extractImage);
        if (null != _folder)
          Marshal.ReleaseComObject(_folder);
        if (null != _desktopFolder)
          Marshal.ReleaseComObject(_desktopFolder);
        Thumbnail?.Dispose();
        _disposed = true;
      }
    }
  }
}