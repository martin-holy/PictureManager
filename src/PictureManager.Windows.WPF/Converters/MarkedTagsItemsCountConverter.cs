using MH.UI.WPF.Converters;
using System.Collections.Generic;

namespace PictureManager.Windows.WPF.Converters;

public class MarkedTagsItemsCountConverter : BaseMultiConverter {
  private static readonly object _lock = new();
  private static MarkedTagsItemsCountConverter _inst;
  public static MarkedTagsItemsCountConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object[] values, object parameter) =>
    values is [Dictionary<object, int> tags, _]
      ? tags.ContainsKey(values[1])
        ? tags[values[1]].ToString()
        : string.Empty
      : string.Empty;
}