using MH.UI.WPF.BaseClasses;
using PictureManager.Dialogs;

namespace PictureManager.ViewModels {
  public static class FolderKeywordsVM {
    public static RelayCommand<object> OpenFolderKeywordsListCommand { get; } = new(
      FolderKeywordList.Open);
  }
}
