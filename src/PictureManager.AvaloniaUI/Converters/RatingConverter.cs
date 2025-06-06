﻿using Avalonia.Media;
using MH.UI.AvaloniaUI.Converters;

namespace PictureManager.AvaloniaUI.Converters;

public sealed class RatingConverter : BaseConverter {
  private static readonly object _lock = new();
  private static RatingConverter? _inst;
  public static RatingConverter Inst { get { lock (_lock) { return _inst ??= new(); } } }

  private static readonly SolidColorBrush _white = new(Color.FromRgb(255, 255, 255));
  private static readonly SolidColorBrush _gray = new(Color.FromRgb(104, 104, 104));

  public override object? Convert(object? value, object? parameter) =>
    value is not int v || !int.TryParse(parameter as string, out int p)
      ? null
      : p < v
        ? _white
        : _gray;
}