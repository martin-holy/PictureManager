using MH.Utils.BaseClasses;
using MovieManager.Common.Features.Actor;
using MovieManager.Common.Features.Character;
using MovieManager.Common.Features.Import;
using MovieManager.Common.Features.Movie;
using PM = PictureManager.Common;

namespace MovieManager.Common;

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