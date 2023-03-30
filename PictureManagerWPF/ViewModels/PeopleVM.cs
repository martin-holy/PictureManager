using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MH.UI.WPF.Controls;
using MH.UI.WPF.Converters;
using MH.Utils.BaseClasses;
using PictureManager.Domain;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public sealed class PeopleVM : ObservableObject {
    private readonly Core _core;
    private object _scrollToItem;
    private TreeWrapGroup _peopleRoot;

    public PeopleM PeopleM { get; }
    public object ScrollToItem { get => _scrollToItem; set { _scrollToItem = value; OnPropertyChanged(); } }
    public TreeWrapGroup PeopleRoot { get => _peopleRoot; private set { _peopleRoot = value; OnPropertyChanged(); } }
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
      if (e.OriginalSource is Image { DataContext: SegmentM segmentM })
        PeopleM.Select(null, segmentM.Person, e.IsCtrlOn, e.IsShiftOn);

      if (e.OriginalSource is FrameworkElement { DataContext: PersonM personM })
        PeopleM.Select(null, personM, e.IsCtrlOn, e.IsShiftOn);
    }

    private void PanelSizeChanged(SizeChangedEventArgs e) {
      if (e.WidthChanged && !_core.MainWindowM.IsFullScreenIsChanging && e.Source is TreeWrapView twv)
        twv.ReWrap();
    }

    public void Reload() {
      PeopleRoot = PeopleM.Reload();
      ScrollToItem = (PeopleRoot?.Items.FirstOrDefault() as TreeWrapGroup)?.Items.FirstOrDefault();
    }
  }
}
