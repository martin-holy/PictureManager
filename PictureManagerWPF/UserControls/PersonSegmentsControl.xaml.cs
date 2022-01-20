using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MH.UI.WPF.Converters;
using MH.Utils.Extensions;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using PictureManager.Views;

namespace PictureManager.UserControls {
  public partial class PersonSegmentsControl : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged.Invoke(this, new(name));

    private readonly int _segmentGridWidth = 100 + 6; //border, margin, padding, ... //TODO find the real value
    private PersonM _person;

    public List<SegmentM> AllSegments { get; } = new();
    public PersonM Person { get => _person; set { _person = value; OnPropertyChanged(); } }

    public PersonSegmentsControl() {
      InitializeComponent();
      AttachEvents();
    }

    private void AttachEvents() {
      DragDropFactory.SetDrag(AllSegmentsGrid, CanDrag);
      DragDropFactory.SetDrag(TopSegmentsGrid, CanDrag);
      DragDropFactory.SetDrop(TopSegmentsGrid, CanDrop, TopSegmentsDrop);

      BtnReload.Click += (o, e) => _ = ReloadPersonSegmentsAsync(Person);

      if (AppCore.OnToggleKeyword?.IsRegistered(this) != true)
        AppCore.OnToggleKeyword += (o, e) => _ = ReloadPersonSegmentsAsync(Person);

      if (AppCore.OnSetPerson?.IsRegistered(this) != true)
        AppCore.OnSetPerson += (o, e) => _ = ReloadPersonSegmentsAsync(Person);
    }

    private object CanDrag(MouseEventArgs e) => (e.OriginalSource as FrameworkElement)?.DataContext as SegmentV;

    private DragDropEffects CanDrop(DragEventArgs e, object source, object data) {
      if (AllSegmentsGrid.Equals(source) && Person.Segments?.Contains((data as SegmentV)?.Segment) != true)
        return DragDropEffects.Copy;
      if (TopSegmentsGrid.Equals(source) && data != (e.OriginalSource as FrameworkElement)?.DataContext)
        return DragDropEffects.Move;

      return DragDropEffects.None;
    }

    private void TopSegmentsDrop(DragEventArgs e, object source, object data) {
      if (data is not SegmentV segmentV) return;

      Person.Segments = ListExtensions.Toggle(Person.Segments, segmentV.Segment, true);
      Person.OnPropertyChanged(nameof(Person.Segments));

      if (Person.Segments?.Count > 0)
        Person.Segment = Person.Segments[0];

      App.Core.PeopleM.DataAdapter.IsModified = true;
      ReloadTopSegments();
    }

    private void ReloadTopSegments() {
      TopSegmentsGrid.ClearRows();
      if (Person.Segments == null) return;
      UpdateLayout();
      TopSegmentsGrid.UpdateMaxRowWidth();

      foreach (var segment in Person.Segments)
        TopSegmentsGrid.AddItem(segment, _segmentGridWidth);

      TopSegmentsGrid.ScrollToTop();
    }

    public async Task ReloadPersonSegmentsAsync(PersonM person) {
      Person = person;
      AllSegments.Clear();
      AllSegmentsGrid.ClearRows();
      if (person == null) return;

      UpdateLayout();
      AllSegmentsGrid.UpdateMaxRowWidth();

      ReloadTopSegments();

      await Task.Run(async () => {
        foreach (var group in App.Core.SegmentsM.All
          .Where(x => x.PersonId == person.Id)
          .GroupBy(x => x.Keywords == null
            ? string.Empty
            : string.Join(", ", KeywordsM.GetAllKeywords(x.Keywords).Select(k => k.Name)))
          .OrderBy(x => x.Key)) {

          // add group
          if (!string.IsNullOrEmpty(group.Key))
            await Core.RunOnUiThread(() => AllSegmentsGrid.AddGroup("IconTag", group.Key));

          // add segments
          foreach (var segment in group.OrderBy(x => x.MediaItem.FileName)) {
            await segment.SetPictureAsync(App.Core.SegmentsM.SegmentSize);
            segment.MediaItem.SetThumbSize();
            await Core.RunOnUiThread(() => {
              segment.MediaItem.SetInfoBox();
              AllSegments.Add(segment);
              AllSegmentsGrid.AddItem(segment, _segmentGridWidth);
              OnPropertyChanged(nameof(AllSegments));
            });
          }
        }
      });

      AllSegmentsGrid.ScrollToTop();
    }

    private void OnSegmentSelected(object o, ClickEventArgs e) {
      if (e.DataContext is SegmentV segmentV)
        App.Core.SegmentsM.Select(AllSegments, segmentV.Segment, e.IsCtrlOn, e.IsShiftOn);
    }
  }
}
