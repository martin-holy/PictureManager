using MH.Utils.BaseClasses;
using MovieManager.Common.Repositories;
using PM = PictureManager.Common;

namespace MovieManager.Common.Services;

public sealed class CoreS : ObservableObject {
  public PM.Services.CoreS PMCoreS { get; }

  public ActorS Actor { get; } = new();
  public CharacterS Character { get; } = new();
  public ImportS Import { get; }
  public MovieS Movie { get; } = new();

  public CoreS(PM.Services.CoreS pmCoreS, CoreR coreR) {
    PMCoreS = pmCoreS;
    Import = new(coreR, this);
  }
}