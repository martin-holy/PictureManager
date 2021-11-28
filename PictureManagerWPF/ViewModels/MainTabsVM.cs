using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using MH.UI.WPF.BaseClasses;
using MH.Utils.BaseClasses;
using PictureManager.Interfaces;

namespace PictureManager.ViewModels {
  public sealed class MainTabsVM : ObservableObject {
    private IMainTabsItem _selected;
    private double _tabsActualHeight;

    public RelayCommand<double> UpdateTabHeadersSizeCommand { get; }
    public RelayCommand<SelectionChangedEventArgs> SelectionChangedCommand { get; }
    public RelayCommand<IMainTabsItem> TabCloseCommand { get; }

    public event EventHandler SelectionChanged = delegate { };
    public event EventHandler TabClosed = delegate { };

    public ObservableCollection<IMainTabsItem> Items { get; } = new();
    public IMainTabsItem Selected { get => _selected; set { _selected = value; OnPropertyChanged(); } }
    public double TabMaxHeight => _tabsActualHeight / Items.Count;

    public MainTabsVM() {
      UpdateTabHeadersSizeCommand = new(UpdateTabHeadersSize);
      SelectionChangedCommand = new(OnSelectionChanged);
      TabCloseCommand = new(OnTabClosed);

      Items.CollectionChanged += (_, _) => OnPropertyChanged(nameof(TabMaxHeight));
    }

    public T ActivateTab<T>() where T : IMainTabsItem, new() {
      var item = Items.FirstOrDefault(x => x.GetType() == typeof(T));

      if (item == null) {
        item = new T();
        AddTab(item);
      }
      else
        Selected = item;

      return (T)item;
    }

    public void AddTab(IMainTabsItem tab) {
      Items.Add(tab);
      Selected = tab;
    }

    private void OnSelectionChanged(SelectionChangedEventArgs e) {
      SelectionChanged.Invoke(this, EventArgs.Empty);
    }

    private void OnTabClosed(IMainTabsItem item) {
      Items.Remove(item);
      TabClosed.Invoke(item, EventArgs.Empty);
    }

    private void UpdateTabHeadersSize(double actualHeight) {
      _tabsActualHeight = actualHeight;
      OnPropertyChanged(nameof(TabMaxHeight));
    }
  }
}
