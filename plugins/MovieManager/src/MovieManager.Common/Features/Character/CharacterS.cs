using MH.Utils;
using MH.Utils.BaseClasses;

namespace MovieManager.Common.Features.Character;

public class CharacterS {
  public Selecting<CharacterM> Selected { get; } = new();

  public void Select(SelectionEventArgs<CharacterM> e) {
    Selected.Select(e.Item);
    Core.S.Actor.Selected.Select(e.Item.Actor);
  }
}