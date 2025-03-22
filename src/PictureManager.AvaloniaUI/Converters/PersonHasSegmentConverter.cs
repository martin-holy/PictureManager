using MH.UI.AvaloniaUI.Converters;
using PictureManager.Common.Features.Person;

namespace PictureManager.AvaloniaUI.Converters;

public sealed class PersonHasSegmentConverter : BaseConverter {
  private static readonly object _lock = new();
  private static PersonHasSegmentConverter? _inst;
  public static PersonHasSegmentConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  public override object Convert(object? value, object? parameter) =>
    value is PersonM { Segment: not null };
}