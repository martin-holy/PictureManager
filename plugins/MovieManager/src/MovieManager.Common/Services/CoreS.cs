using MH.Utils.BaseClasses;
using MovieManager.Common.Repositories;

namespace MovieManager.Common.Services;

public sealed class CoreS(CoreR coreR) : ObservableObject {
  public MovieS Movie { get; } = new(coreR);
}