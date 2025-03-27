using MH.UI.AvaloniaUI.Controls;
using MH.Utils.Interfaces;

namespace PictureManager.AvaloniaUI.Controls;

// It can be IListItem or RatingM (RatingM DataTemplate doesn't have key)
public class MediaItemsFilterDataTemplateSelector() : TypeDataTemplateSelector(_mappings) {
  private static readonly TypeTemplateMapping[] _mappings = [
    new(typeof(IListItem), "MH.DT.IListItem")
  ];
}