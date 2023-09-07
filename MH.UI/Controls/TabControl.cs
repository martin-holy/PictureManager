using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace MH.UI.Controls {
  public class TabControl : ObservableObject {
    private IListItem _selected;
    private double _tabsSize;
    private bool _canCloseTabs;

    public ObservableCollection<IListItem> Tabs { get; } = new();
    public IListItem Selected { get => _selected; set { _selected = value; OnPropertyChanged(); OnPropertyChanged(nameof(Selected.Data)); } }
    public double TabMaxSize { get => _tabsSize / Tabs.Count; set { _tabsSize = value; OnPropertyChanged(); } }
    public bool CanCloseTabs { get => _canCloseTabs; set { _canCloseTabs = value; OnPropertyChanged(); OnPropertyChanged(nameof(Selected.Data)); } }

    public RelayCommand<IListItem> CloseTabCommand { get; }

    public event EventHandler<ObjectEventArgs<IListItem>> TabClosedEvent = delegate { };

    public TabControl() {
      CloseTabCommand = new(Close);

      Tabs.CollectionChanged += (_, _) =>
        OnPropertyChanged(nameof(TabMaxSize));
    }

    public void Activate(string icon, string name, object data) {
      var tab = Tabs.SingleOrDefault(x => ReferenceEquals(data, x.Data));
      if (tab != null)
        Selected = tab;
      else
        Add(icon, name, data);
    }

    public void Activate(IListItem tab) {
      if (Tabs.Contains(tab))
        Selected = tab;
      else
        Add(tab);
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
      if (tab == null) return;

      Tabs.Remove(tab);

      if (ReferenceEquals(Selected, tab))
        Selected = Tabs.FirstOrDefault();

      TabClosedEvent(this, new(tab));
    }

    public IListItem GetTabByData(object data) =>
      Tabs.FirstOrDefault(x => ReferenceEquals(x.Data, data));
  }
}
