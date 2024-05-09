using MH.Utils.BaseClasses;
using MovieManager.Common.Repositories;
using PictureManager.Interfaces.Services;

namespace MovieManager.Common.Services;

public sealed class CoreS : ObservableObject {
  public IPMCoreS PMCoreS { get; }

  public ActorS Actor { get; } = new();
  public CharacterS Character { get; } = new();
  public ImportS Import { get; }
  public MovieS Movie { get; } = new();

  public CoreS(IPMCoreS pmCoreS, CoreR coreR) {
    PMCoreS = pmCoreS;
    Import = new(coreR, this);
  }
}