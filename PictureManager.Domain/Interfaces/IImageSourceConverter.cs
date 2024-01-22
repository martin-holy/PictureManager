using System.Collections.Generic;

namespace PictureManager.Domain.Interfaces;

public interface IImageSourceConverter<T> {
  public HashSet<T> IgnoreCache { get; }
}