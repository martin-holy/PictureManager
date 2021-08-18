using MahApps.Metro.Controls;
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
    private List<MediaItem> _mediaItems;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }

    public FaceRecognitionControl() {
      InitializeComponent();

      Title = "Face Recognition";

      _progress = new Progress<int>(x => {
        App.Ui.AppInfo.ProgressBarValueA = x;
        App.Ui.AppInfo.ProgressBarValueB = x;
      });

      AttachEvents();

      foreach (var person in App.Core.People.All.Cast<Person>())
        person.UpdateDisplayKeywords();
    }

    private void AttachEvents() {
      BtnSamePerson.Click += (o, e) => {
        App.Core.Faces.SetSelectedAsSamePerson();
        App.Core.Faces.DeselectAll();
        _ = SortAndReload(ChbAutoSort.IsChecked == true, ChbAutoSort.IsChecked == true);
      };

      BtnUnknown.Click += (o, e) => {
        App.Core.Faces.SetSelectedAsUnknown();
        App.Core.Faces.DeselectAll();
        _ = SortAndReload(ChbAutoSort.IsChecked == true, ChbAutoSort.IsChecked == true);
      };

      BtnNotAFace.Click += (o, e) => {
        if (!MessageDialog.Show("Delete Confirmation", "Do you really want to delete selected faces?", true)) return;
        App.Core.Faces.DeleteSelected();
        _ = SortAndReload(ChbAutoSort.IsChecked == true, ChbAutoSort.IsChecked == true);
      };

      BtnGroupConfirmed.Click += (o, e) => _ = Reload(false, true);

      BtnGroupFaces.Click += async (o, e) => {
        await Reload(true, false);
        UpdateLayout();
        FacesGrid.ScrollToTop();
      };

      BtnDetectNew.Click += (o, e) => _ = LoadFacesAsync(true, false);

      BtnCompare.Click += async (o, e) => {
        if (_loading) return;
        await CompareAsync();
        _ = SortAndReload(true, true);
      };

      BtnCompareAllGroups.Click += (o, e) => _ = LoadFacesAsync(false, true);

      BtnSort.Click += (o, e) => _ = SortAndReload(true, true);
    }

    public async Task LoadFacesAsync(bool detectNewFaces, bool withPersonOnly) {
      _loading = true;
      await _workTask.Cancel();

      FacesGrid.ClearRows();
      FacesGrid.AddGroup(new VirtualizingWrapPanelGroupItem[] { new() { Icon = IconName.People, Title = "?" } });
      ConfirmedFacesGrid.ClearRows();
      App.Core.Faces.GroupFaces = false;
      App.Ui.AppInfo.ResetProgressBars(withPersonOnly
        ? App.Core.Faces.All.Cast<Face>().Select(x => x.PersonId).Distinct().Count() - 1
        : _mediaItems.Count);

      await _workTask.Start(Task.Run(async () => {
        if (withPersonOnly) {
          await foreach (var face in App.Core.Faces.GetAllFacesAsync(_progress, _workTask.Token))
            await App.Core.RunOnUiThread(() => FacesGrid.AddItem(face, _faceGridWidth));
        }
        else {
          await foreach (var face in App.Core.Faces.GetFacesAsync(_mediaItems, detectNewFaces, _progress, _workTask.Token))
            await App.Core.RunOnUiThread(() => FacesGrid.AddItem(face, _faceGridWidth));
        }
      }));

      _loading = false;
      _ = SortAndReload(App.Core.Faces.GroupFaces, true);
    }

    public void SetMediaItems(List<MediaItem> mediaItems) {
      _mediaItems = mediaItems;
      App.Core.Faces.FacesForComparison = null;
    }

    public async Task CompareAsync() {
      await _workTask.Cancel();
      await App.Core.Faces.SetFacesForComparison();
      App.Ui.AppInfo.ResetProgressBars(App.Core.Faces.FacesForComparison.Count());
      await _workTask.Start(App.Core.Faces.FindSimilaritiesAsync(App.Core.Faces.FacesForComparison, _progress, _workTask.Token));
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

    public async Task SortAndReload(bool faces, bool confirmedFaces) {
      await Sort(faces, confirmedFaces);
      await Reload(faces, confirmedFaces);
    }

    private static async Task Sort(bool faces, bool confirmedFaces) {
      if (faces) await App.Core.Faces.ReloadLoadedGroupedByPersonAsync();
      if (confirmedFaces) await App.Core.Faces.ReloadConfirmedFaces();
    }

    private async Task Reload(bool faces, bool confirmedFaces) {
      if (faces) await ReloadLoadedFaces();
      if (confirmedFaces) ReloadConfirmedFaces();
    }

    private async Task ReloadLoadedFaces() {
      if (_loading) return;
      var itemToScrollTo = FacesGrid.GetFirstItemFromRow(GetTopRowIndex(FacesGrid));
      FacesGrid.ClearRows();
      FacesGrid.UpdateMaxRowWidth();

      if (App.Core.Faces.GroupFaces) {
        if (App.Core.Faces.LoadedGroupedByPerson.Count == 0)
          await App.Core.Faces.ReloadLoadedGroupedByPersonAsync();

        foreach (var group in App.Core.Faces.LoadedGroupedByPerson) {
          var groupTitle = group[0].Person != null ? group[0].Person.Title : group[0].PersonId.ToString();
          FacesGrid.AddGroup(IconName.People, groupTitle);

          foreach (var face in group)
            FacesGrid.AddItem(face, _faceGridWidth);
        }
      }
      else {
        FacesGrid.AddGroup(IconName.People, "?");
        foreach (var face in App.Core.Faces.Loaded)
          FacesGrid.AddItem(face, _faceGridWidth);
      }

      if (GetTopRowIndex(FacesGrid) > 0)
        FacesGrid.ScrollTo(itemToScrollTo);
    }

    private void ReloadConfirmedFaces() {
      if (_loading) return;
      var itemToScrollTo = ConfirmedFacesGrid.GetFirstItemFromRow(GetTopRowIndex(ConfirmedFacesGrid));
      ConfirmedFacesGrid.ClearRows();
      ConfirmedFacesGrid.UpdateMaxRowWidth();

      if (App.Core.Faces.GroupConfirmedFaces) {
        foreach (var (personId, face, similar) in App.Core.Faces.ConfirmedFaces) {
          var groupTitle = face.Person != null ? face.Person.Title : personId.ToString();
          ConfirmedFacesGrid.AddGroup(IconName.People, groupTitle);
          ConfirmedFacesGrid.AddItem(face, _faceGridWidth);

          foreach (var simGroup in similar.OrderByDescending(x => x.sim))
            ConfirmedFacesGrid.AddItem(simGroup.face, _faceGridWidth);
        }
      }
      else {
        foreach (var group in App.Core.Faces.ConfirmedFaces
          .GroupBy(x => {
            if (x.face.Person == null) return "Unknown";
            if (x.face.Person.DisplayKeywords == null) return string.Empty;
            return string.Join(", ", x.face.Person.DisplayKeywords.Select(k => k.Title));
          })
          .OrderBy(g => g.First().personId < 0).ThenBy(g => g.Key)) {

          // add group
          if (!string.IsNullOrEmpty(group.Key) && !group.Key.Equals("Unknown"))
            ConfirmedFacesGrid.AddGroup(IconName.Tag, group.Key);
          if (group.Key.Equals("Unknown"))
            ConfirmedFacesGrid.AddGroup(IconName.People, group.Key);

          // add people
          foreach (var (_, face, _) in group)
            ConfirmedFacesGrid.AddItem(face, _faceGridWidth);
        }
      }

      ConfirmedFacesGrid.ScrollTo(itemToScrollTo);
    }

    public void ChangePerson(Person person) {
      var faceCount = App.Core.Faces.SelectedCount > 1 ? $"'s ({App.Core.Faces.SelectedCount})" : string.Empty;
      if (!MessageDialog.Show("Change Person", $"Do you want to set ({person.Title}) to selected face{faceCount}?", true))
        return;

      App.Core.Faces.SetSelectedAsPerson(person);
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

    private async void ControlSizeChanged(object sender, SizeChangedEventArgs e) {
      if (_loading) return;
      await Reload(true, true);
      UpdateLayout();
      ConfirmedFacesGrid.ScrollToTop();
    }
  }
}
