using MH.Utils.BaseClasses;
using MovieManager.Common.Models;

namespace MovieManager.Common.Repositories;

public sealed class GenreR(CoreR coreR) : TableDataAdapter<GenreM>(coreR, "Genres", 2) {
  public override GenreM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), csv[1]);

  public override string ToCsv(GenreM g) =>
    string.Join("|", g.GetHashCode().ToString(), g.Name);
}