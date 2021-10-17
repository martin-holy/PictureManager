using System;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.EventsArgs {
  public class ViewerDeletedEventArgs : EventArgs {
    public ViewerM Viewer { get; }

    public ViewerDeletedEventArgs(ViewerM viewer) {
      Viewer = viewer;
    }
  }
}
