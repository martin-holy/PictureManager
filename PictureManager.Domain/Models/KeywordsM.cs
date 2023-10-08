using MH.Utils;
using MH.Utils.Extensions;
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

  // TODO refactor using Items and not All
  public KeywordM GetByFullPath(string fullPath) {
    if (string.IsNullOrEmpty(fullPath)) return null;

    var pathNames = fullPath.Split('/');

    // get top level Keyword => Parent is not Keyword but Keywords or CategoryGroup
    var keyword = Core.Db.Keywords.All.SingleOrDefault(x => x.Parent is not KeywordM && x.Name.Equals(pathNames[0]));

    // return Keyword if it was found and is 1 level type
    if (keyword != null && pathNames.Length == 1)
      return keyword;

    // set root as => Parent of the first Keyword from fullPath (or) CategoryGroup "Auto Added"
    var root = keyword?.Parent ?? TreeCategory.AutoAddedGroup;

    // for each keyword in pathNames => find or create
    foreach (var name in pathNames)
      root = root.Items.OfType<KeywordM>().SingleOrDefault(x => x.Name.Equals(name))
             ?? Core.Db.Keywords.ItemCreate(root, name);

    return root as KeywordM;
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

  public static IEnumerable<KeywordM> GetAllKeywords(IEnumerable<KeywordM> keywords) =>
    keywords
      .EmptyIfNull()
      .SelectMany(x => x.GetThisAndParents())
      .Distinct()
      .OrderBy(x => x.FullName)
      .ToArray();

  public static IEnumerable<KeywordM> GetFromPeople(IEnumerable<PersonM> people) =>
    people
      .EmptyIfNull()
      .Where(x => x.Keywords != null)
      .SelectMany(x => x.Keywords)
      .Distinct();
}