using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PictureManager.Domain.Interfaces;

namespace PictureManager.Domain.CatTreeViewModels {
  public class CatTreeViewItem : INotifyPropertyChanged, ICatTreeViewItem {
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    public object Tag { get; set; }
    public event EventHandler OnExpandedChanged = delegate { };

    private bool _isExpanded;
    private bool _isSelected;
    private bool _isHidden;
    private bool _isMarked;
    private int _picCount;
    private string _title;
    private string _toolTip;
    private IconName _iconName;
    private BackgroundBrush _backgroundBrush;
    private ITreeBranch _parent;

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
    public bool IsMarked { get => _isMarked; set { _isMarked = value; OnPropertyChanged(); } }
    public int PicCount { get => _picCount; set { _picCount = value; OnPropertyChanged(); } }
    public virtual string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public string ToolTip { get => _toolTip; set { _toolTip = value; OnPropertyChanged(); } }
    public IconName IconName { get => _iconName; set { _iconName = value; OnPropertyChanged(); } }
    public BackgroundBrush BackgroundBrush { get => _backgroundBrush; set { _backgroundBrush = value; OnPropertyChanged(); } }
    public ITreeBranch Parent { get => _parent; set { _parent = value; OnPropertyChanged(); } }

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
  }
}
