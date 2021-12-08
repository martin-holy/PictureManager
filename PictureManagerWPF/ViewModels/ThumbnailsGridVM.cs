using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MH.UI.WPF.BaseClasses;
using PictureManager.CustomControls;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Interfaces;
using PictureManager.Utils;
using ObservableObject = MH.Utils.BaseClasses.ObservableObject;
using WindowCommands = PictureManager.Commands.WindowCommands;

namespace PictureManager.ViewModels {
  public sealed class ThumbnailsGridVM: ObservableObject, IMainTabsItem {
    private readonly Core _core;
    private readonly AppCore _coreVM;
    private readonly MediaElement _videoPreview;

    #region IMainTabsItem implementation
    private string _title;
    private object _toolTip;
    private ContextMenu _contextMenu;

    public string IconName { get; set; }
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public object ToolTip { get => _toolTip; set { _toolTip = value; OnPropertyChanged(); } }
    public ContextMenu ContextMenu { get => _contextMenu; set { _contextMenu = value; OnPropertyChanged(); } }
    #endregion

    public ThumbnailsGridM Model { get; }

    public RelayCommand<object> ShowVideoPreviewCommand { get; }
    public RelayCommand<MediaItemM> HideVideoPreviewCommand { get; }
    public RelayCommand<MouseButtonEventArgs> SelectMediaItemCommand { get; }
    public RelayCommand<MouseButtonEventArgs> OpenMediaItemCommand { get; }
    public RelayCommand<MouseWheelEventArgs> ZoomCommand { get; }
    public RelayCommand<object> RefreshCommand { get; }
    public RelayCommand<object> SelectAllCommand { get; }

    public VirtualizingWrapPanel Panel { get; }

    public ThumbnailsGridVM(Core core, AppCore coreVM, ThumbnailsGridM model, string tabTitle) {
      _core = core;
      _coreVM = coreVM;
      Model = model;
      Title = tabTitle;
      IconName = "IconFolder";
      Panel = new();
      DragDropFactory.SetDrag(Panel, CanDrag, DataFormats.FileDrop);

      ShowVideoPreviewCommand = new(ShowVideoPreview);
      HideVideoPreviewCommand = new(HideVideoPreview);
      RefreshCommand = new(Refresh);
      SelectAllCommand = new(() => Model.SelectAll());

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
          await _coreVM.ThumbnailsGridsVM.ThumbsGridReloadItems();
        },
        e => (Keyboard.Modifiers & ModifierKeys.Control) > 0);

      _videoPreview = new() {
        LoadedBehavior = MediaState.Manual,
        IsMuted = true,
        Stretch = Stretch.Fill
      };

      _videoPreview.MediaEnded += (o, _) => {
        // MediaElement.Stop()/Play() doesn't work when is video shorter than 1s
        ((MediaElement)o).Position = TimeSpan.FromMilliseconds(1);
      };

      ContextMenu = Application.Current.FindResource("ThumbnailsGridContextMenu") as ContextMenu;
    }

    private object CanDrag(MouseEventArgs e) {
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

      WindowCommands.SwitchToFullScreen();
      // TODO add command with SwitchToFullScreen and setting MediaViewer
      App.WMain.MediaViewer.SetMediaItems(Model.FilteredItems);
      App.WMain.MediaViewer.SetMediaItemSource(mi);
    }

    private async void Refresh() {
      await Model.ReloadFilteredItems();
      await _coreVM.ThumbnailsGridsVM.ThumbsGridReloadItems();
    }
  }
}
