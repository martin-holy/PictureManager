using System;

namespace MovieManager.Common.Services;

public class ImportS {
  public void Import(string titles) {
    var lines = titles.Split(
      new [] { Environment.NewLine },
      StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
    );

    foreach (var title in lines) {
      var foundTitles = Core.TitleSearch.SearchMovie(title);
    }
  }
}