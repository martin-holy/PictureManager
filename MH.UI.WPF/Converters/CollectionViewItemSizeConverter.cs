using MH.UI.Interfaces;
using MH.Utils.Interfaces;
using System.Windows.Data;

namespace MH.UI.WPF.Converters;

public class CollectionViewItemSizeConverter : BaseMarkupExtensionMultiConverter {
  public override object Convert(object[] values, object parameter) =>
    values is [{ } item, ITreeItem { Parent: ICollectionViewGroup g }] && parameter is bool getWidth
      ? (double)g.GetItemSize(item, getWidth)
      : Binding.DoNothing;
}