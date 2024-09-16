using MH.UI.Interfaces;
using MH.UI.WPF.Extensions;
using MH.Utils.Interfaces;
using System.Windows;
using System.Windows.Controls;

namespace MH.UI.WPF.Converters;

public sealed class CollectionViewItemTemplateConverter : BaseConverter {
  private static readonly object _lock = new();
  private static CollectionViewItemTemplateConverter? _inst;
  public static CollectionViewItemTemplateConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object? Convert(object? value, object? parameter) =>
    value is FrameworkElement fe && fe.TryFindParent<StackPanel>() is
      { DataContext: ITreeItem { Parent: ICollectionViewGroup g } }
      ? ResourceConverter.Inst.Convert(g.GetItemTemplateName(), null) as DataTemplate
      : null;
}