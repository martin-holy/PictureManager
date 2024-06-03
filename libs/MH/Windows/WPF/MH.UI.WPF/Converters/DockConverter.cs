namespace MH.UI.WPF.Converters;

public class DockConverter : BaseConverter {
  private static readonly object _lock = new();
  private static DockConverter _inst;
  public static DockConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object value, object parameter) =>
    (System.Windows.Controls.Dock)(int)(MH.UI.Controls.Dock)value;

  public override object ConvertBack(object value, object parameter) =>
    (MH.UI.Controls.Dock)(int)(System.Windows.Controls.Dock)value;
}