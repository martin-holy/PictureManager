using MH.Utils;
using MH.Utils.Interfaces;
using PictureManager.Domain.Database;
using PictureManager.Domain.TreeCategories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models;

public sealed class KeywordsM {
  public KeywordsTreeCategory TreeCategory { get; }

  public KeywordsM(KeywordsDataAdapter da) {
    TreeCategory = new(da);
  }

  public KeywordM GetByFullPath(string fullPath) {
    if (string.IsNullOrEmpty(fullPath)) return null;

    var pathNames = fullPath.Split('/');
    var name = pathNames[0];
    ITreeItem root = Core.Db.Keywords.All.SingleOrDefault(
                       x => x.Parent is not KeywordM && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                     ?? Core.Db.Keywords.ItemCreate(TreeCategory.AutoAddedGroup, name);

    for (int i = 1; i < pathNames.Length; i++) {
      name = pathNames[i];
      root = root.Items.SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
             ?? Core.Db.Keywords.ItemCreate(root, name);
    }

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
}