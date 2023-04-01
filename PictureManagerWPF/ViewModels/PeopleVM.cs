using System.Windows;
using MH.UI.WPF.Controls;
using MH.Utils.BaseClasses;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public sealed class PeopleVM : ObservableObject {
    private readonly Core _core;

    public PeopleM PeopleM { get; }
    public RelayCommand<SizeChangedEventArgs> PanelSizeChangedCommand { get; }

    public PeopleVM(Core core, PeopleM peopleM) {
      _core = core;
      PeopleM = peopleM;

      PeopleM.MainTabsItem = new(this, "People");
      PanelSizeChangedCommand = new(PanelSizeChanged);
    }

    private void PanelSizeChanged(SizeChangedEventArgs e) {
      if (e.WidthChanged && !_core.MainWindowM.IsFullScreenIsChanging && e.Source is TreeWrapView twv)
        twv.ReWrap();
    }
  }
}
