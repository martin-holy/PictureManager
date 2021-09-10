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
  public partial class SegmentMatchingControl : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private readonly WorkTask _workTask = new();
    private readonly IProgress<int> _progress;
    private string _title;
    private bool _loading;
    private readonly int _segmentGridWidth = 100 + 6; //border, margin, padding, ... //TODO find the real value
    private List<MediaItem> _mediaItems;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }

    public SegmentMatchingControl() {
      InitializeComponent();

      Title = "Segment Matching";

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
        App.Core.Segments.SetSelectedAsSamePerson();
        App.Core.Segments.DeselectAll();
        _ = SortAndReload();
      };

      BtnUnknown.Click += (o, e) => {
        App.Core.Segments.SetSelectedAsUnknown();
        App.Core.Segments.DeselectAll();
        _ = SortAndReload();
      };

      BtnGroupConfirmed.Click += (o, e) => _ = Reload(false, true);

      BtnGroupSegments.Click += async (o, e) => {
        await Reload(true, false);
        UpdateLayout();
        SegmentsGrid.ScrollToTop();
      };

      BtnCompare.Click += async (o, e) => {
        if (_loading) return;
        await CompareAsync();
        _ = SortAndReload(true, true);
      };

      BtnCompareAllGroups.Click += (o, e) => _ = LoadSegmentsAsync(true);

      BtnSort.Click += (o, e) => _ = SortAndReload(true, true);

      if (AppCore.OnToggleKeyword?.IsRegistered(this) != true)
        AppCore.OnToggleKeyword += (o, e) => _ = SortAndReload();

      if (AppCore.OnSetPerson?.IsRegistered(this) != true)
        AppCore.OnSetPerson += (o, e) => _ = SortAndReload();
    }

    public async Task LoadSegmentsAsync(bool withPersonOnly) {
      _loading = true;
      await _workTask.Cancel();

      SegmentsGrid.ClearRows();
      SegmentsGrid.AddGroup(new VirtualizingWrapPanelGroupItem[] { new() { Icon = IconName.People, Title = "?" } });
      ConfirmedSegmentsGrid.ClearRows();
      App.Core.Segments.GroupSegments = false;
      var segments = App.Core.Segments.GetSegments(_mediaItems, withPersonOnly);

      App.Ui.AppInfo.ResetProgressBars(segments.Length);

      await _workTask.Start(Task.Run(async () => {
        await foreach (var segment in App.Core.Segments.LoadSegmentsAsync(segments, _progress, _workTask.Token))
          await App.Core.RunOnUiThread(() => SegmentsGrid.AddItem(segment, _segmentGridWidth));
      }));

      _loading = false;
      _ = SortAndReload(App.Core.Segments.GroupSegments, true);
    }

    public void SetMediaItems(List<MediaItem> mediaItems) => _mediaItems = mediaItems;

    public async Task CompareAsync() {
      await _workTask.Cancel();
      await App.Core.Segments.AddSegmentsForComparison();
      App.Ui.AppInfo.ResetProgressBars(App.Core.Segments.Loaded.Count);
      await _workTask.Start(App.Core.Segments.FindSimilaritiesAsync(App.Core.Segments.Loaded, _progress, _workTask.Token));
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

    public async Task SortAndReload() => await SortAndReload(ChbAutoSort.IsChecked == true, ChbAutoSort.IsChecked == true);

    public async Task SortAndReload(bool segments, bool confirmedSegments) {
      await Sort(segments, confirmedSegments);
      await Reload(segments, confirmedSegments);
    }

    private static async Task Sort(bool segments, bool confirmedSegments) {
      if (segments) await App.Core.Segments.ReloadLoadedGroupedByPersonAsync();
      if (confirmedSegments) await App.Core.Segments.ReloadConfirmedSegments();
    }

    private async Task Reload(bool segments, bool confirmedSegments) {
      if (segments) await ReloadLoadedSegments();
      if (confirmedSegments) ReloadConfirmedSegments();
    }

    private async Task ReloadLoadedSegments() {
      if (_loading) return;
      var rowIndex = GetTopRowIndex(SegmentsGrid);
      var itemToScrollTo = SegmentsGrid.GetFirstItemFromRow(rowIndex);
      SegmentsGrid.ClearRows();
      SegmentsGrid.UpdateMaxRowWidth();

      if (App.Core.Segments.GroupSegments) {
        if (App.Core.Segments.LoadedGroupedByPerson.Count == 0)
          await App.Core.Segments.ReloadLoadedGroupedByPersonAsync();

        foreach (var group in App.Core.Segments.LoadedGroupedByPerson) {
          var groupTitle = group[0].Person != null ? group[0].Person.Title : group[0].PersonId.ToString();
          SegmentsGrid.AddGroup(IconName.People, groupTitle);

          foreach (var segment in group)
            SegmentsGrid.AddItem(segment, _segmentGridWidth);
        }
      }
      else {
        SegmentsGrid.AddGroup(IconName.People, "?");
        foreach (var segment in App.Core.Segments.Loaded)
          SegmentsGrid.AddItem(segment, _segmentGridWidth);
      }

      if (rowIndex > 0)
        SegmentsGrid.ScrollTo(itemToScrollTo);
    }

    private void ReloadConfirmedSegments() {
      if (_loading) return;
      var itemToScrollTo = ConfirmedSegmentsGrid.GetFirstItemFromRow(GetTopRowIndex(ConfirmedSegmentsGrid));
      ConfirmedSegmentsGrid.ClearRows();
      ConfirmedSegmentsGrid.UpdateMaxRowWidth();

      if (App.Core.Segments.GroupConfirmedSegments) {
        foreach (var (personId, segment, similar) in App.Core.Segments.ConfirmedSegments) {
          var groupTitle = segment.Person != null ? segment.Person.Title : personId.ToString();
          ConfirmedSegmentsGrid.AddGroup(IconName.People, groupTitle);
          ConfirmedSegmentsGrid.AddItem(segment, _segmentGridWidth);

          foreach (var simGroup in similar.OrderByDescending(x => x.sim))
            ConfirmedSegmentsGrid.AddItem(simGroup.segment, _segmentGridWidth);
        }
      }
      else {
        foreach (var group in App.Core.Segments.ConfirmedSegments
          .GroupBy(x => {
            if (x.segment.Person == null) return "Unknown";
            if (x.segment.Person.DisplayKeywords == null) return string.Empty;
            return string.Join(", ", x.segment.Person.DisplayKeywords.Select(k => k.Title));
          })
          .OrderBy(g => g.First().personId < 0).ThenBy(g => g.Key)) {

          // add group
          if (!string.IsNullOrEmpty(group.Key) && !group.Key.Equals("Unknown"))
            ConfirmedSegmentsGrid.AddGroup(IconName.Tag, group.Key);
          if (group.Key.Equals("Unknown"))
            ConfirmedSegmentsGrid.AddGroup(IconName.People, group.Key);

          // add people
          foreach (var (_, segment, _) in group)
            ConfirmedSegmentsGrid.AddItem(segment, _segmentGridWidth);
        }
      }

      ConfirmedSegmentsGrid.ScrollTo(itemToScrollTo);
    }

    private void Segment_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
      var (isCtrlOn, isShiftOn) = InputUtils.GetKeyboardModifiers(e);
      var segment = (Segment)((FrameworkElement)sender).DataContext;
      var list = ((FrameworkElement)sender).TryFindParent<StackPanel>()?.DataContext is VirtualizingWrapPanelRow row && row.Group != null
        ? row.Group.Items.Cast<Segment>().ToList()
        : new List<Segment>() { segment };
      App.Core.Segments.Select(list, segment, isCtrlOn, isShiftOn);
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
      ConfirmedSegmentsGrid.ScrollToTop();
    }
  }
}
