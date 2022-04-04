using System.ComponentModel;

namespace MH.Utils.Interfaces {
  public interface IDialog : INotifyPropertyChanged {
    string Title { get; set; }
    int Result { get; set; }
  }
}
