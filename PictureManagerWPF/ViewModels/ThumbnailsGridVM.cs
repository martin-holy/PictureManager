using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;

namespace PictureManager.ViewModels {
  public sealed class ThumbnailsGridVM: ObservableObject {
    private readonly AppCore _coreVM;
    private readonly MediaElement _videoPreview;
    private VirtualizingWrapPanel _panel;

    public ThumbnailsGridM Model { get; }
    public HeaderedListItem<object, string> MainTabsItem { get; }

    public RelayCommand<object> ShowVideoPreviewCommand { get; }
    public RelayCommand<MediaItemM> HideVideoPreviewCommand { get; }
    public RelayCommand<MouseButtonEventArgs> SelectMediaItemCommand { get; }
    public RelayCommand<MouseButtonEventArgs> OpenMediaItemCommand { get; }
    public RelayCommand<MouseWheelEventArgs> ZoomCommand { get; }
    public RelayCommand<object> RefreshCommand { get; }
    public RelayCommand<object> SelectAllCommand { get; }
    public RelayCommand<RoutedEventArgs> PanelLoadedCommand { get; }
    public RelayCommand<SizeChangedEventArgs> PanelSizeChangedCommand { get; }

    public ThumbnailsGridVM(AppCore coreVM, ThumbnailsGridM model, string tabTitle) {
      _coreVM = coreVM;
      Model = model;

      MainTabsItem = new(this, tabTitle);

      ShowVideoPreviewCommand = new(ShowVideoPreview);
      HideVideoPreviewCommand = new(HideVideoPreview);
      RefreshCommand = new(async () => await Model.ReapplyFilter());
      SelectAllCommand = new(() => Model.SelectAll());
      PanelLoadedCommand = new(PanelLoaded);
      PanelSizeChangedCommand = new(PanelSizeChanged);

      SelectMediaItemCommand = new(e => {
        if ((e.Source as FrameworkElement)?.DataContext is not MediaItemM mi) return;
        var (isCtrlOn, isShiftOn) = InputUtils.GetKeyboardModifiers(e);
        Model.Select(mi, isCtrlOn, isShiftOn);
      });

      OpenMediaItemCommand = new(
        e => OpenMediaItem((e.Source as FrameworkElement)?.DataContext as MediaItemM),
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

    private void PanelLoaded(RoutedEventArgs e) {
      _panel = e.Source as VirtualizingWrapPanel;
      DragDropFactory.SetDrag(_panel, CanDrag, DataFormats.FileDrop);
    }

    private void PanelSizeChanged(SizeChangedEventArgs e) {
      if (e.WidthChanged && !_coreVM.MediaViewerVM.IsVisible && !_coreVM.MainWindowVM.IsFullScreenIsChanging)
        _panel.ReWrap();
    }

    private object CanDrag(MouseEventArgs e) {
      if (e.OriginalSource is MetroThumb) return null;
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

      _coreVM.MainWindowVM.IsFullScreen = true;
      _coreVM.MediaViewerVM.SetMediaItems(Model.FilteredItems.ToList(), mi);
    }
  }
}
