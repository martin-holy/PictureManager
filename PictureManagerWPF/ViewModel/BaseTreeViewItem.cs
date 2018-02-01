using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PictureManager.ViewModel {
  public class BaseTreeViewItem : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = "") {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public ObservableCollection<BaseTreeViewItem> Items { get; set; }
    public AppCore ACore;
    public object Tag;

    private bool _isExpanded;
    private bool _isSelected;
    private string _title;
    private string _iconName;
    private string _toolTip;
    private BgBrushes _bgBrush;
    private BaseTreeViewItem _parent;

    public virtual bool IsExpanded { get => _isExpanded; set { _isExpanded = value; OnPropertyChanged(); } }
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public string IconName { get => _iconName; set { _iconName = value; OnPropertyChanged(); } }
    public string ToolTip { get => _toolTip; set { _toolTip = value; OnPropertyChanged(); } }
    public BgBrushes BgBrush { get => _bgBrush; set { _bgBrush = value; OnPropertyChanged(); } }
    public BaseTreeViewItem Parent { get => _parent; set { _parent = value; OnPropertyChanged(); } }

    public BaseTreeViewItem() {
      Items = new ObservableCollection<BaseTreeViewItem>();
      ACore = (AppCore) Application.Current.Properties[nameof(AppProps.AppCore)];
    }

    public BaseTreeViewItem GetTopParent() {
      return Parent == null ? this : Parent.GetTopParent();
    }
  }
}
