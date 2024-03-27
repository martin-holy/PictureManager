using MH.Utils.BaseClasses;
using MovieManager.Common.Repositories;

namespace MovieManager.Common.Services;

public sealed class CoreS(CoreR coreR) : ObservableObject {
  public ImportS Import { get; } = new();
  public MovieS Movie { get; } = new();
}