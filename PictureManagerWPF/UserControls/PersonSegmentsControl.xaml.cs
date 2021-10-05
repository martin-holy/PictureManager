using PictureManager.Domain;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PictureManager.UserControls {
  public partial class PersonSegmentsControl : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private readonly int _segmentGridWidth = 100 + 6; //border, margin, padding, ... //TODO find the real value
    private Person _person;

    public List<Segment> AllSegments { get; } = new();
    public Person Person { get => _person; set { _person = value; OnPropertyChanged(); } }

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

    private object CanDrag(MouseEventArgs e) => (e.OriginalSource as FrameworkElement)?.DataContext as Segment;

    private DragDropEffects CanDrop(DragEventArgs e, object source, object data) {
      if (source == AllSegmentsGrid && Person.Segments?.Contains(data) != true)
        return DragDropEffects.Copy;
      if (source == TopSegmentsGrid && data != (e.OriginalSource as FrameworkElement)?.DataContext)
        return DragDropEffects.Move;

      return DragDropEffects.None;
    }

    private void TopSegmentsDrop(DragEventArgs e, object source, object data) {
      if (data is not Segment segment) return;

      Person.Segments = Extension.Toggle(Person.Segments, segment, true);
      Person.OnPropertyChanged(nameof(Person.Segments));

      if (Person.Segments?.Count > 0)
        Person.Segment = Person.Segments[0];

      App.Core.People.DataAdapter.IsModified = true;
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

    public async Task ReloadPersonSegmentsAsync(Person person) {
      Person = person;
      AllSegments.Clear();
      AllSegmentsGrid.ClearRows();
      if (person == null) return;

      UpdateLayout();
      AllSegmentsGrid.UpdateMaxRowWidth();

      ReloadTopSegments();

      await Task.Run(async () => {
        foreach (var group in App.Core.Segments.All.Cast<Segment>()
          .Where(x => x.PersonId == person.Id)
          .GroupBy(x => x.Keywords == null
            ? string.Empty
            : string.Join(", ", Keywords.GetAllKeywords(x.Keywords).Select(k => k.Title)))
          .OrderBy(x => x.Key)) {

          // add group
          if (!string.IsNullOrEmpty(group.Key))
            await App.Core.RunOnUiThread(() => AllSegmentsGrid.AddGroup(IconName.Tag, group.Key));

          // add segments
          foreach (var segment in group.OrderBy(x => x.MediaItem.FileName)) {
            await segment.SetPictureAsync(App.Core.Segments.SegmentSize);
            segment.MediaItem.SetThumbSize();
            await App.Core.RunOnUiThread(() => {
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

    private void OnSegmentSelected(object sender, MouseButtonEventArgs e) {
      var (isCtrlOn, isShiftOn) = InputUtils.GetKeyboardModifiers(e);
      var segment = (Segment)((FrameworkElement)sender).DataContext;
      App.Core.Segments.Select(AllSegments, segment, isCtrlOn, isShiftOn);
    }
  }
}
