using MH.UI.AvaloniaUI.Controls;
using MH.Utils.Interfaces;
using PictureManager.Common.Features.Person;

namespace PictureManager.AvaloniaUI.Controls;

public class GroupByDialogDataTemplateSelector() : TypeDataTemplateSelector(_mappings) {
  private static readonly TypeTemplateMapping[] _mappings = [
    new(typeof(PersonM), "PM.DT.Person.ListItem"),
    new(typeof(IListItem), "MH.DT.IListItem")
  ];
}