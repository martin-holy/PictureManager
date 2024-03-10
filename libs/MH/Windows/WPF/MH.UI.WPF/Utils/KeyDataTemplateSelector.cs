using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MH.UI.WPF.Utils {
  public class KeyDataTemplateSelector : DataTemplateSelector {
    public object[] Keys { get; set; }

    public override DataTemplate SelectTemplate(object item, DependencyObject container) {
      if (item == null || container is not FrameworkElement containerElement)
        return base.SelectTemplate(item, container);

      var type = item.GetType();
      var typeName = string.Join('.', type.Namespace, type.Name);

      if (!Keys.Contains(typeName))
        return base.SelectTemplate(item, container);

      var template = containerElement.TryFindResource(typeName) as DataTemplate;

      return template ?? base.SelectTemplate(item, container);
    }
  }
}
