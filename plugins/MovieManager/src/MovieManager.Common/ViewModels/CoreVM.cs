using MH.Utils.BaseClasses;
using MovieManager.Common.Repositories;
using MovieManager.Common.Services;

namespace MovieManager.Common.ViewModels;

public sealed class CoreVM : ObservableObject {
  private readonly CoreS _coreS;
  private readonly CoreR _coreR;

  public CoreVM(CoreS coreS, CoreR coreR) {
    _coreS = coreS;
    _coreR = coreR;
  }
}