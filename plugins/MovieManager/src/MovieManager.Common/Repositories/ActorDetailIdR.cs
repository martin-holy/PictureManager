using MH.Utils.BaseClasses;
using MovieManager.Common.Models;
using MovieManager.Plugins.Common.Models;
using System.Linq;

namespace MovieManager.Common.Repositories;

/// <summary>
/// DB fields: Id|DetailId|DetailName|Actor
/// </summary>
public sealed class ActorDetailIdR(CoreR coreR) : TableDataAdapter<ActorDetailIdM>(coreR, "ActorDetailIds", 4) {
  public override ActorDetailIdM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1], csv[2], ActorR.Dummy);

  public override string ToCsv(ActorDetailIdM item) =>
    string.Join("|",
      item.GetHashCode().ToString(),
      item.DetailId,
      item.DetailName,
      item.Actor.GetHashCode().ToString());

  public override void LinkReferences() {
    foreach (var (item, csv) in AllCsv) {
      item.Actor = coreR.Actor.GetById(csv[3])!;
      item.Actor.DetailId = item;
    }
  }

  public ActorDetailIdM ItemCreate(DetailId detailId, ActorM actor) =>
    ItemCreate(new(GetNextId(), detailId.Id, detailId.Name, actor));

  public ActorM? GetActor(DetailId detailId) =>
    All.FirstOrDefault(x =>
      x.DetailName.Equals(detailId.Name)
      && x.DetailId.Equals(detailId.Id))?.Actor;
}