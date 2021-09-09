using MahApps.Metro.Controls;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
      AllowDrop = true;
      PreviewMouseLeftButtonDown += SetDragObject;
      PreviewMouseLeftButtonUp += ReleaseDragObject;
      MouseMove += StartDragDrop;
      DragEnter += AllowDropCheck;
      DragLeave += AllowDropCheck;
      DragOver += AllowDropCheck;
      Drop += OnDrop;

      BtnReload.Click += (o, e) => _ = ReloadPersonSegmentsAsync(Person);
      BtnClose.Click += (o, e) => {
        Visibility = Visibility.Collapsed;
        App.Core.People.DeselectAll();
      };

      if (AppCore.OnToggleKeyword?.IsRegistered(this) != true)
        AppCore.OnToggleKeyword += (o, e) => _ = ReloadPersonSegmentsAsync(Person);

      if (AppCore.OnSetPerson?.IsRegistered(this) != true)
        AppCore.OnSetPerson += (o, e) => _ = ReloadPersonSegmentsAsync(Person);
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
      Visibility = Visibility.Visible;
      Person = person;
      AllSegments.Clear();
      AllSegmentsGrid.ClearRows();
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

    private void Segment_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
      var (isCtrlOn, isShiftOn) = InputUtils.GetKeyboardModifiers(e);
      var segment = (Segment)((FrameworkElement)sender).DataContext;
      App.Core.Segments.Select(AllSegments, segment, isCtrlOn, isShiftOn);
    }

    #region Drag & Drop

    private Point _dragDropStartPosition;
    private FrameworkElement _dragDropSource;
    private DragDropEffects _dragDropEffects;

    private void SetDragObject(object sender, MouseButtonEventArgs e) {
      _dragDropSource = null;
      _dragDropStartPosition = new Point(0, 0);

      var src = e.OriginalSource as FrameworkElement;
      if (src?.DataContext is not Segment) return;

      _dragDropSource = src;
      _dragDropEffects = DragDropEffects.Copy;
      _dragDropStartPosition = e.GetPosition(null);
    }

    private void ReleaseDragObject(object sender, MouseButtonEventArgs e) => _dragDropSource = null;

    private void StartDragDrop(object sender, MouseEventArgs e) {
      if (_dragDropSource == null || !IsDragDropStarted(e)) return;
      _ = DragDrop.DoDragDrop(_dragDropSource, _dragDropSource.DataContext, _dragDropEffects);
    }

    private bool IsDragDropStarted(MouseEventArgs e) {
      if (e.LeftButton != MouseButtonState.Pressed) return false;
      var diff = _dragDropStartPosition - e.GetPosition(null);
      return Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;
    }

    private void AllowDropCheck(object sender, DragEventArgs e) {
      var dest = (FrameworkElement)e.OriginalSource;
      var isFromTop = _dragDropSource.IsDescendantOf(TopSegmentsGrid);
      var isInTop = dest.IsDescendantOf(TopSegmentsGrid);
      var segment = e.Data.GetData(typeof(Segment)) as Segment;

      if (segment != null
        && ((isInTop && (Person.Segments?.Contains(segment) != true))
        || (isFromTop && !isInTop && _dragDropSource != dest))) return;

      // can't be dropped
      e.Effects = DragDropEffects.None;
      e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e) {
      var dest = (FrameworkElement)e.OriginalSource;
      var segment = e.Data.GetData(typeof(Segment)) as Segment;
      var dropInTop = dest.IsDescendantOf(TopSegmentsGrid);

      if (segment == null) return;

      if (dropInTop) {
        if (Person.Segments == null) {
          Person.Segments = new();
          Person.OnPropertyChanged(nameof(Person.Segments));
        }
        Person.Segments.Add(segment);
      }
      else {
        _ = Person.Segments.Remove(segment);
        if (Person.Segments.Count == 0)
          Person.Segments = null;
      }

      if (Person.Segments?.Count > 0)
        Person.Segment = Person.Segments[0];

      App.Db.SetModified<People>();
      ReloadTopSegments();
    }

    #endregion Drag & Drop
  }
}
