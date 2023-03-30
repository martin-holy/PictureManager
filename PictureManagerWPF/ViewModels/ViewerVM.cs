using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MH.UI.WPF.Utils;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public sealed class ViewerVM : ObservableObject {
    public ViewersM ViewersM { get; }
    
    public HeaderedListItem<object, string> MainTabsItem { get; }
    public ListBox LbIncludedFolders { get; }
    public ListBox LbExcludedFolders { get; }
    public ListBox LbExcludedKeywords { get; }

    public ViewerVM(ViewersM viewersM) {
      ViewersM = viewersM;

      MainTabsItem = new(this, "Viewer");

      LbIncludedFolders = new();
      LbExcludedFolders = new();
      LbExcludedKeywords = new();

      AttachEvents();
    }

    private void AttachEvents() {
      DragDropFactory.SetDrag(LbIncludedFolders, e => (e.OriginalSource as FrameworkElement)?.DataContext as FolderM);
      DragDropFactory.SetDrag(LbExcludedFolders, e => (e.OriginalSource as FrameworkElement)?.DataContext as FolderM);
      DragDropFactory.SetDrag(LbExcludedKeywords, e => (e.OriginalSource as FrameworkElement)?.DataContext as KeywordM);

      DragDropFactory.SetDrop(
        LbIncludedFolders,
        (e, source, data) => CanDropFolder(e, source, data, true),
        (e, source, data) => DoDropFolder(e, source, data, true));

      DragDropFactory.SetDrop(
        LbExcludedFolders,
        (e, source, data) => CanDropFolder(e, source, data, false),
        (e, source, data) => DoDropFolder(e, source, data, false));

      DragDropFactory.SetDrop(LbExcludedKeywords, CanDropKeyword, DoDropKeyword);
    }

    private DragDropEffects CanDropFolder(DragEventArgs e, object source, object data, bool included) {
      if (data is not FolderM folder)
        return DragDropEffects.None;

      if (!source.Equals(LbIncludedFolders) && !source.Equals(LbExcludedFolders))
        return (included
          ? ViewersM.Selected.IncludedFolders
          : ViewersM.Selected.ExcludedFolders)
          .Contains(folder)
            ? DragDropEffects.None
            : DragDropEffects.Copy;

      if ((e.Source as FrameworkElement)?.TemplatedParent == source)
        return folder.Equals((e.OriginalSource as FrameworkElement)?.DataContext)
          ? DragDropEffects.None
          : DragDropEffects.Move;

      return DragDropEffects.None;
    }

    private void DoDropFolder(DragEventArgs e, object source, object data, bool included) {
      if ((e.Source as FrameworkElement)?.TemplatedParent == source)
        ViewersM.RemoveFolder(ViewersM.Selected, (FolderM)data, included);
      else
        ViewersM.AddFolder(ViewersM.Selected, (FolderM)data, included);
    }

    private DragDropEffects CanDropKeyword(DragEventArgs e, object source, object data) {
      if (data is not KeywordM keyword)
        return DragDropEffects.None;

      if ((e.Source as FrameworkElement)?.TemplatedParent == source)
        return keyword.Equals((e.OriginalSource as FrameworkElement)?.DataContext)
          ? DragDropEffects.None
          : DragDropEffects.Move;
      else
        return ViewersM.Selected.ExcludedKeywords.Contains(keyword)
          ? DragDropEffects.None
          : DragDropEffects.Copy;
    }

    private void DoDropKeyword(DragEventArgs e, object source, object data) {
      if ((e.Source as FrameworkElement)?.TemplatedParent == source)
        ViewersM.RemoveKeyword(ViewersM.Selected, (KeywordM)data);
      else
        ViewersM.AddKeyword(ViewersM.Selected, (KeywordM)data);
    }
  }
}
