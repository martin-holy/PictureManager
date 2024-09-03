using MH.Utils.BaseClasses;

namespace MovieManager.Common.Features.Common;

public sealed class MovieManagerToolBarVM(CoreVM coreVM) : ObservableObject {
  public CoreVM CoreVM { get; } = coreVM;
}