using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MH.Utils;
using PictureManager.CustomControls;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using PictureManager.ViewModels;
using PictureManager.ViewModels.Tree;

namespace PictureManager.UserControls {
  public partial class PeopleControl : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged.Invoke(this, new(name));

    private readonly int _segmentGridWidth = 100 + 6; //border, margin, padding, ... //TODO find the real value
    private readonly WorkTask _workTask = new();
    private bool _loading;
    private string _title;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }

    public PeopleControl() {
      InitializeComponent();
      DataContext = this;
      Title = "People";

      foreach (var person in App.Ui.PeopleBaseVM.All.Values)
        person.UpdateDisplayKeywords();
    }

    public async Task Reload() {
      async Task AddPeopleAsync(string groupTitle, IEnumerable<PersonBaseVM> people, CancellationToken token) {
        // group people by keywords
        foreach (var group in people
          .GroupBy(p => p.DisplayKeywords == null
            ? string.Empty
            : string.Join(", ", p.DisplayKeywords.Select(dk => dk.Name)))
          .OrderBy(g => g.Key)) {

          // add group
          if (!group.Key.Equals(string.Empty)) {
            var groupItems = new List<VirtualizingWrapPanelGroupItem>() { new() { Icon = IconName.Tag, Title = group.Key } };
            if (!string.IsNullOrEmpty(groupTitle))
              groupItems.Insert(0, new() { Icon = IconName.People, Title = groupTitle });
            await App.Core.RunOnUiThread(() => PeopleGrid.AddGroup(groupItems.ToArray()));
          }

          // add people
          foreach (var person in group.OrderBy(p => p.Model.Name)) {
            if (token.IsCancellationRequested) break;
            if (person.Model.Segment != null) {
              await person.Model.Segment.SetPictureAsync(App.Core.Segments.SegmentSize);
              person.Model.Segment.MediaItem.SetThumbSize();
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
        foreach (var group in App.Ui.PeopleTreeVM.Items.OfType<CategoryGroupTreeVM>().Where(x => !x.IsHidden)) {
          if (_workTask.Token.IsCancellationRequested) break;
          await App.Core.RunOnUiThread(() => PeopleGrid.AddGroup(IconName.People, group.BaseVM.Model.Name));
          await AddPeopleAsync(group.BaseVM.Model.Name, group.Items.Cast<PersonTreeVM>().Select(x => x.BaseVM), _workTask.Token);
        }

        var peopleWithoutGroup = App.Ui.PeopleTreeVM.Items.OfType<PersonTreeVM>().ToArray();
        if (peopleWithoutGroup.Length > 0) {
          await App.Core.RunOnUiThread(() => PeopleGrid.AddGroup(IconName.People, string.Empty));
          await AddPeopleAsync(string.Empty, peopleWithoutGroup.Select(x => x.BaseVM), _workTask.Token);
        }
      }));

      _loading = false;
      PeopleGrid.ScrollTo(itemToScrollTo);
    }

    private async void ControlSizeChanged(object sender, SizeChangedEventArgs e) {
      if (_loading) return;
      await Reload();
    }

    private void OnSegmentSelected(object sender, MouseButtonEventArgs e) {
      if (((FrameworkElement)sender).DataContext is Segment segment) {
        var (isCtrlOn, isShiftOn) = InputUtils.GetKeyboardModifiers(e);
        App.Core.Segments.DeselectAll();
        App.Ui.PeopleBaseVM.Select(null, App.Ui.PeopleBaseVM.All[segment.Person.Id], isCtrlOn, isShiftOn);
      }
    }
  }
}