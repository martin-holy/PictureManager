using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MH.Utils.BaseClasses;

public class ObservableObject : INotifyPropertyChanged {
  public event PropertyChangedEventHandler PropertyChanged = delegate { };
  public void OnPropertyChanged([CallerMemberName] string name = null) =>
    PropertyChanged.Invoke(this, new(name));

  protected bool SetIfVary<T>(ref T field, T value, [CallerMemberName] string name = null) {
    if (EqualityComparer<T>.Default.Equals(field, value)) return false;
    field = value;
    OnPropertyChanged(name);
    return true;
  }
}