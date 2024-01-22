using MH.UI.Interfaces;
using MH.Utils.Interfaces;
using System.Windows.Data;

namespace MH.UI.WPF.Converters;

public class CollectionViewItemSizeConverter : BaseMultiConverter {
  private static readonly object _lock = new();
  private static CollectionViewItemSizeConverter _inst;
  public static CollectionViewItemSizeConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object[] values, object parameter) =>
    values is [{ } item, ITreeItem { Parent: ICollectionViewGroup g }] && parameter is bool getWidth
      ? (double)g.GetItemSize(item, getWidth)
      : Binding.DoNothing;
}