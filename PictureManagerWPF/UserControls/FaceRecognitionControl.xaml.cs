﻿using MahApps.Metro.Controls;
using PictureManager.CustomControls;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using PictureManager.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PictureManager.UserControls {
  public partial class FaceRecognitionControl : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private readonly WorkTask _workTask = new();
    private readonly IProgress<int> _progress;
    private string _title;
    private bool _loading;
    private readonly int _faceGridWidth = 100 + 6; //border, margin, padding, ... //TODO find the real value

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public List<MediaItem> MediaItems { get; set; }

    public FaceRecognitionControl() {
      InitializeComponent();

      Title = "Face Recognition";

      _progress = new Progress<int>(x => {
        App.Ui.AppInfo.ProgressBarValueA = x;
        App.Ui.AppInfo.ProgressBarValueB = x;
      });

      AttachEvents();
    }

    private void AttachEvents() {
      BtnSamePerson.Click += (o, e) => {
        App.Core.Faces.SetSelectedAsSamePerson();
        App.Core.Faces.DeselectAll();
        _ = SortAndReload(ChbAutoSort.IsChecked == true, ChbAutoSort.IsChecked == true);
      };

      BtnAnotherPerson.Click += (o, e) => {
        App.Core.Faces.SetSelectedAsAnotherPerson();
        App.Core.Faces.DeselectAll();
        _ = SortAndReload(ChbAutoSort.IsChecked == true, ChbAutoSort.IsChecked == true);
      };

      BtnNotAFace.Click += (o, e) => {
        if (!MessageDialog.Show("Delete Confirmation", "Do you really want to delete selected faces?", true)) return;
        App.Core.Faces.DeleteSelected();
        App.Core.Faces.DeselectAll();
        _ = SortAndReload(ChbAutoSort.IsChecked == true, ChbAutoSort.IsChecked == true);
      };

      BtnGroupConfirmed.Click += (o, e) => Reload(false, true);

      BtnGroupFaces.Click += (o, e) => {
        FacesGrid.ScrollToTop();
        UpdateLayout();
        Reload(true, false);
      };

      BtnDetectNew.Click += (o, e) => _ = LoadFaces(true, false);

      BtnCompare.Click += async (o, e) => {
        if (_loading) return;
        await CompareAsync();
        _ = SortAndReload(true, true);
      };

      BtnCompareAllGroups.Click += (o, e) => _ = LoadFaces(false, true);

      BtnSort.Click += (o, e) => _ = SortAndReload(true, true);
    }

    public async Task LoadFaces(bool detectNewFaces, bool withPersonOnly) {
      _loading = true;
      await _workTask.Cancel();

      FacesGrid.ClearRows();
      FacesGrid.AddGroup(new VirtualizingWrapPanelGroupItem[] { new() { Icon = IconName.People, Title = "?" } });
      ConfirmedFacesGrid.ClearRows();
      App.Core.Faces.GroupFaces = false;
      App.Ui.AppInfo.ResetProgressBars(withPersonOnly
        ? (App.Core.Faces.All.Cast<Face>().GroupBy(x => x.PersonId).Count() - 1) * App.Core.Faces.MaxFacesInGroup
        : MediaItems.Count);

      await _workTask.Start(Task.Run(async () => {
        if (withPersonOnly) {
          await foreach (var face in App.Core.Faces.GetAllFacesAsync(_progress, _workTask.Token))
            await App.Core.RunOnUiThread(() => FacesGrid.AddItem(face, _faceGridWidth));
        }
        else {
          await foreach (var face in App.Core.Faces.GetFacesAsync(MediaItems, detectNewFaces, _progress, _workTask.Token))
            await App.Core.RunOnUiThread(() => FacesGrid.AddItem(face, _faceGridWidth));
        }
      }));

      //TODO count real value for withPersonOnly
      await App.Core.RunOnUiThread(() => _progress.Report(App.Ui.AppInfo.ProgressBarMaxA));

      _loading = false;
      _ = SortAndReload(App.Core.Faces.GroupFaces, true);

      if (App.Core.Faces.Helper.AreTablePropsModified)
        App.Core.Faces.Helper.SaveTablePropsToFile();
    }

    public async Task CompareAsync() {
      await _workTask.Cancel();
      App.Ui.AppInfo.ResetProgressBars(App.Core.Faces.Loaded.Count);
      await _workTask.Start(App.Core.Faces.FindSimilaritiesAsync(_progress, _workTask.Token));
    }

    private static int GetTopRowIndex(VirtualizingWrapPanel panel) {
      var rowIndex = 0;
      VisualTreeHelper.HitTest(panel, null, (e) => {
        if (e.VisualHit is FrameworkElement elm) {
          rowIndex = panel.GetRowIndex(elm);
          return HitTestResultBehavior.Stop;
        }
        return HitTestResultBehavior.Continue;
      }, new PointHitTestParameters(new Point(10, 40)));

      return rowIndex;
    }

    private async Task SortAndReload(bool faces, bool confirmedFaces) {
      await Sort(faces, confirmedFaces);
      Reload(faces, confirmedFaces);
    }

    private static async Task Sort(bool faces, bool confirmedFaces) {
      if (faces) await App.Core.Faces.ReloadLoadedInGroupsAsync();
      if (confirmedFaces) App.Core.Faces.ReloadConfirmedFaces();
    }

    private void Reload(bool faces, bool confirmedFaces) {
      if (faces) _ = ReloadFaces();
      if (confirmedFaces) ReloadConfirmedFaces();
    }

    private async Task ReloadFaces() {
      if (_loading) return;
      var rowIndex = GetTopRowIndex(FacesGrid);
      FacesGrid.ClearRows();
      FacesGrid.UpdateMaxRowWidth();

      if (App.Core.Faces.GroupFaces) {
        if (App.Core.Faces.LoadedInGroups.Count == 0)
          await App.Core.Faces.ReloadLoadedInGroupsAsync();

        foreach (var group in App.Core.Faces.LoadedInGroups) {
          var groupTitle = group[0].Person != null ? group[0].Person.Title : group[0].PersonId.ToString();
          FacesGrid.AddGroup(new VirtualizingWrapPanelGroupItem[] {
            new() { Icon = IconName.People, Title = groupTitle } });

          foreach (var face in group)
            FacesGrid.AddItem(face, _faceGridWidth);
        }
      }
      else {
        FacesGrid.AddGroup(new VirtualizingWrapPanelGroupItem[] { new() { Icon = IconName.People, Title = "?" } });
        foreach (var face in App.Core.Faces.Loaded)
          FacesGrid.AddItem(face, _faceGridWidth);
      }

      if (rowIndex != 0)
        FacesGrid.ScrollTo(rowIndex);
    }

    private void ReloadConfirmedFaces() {
      if (_loading) return;
      var rowIndex = GetTopRowIndex(ConfirmedFacesGrid);
      ConfirmedFacesGrid.ClearRows();
      ConfirmedFacesGrid.UpdateMaxRowWidth();

      if (App.Core.Faces.GroupConfirmedFaces) {
        foreach (var (personId, face, similar) in App.Core.Faces.ConfirmedFaces) {
          var groupTitle = face.Person != null ? face.Person.Title : personId.ToString();
          ConfirmedFacesGrid.AddGroup(new VirtualizingWrapPanelGroupItem[] {
            new() { Icon = IconName.People, Title = groupTitle } });

          ConfirmedFacesGrid.AddItem(face, _faceGridWidth);
          foreach (var simGroup in similar.OrderByDescending(x => x.sim))
            ConfirmedFacesGrid.AddItem(simGroup.face, _faceGridWidth);
        }
      }
      else {
        foreach (var (_, face, _) in App.Core.Faces.ConfirmedFaces)
          ConfirmedFacesGrid.AddItem(face, _faceGridWidth);
      }

      if (rowIndex != 0)
        ConfirmedFacesGrid.ScrollTo(rowIndex);
    }

    public void ChangePerson(Person person) {
      var facesA = App.Core.Faces.Selected.Where(x => x.PersonId != 0).GroupBy(x => x.PersonId);
      var facesB = App.Core.Faces.Selected.Where(x => x.PersonId == 0);
      var groupsIds = facesA.Select(x => x.Key).OrderByDescending(x => x);

      if (groupsIds.Any() && !MessageDialog.Show("Change Person", $"Set Person ({person.Title}) to Faces with ID ({string.Join(", ", groupsIds.ToArray())})?", true))
        return;

      foreach (var group in facesA)
        App.Core.Faces.ChangePerson(group.Key, person);

      foreach (var face in facesB)
        Faces.ChangePerson(face, person);

      App.Core.Faces.DeselectAll();
      _ = SortAndReload(ChbAutoSort.IsChecked == true, ChbAutoSort.IsChecked == true);
    }

    private void Face_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
      var (isCtrlOn, isShiftOn) = InputUtils.GetKeyboardModifiers(e);
      var face = (Face)((FrameworkElement)sender).DataContext;
      var list = ((FrameworkElement)sender).TryFindParent<StackPanel>()?.DataContext is VirtualizingWrapPanelRow row && row.Group != null
        ? row.Group.Items.Cast<Face>().ToList()
        : new List<Face>() { face };
      App.Core.Faces.Select(isCtrlOn, isShiftOn, list, face);
      MoveControlButtons();
    }

    private void MoveControlButtons() {
      var mouseLoc = Mouse.GetPosition(this);
      mouseLoc.Y += ControlButtons.Height + 10;
      mouseLoc.X -= ControlButtons.Width / 2;
      ControlButtons.RenderTransform = new TranslateTransform(mouseLoc.X, mouseLoc.Y);
    }

    private void ControlSizeChanged(object sender, SizeChangedEventArgs e) {
      if (_loading) return;
      Reload(true, true);
      UpdateLayout();
      ConfirmedFacesGrid.ScrollToTop();
    }
  }
}
