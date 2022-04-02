using System;
using System.Collections.ObjectModel;
using System.Linq;
using MH.UI.WPF.BaseClasses;
using MH.Utils.BaseClasses;

namespace PictureManager.ViewModels {
  public sealed class MainTabsVM : ObservableObject {
    private HeaderedListItem<object, string> _selected;
    private double _tabsActualHeight;

    public RelayCommand<double> UpdateTabHeadersSizeCommand { get; }
    public RelayCommand<HeaderedListItem<object, string>> CloseTabCommand { get; }

    public event EventHandler<ObjectEventArgs> TabClosedEvent = delegate { };

    public ObservableCollection<HeaderedListItem<object, string>> Items { get; } = new();
    public HeaderedListItem<object, string> Selected { get => _selected; set { _selected = value; OnPropertyChanged(); } }
    public double TabMaxHeight => _tabsActualHeight / Items.Count;

    public MainTabsVM() {
      UpdateTabHeadersSizeCommand = new(UpdateTabHeadersSize);
      CloseTabCommand = new(CloseTab);

      Items.CollectionChanged += (_, _) =>
        OnPropertyChanged(nameof(TabMaxHeight));
    }

    public void Activate(HeaderedListItem<object, string> item) {
      if (!Items.Contains(item))
        AddItem(item);
      else
        Selected = item;
    }

    public void AddItem(HeaderedListItem<object, string> item) {
      Items.Add(item);
      OnPropertyChanged(nameof(TabMaxHeight));
      Selected = item;
    }

    private void CloseTab(HeaderedListItem<object, string> item) {
      Items.Remove(item);

      if (Selected.Equals(item))
        Selected = Items.FirstOrDefault();

      OnPropertyChanged(nameof(TabMaxHeight));
      TabClosedEvent(this, new(item));
    }

    private void UpdateTabHeadersSize(double actualHeight) {
      _tabsActualHeight = actualHeight;
      OnPropertyChanged(nameof(TabMaxHeight));
    }
  }
}
