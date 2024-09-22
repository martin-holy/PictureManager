using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.ComponentModel;

namespace MH.UI.Interfaces;

public interface ITreeView : INotifyPropertyChanged {
  public ExtObservableCollection<object> RootHolder { get; }
  public bool ShowTreeItemSelection { get; set; }
  public bool IsVisible { get; set; }
  public ITreeItem? TopTreeItem { get; set; }
  public ITreeItem[] TopTreeItemPath { get; }
  public Action? ScrollToTopAction { get; set; }
  public Action<object[], bool>? ScrollToItemsAction { get; set; }
  public Action<ITreeItem>? ExpandRootWhenReadyAction { get; set; }
  public RelayCommand ScrollToTopCommand { get; }
  public RelayCommand ScrollSiblingUpCommand { get; }
  public RelayCommand ScrollLevelUpCommand { get; }
  public RelayCommand<object> TreeItemSelectedCommand { get; }
}