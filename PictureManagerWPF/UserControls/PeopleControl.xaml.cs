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

    private readonly int _faceGridWidth = 100 + 6; //border, margin, padding, ... //TODO find the real value
    private readonly WorkTask _workTask = new();
    private bool _loading;
    private string _title;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }

    public PeopleControl() {
      InitializeComponent();

      Title = "People";
    }

    public async Task Reload() {
      async Task AddPeopleAsync(IEnumerable<Person> people, CancellationToken token) {
        foreach (var person in people) {
          if (token.IsCancellationRequested) break;
          if (person.Face != null) {
            await person.Face.SetPictureAsync(App.Core.Faces.FaceSize);
            person.Face.MediaItem.SetThumbSize();
          }
          await App.Core.RunOnUiThread(() => PeopleGrid.AddItem(person, _faceGridWidth));
        }
      }

      _loading = true;
      await _workTask.Cancel();
      PeopleGrid.ClearRows();
      UpdateLayout();
      PeopleGrid.UpdateMaxRowWidth();

      await _workTask.Start(Task.Run(async () => {
        foreach (var group in App.Core.People.Items.OfType<ICatTreeViewGroup>()) {
          if (_workTask.Token.IsCancellationRequested) break;
          await App.Core.RunOnUiThread(() => PeopleGrid.AddGroup(IconName.People, group.Title));
          await AddPeopleAsync(group.Items.Cast<Person>(), _workTask.Token);
        }

        var peopleWithoutGroup = App.Core.People.Items.OfType<Person>().ToArray();
        if (peopleWithoutGroup.Length > 0) {
          await App.Core.RunOnUiThread(() => PeopleGrid.AddGroup(IconName.People, string.Empty));
          await AddPeopleAsync(peopleWithoutGroup, _workTask.Token);
        }
      }));

      _loading = false;
    }

    public void ChangePerson(Person person) {
      if (PersonFacesEditor.Visibility != Visibility.Visible) return;
      PersonFacesEditor.ChangePerson(person);
    }

    private async void ControlSizeChanged(object sender, SizeChangedEventArgs e) {
      if (_loading) return;
      await Reload();
      PeopleGrid.ScrollToTop();
    }

    private async void Face_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      if (sender is FaceControl fc)
        await PersonFacesEditor.ReloadPersonFacesAsync(((Face)fc.DataContext).Person);
    }
  }
}