using MahApps.Metro.Controls;
using PictureManager.Dialogs;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using System;
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

    //private readonly int _faceGridWidth = 100 + 6; //border, margin, padding, ... //TODO find the real value
    private Person _person;

    public ObservableCollection<Face> AllPersonFaces { get; } = new();
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

      BtnClose.Click += (o, e) => Visibility = Visibility.Collapsed;
      //BtnUpdateFaceGroups.Click += async (o, e) => {
      //  App.Core.Faces.ReloadFaceGroups(Person.Id, App.Core.Faces.SimilarityLimit / 100.0);
      //  await ReloadPersonFacesAsync(Person);
      //  UpdateLayout();
      //};
    }

    public void ChangePerson(Person person) {
      if (!MessageDialog.Show("Change Person", $"Do you realy want to change selected ({App.Core.Faces.SelectedCount}) faces to person ({person.Title})?", true))
        return;

      foreach (var face in App.Core.Faces.Selected) {
        Faces.ChangePerson(face, person);
        _ = AllPersonFaces.Remove(face);
        if (Person.Face == face)
          Person.Face = null;
        if (Person.Faces.Remove(face))
          App.Db.SetModified<People>();
      }

      App.Core.Faces.DeselectAll();
    }

    public async Task ReloadPersonFacesAsync(Person person) {
      Person = person;
      AllPersonFaces.Clear();
      Visibility = Visibility.Visible;

      await Task.Run(async () => {
        foreach (var face in App.Core.Faces.All.Cast<Face>().Where(x => x.PersonId == person.Id)) {
          await face.SetPictureAsync(App.Core.Faces.FaceSize);
          face.MediaItem.SetThumbSize();
          await App.Core.RunOnUiThread(() => {
            face.MediaItem.SetInfoBox();
            AllPersonFaces.Add(face);
          });
        }
      });

      IcTopFaces.FindChild<ScrollViewer>().ScrollToHome();
      IcAllFaces.FindChild<ScrollViewer>().ScrollToHome();
    }

    // faces in groups version (current face comparer (template matching) is not sufficiente enough to find groups of similar faces)
    //public async Task ReloadPersonFacesAsync(Person person) {
    //  Person = person;
    //  VwpAllFaces.ClearRows();
    //  Visibility = Visibility.Visible;

    //  await Task.Run(async () => {
    //    foreach (var group in App.Core.Faces.All.Cast<Face>().Where(x => x.PersonId == person.Id).GroupBy(x => x.GroupId)) {
    //      await App.Core.RunOnUiThread(() => {
    //        VwpAllFaces.AddGroup(IconName.PeopleMultiple, group.Key.ToString());
    //      });
    //      foreach (var face in group) {
    //        await face.SetPictureAsync(App.Core.Faces.FaceSize);
    //        face.MediaItem.SetThumbSize();
    //        await App.Core.RunOnUiThread(() => {
    //          VwpAllFaces.AddItem(face, _faceGridWidth);
    //        });
    //      }
    //    }
    //  });

    //  IcTopFaces.FindChild<ScrollViewer>().ScrollToHome();
    //  IcAllFaces.FindChild<ScrollViewer>().ScrollToHome();
    //  VwpAllFaces.ScrollToTop();
    //}

    private void MouseWheelScroll(object sender, MouseWheelEventArgs e) {
      var sv = (ScrollViewer)sender;
      sv.ScrollToHorizontalOffset(sv.ContentHorizontalOffset + (e.Delta * -1));
      e.Handled = true;
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
      var isFromTop = _dragDropSource.IsDescendantOf(IcTopFaces);
      var isInTop = dest.IsDescendantOf(IcTopFaces);
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
      var dropInTop = dest.IsDescendantOf(IcTopFaces);

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
    }

    #endregion Drag & Drop

    private void Face_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
      var (isCtrlOn, isShiftOn) = InputUtils.GetKeyboardModifiers(e);
      var face = (Face)((FrameworkElement)sender).DataContext;
      App.Core.Faces.Select(isCtrlOn, isShiftOn, AllPersonFaces.ToList(), face);
    }
  }
}
