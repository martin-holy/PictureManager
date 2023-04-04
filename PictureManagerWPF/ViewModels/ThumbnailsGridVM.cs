using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MH.UI.WPF.Controls;
using MH.UI.WPF.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using static MH.Utils.DragDropHelper;

namespace PictureManager.ViewModels {
  public sealed class ThumbnailsGridVM: ObservableObject {
    private readonly Core _coreM;
    private readonly AppCore _coreVM;
    private readonly MediaElement _videoPreview;
    private bool _reWrapItems;

    public ThumbnailsGridM Model { get; }
    public HeaderedListItem<object, string> MainTabsItem { get; }
    public CanDragFunc CanDragFunc { get; }

    public bool ReWrapItems { get => _reWrapItems; set { _reWrapItems = value; OnPropertyChanged(); } }
    public RelayCommand<object> ShowVideoPreviewCommand { get; }
    public RelayCommand<MediaItemM> HideVideoPreviewCommand { get; }
    public RelayCommand<ClickEventArgs> SelectMediaItemCommand { get; }
    public RelayCommand<ClickEventArgs> OpenMediaItemCommand { get; }
    public RelayCommand<MouseWheelEventArgs> ZoomCommand { get; }
    public RelayCommand<object> PanelWidthChangedCommand { get; }

    public ThumbnailsGridVM(Core coreM, AppCore coreVM, ThumbnailsGridM model, string tabTitle) {
      _coreM = coreM;
      _coreVM = coreVM;
      Model = model;

      MainTabsItem = new(this, tabTitle);
      CanDragFunc = CanDrag;

      ShowVideoPreviewCommand = new(ShowVideoPreview);
      HideVideoPreviewCommand = new(HideVideoPreview);

      PanelWidthChangedCommand = new(
        () => ReWrapItems = true,
        () => !_coreM.MediaViewerM.IsVisible && !_coreM.MainWindowM.IsFullScreenIsChanging);

      SelectMediaItemCommand = new(e => {
        if (e.DataContext is MediaItemM mi)
          Model.Select(mi, e.IsCtrlOn, e.IsShiftOn);
      });

      OpenMediaItemCommand = new(
        e => OpenMediaItem(e.DataContext as MediaItemM),
        e => e.ClickCount == 2);

      ZoomCommand = new(
        async e => {
          Model.Zoom(e.Delta);
          await Model.ThumbsGridReloadItems();
        },
        _ => (Keyboard.Modifiers & ModifierKeys.Control) > 0);

      _videoPreview = new() {
        LoadedBehavior = MediaState.Manual,
        IsMuted = true,
        Stretch = Stretch.Fill
      };

      _videoPreview.MediaEnded += (o, _) => {
        // MediaElement.Stop()/Play() doesn't work when is video shorter than 1s
        ((MediaElement)o).Position = TimeSpan.FromMilliseconds(1);
      };
    }

    private object CanDrag(object source) {
      if (source is not MediaItemM) return null;
      var data = Model.FilteredItems.Where(x => x.IsSelected).Select(p => p.FilePath).ToArray();
      return data.Length == 0 ? null : data;
    }

    private void ShowVideoPreview(object o) {
      var grid = (o as DependencyObject)?.FindChild<Grid>("PART_Grid");
      if (grid?.DataContext is not MediaItemM mi || mi.MediaType != MediaType.Video) return;

      var rotation = new TransformGroup();
      rotation.Children.Add(new RotateTransform(mi.RotationAngle));
      (_videoPreview.Parent as Grid)?.Children.Remove(_videoPreview);
      _videoPreview.LayoutTransform = rotation;
      _videoPreview.Source = new(mi.FilePath);
      grid.Children.Insert(2, _videoPreview);
      _videoPreview.Play();
    }

    private void HideVideoPreview(MediaItemM mi) {
      if (mi?.MediaType != MediaType.Video) return;

      (_videoPreview.Parent as Grid)?.Children.Remove(_videoPreview);
      _videoPreview.Source = null;
    }

    private void OpenMediaItem(MediaItemM mi) {
      if (mi == null) return;

      Model.DeselectAll();
      Model.CurrentMediaItem = mi;

      if (mi.MediaType == MediaType.Video) {
        (_videoPreview.Parent as Grid)?.Children.Remove(_videoPreview);
        _videoPreview.Source = null;
      }

      _coreM.MainWindowM.IsFullScreen = true;
      _coreM.MediaViewerM.SetMediaItems(Model.FilteredItems.ToList(), mi);
    }
  }
}
