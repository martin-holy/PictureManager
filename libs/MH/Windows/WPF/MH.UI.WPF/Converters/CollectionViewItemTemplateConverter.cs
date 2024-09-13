using MH.UI.Interfaces;
using MH.UI.WPF.Controls;
using MH.UI.WPF.Extensions;
using MH.Utils.Interfaces;
using System.Windows;
using System.Windows.Controls;

namespace MH.UI.WPF.Converters;

public sealed class CollectionViewItemTemplateConverter : BaseConverter {
  private static readonly object _lock = new();
  private static CollectionViewItemTemplateConverter? _inst;
  public static CollectionViewItemTemplateConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  // TODO new version
  /*public override object? Convert(object? value, object? parameter) =>
    value is FrameworkElement fe && fe.TryFindParent<StackPanel>() is
      { DataContext: ITreeItem { Parent: ICollectionViewGroup g } }
      ? ResourceConverter.Inst.Convert(g.GetItemTemplateName(), null) as DataTemplate
      : null;*/

  // TODO temporary version
  public override object? Convert(object? value, object? parameter) {
    if (value is not FrameworkElement fe || fe.TryFindParent<StackPanel>()
          is not { DataContext: ITreeItem { Parent: ICollectionViewGroup g } })
      return null;

    var templateName = g.GetItemTemplateName();
    if (string.IsNullOrEmpty(templateName))
      return g.UIView is CollectionView cv ? cv.InnerItemTemplate : null;

    return ResourceConverter.Inst.Convert(templateName, null) as DataTemplate;
  }

  // TODO old version
  /*public override object? Convert(object? value, object? parameter) =>
    value is FrameworkElement fe && fe.TryFindParent<StackPanel>() is
      { DataContext: ITreeItem { Parent: ICollectionViewGroup { UIView: CollectionView cv } } }
      ? cv.InnerItemTemplate
      : null;*/
}