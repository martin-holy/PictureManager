using MH.Utils;
using PictureManager.Domain.Database;
using PictureManager.Domain.TreeCategories;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models;

public sealed class KeywordsM {
  public KeywordsTreeCategory TreeCategory { get; }

  public KeywordsM(KeywordsDataAdapter da) {
    TreeCategory = new(da);
  }

  public static List<KeywordM> Toggle(List<KeywordM> list, KeywordM keyword) {
    list ??= new();

    if (list.SelectMany(x => x.GetThisAndParents()).Any(x => x.Equals(keyword))) {
      list.Remove(keyword);
      if (list.Count == 0)
        list = null;
    }
    else {
      // remove possible redundant keywords 
      // example: if new keyword is "Weather/Sunny" keyword "Weather" is redundant
      foreach (var newKeyword in keyword.GetThisAndParents())
        list.Remove(newKeyword);

      list.Add(keyword);
    }

    return list;
  }
}