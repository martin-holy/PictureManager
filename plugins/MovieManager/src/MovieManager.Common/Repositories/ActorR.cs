using MH.Utils.BaseClasses;
using MovieManager.Common.Models;
using PictureManager.Plugins.Common.Interfaces.Repositories;

namespace MovieManager.Common.Repositories;

/// <summary>
/// DB fields: Id|Name|Person
/// </summary>
public class ActorR(CoreR coreR, IPluginHostCoreR phCoreR) : TableDataAdapter<ActorM>(coreR, "Actors", 3) {
  public override ActorM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1]);

  public override string ToCsv(ActorM item) =>
    string.Join("|",
      item.GetHashCode().ToString(),
      item.Name,
      item.Person?.GetHashCode().ToString() ?? string.Empty);

  public override void LinkReferences() {
    foreach (var (item, csv) in AllCsv)
      item.Person = phCoreR.Person.GetById(csv[2], true);
  }

  public ActorM ItemCreate(string name) =>
    ItemCreate(new ActorM(GetNextId(), name));
}