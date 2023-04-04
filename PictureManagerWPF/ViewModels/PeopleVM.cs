using System.Windows;
using MH.UI.WPF.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public sealed class PeopleVM : ObservableObject {
    private readonly Core _core;
    private bool _reWrapItems;

    public PeopleM PeopleM { get; }
    public RelayCommand<object> PanelWidthChangedCommand { get; }
    public bool ReWrapItems { get => _reWrapItems; set { _reWrapItems = value; OnPropertyChanged(); } }

    public PeopleVM(Core core, PeopleM peopleM) {
      _core = core;
      PeopleM = peopleM;

      PeopleM.MainTabsItem = new(this, "People");
      PanelWidthChangedCommand = new(
        () => ReWrapItems = true,
        () => !_core.MainWindowM.IsFullScreenIsChanging);
    }
  }
}
