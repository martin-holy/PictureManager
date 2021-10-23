using System;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.EventsArgs {
  public class FolderDeletedEventArgs : EventArgs {
    public FolderM Folder { get; }

    public FolderDeletedEventArgs(FolderM folder) {
      Folder = folder;
    }
  }
}
