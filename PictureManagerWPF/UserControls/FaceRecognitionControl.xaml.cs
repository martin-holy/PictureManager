using PictureManager.Commands;
using PictureManager.CustomControls;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using System;
using System.Collections.Generic;
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
    private readonly IProgress<int> _progressA;
    private readonly IProgress<int> _progressB;
    private string _title;
    private bool _loading;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }

    public FaceRecognitionControl() {
      InitializeComponent();

      Title = "Face Recognition";

      _progressA = new Progress<int>(x => {
        App.Ui.AppInfo.ProgressBarValueA = x;
      });

      _progressB = new Progress<int>(x => {
        App.Ui.AppInfo.ProgressBarValueB = x;
      });

      FacesGrid.Rows = new();
      ConfirmedFacesGrid.Rows = new();
    }

    public async void LoadFaces(List<MediaItem> mediaItems) {
      // cancel previous work
      if (_workTask != null) {
        _cts?.Cancel();
        await _workTask;
      }

      _cts?.Dispose();
      _cts = new();

      _loading = true;
      FacesGrid.ClearRows();
      ConfirmedFacesGrid.ClearRows();
      App.Ui.AppInfo.ResetProgressBarA(mediaItems.Count);
      App.Ui.AppInfo.ResetProgressBarB(1);

      _workTask = Task.Run(async () => {
        await App.Core.RunOnUiThread(() => {
          FacesGrid.AddGroup(new VirtualizingWrapPanelGroupItem[] { new() { Icon = IconName.People, Title = "?" } });
        });

        await foreach (var face in App.Core.Faces.GetFacesAsync(mediaItems, _progressA, _cts.Token))
          await App.Core.RunOnUiThread(() => { AddFaceToGrid(FacesGrid, face); });

        await App.Core.RunOnUiThread(() => { App.Ui.AppInfo.ResetProgressBarB(App.Core.Faces.Loaded.Count); });
        App.Core.Faces.FindSimilarities(_progressB);
      });

      await _workTask;
      _loading = false;
      _cts?.Dispose();
      _cts = null;

      App.Core.Faces.SortConfirmed();
      Reload();

      if (App.Core.Faces.Helper.IsModified)
        App.Core.Faces.Helper.SaveToFile(App.Core.Faces.All);
      if (App.Core.Faces.Helper.AreTablePropsModified)
        App.Core.Faces.Helper.SaveTablePropsToFile();
    }

    private static void AddFaceToGrid(VirtualizingWrapPanel grid, Face face) {
      const int itemOffset = 6; //border, margin, padding, ... //TODO find the real value
      grid.AddItem(face, 100 + itemOffset);
    }

    private static int GetTopRowIndex(VirtualizingWrapPanel panel) {
      var rowIndex = 0;
      VisualTreeHelper.HitTest(panel, null, (e) => {
        if (e.VisualHit is FrameworkElement elm) {
          switch (elm.DataContext) {
            case VirtualizingWrapPanelGroup g: {
              for (var i = 0; i < panel.Rows.Count; i++) {
                if (panel.Rows[i] == g) {
                  rowIndex = i;
                  break;
                }
              }
              return HitTestResultBehavior.Stop;
            }
            case Face f: {
              rowIndex = panel.GetRowIndex(f);
              return HitTestResultBehavior.Stop;
            }
          }
        }
        return HitTestResultBehavior.Continue;
      }, new PointHitTestParameters(new Point(10, 40)));

      return rowIndex;
    }

    private async void Sort() {
      if (BtnGroupFaces.IsChecked == true)
        await App.Core.Faces.SortLoadedAsync();
      App.Core.Faces.SortConfirmed();
      Reload();
    }

    private void Reload() {
      ReloadFaces();
      ReloadConfirmedFaces();
    }

    private async void ReloadFaces() {
      if (_loading) return;
      var rowIndex = GetTopRowIndex(FacesGrid);
      FacesGrid.ClearRows();
      FacesGrid.UpdateMaxRowWidth();

      if (BtnGroupFaces.IsChecked == true) {
        if (App.Core.Faces.LoadedInGroups.Count == 0)
          await App.Core.Faces.SortLoadedAsync();

        foreach (var group in App.Core.Faces.LoadedInGroups.Where(x => x.Count > 0)) {
          var groupTitle = group[0].Person != null ? group[0].Person.Title : group[0].PersonId.ToString();
          FacesGrid.AddGroup(new VirtualizingWrapPanelGroupItem[] {
            new() { Icon = IconName.People, Title = groupTitle } });

          foreach (var face in group)
            AddFaceToGrid(FacesGrid, face);
        }
      }
      else {
        FacesGrid.AddGroup(new VirtualizingWrapPanelGroupItem[] { new() { Icon = IconName.People, Title = "?" } });
        foreach (var face in App.Core.Faces.Loaded.OrderBy(x => x.MediaItem.FileName))
          AddFaceToGrid(FacesGrid, face);
      }

      if (rowIndex != 0)
        FacesGrid.ScrollTo(rowIndex);
    }

    private void ReloadConfirmedFaces() {
      if (_loading) return;
      var rowIndex = GetTopRowIndex(ConfirmedFacesGrid);
      ConfirmedFacesGrid.ClearRows();
      ConfirmedFacesGrid.UpdateMaxRowWidth();

      if (BtnGroupConfirmed.IsChecked == true) {
        foreach (var group in App.Core.Faces.ConfirmedFaces) {
          var groupTitle = group.face.Person != null ? group.face.Person.Title : group.personId.ToString();
          ConfirmedFacesGrid.AddGroup(new VirtualizingWrapPanelGroupItem[] {
            new() { Icon = IconName.People, Title = groupTitle } });

          AddFaceToGrid(ConfirmedFacesGrid, group.face);
          foreach (var simGroup in group.similar.OrderByDescending(x => x.sim))
            AddFaceToGrid(ConfirmedFacesGrid, simGroup.face);
        }
      }
      else {
        foreach (var group in App.Core.Faces.ConfirmedFaces)
          AddFaceToGrid(ConfirmedFacesGrid, group.face);
      }

      if (rowIndex != 0)
        ConfirmedFacesGrid.ScrollTo(rowIndex);
    }

    public void ChangePerson(Person person) {
      var facesA = App.Core.Faces.Selected.Where(x => x.PersonId != 0).GroupBy(x => x.PersonId);
      var facesB = App.Core.Faces.Selected.Where(x => x.PersonId == 0);
      var groupsIds = facesA.Select(x => x.Key).OrderByDescending(x => x);

      if (groupsIds.Any())
        if (!MessageDialog.Show("Change Person", $"Set Person ({person.Title}) to Faces with ID ({string.Join(", ", groupsIds.ToArray())})?", true)) return;

      foreach (var group in facesA)
        App.Core.Faces.ChangePerson(group.Key, person);

      foreach (var face in facesB)
        Faces.ChangePerson(face, person);

      if (ChbAutoSort.IsChecked == true)
        Sort();
      else {
        App.Core.Faces.SortConfirmed();
        ReloadConfirmedFaces();
      }
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

      MoveControlButtons();
      App.Core.Faces.Select(isCtrlOn, isShiftOn, face);
    }

    private void MoveControlButtons() {
      var mouseLoc = Mouse.GetPosition(this);
      mouseLoc.Y += ControlButtons.ActualHeight + 10;
      mouseLoc.X -= ControlButtons.ActualWidth / 2;
      ControlButtons.RenderTransform = new TranslateTransform(mouseLoc.X, mouseLoc.Y);
    }

    private void Face_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      if (e.ClickCount != 2) return;
      var face = (Face)((FrameworkElement)sender).DataContext;
      App.Core.MediaItems.Current = face.MediaItem;
      WindowCommands.SwitchToFullScreen();
      App.WMain.SetMediaItemSource(face.MediaItem);
    }

    private void ControlSizeChanged(object sender, SizeChangedEventArgs e) {
      if (_loading) return;
      Reload();
      UpdateLayout();
      ConfirmedFacesGrid.ScrollToTop();
    }

    private void BtnSortClick(object sender, RoutedEventArgs e) => Sort();

    private void BtnSamePersonClick(object sender, RoutedEventArgs e) {
      App.Core.Faces.SetSelectedAsSamePerson();
      if (ChbAutoSort.IsChecked == true)
        Sort();
      else {
        App.Core.Faces.SortConfirmed();
        ReloadConfirmedFaces();
      }
    }

    /*private void BtnNotThisPersonClick(object sender, RoutedEventArgs e) {
      App.Core.Faces.SetSelectedAsNotThisPerson();
      if (ChbAutoSort.IsChecked == true)
        Sort();
      else {
        App.Core.Faces.SortConfirmed();
        ReloadConfirmedFaces();
      }
    }*/

    private void BtnAnotherPersonClick(object sender, RoutedEventArgs e) {
      App.Core.Faces.SetSelectedAsAnotherPerson();
      if (ChbAutoSort.IsChecked == true)
        Sort();
      else {
        App.Core.Faces.SortConfirmed();
        ReloadConfirmedFaces();
      }
    }

    private async void BtnNotAFaceClick(object sender, RoutedEventArgs e) {
      if (!MessageDialog.Show("Delete Confirmation", "Do you really want to delete selected faces?", true)) return;
      App.Core.Faces.DeleteSelected();
      if (ChbAutoSort.IsChecked == true) {
        await App.Core.Faces.SortLoadedAsync();
        ReloadFaces();
      }
    }

    private void BtnGroupConfirmed_Click(object sender, RoutedEventArgs e) {
      ReloadConfirmedFaces();
    }

    private void BtnGroupFaces_Click(object sender, RoutedEventArgs e) {
      FacesGrid.ScrollToTop();
      UpdateLayout();
      ReloadFaces();
    }
  }
}
