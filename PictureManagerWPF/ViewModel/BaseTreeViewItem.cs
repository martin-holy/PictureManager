using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PictureManager.ViewModel {
  public class BaseTreeViewItem : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = "") {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public ObservableCollection<BaseTreeViewItem> Items { get; set; }

    private bool _isExpanded;
    private bool _isSelected;
    private string _title;
    private string _iconName;
    private string _toolTip;
    private BgBrushes _bgBrush;
    private BaseTreeViewItem _parent;
    private object _dbData;

    public virtual bool IsExpanded { get { return _isExpanded; } set { _isExpanded = value; OnPropertyChanged(); } }
    public bool IsSelected { get { return _isSelected; } set { _isSelected = value; OnPropertyChanged(); } }
    public string Title { get { return _title; } set { _title = value; OnPropertyChanged(); } }
    public string IconName { get { return _iconName; } set { _iconName = value; OnPropertyChanged(); } }
    public string ToolTip { get { return _toolTip; } set { _toolTip = value; OnPropertyChanged(); } }
    public BgBrushes BgBrush { get { return _bgBrush; } set { _bgBrush = value; OnPropertyChanged(); } }
    public BaseTreeViewItem Parent { get { return _parent; } set { _parent = value; OnPropertyChanged(); } }
    public object DbData { get { return _dbData; } set { _dbData = value; OnPropertyChanged(); } }

    public BaseTreeViewItem() {
      Items = new ObservableCollection<BaseTreeViewItem>();
    }
  }
}
