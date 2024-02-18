using MH.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Repositories;
using PictureManager.Domain.Services;
using static MH.Utils.DragDropHelper;

namespace PictureManager.Domain.Models;

public sealed class ViewerDetailM : ObservableObject {
  private readonly ViewerR _r;

  public ViewerS ViewerS { get; }

  public CanDragFunc CanDragFolder { get; set; }
  public CanDropFunc CanDropFolderIncluded { get; }
  public DoDropAction DoDropFolderIncluded { get; }
  public CanDropFunc CanDropFolderExcluded { get; }
  public DoDropAction DoDropFolderExcluded { get; }
  public CanDropFunc CanDropKeyword { get; }
  public DoDropAction DoDropKeyword { get; }

  public ViewerDetailM(ViewerR r, ViewerS s) {
    ViewerS = s;
    _r = r;
    CanDragFolder = source => source is FolderM ? source : null;
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
          ? ViewerS.Selected.IncludedFolders
          : ViewerS.Selected.ExcludedFolders)
        .Contains(folder)
          ? DragDropEffects.None
          : DragDropEffects.Copy;

    return folder.Equals(target)
      ? DragDropEffects.None
      : DragDropEffects.Move;
  }

  private void DoDropFolder(object data, bool haveSameOrigin, bool included) {
    if (haveSameOrigin)
      _r.RemoveFolder(ViewerS.Selected, (FolderM)data, included);
    else
      _r.AddFolder(ViewerS.Selected, (FolderM)data, included);
  }

  private DragDropEffects CanDropKeywordMethod(object target, object data, bool haveSameOrigin) {
    if (data is not KeywordM keyword)
      return DragDropEffects.None;

    if (haveSameOrigin)
      return keyword.Equals(target)
        ? DragDropEffects.None
        : DragDropEffects.Move;

    return ViewerS.Selected.ExcludedKeywords.Contains(keyword)
      ? DragDropEffects.None
      : DragDropEffects.Copy;
  }

  private void DoDropKeywordMethod(object data, bool haveSameOrigin) {
    if (haveSameOrigin)
      _r.RemoveKeyword(ViewerS.Selected, (KeywordM)data);
    else
      _r.AddKeyword(ViewerS.Selected, (KeywordM)data);
  }
}