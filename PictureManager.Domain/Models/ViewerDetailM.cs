using MH.Utils;
using MH.Utils.BaseClasses;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Domain.Models {
  public sealed class ViewerDetailM : ObservableObject {
    public ViewersM ViewersM { get; }

    public CanDragFunc CanDragFolder { get; set; }
    public CanDropFunc CanDropFolderIncluded { get; }
    public DoDropAction DoDropFolderIncluded { get; }
    public CanDropFunc CanDropFolderExcluded { get; }
    public DoDropAction DoDropFolderExcluded { get; }
    public CanDropFunc CanDropKeyword { get; }
    public DoDropAction DoDropKeyword { get; }

    public ViewerDetailM(ViewersM viewersM) {
      ViewersM = viewersM;

      ViewersM.ViewerMainTabsItem = new(this, "Viewer");

      CanDragFolder = (source) => source is FolderM ? source : null;
      CanDropFolderIncluded = (a, b, c) => CanDropFolder(a, b, c, true);
      CanDropFolderExcluded = (a, b, c) => CanDropFolder(a, b, c, false);
      DoDropFolderIncluded = (a, b) => DoDropFolder(a, b, true);
      DoDropFolderExcluded = (a, b) => DoDropFolder(a, b, false);
      CanDropKeyword = CanDropKeywordMethod;
      DoDropKeyword = DoDropKeywordMethod;
    }

    private DragDropEffects CanDropFolder(object target, object data, bool haveSameOrigin, bool included) {
      if (data is not FolderM folder)
        return DragDropEffects.None;

      if (!haveSameOrigin)
        return (included
          ? ViewersM.Selected.IncludedFolders
          : ViewersM.Selected.ExcludedFolders)
          .Contains(folder)
            ? DragDropEffects.None
            : DragDropEffects.Copy;

      if (haveSameOrigin)
        return folder.Equals(target)
          ? DragDropEffects.None
          : DragDropEffects.Move;

      return DragDropEffects.None;
    }

    private void DoDropFolder(object data, bool haveSameOrigin, bool included) {
      if (haveSameOrigin)
        ViewersM.RemoveFolder(ViewersM.Selected, (FolderM)data, included);
      else
        ViewersM.AddFolder(ViewersM.Selected, (FolderM)data, included);
    }

    private DragDropEffects CanDropKeywordMethod(object target, object data, bool haveSameOrigin) {
      if (data is not KeywordM keyword)
        return DragDropEffects.None;

      if (haveSameOrigin)
        return keyword.Equals(target)
          ? DragDropEffects.None
          : DragDropEffects.Move;
      else
        return ViewersM.Selected.ExcludedKeywords.Contains(keyword)
          ? DragDropEffects.None
          : DragDropEffects.Copy;
    }

    private void DoDropKeywordMethod(object data, bool haveSameOrigin) {
      if (haveSameOrigin)
        ViewersM.RemoveKeyword(ViewersM.Selected, (KeywordM)data);
      else
        ViewersM.AddKeyword(ViewersM.Selected, (KeywordM)data);
    }
  }
}
