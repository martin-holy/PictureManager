using System.Collections.Generic;
using System.Windows;
using PictureManager.ShellStuff.Interfaces;

namespace PictureManager.ShellStuff {
  public class PicFileOperationProgressSink: FileOperationProgressSink {
    public override void PostCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string pszNewName, uint hrCopy, IShellItem psiNewlyCreated) {
      if (hrCopy != 0) return;
      ((Dictionary<string, string>)Application.Current.Properties["FileOperationResult"]).Add(
        psiItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH), 
        psiNewlyCreated.GetDisplayName(SIGDN.SIGDN_FILESYSPATH));
    }

    public override void PostMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string pszNewName, uint hrCopy, IShellItem psiNewlyCreated) {
      if (hrCopy != 0) return;
      ((Dictionary<string, string>)Application.Current.Properties["FileOperationResult"]).Add(
        psiItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH),
        psiNewlyCreated.GetDisplayName(SIGDN.SIGDN_FILESYSPATH));
    }
  }
}
