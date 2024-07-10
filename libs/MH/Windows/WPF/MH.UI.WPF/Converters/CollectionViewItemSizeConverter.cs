using MH.UI.Interfaces;
using MH.UI.WPF.Extensions;
using MH.Utils.Interfaces;
using System.Windows;
using System.Windows.Controls;

namespace MH.UI.WPF.Converters;

public class CollectionViewItemSizeConverter : BaseConverter {
  private static readonly object _lock = new();
  private static CollectionViewItemSizeConverter? _inst;
  public static CollectionViewItemSizeConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object? Convert(object? value, object? parameter) {
    if (value is not FrameworkElement fe
        || fe.TryFindParent<ItemsControl>() is not { DataContext: ITreeItem { Parent: ICollectionViewGroup g } })
      return null;
    
    fe.Width = g.GetItemSize(fe.DataContext, true);
    fe.Height = g.GetItemSize(fe.DataContext, false);

    return null;
  }
}