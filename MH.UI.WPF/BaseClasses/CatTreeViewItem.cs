using System;
using System.Collections.ObjectModel;
using MH.UI.WPF.Interfaces;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;

namespace MH.UI.WPF.BaseClasses {
  public class CatTreeViewItem : ObservableObject, ICatTreeViewItem {
    #region ITreeBranch implementation
    private ITreeBranch _parent;

    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    public ITreeBranch Parent { get => _parent; set { _parent = value; OnPropertyChanged(); } }
    #endregion

    #region ICatTreeViewItem implementation
    private bool _isExpanded;
    private bool _isSelected;
    private bool _isHidden;

    public bool IsExpanded {
      get => _isExpanded;
      set {
        _isExpanded = value;
        OnExpandedChanged(this, EventArgs.Empty);
        OnPropertyChanged();
      }
    }
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    public bool IsHidden { get => _isHidden; set { _isHidden = value; OnPropertyChanged(); } }
    #endregion

    public event EventHandler OnExpandedChanged = delegate { };
  }
}
