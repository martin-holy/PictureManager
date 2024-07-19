using MH.Utils.BaseClasses;
using MovieManager.Common.Models;
using PictureManager.Common.Models;
using PictureManager.Common.Models.MediaItems;
using System;
using System.Linq;
using PM = PictureManager.Common;

namespace MovieManager.Common.Repositories;

/// <summary>
/// DB fields: Id|Name|Person|Image
/// </summary>
public class ActorR(CoreR coreR, PM.Repositories.CoreR pmCoreR) : TableDataAdapter<ActorM>(coreR, "Actors", 4) {
  public static ActorM Dummy { get; } = new(0, string.Empty);
  public event EventHandler<ActorM> ActorPersonChangedEvent = delegate { };
    
  public override ActorM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1]);

  public override string ToCsv(ActorM item) =>
    string.Join("|",
      item.GetHashCode().ToString(),
      item.Name,
      item.Person?.GetHashCode().ToString(),
      item.Image?.GetHashCode().ToString());

  public override void LinkReferences() {
    foreach (var (item, csv) in AllCsv) {
      item.Person = pmCoreR.Person.GetById(csv[2], true);
      item.Image = pmCoreR.MediaItem.GetById(csv[3], true);
    }
  }

  public ActorM ItemCreate(string name) =>
    ItemCreate(new ActorM(GetNextId(), name));

  public void SetPerson(ActorM actor, PersonM? person) {
    actor.Person = person;
    IsModified = true;
    ActorPersonChangedEvent(this, actor);
  }

  public void OnMediaItemDeleted(MediaItemM mi) {
    foreach (var actor in All.Where(x => ReferenceEquals(x.Image, mi))) {
      actor.Image = null;
      IsModified = true;
    }
  }

  public void OnPersonDeleted(PersonM person) {
    foreach (var actor in All.Where(x => ReferenceEquals(x.Person, person)))
      SetPerson(actor, null);
  }
}