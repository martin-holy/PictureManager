using PictureManager.CustomControls;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PictureManager.UserControls {
  public partial class FaceRecognitionControl : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private CancellationTokenSource _cts;
    private Task _workTask;
    private readonly IProgress<int> _progress;
    private string _title;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public ObservableCollection<object> Rows { get; } = new();

    public FaceRecognitionControl() {
      InitializeComponent();

      Title = "Face Recognition";
      App.Core.Faces.LoadFromFile();
      App.Core.Faces.Helper.LoadPropsFromFile();
      App.Core.Faces.LinkReferences();

      _progress = new Progress<int>(x => {
        App.Ui.AppInfo.ProgressBarValueA = x;
        App.Ui.AppInfo.ProgressBarValueB = x;
      });
    }

    public async void LoadFaces(List<MediaItem> mediaItems) {
      // cancel previous work
      if (_workTask != null) {
        _cts?.Cancel();
        await _workTask;
      }

      _cts?.Dispose();
      _cts = new();

      ThumbsGrid.ClearRows();
      App.Ui.AppInfo.ProgressBarValueA = 0;
      App.Ui.AppInfo.ProgressBarValueB = 0;
      
      _workTask = Task.Run(async () => {
        await foreach (var face in App.Core.Faces.GetFacesAsync(mediaItems, _progress, _cts.Token))
          await App.Core.RunOnUiThread(() => { AddFaceToGrid(face); });
      });

      await _workTask;
      _cts?.Dispose();
      _cts = null;

      if (App.Core.Faces.Helper.IsModified)
        App.Core.Faces.Helper.SaveToFile(App.Core.Faces.All);
      if (App.Core.Faces.Helper.AreTablePropsModified)
        App.Core.Faces.Helper.SaveTablePropsToFile();

      // sort by similarity
      App.Core.Faces.SortLoaded();
      ThumbsGrid.ClearRows();
      foreach (var face in App.Core.Faces.Loaded)
        AddFaceToGrid(face);
    }

    private void AddFaceToGrid(Face face) {
      const int itemOffset = 6; //border, margin, padding, ... //TODO find the real value
      ThumbsGrid.AddItem(face, 100 + itemOffset, new VirtualizingWrapPanelGroupItem[0]);
    }

    private void Face_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
      var isCtrlOn = (Keyboard.Modifiers & ModifierKeys.Control) > 0;
      var isShiftOn = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var face = (Face)((FrameworkElement)sender).DataContext;

      // use middle and right button like CTRL + left button
      if (e.ChangedButton is MouseButton.Middle or MouseButton.Right) {
        isCtrlOn = true;
        isShiftOn = false;
      }

      App.Core.Faces.Select(isCtrlOn, isShiftOn, face);
    }
  }
}
