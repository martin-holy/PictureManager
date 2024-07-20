using MH.Utils.BaseClasses;
using MovieManager.Common.Models;
using System;
using System.Linq;

namespace MovieManager.Common.Repositories;

/// <summary>
/// DB fields: Id|Name
/// </summary>
public sealed class GenreR(CoreR coreR) : TableDataAdapter<GenreM>(coreR, "Genres", 2) {
  public override GenreM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1]);

  public override string ToCsv(GenreM item) =>
    string.Join("|", item.GetHashCode().ToString(), item.Name);

  public GenreM? GetGenre(string name, bool create) =>
    string.IsNullOrEmpty(name)
      ? null
      : All.SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ?? (create ? ItemCreate(new(GetNextId(), name)) : null);
}