using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MH.Utils.BaseClasses {
  public class ObservableObject : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged.Invoke(this, new(name));
  }
}
