using MH.Utils;
using MH.Utils.BaseClasses;
using MovieManager.Common.Models;

namespace MovieManager.Common.Services;

public class CharacterS {
  public Selecting<CharacterM> Selected { get; } = new();

  public void Select(SelectionEventArgs<CharacterM> e) {
    Selected.Select(e.Item);
    Core.S.Actor.Selected.Select(e.Item.Actor);
  }
}