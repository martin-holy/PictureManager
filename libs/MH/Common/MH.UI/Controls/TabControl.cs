using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System.Collections.ObjectModel;
using System.Linq;

namespace MH.UI.Controls;

public class TabControl : ObservableObject {
  private IListItem _selected;
  private bool _canCloseTabs;
  private double _maxTabSize;

  public ObservableCollection<IListItem> Tabs { get; } = [];
  public TabStrip TabStrip { get; set; } = new();
  public IListItem Selected { get => _selected; set { _selected = value; OnPropertyChanged(); OnPropertyChanged(nameof(Selected.Data)); } }
  public bool CanCloseTabs { get => _canCloseTabs; set { _canCloseTabs = value; OnPropertyChanged(); OnPropertyChanged(nameof(Selected.Data)); } }
  //TODO delete this when the rewrite is done
  public double MaxTabSize { get => _maxTabSize; set { _maxTabSize = value; OnPropertyChanged(); } }

  public RelayCommand<IListItem> CloseTabCommand { get; }

  public event DataEventHandler<IListItem> TabActivatedEvent = delegate { };
  public event DataEventHandler<IListItem> TabClosedEvent = delegate { };

  public TabControl() {
    CloseTabCommand = new(Close, Res.IconXCross, "Close");

    Tabs.CollectionChanged += (_, _) => {
      TabStrip.UpdateMaxTabSize(Tabs.Count);
      OnPropertyChanged(nameof(MaxTabSize));
    };
  }

  public void Activate(string icon, string name, object data) {
    var tab = Tabs.SingleOrDefault(x => ReferenceEquals(data, x.Data));
    if (tab != null)
      Selected = tab;
    else
      Add(icon, name, data);

    TabActivatedEvent(Selected);
  }

  public void Add(string icon, string name, object data) =>
    Add(new ListItem(icon, name, data));

  public void Add(IListItem tab) {
    Tabs.Add(tab);
    Selected = tab;
  }

  public void Select(object data) {
    var tab = GetTabByData(data);
    if (tab != null)
      Selected = tab;
  }

  public void Close(object data) =>
    Close(GetTabByData(data));

  public void Close(IListItem tab) {
    if (tab == null || !CanCloseTabs) return;

    Tabs.Remove(tab);

    if (ReferenceEquals(Selected, tab))
      Selected = Tabs.FirstOrDefault();

    TabClosedEvent(tab);
  }

  public IListItem GetTabByData(object data) =>
    Tabs.FirstOrDefault(x => ReferenceEquals(x.Data, data));

  public void UpdateMaxTabSize(double? width, double? height) {
    TabStrip.UpdateMaxTabSize(width, height, Tabs.Count);

    if (width is { } w && height is { } h)
      MaxTabSize = TabStrip.Placement is Dock.Top or Dock.Bottom ? w : h;
  }
}