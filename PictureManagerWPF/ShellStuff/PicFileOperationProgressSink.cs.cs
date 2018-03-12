using System.Collections.Generic;
using System.Windows;
using PictureManager.ShellStuff.Interfaces;

namespace PictureManager.ShellStuff {
  public class PicFileOperationProgressSink: FileOperationProgressSink {
    public override void PostCopyItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string pszNewName, uint hrCopy, IShellItem psiNewlyCreated) {
      if ((CopyEngineResult)hrCopy != CopyEngineResult.COPYENGINE_OK && (CopyEngineResult)hrCopy != CopyEngineResult.COPYENGINE_S_DONT_PROCESS_CHILDREN) return;
      ((Dictionary<string, string>)Application.Current.Properties[nameof(AppProperty.FileOperationResult)]).Add(
        psiItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH), 
        psiNewlyCreated.GetDisplayName(SIGDN.SIGDN_FILESYSPATH));
    }

    public override void PostMoveItem(uint dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string pszNewName, uint hrMove, IShellItem psiNewlyCreated) {
      if ((CopyEngineResult)hrMove != CopyEngineResult.COPYENGINE_OK && (CopyEngineResult)hrMove != CopyEngineResult.COPYENGINE_S_DONT_PROCESS_CHILDREN) return;
      ((Dictionary<string, string>)Application.Current.Properties[nameof(AppProperty.FileOperationResult)]).Add(
        psiItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH),
        psiNewlyCreated.GetDisplayName(SIGDN.SIGDN_FILESYSPATH));
    }

    public override void PostDeleteItem(uint dwFlags, IShellItem psiItem, uint hrDelete, IShellItem psiNewlyCreated) {
      if ((CopyEngineResult)hrDelete != CopyEngineResult.COPYENGINE_OK && (CopyEngineResult)hrDelete != CopyEngineResult.COPYENGINE_S_DONT_PROCESS_CHILDREN) return;
      ((Dictionary<string, string>)Application.Current.Properties[nameof(AppProperty.FileOperationResult)]).Add(
        psiItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH),
        null);
    }
  }
}
