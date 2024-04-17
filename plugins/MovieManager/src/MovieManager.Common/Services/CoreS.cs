using MH.Utils.BaseClasses;
using PictureManager.Interfaces.Services;

namespace MovieManager.Common.Services;

public sealed class CoreS : ObservableObject {
  public ICoreS PhCoreS { get; }

  public ActorS Actor { get; } = new();
  public CharacterS Character { get; } = new();
  public ImportS Import { get; } = new();
  public MovieS Movie { get; } = new();

  public CoreS(ICoreS phCoreS) {
    PhCoreS = phCoreS;
  }
}