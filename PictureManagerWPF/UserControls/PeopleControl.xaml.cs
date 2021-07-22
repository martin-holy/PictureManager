using PictureManager.Domain;
using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PictureManager.UserControls {
  public partial class PeopleControl : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private readonly int _faceGridWidth = 100 + 6; //border, margin, padding, ... //TODO find the real value
    private bool _loading;
    private string _title;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }

    public PeopleControl() {
      InitializeComponent();

      Title = "People";

      PeopleGrid.Rows = new();
    }

    public async void Reload() {
      async Task AddPeopleAsync(IEnumerable<Person> people) {
        foreach (var person in people) {
          if (person.Face != null) {
            await person.Face.SetPictureAsync(App.Core.Faces.FaceSize);
            person.Face.MediaItem.SetThumbSize();
          }
          PeopleGrid.AddItem(person, _faceGridWidth);
        }
      };

      _loading = true;
      PeopleGrid.ClearRows();
      PeopleGrid.UpdateMaxRowWidth();

      foreach (var group in App.Core.People.Items.OfType<ICatTreeViewGroup>()) {
        PeopleGrid.AddGroup(IconName.People, group.Title);
        await AddPeopleAsync(group.Items.Cast<Person>());
      }

      var peopleWithoutGroup = App.Core.People.Items.OfType<Person>().ToArray();
      if (peopleWithoutGroup.Length > 0) {
        PeopleGrid.AddGroup(IconName.People, string.Empty);
        await AddPeopleAsync(peopleWithoutGroup);
      }

      _loading = false;
    }

    private void ControlSizeChanged(object sender, SizeChangedEventArgs e) {
      if (_loading) return;
      Reload();
      UpdateLayout();
      PeopleGrid.ScrollToTop();
    }

    private async void Face_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
      if (e.ClickCount != 2) return;
      if (((FrameworkElement)sender).DataContext is Face face)
        await PersonFacesEditor.ReloadPersonFacesAsync(face.Person);
    }
  }
}