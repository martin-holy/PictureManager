using System.Collections.Generic;
using System.Windows;
using PictureManager.Domain;
using PictureManager.ShellStuff;
using PictureManager.ViewModels;
using MH.Utils.BaseClasses;
using MH.UI.WPF.Controls;
using PictureManager.Domain.Models;
using System.Linq;

namespace PictureManager {
  public sealed class AppCore : ObservableObject {
    public MainWindowVM MainWindowVM { get; }
    public MainWindowToolBarVM MainWindowToolBarVM { get; }
    public MediaItemsVM MediaItemsVM { get; }
    public MediaViewerVM MediaViewerVM { get; }
    public PeopleVM PeopleVM { get; }
    public SegmentsVM SegmentsVM { get; }
    public ThumbnailsGridsVM ThumbnailsGridsVM { get; }
    public VideoClipsVM VideoClipsVM { get; }
    public PersonVM PersonVM { get; }
    public ViewerVM ViewerVM { get; }
    public ImageComparerVM ImageComparerVM { get; } = new();

    public AppCore() {
      Core.DialogHostShow = DialogHost.Show;
      Core.FileOperationDelete = FileOperationDelete;
      MH.UI.WPF.Resources.Dictionaries.IconNameToBrush = ResourceDictionaries.Dictionaries.IconNameToBrush;
      PictureManager.Domain.Utils.Imaging.ResizeJpg = MH.UI.WPF.Utils.Imaging.ResizeJpg;

      MainWindowVM = new(App.Core, this, App.Core.MainWindowM);
      MainWindowToolBarVM = new(App.Core, this);

      MediaItemsVM = new(App.Core, this, App.Core.MediaItemsM);
      MediaViewerVM = new(this, App.Core.MediaViewerM);
      PeopleVM = new(App.Core, App.Core.PeopleM);
      SegmentsVM = new(App.Core, this, App.Core.SegmentsM);
      ThumbnailsGridsVM = new(App.Core, this, App.Core.ThumbnailsGridsM);
      VideoClipsVM = new(App.Core.VideoClipsM);

      PersonVM = new(App.Core.PeopleM, App.Core.SegmentsM);
      ViewerVM = new(App.Core.ViewersM);

      AttachEvents();
    }

    private void AttachEvents() {
      App.Core.SegmentsM.SegmentsPersonChangedEvent += (_, e) => {
        if (e.Data.Any(x => x.Equals(PersonVM.PersonM)))
          PersonVM.ReloadPersonSegments();
        App.Core.SegmentsM.Reload();

        if (App.Core.MediaViewerM.IsVisible)
          App.Core.MediaViewerM.Current?.SetInfoBox();
      };

      App.Core.SegmentsM.SegmentsKeywordChangedEvent += (_, e) => {
        if (e.Data.Any(x => x.Equals(PersonVM.PersonM)))
          PersonVM.ReloadPersonSegments();
        App.Core.SegmentsM.Reload();
      };

      App.Core.SegmentsM.SegmentDeletedEventHandler += (_, e) => {
        if (PersonVM.PersonM?.Equals(e.Data.Person) == true)
          PersonVM.ReloadPersonSegments();

        App.Core.SegmentsM.SegmentsDrawerRemove(e.Data);
      };

      App.Core.MainTabsM.TabClosedEventHandler += (_, e) => {
        switch (e.Data.Content) {
          case ThumbnailsGridVM grid:
            ThumbnailsGridsVM.CloseGrid(grid);
            break;
          case PeopleVM people:
            people.PeopleM.DeselectAll();
            break;
        }
      };

      App.Core.MainTabsM.PropertyChanged += async (_, e) => {
        if (nameof(App.Core.MainTabsM.Selected).Equals(e.PropertyName)) {
          await ThumbnailsGridsVM.SetCurrentGrid(App.Core.MainTabsM.Selected?.Content as ThumbnailsGridVM);

          if (App.Core.MainTabsM.Selected is not { Content: ViewModels.PeopleVM })
            App.Core.PeopleM.DeselectAll();
        }
      };
    }
    
    public static Dictionary<string, string> FileOperationDelete(List<string> items, bool recycle, bool silent) {
      var fops = new PicFileOperationProgressSink();
      using var fo = new FileOperation(fops);
      fo.SetOperationFlags(
        (recycle ? FileOperationFlags.FOFX_RECYCLEONDELETE : FileOperationFlags.FOF_WANTNUKEWARNING) |
        (silent
          ? FileOperationFlags.FOF_SILENT | FileOperationFlags.FOF_NOCONFIRMATION |
            FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOFX_KEEPNEWERFILE
          : FileOperationFlags.FOF_NOCONFIRMMKDIR));

      foreach (var x in items)
        fo.DeleteItem(x);
      fo.PerformOperations();

      return fops.FileOperationResult;
    }
  }
}
