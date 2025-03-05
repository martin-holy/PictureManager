using MH.UI.AvaloniaUI.Converters;
using System.Collections.Generic;

namespace PictureManager.AvaloniaUI.Converters;

public class MarkedTagsItemsCountConverter : BaseMultiConverter {
  private static readonly object _lock = new();
  private static MarkedTagsItemsCountConverter? _inst;
  public static MarkedTagsItemsCountConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(IList<object?> values, object? parameter) =>
    values is [Dictionary<object, int> tags, { } tag]
      ? tags.ContainsKey(tag)
        ? tags[tag].ToString()
        : string.Empty
      : string.Empty;
}