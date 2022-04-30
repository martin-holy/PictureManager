using System.Windows;
using System.Windows.Controls;
using MH.UI.WPF.Controls;
using MH.UI.WPF.Converters;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public sealed class PeopleVM : ObservableObject {
    private readonly AppCore _coreVM;

    public PeopleM PeopleM { get; }
    public HeaderedListItem<object, string> MainTabsItem { get; }
    public RelayCommand<ClickEventArgs> SelectCommand { get; }
    public RelayCommand<SizeChangedEventArgs> PanelSizeChangedCommand { get; }

    public PeopleVM(AppCore coreVM, PeopleM peopleM) {
      _coreVM = coreVM;
      PeopleM = peopleM;

      MainTabsItem = new(this, "People");

      SelectCommand = new(Select);
      PanelSizeChangedCommand = new(PanelSizeChanged);

      // TODO do it just for loaded
      foreach (var person in App.Core.PeopleM.All)
        person.UpdateDisplayKeywords();
    }

    private void Select(ClickEventArgs e) {
      if (e.OriginalSource is Image { DataContext: SegmentM segmentM })
        PeopleM.Select(null, segmentM.Person, e.IsCtrlOn, e.IsShiftOn);

      if (e.OriginalSource is FrameworkElement { DataContext: PersonM personM })
        PeopleM.Select(null, personM, e.IsCtrlOn, e.IsShiftOn);
    }

    private void PanelSizeChanged(SizeChangedEventArgs e) {
      if (e.WidthChanged && !_coreVM.MainWindowVM.IsFullScreenIsChanging && e.Source is VirtualizingWrapPanel panel)
        panel.ReWrap();
    }
  }
}
