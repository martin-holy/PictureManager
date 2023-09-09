using MH.UI.WPF.Converters;
using System.Collections.Generic;

namespace PictureManager.Converters; 

public class MarkedTagsItemsCountConverter : BaseMarkupExtensionMultiConverter {
  public override object Convert(object[] values, object parameter) =>
    values?.Length == 2 && values[0] is Dictionary<object, int> tags
      ? tags.ContainsKey(values[1])
        ? tags[values[1]].ToString()
        : string.Empty
      : string.Empty;
}