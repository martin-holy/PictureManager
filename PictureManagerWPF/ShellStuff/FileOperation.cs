// Stephen Toub

using System;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace MsdnMag {
  internal class FileOperation : IDisposable {
    private bool _disposed;
    private IFileOperation _fileOperation;
    private FileOperationProgressSink _callbackSink;
    private uint _sinkCookie;

    public uint SinkCookie {
      get { return _sinkCookie; }
    }

    public FileOperation() : this(null) {}
    public FileOperation(FileOperationProgressSink callbackSink) : this(callbackSink, null) {}

    public FileOperation(FileOperationProgressSink callbackSink, IWin32Window owner) {
      _callbackSink = callbackSink;
      _fileOperation = (IFileOperation) Activator.CreateInstance(_fileOperationType);

      _fileOperation.SetOperationFlags(FileOperationFlags.FOF_NOCONFIRMMKDIR);
      if (_callbackSink != null) _sinkCookie = _fileOperation.Advise(_callbackSink);
      if (owner != null) _fileOperation.SetOwnerWindow((uint) owner.Handle);
    }

    public void CopyItem(string source, string destination, string newName) {
      ThrowIfDisposed();
      using (ComReleaser<IShellItem> sourceItem = CreateShellItem(source))
      using (ComReleaser<IShellItem> destinationItem = CreateShellItem(destination)) {
        _fileOperation.CopyItem(sourceItem.Item, destinationItem.Item, newName, null);
      }
    }

    public void MoveItem(string source, string destination, string newName) {
      ThrowIfDisposed();
      using (ComReleaser<IShellItem> sourceItem = CreateShellItem(source))
      using (ComReleaser<IShellItem> destinationItem = CreateShellItem(destination)) {
        _fileOperation.MoveItem(sourceItem.Item, destinationItem.Item, newName, null);
      }
    }

    public void RenameItem(string source, string newName) {
      ThrowIfDisposed();
      using (ComReleaser<IShellItem> sourceItem = CreateShellItem(source)) {
        _fileOperation.RenameItem(sourceItem.Item, newName, null);
      }
    }

    public void DeleteItem(string source) {
      ThrowIfDisposed();
      using (ComReleaser<IShellItem> sourceItem = CreateShellItem(source)) {
        _fileOperation.DeleteItem(sourceItem.Item, null);
      }
    }

    public void NewItem(string folderName, string name, FileAttributes attrs) {
      ThrowIfDisposed();
      using (ComReleaser<IShellItem> folderItem = CreateShellItem(folderName)) {
        _fileOperation.NewItem(folderItem.Item, attrs, name, string.Empty, _callbackSink);
      }
    }

    public void PerformOperations() {
      ThrowIfDisposed();
      _fileOperation.PerformOperations();
    }

    private void ThrowIfDisposed() {
      if (_disposed) throw new ObjectDisposedException(GetType().Name);
    }

    public void Dispose() {
      if (!_disposed) {
        _disposed = true;
        if (_callbackSink != null) _fileOperation.Unadvise(_sinkCookie);
        Marshal.FinalReleaseComObject(_fileOperation);
      }
    }

    private static ComReleaser<IShellItem> CreateShellItem(string path) {
      try {
        //I don't know how to do it better
        if (!Path.HasExtension(path)) {
          DirectoryInfo di = new DirectoryInfo(path);
          if (!di.Exists)
            di.Create();
        }
        return new ComReleaser<IShellItem>((IShellItem) SHCreateItemFromParsingName(path, null, ref _shellItemGuid));
      }
      catch (Exception) {
        return null;
      }
    }

    [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode, PreserveSig = false)]
    [return: MarshalAs(UnmanagedType.Interface)]
    private static extern object SHCreateItemFromParsingName(
      [MarshalAs(UnmanagedType.LPWStr)] string pszPath, IBindCtx pbc, ref Guid riid);

    private static readonly Guid CLSID_FileOperation = new Guid("3ad05575-8857-4850-9277-11b85bdb8e09");
    private static readonly Type _fileOperationType = Type.GetTypeFromCLSID(CLSID_FileOperation);
    private static Guid _shellItemGuid = typeof (IShellItem).GUID;
  }
}
