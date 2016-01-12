using MsdnMag;

namespace PictureManager.ShellStuff {
  public class PicFileOperationProgressSink: FileOperationProgressSink {
    public override void PostCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string pszNewName, uint hrCopy, IShellItem psiNewlyCreated) {
      //int i = 0;
    }
  }
}
