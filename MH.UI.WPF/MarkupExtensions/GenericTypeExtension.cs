using System;
using System.Windows.Markup;

namespace MH.UI.WPF.MarkupExtensions {
  public class GenericTypeExtension : MarkupExtension {
    public Type BaseType { get; set; }
    public Type[] InnerTypes { get; set; }

    public GenericTypeExtension() { }

    public GenericTypeExtension(Type baseType, Type[] innerTypes) {
      BaseType = baseType;
      InnerTypes = innerTypes;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) =>
      BaseType.MakeGenericType(InnerTypes);
  }
}
