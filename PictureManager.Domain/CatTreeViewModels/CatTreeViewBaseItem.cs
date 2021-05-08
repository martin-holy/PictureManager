using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PictureManager.Domain.CatTreeViewModels {
  public class CatTreeViewBaseItem : INotifyPropertyChanged, ICatTreeViewBaseItem {
    public ObservableCollection<ICatTreeViewBaseItem> Items { get; set; } = new ObservableCollection<ICatTreeViewBaseItem>();
    public object Tag { get; set; }

    private bool _isExpanded;
    private bool _isSelected;
    private string _title;
    private IconName _iconName;
    private string _toolTip;
    private BackgroundBrush _backgroundBrush;
    private ICatTreeViewBaseItem _parent;

    public virtual bool IsExpanded { get => _isExpanded; set { _isExpanded = value; OnPropertyChanged(); } }
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    public virtual string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public IconName IconName { get => _iconName; set { _iconName = value; OnPropertyChanged(); } }
    public string ToolTip { get => _toolTip; set { _toolTip = value; OnPropertyChanged(); } }
    public BackgroundBrush BackgroundBrush { get => _backgroundBrush; set { _backgroundBrush = value; OnPropertyChanged(); } }
    public ICatTreeViewBaseItem Parent { get => _parent; set { _parent = value; OnPropertyChanged(); } }

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
