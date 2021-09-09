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
  public partial class PersonFacesControl : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private readonly int _segmentGridWidth = 100 + 6; //border, margin, padding, ... //TODO find the real value
    private Person _person;

    public List<Face> AllSegments { get; } = new();
    public Person Person { get => _person; set { _person = value; OnPropertyChanged(); } }

    public PersonFacesControl() {
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

      BtnReload.Click += (o, e) => _ = ReloadPersonFacesAsync(Person);
      BtnClose.Click += (o, e) => {
        Visibility = Visibility.Collapsed;
        App.Core.People.DeselectAll();
      };

      AppCore.OnToggleKeyword += (o, e) => _ = ReloadPersonFacesAsync(Person);
      AppCore.OnSetPerson += (o, e) => _ = ReloadPersonFacesAsync(Person);
    }

    private void ReloadTopSegments() {
      TopSegmentsGrid.ClearRows();
      if (Person.Faces == null) return;
      UpdateLayout();
      TopSegmentsGrid.UpdateMaxRowWidth();

      foreach (var face in Person.Faces)
        TopSegmentsGrid.AddItem(face, _segmentGridWidth);

      TopSegmentsGrid.ScrollToTop();
    }

    public async Task ReloadPersonFacesAsync(Person person) {
      Visibility = Visibility.Visible;
      Person = person;
      AllSegments.Clear();
      AllSegmentsGrid.ClearRows();
      UpdateLayout();
      AllSegmentsGrid.UpdateMaxRowWidth();

      ReloadTopSegments();

      await Task.Run(async () => {
        foreach (var group in App.Core.Faces.All.Cast<Face>()
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
            await segment.SetPictureAsync(App.Core.Faces.FaceSize);
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

    private void Face_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
      var (isCtrlOn, isShiftOn) = InputUtils.GetKeyboardModifiers(e);
      var face = (Face)((FrameworkElement)sender).DataContext;
      App.Core.Faces.Select(AllSegments, face, isCtrlOn, isShiftOn);
    }

    #region Drag & Drop

    private Point _dragDropStartPosition;
    private FrameworkElement _dragDropSource;
    private DragDropEffects _dragDropEffects;

    private void SetDragObject(object sender, MouseButtonEventArgs e) {
      _dragDropSource = null;
      _dragDropStartPosition = new Point(0, 0);

      var src = e.OriginalSource as FrameworkElement;
      if (src?.DataContext is not Face) return;

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
      var face = e.Data.GetData(typeof(Face)) as Face;

      if (face != null
        && ((isInTop && (Person.Faces?.Contains(face) != true))
        || (isFromTop && !isInTop && _dragDropSource != dest))) return;

      // can't be dropped
      e.Effects = DragDropEffects.None;
      e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e) {
      var dest = (FrameworkElement)e.OriginalSource;
      var face = e.Data.GetData(typeof(Face)) as Face;
      var dropInTop = dest.IsDescendantOf(TopSegmentsGrid);

      if (face == null) return;

      if (dropInTop) {
        if (Person.Faces == null) {
          Person.Faces = new();
          Person.OnPropertyChanged(nameof(Person.Faces));
        }
        Person.Faces.Add(face);
      }
      else {
        _ = Person.Faces.Remove(face);
        if (Person.Faces.Count == 0)
          Person.Faces = null;
      }

      if (Person.Faces?.Count > 0)
        Person.Face = Person.Faces[0];

      App.Db.SetModified<People>();
      ReloadTopSegments();
    }

    #endregion Drag & Drop
  }
}
