using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MH.UI.WPF.Utils;

public class TypeDataTemplateSelector : DataTemplateSelector {
  public TypeTemplateMapping[]? TemplateMappings { get; set; }

  public override DataTemplate? SelectTemplate(object? item, DependencyObject container) {
    if (item == null || TemplateMappings == null || container is not FrameworkElement fe)
      return base.SelectTemplate(item, container);

    var itemType = item.GetType();
    var tm = TemplateMappings.FirstOrDefault(x => x.Type.IsAssignableFrom(itemType));

    if (!string.IsNullOrEmpty(tm?.TemplateKey) && fe.TryFindResource(tm.TemplateKey) is DataTemplate dt)
      return dt;

    return base.SelectTemplate(item, container);
  }
}

public class TypeTemplateMapping {
  public Type Type { get; set; } = null!;
  public string TemplateKey { get; set; } = null!;
}