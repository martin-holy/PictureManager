using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PictureManager.Data {
  public class DataBase : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    private string _title;
    public string Title {
      get { return _title; }
      set { _title = value; OnPropertyChanged("Title"); }
    }

    private bool _isExpanded;
    public virtual bool IsExpanded {
      get { return _isExpanded; }
      set { _isExpanded = value; OnPropertyChanged("IsExpanded"); }
    }

    private bool _isSelected;
    public bool IsSelected {
      get { return _isSelected; }
      set { _isSelected = value; OnPropertyChanged("IsSelected"); }
    }

    private bool _isMarked;
    public bool IsMarked {
      get { return _isMarked; }
      set { _isMarked = value; OnPropertyChanged("IsMarked"); }
    }

    private string _imageName;
    public string ImageName {
      get { return _imageName;}
      set { _imageName = value; OnPropertyChanged("ImageName"); }
    }

    public string FullPath { get; set; }
    public bool Accessible { get; set; } = true;
    public bool Category { get; set; } = false;
    public ObservableCollection<DataBase> Items { get; set; }

    public DataBase() {
      Items = new ObservableCollection<DataBase>();
    }

    public void OnPropertyChanged(string name) {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
  }
}
