namespace MH.Utils.EventsArgs;

public class PropertyChangedEventArgs<T> : RoutedEventArgs {
  public T NewValue { get; }
  public T OldValue { get; }

  public PropertyChangedEventArgs(T oldValue, T newValue) {
    OldValue = oldValue;
    NewValue = newValue;
  }
}