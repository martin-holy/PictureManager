using System.Linq;
using System.Windows;
using MH.UI.WPF.Controls;
using MH.Utils.BaseClasses;
using MH.Utils.EventsArgs;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public sealed class PeopleVM : ObservableObject {
    private readonly Core _core;

    public PeopleM PeopleM { get; }
    public RelayCommand<ClickEventArgs> SelectCommand { get; }
    public RelayCommand<SizeChangedEventArgs> PanelSizeChangedCommand { get; }

    public PeopleVM(Core core, PeopleM peopleM) {
      _core = core;
      PeopleM = peopleM;

      PeopleM.MainTabsItem = new(this, "People");

      SelectCommand = new(Select);
      PanelSizeChangedCommand = new(PanelSizeChanged);

      // TODO do it just for loaded
      foreach (var person in PeopleM.DataAdapter.All.Values)
        person.UpdateDisplayKeywords();
    }

    private void Select(ClickEventArgs e) {
      if (e.DataContext is SegmentM segmentM)
        PeopleM.Select(null, segmentM.Person, e.IsCtrlOn, e.IsShiftOn);
    }

    private void PanelSizeChanged(SizeChangedEventArgs e) {
      if (e.WidthChanged && !_core.MainWindowM.IsFullScreenIsChanging && e.Source is TreeWrapView twv)
        twv.ReWrap();
    }
  }
}
