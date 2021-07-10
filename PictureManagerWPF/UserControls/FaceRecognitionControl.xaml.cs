using PictureManager.Commands;
using PictureManager.CustomControls;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace PictureManager.UserControls {
  public partial class FaceRecognitionControl : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private CancellationTokenSource _cts;
    private Task _workTask;
    private readonly Random _random = new();
    private readonly IProgress<int> _progressA;
    private readonly IProgress<int> _progressB;
    private string _title;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public ObservableCollection<object> Rows { get; } = new();

    public FaceRecognitionControl() {
      InitializeComponent();

      Title = "Face Recognition";

      if (App.Core.Faces.All.Count == 0) {
        App.Core.Faces.LoadFromFile();
        App.Core.Faces.Helper.LoadPropsFromFile();
        App.Core.Faces.LinkReferences();
      }

      _progressA = new Progress<int>(x => {
        App.Ui.AppInfo.ProgressBarValueA = x;
      });

      _progressB = new Progress<int>(x => {
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

      App.Core.Faces.ConfirmedPeople.Clear();
      ThumbsGrid.ClearRows();
      App.Ui.AppInfo.ResetProgressBarA(mediaItems.Count);
      App.Ui.AppInfo.ResetProgressBarB(1);

      _workTask = Task.Run(async () => {
        var groupItems = new VirtualizingWrapPanelGroupItem[] { new() { Icon = IconName.People, Title = "?" } };

        await foreach (var face in App.Core.Faces.GetFacesAsync(mediaItems, _progressA, _cts.Token))
          await App.Core.RunOnUiThread(() => { AddFaceToGrid(face, groupItems); });

        await App.Core.RunOnUiThread(() => { App.Ui.AppInfo.ResetProgressBarB(App.Core.Faces.Loaded.Count * 2); });
        App.Core.Faces.FindSimilarities(_progressB);
      });

      await _workTask;
      _cts?.Dispose();
      _cts = null;

      if (App.Core.Faces.Helper.IsModified)
        App.Core.Faces.Helper.SaveToFile(App.Core.Faces.All);
      if (App.Core.Faces.Helper.AreTablePropsModified)
        App.Core.Faces.Helper.SaveTablePropsToFile();
    }

    private void AddFaceToGrid(Face face, VirtualizingWrapPanelGroupItem[] groupItems) {
      const int itemOffset = 6; //border, margin, padding, ... //TODO find the real value
      ThumbsGrid.AddItem(face, 100 + itemOffset, groupItems);
    }

    private int GetTopRowIndex() {
      var rowIndex = 0;
      var mouseLoc = Mouse.GetPosition(ThumbsGrid);
      mouseLoc.X = 50;
      VisualTreeHelper.HitTest(ThumbsGrid, null, (e) => {
        if (e.VisualHit is FrameworkElement elm) {
          switch (elm.DataContext) {
            case VirtualizingWrapPanelGroup g: {
              for (var i = 0; i < ThumbsGrid.Rows.Count; i++) {
                if (ThumbsGrid.Rows[i] == g) {
                  rowIndex = i;
                  break;
                }
              }
              return HitTestResultBehavior.Stop;
            }
            case Face f: {
              rowIndex = ThumbsGrid.GetRowIndex(f);
              return HitTestResultBehavior.Stop;
            }
          }
        }
        return HitTestResultBehavior.Continue;
      }, new PointHitTestParameters(mouseLoc));

      return rowIndex;
    }

    private async void Sort() {
      await App.Core.Faces.SortLoadedAsync();
      Reload();
    }

    private void Reload() {
      var rowIndex = GetTopRowIndex();
      App.Core.Faces.ConfirmedPeople.Clear();
      ThumbsGrid.ClearRows();

      foreach (var group in App.Core.Faces.LoadedInGroups.Where(x => x.Count > 0)) {
        if (group[0].PersonId != 0) {
          var randomFaceIndex = _random.Next(group.Count(x => x.PersonId != 0) - 1);
          App.Core.Faces.ConfirmedPeople.Add(group[randomFaceIndex]);
        }

        var groupItems = new VirtualizingWrapPanelGroupItem[] {
          new() { Icon = IconName.People, Title = group[0].PersonId.ToString() } };

        foreach (var face in group)
          AddFaceToGrid(face, groupItems);
      }

      if (rowIndex != 0)
        ThumbsGrid.ScrollTo(rowIndex);
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

    private void Face_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      if (e.ClickCount != 2) return;
      var face = (Face)((FrameworkElement)sender).DataContext;
      App.Core.MediaItems.Current = face.MediaItem;
      WindowCommands.SwitchToFullScreen();
      App.WMain.SetMediaItemSource(face.MediaItem);
    }

    private void ControlSizeChanged(object sender, SizeChangedEventArgs e) => Reload();
    private void BtnSortClick(object sender, RoutedEventArgs e) => Sort();
    private void BtnSamePersonClick(object sender, RoutedEventArgs e) {
      App.Core.Faces.SetSelectedAsSamePerson();
      if (ChbAutoSort.IsChecked == true)
        Sort();
    }

    private void BtnNotThisPersonClick(object sender, RoutedEventArgs e) {
      App.Core.Faces.SetSelectedAsNotThisPerson();
      if (ChbAutoSort.IsChecked == true)
        Sort();
    }

    private void BtnAnotherPersonClick(object sender, RoutedEventArgs e) {
      App.Core.Faces.SetSelectedAsAnotherPerson();
      if (ChbAutoSort.IsChecked == true)
        Sort();
    }
  }
}
