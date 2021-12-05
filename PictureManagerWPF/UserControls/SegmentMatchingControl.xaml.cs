using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using MH.UI.WPF.Converters;
using MH.Utils;
using MH.Utils.Extensions;
using PictureManager.CustomControls;
using PictureManager.Domain.Models;
using PictureManager.Interfaces;
using PictureManager.Utils;
using PictureManager.Views;

namespace PictureManager.UserControls {
  public partial class SegmentMatchingControl : INotifyPropertyChanged, IMainTabsItem {
    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged.Invoke(this, new(name));

    #region IMainTabsItem implementation
    private string _title;

    public string IconName { get; set; }
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    #endregion

    private readonly WorkTask _workTask = new();
    private readonly IProgress<int> _progress;
    private bool _loading;
    private readonly int _segmentGridWidth = 100 + 6; //border, margin, padding, ... //TODO find the real value
    private List<MediaItemM> _mediaItems;

    public SegmentMatchingControl() {
      InitializeComponent();
      Title = "Segment Matching";
      IconName = "IconEquals";

      _progress = new Progress<int>(x => {
        App.Ui.AppInfo.ProgressBarValueA = x;
        App.Ui.AppInfo.ProgressBarValueB = x;
      });

      AttachEvents();

      foreach (var person in App.Ui.PeopleBaseVM.All.Values)
        person.UpdateDisplayKeywords();
    }

    private void AttachEvents() {
      BtnUnknown.Click += (o, e) => {
        App.Core.SegmentsM.SetSelectedAsUnknown();
        App.Core.SegmentsM.DeselectAll();
        AppCore.OnSetPerson?.Invoke(null, EventArgs.Empty);
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

      BtnOpenSegmentsDrawer.Click += (o, e) => _ = OpenSegmentsDrawer();

      if (AppCore.OnToggleKeyword?.IsRegistered(this) != true)
        AppCore.OnToggleKeyword += (o, e) => _ = SortAndReload();

      if (AppCore.OnSetPerson?.IsRegistered(this) != true)
        AppCore.OnSetPerson += (o, e) => _ = SortAndReload();

      DragDropFactory.SetDrag(this, (e) => (e.OriginalSource as FrameworkElement)?.DataContext as SegmentV);
    }

    public async Task LoadSegmentsAsync(bool withPersonOnly) {
      _loading = true;
      await _workTask.Cancel();

      SegmentsGrid.ClearRows();
      SegmentsGrid.AddGroup(new VirtualizingWrapPanelGroupItem[] { new() { Icon = Domain.IconName.People, Title = "?" } });
      ConfirmedSegmentsGrid.ClearRows();
      App.Core.SegmentsM.GroupSegments = false;
      var segments = App.Core.SegmentsM.GetSegments(_mediaItems, withPersonOnly);

      App.Ui.AppInfo.ResetProgressBars(segments.Length);

      await _workTask.Start(Task.Run(async () => {
        await foreach (var segment in App.Core.SegmentsM.LoadSegmentsAsync(segments, _progress, _workTask.Token))
          await App.Core.RunOnUiThread(() => SegmentsGrid.AddItem(segment, _segmentGridWidth));
      }));

      _loading = false;
      _ = SortAndReload(App.Core.SegmentsM.GroupSegments, true);
    }

    public void SetMediaItems(List<MediaItemM> mediaItems) => _mediaItems = mediaItems;

    public async Task CompareAsync() {
      await _workTask.Cancel();
      await App.Core.SegmentsM.AddSegmentsForComparison();
      App.Ui.AppInfo.ResetProgressBars(App.Core.SegmentsM.Loaded.Count);
      await _workTask.Start(App.Core.SegmentsM.FindSimilaritiesAsync(App.Core.SegmentsM.Loaded, _progress, _workTask.Token));
    }

    public async Task SortAndReload() => await SortAndReload(ChbAutoSort.IsChecked == true, ChbAutoSort.IsChecked == true);

    public async Task SortAndReload(bool segments, bool confirmedSegments) {
      await Sort(segments, confirmedSegments);
      await Reload(segments, confirmedSegments);
    }

    private static async Task Sort(bool segments, bool confirmedSegments) {
      if (segments) await App.Core.SegmentsM.ReloadLoadedGroupedByPersonAsync();
      if (confirmedSegments) await App.Core.SegmentsM.ReloadConfirmedSegments();
    }

    private async Task Reload(bool segments, bool confirmedSegments) {
      if (segments) await ReloadLoadedSegments();
      if (confirmedSegments) ReloadConfirmedSegments();
    }

    private async Task ReloadLoadedSegments() {
      if (_loading) return;
      var rowIndex = SegmentsGrid.GetTopRowIndex();
      var itemToScrollTo = SegmentsGrid.GetFirstItemFromRow(rowIndex);
      SegmentsGrid.ClearRows();
      SegmentsGrid.UpdateMaxRowWidth();

      if (App.Core.SegmentsM.GroupSegments) {
        if (App.Core.SegmentsM.LoadedGroupedByPerson.Count == 0)
          await App.Core.SegmentsM.ReloadLoadedGroupedByPersonAsync();

        foreach (var group in App.Core.SegmentsM.LoadedGroupedByPerson) {
          var groupTitle = group[0].Person != null ? group[0].Person.Name : group[0].PersonId.ToString();
          SegmentsGrid.AddGroup(Domain.IconName.People, groupTitle);

          foreach (var segment in group)
            SegmentsGrid.AddItem(segment, _segmentGridWidth);
        }
      }
      else {
        SegmentsGrid.AddGroup(Domain.IconName.People, "?");
        foreach (var segment in App.Core.SegmentsM.Loaded)
          SegmentsGrid.AddItem(segment, _segmentGridWidth);
      }

      if (rowIndex > 0)
        SegmentsGrid.ScrollTo(itemToScrollTo);
    }

    private void ReloadConfirmedSegments() {
      if (_loading) return;
      var itemToScrollTo = ConfirmedSegmentsGrid.GetFirstItemFromRow(ConfirmedSegmentsGrid.GetTopRowIndex());
      ConfirmedSegmentsGrid.ClearRows();
      ConfirmedSegmentsGrid.UpdateMaxRowWidth();

      if (App.Core.SegmentsM.GroupConfirmedSegments) {
        foreach (var (personId, segment, similar) in App.Core.SegmentsM.ConfirmedSegments) {
          var groupTitle = segment.Person != null ? segment.Person.Name : personId.ToString();
          ConfirmedSegmentsGrid.AddGroup(Domain.IconName.People, groupTitle);
          ConfirmedSegmentsGrid.AddItem(segment, _segmentGridWidth);

          foreach (var simGroup in similar.OrderByDescending(x => x.sim))
            ConfirmedSegmentsGrid.AddItem(simGroup.segment, _segmentGridWidth);
        }
      }
      else {
        foreach (var group in App.Core.SegmentsM.ConfirmedSegments
          .GroupBy(x => {
            if (x.segment.Person == null) return "Unknown";
            if (x.segment.Person.Keywords == null) return string.Empty;
            return string.Join(", ", App.Ui.PeopleBaseVM.All[x.segment.Person.Id].DisplayKeywords.Select(k => k.Name));
          })
          .OrderBy(g => g.First().personId < 0).ThenBy(g => g.Key)) {

          // add group
          if (!string.IsNullOrEmpty(group.Key) && !group.Key.Equals("Unknown"))
            ConfirmedSegmentsGrid.AddGroup(Domain.IconName.Tag, group.Key);
          if (group.Key.Equals("Unknown"))
            ConfirmedSegmentsGrid.AddGroup(Domain.IconName.People, group.Key);

          // add people
          foreach (var (_, segment, _) in group)
            ConfirmedSegmentsGrid.AddItem(segment, _segmentGridWidth);
        }
      }

      ConfirmedSegmentsGrid.ScrollTo(itemToScrollTo);
    }

    private void OnSegmentSelected(object o, ClickEventArgs e) {
      if (e.DataContext is SegmentV segmentV) {
        var list = ((FrameworkElement)e.Source).TryFindParent<StackPanel>()?.DataContext is VirtualizingWrapPanelRow { Group: { } } row
          ? row.Group.Items.Cast<SegmentM>().ToList()
          : new() { segmentV.Segment };
        App.Core.SegmentsM.Select(list, segmentV.Segment, e.IsCtrlOn, e.IsShiftOn);
      }
    }

    private async void ControlSizeChanged(object sender, SizeChangedEventArgs e) {
      if (_loading) return;
      await Reload(true, true);
      UpdateLayout();
      ConfirmedSegmentsGrid.ScrollToTop();
    }

    private static async Task OpenSegmentsDrawer() {
      App.WMain.ToolsTabs.Activate(App.WMain.ToolsTabs.TabSegments, true);
      App.WMain.RightSlidePanel.IsOpen = true;
      foreach (var segment in App.Core.SegmentsM.SegmentsDrawer) {
        await segment.SetPictureAsync(App.Core.SegmentsM.SegmentSize);
        segment.MediaItem.SetThumbSize();
        App.Ui.MediaItemsBaseVM.SetInfoBox(segment.MediaItem);
      }
    }
  }
}
