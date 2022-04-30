using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public static class ViewersVM {
    public static readonly RelayCommand<ViewerM> SetCurrentCommand = new(
      App.Core.ViewersM.SetCurrent);
  }
}
