using PictureManager.CustomControls;
using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PictureManager.UserControls {
  public partial class PeopleControl : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private readonly int _segmentGridWidth = 100 + 6; //border, margin, padding, ... //TODO find the real value
    private readonly WorkTask _workTask = new();
    private bool _loading;
    private string _title;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }

    public PeopleControl() {
      InitializeComponent();
      DataContext = this;
      Title = "People";

      foreach (var person in App.Core.People.All.Cast<Person>())
        person.UpdateDisplayKeywords();
    }

    public async Task Reload() {
      async Task AddPeopleAsync(string groupTitle, IEnumerable<Person> people, CancellationToken token) {
        // group people by keywords
        foreach (var group in people
          .GroupBy(p => p.DisplayKeywords == null
            ? string.Empty
            : string.Join(", ", p.DisplayKeywords.Select(dk => dk.Title)))
          .OrderBy(g => g.Key)) {

          // add group
          if (!group.Key.Equals(string.Empty)) {
            var groupItems = new List<VirtualizingWrapPanelGroupItem>() { new() { Icon = IconName.Tag, Title = group.Key } };
            if (!string.IsNullOrEmpty(groupTitle))
              groupItems.Insert(0, new() { Icon = IconName.People, Title = groupTitle });
            await App.Core.RunOnUiThread(() => PeopleGrid.AddGroup(groupItems.ToArray()));
          }

          // add people
          foreach (var person in group.OrderBy(p => p.Title)) {
            if (token.IsCancellationRequested) break;
            if (person.Segment != null) {
              await person.Segment.SetPictureAsync(App.Core.Segments.SegmentSize);
              person.Segment.MediaItem.SetThumbSize();
            }
            await App.Core.RunOnUiThread(() => PeopleGrid.AddItem(person, _segmentGridWidth));
          }
        }
      }

      _loading = true;
      await _workTask.Cancel();
      var itemToScrollTo = PeopleGrid.GetFirstItemFromRow(PeopleGrid.GetTopRowIndex());
      PeopleGrid.ClearRows();
      UpdateLayout();
      PeopleGrid.UpdateMaxRowWidth();

      await _workTask.Start(Task.Run(async () => {
        foreach (var group in App.Core.People.Items.OfType<ICatTreeViewGroup>().Where(x => !x.IsHidden)) {
          if (_workTask.Token.IsCancellationRequested) break;
          await App.Core.RunOnUiThread(() => PeopleGrid.AddGroup(IconName.People, group.Title));
          await AddPeopleAsync(group.Title, group.Items.Cast<Person>(), _workTask.Token);
        }

        var peopleWithoutGroup = App.Core.People.Items.OfType<Person>().ToArray();
        if (peopleWithoutGroup.Length > 0) {
          await App.Core.RunOnUiThread(() => PeopleGrid.AddGroup(IconName.People, string.Empty));
          await AddPeopleAsync(string.Empty, peopleWithoutGroup, _workTask.Token);
        }
      }));

      _loading = false;
      PeopleGrid.ScrollTo(itemToScrollTo);
    }

    private async void ControlSizeChanged(object sender, SizeChangedEventArgs e) {
      if (_loading) return;
      await Reload();
    }

    private void Segment_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      if (sender is SegmentControl fc && fc.DataContext != null) {
        var segment = (Segment)fc.DataContext;
        App.Core.Segments.DeselectAll();
        App.Core.People.Select(null, segment.Person, false, false);
      }
    }
  }
}