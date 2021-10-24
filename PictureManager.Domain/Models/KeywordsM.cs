﻿using PictureManager.Domain.DataAdapters;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PictureManager.Domain.EventsArgs;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Utils;

namespace PictureManager.Domain.Models {
  public sealed class KeywordsM : ITreeBranch {
    #region ITreeBranch implementation
    public ITreeBranch Parent { get; set; }
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    #endregion

    private readonly Core _core;
    
    public DataAdapter DataAdapter { get; }
    public List<KeywordM> All { get; } = new();
    public Dictionary<int, KeywordM> AllDic { get; set; }
    public CategoryGroupM AutoAddedGroup { get; set; }

    public event EventHandler<KeywordDeletedEventArgs> KeywordDeletedEvent = delegate { };

    public KeywordsM(Core core) {
      _core = core;
      DataAdapter = new KeywordsDataAdapter(core, this);
    }

    private static string GetItemName(object item) => item is KeywordM k ? k.Name : string.Empty;

    // TODO refactor using Items and not All
    public KeywordM GetByFullPath(string fullPath) {
      if (string.IsNullOrEmpty(fullPath)) return null;

      var pathNames = fullPath.Split('/');

      // get top level Keyword => Parent is not Keyword but Keywords or CategoryGroup
      var keyword = All.SingleOrDefault(x => x.Parent is not KeywordM && x.Name.Equals(pathNames[0]));

      // return Keyword if it was found and is 1 level type
      if (keyword != null && pathNames.Length == 1)
        return keyword;

      // set root as => Parent of the first Keyword from fullPath (or) CategoryGroup "Auto Added"
      var root = keyword?.Parent ?? AutoAddedGroup;

      // for each keyword in pathNames => find or create
      foreach (var name in pathNames)
        root = root.Items.OfType<KeywordM>().SingleOrDefault(x => x.Name.Equals(name)) ?? ItemCreate(root, name);

      return root as KeywordM;
    }

    public KeywordM ItemCreate(ITreeBranch root, string name) {
      var item = new KeywordM(DataAdapter.GetNextId(), name, root);
      root.Items.SetInOrder(item, GetItemName);
      All.Add(item);
      DataAdapter.IsModified = true;

      return item;
    }

    public void ItemMove(KeywordM item, ITreeLeaf dest, bool aboveDest) {
      Tree.ItemMove(item, dest, aboveDest, GetItemName);
      DataAdapter.IsModified = true;
    }

    public static bool ItemCanRename(ITreeBranch root, string name) =>
      !root.Items.OfType<KeywordM>().Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public void ItemRename(KeywordM item, string name) {
      item.Name = name;
      item.Parent.Items.SetInOrder(item, GetItemName);
      DataAdapter.IsModified = true;
    }

    public void ItemDelete(KeywordM item) {
      var keywords = new List<KeywordM>();
      Tree.GetThisAndItemsRecursive(item, ref keywords);

      _core.PeopleM.RemoveKeywordsFromPeople(keywords);
      _core.Segments.RemoveKeywordsFromSegments(keywords);
      _core.MediaItems.RemoveKeywordsFromMediaItems(keywords);

      item.Parent.Items.Remove(item);

      foreach (var keyword in keywords) {
        keyword.Parent = null;
        keyword.Items = null;
        All.Remove(keyword);
        KeywordDeletedEvent(this, new(keyword));
        DataAdapter.IsModified = true;
      }
    }

    public static List<KeywordM> Toggle(List<KeywordM> list, KeywordM keyword) {
      list ??= new();

      var allKeywords = new List<KeywordM>();
      foreach (var k in list)
        Tree.GetThisAndParentRecursive(k, ref allKeywords);

      if (allKeywords.Any(x => x.Id.Equals(keyword.Id))) {
        list.Remove(keyword);
        if (list.Count == 0)
          list = null;
      }
      else {
        // remove possible redundant keywords 
        // example: if new keyword is "Weather/Sunny" keyword "Weather" is redundant
        var newKeywords = new List<KeywordM>();
        Tree.GetThisAndParentRecursive(keyword, ref newKeywords);
        foreach (var newKeyword in newKeywords)
          list.Remove(newKeyword);

        list.Add(keyword);
      }

      return list;
    }

    public static List<KeywordM> GetAllKeywords(List<KeywordM> keywords) {
      var outKeywords = new List<KeywordM>();
      if (keywords == null) return outKeywords;
      var allKeywords = new List<KeywordM>();

      foreach (var keyword in keywords)
        Tree.GetThisAndParentRecursive(keyword, ref allKeywords);

      outKeywords.AddRange(allKeywords.Distinct().OrderBy(x => x.FullName));

      return outKeywords;
    }

    public void DeleteNotUsed(IEnumerable<KeywordM> list) {
      var keywords = new HashSet<KeywordM>(list);
      foreach (var mi in _core.MediaItems.All.Cast<MediaItem>()) {
        if (mi.Keywords != null)
          foreach (var keyword in mi.Keywords.Where(x => keywords.Contains(x)))
            keywords.Remove(keyword);

        if (mi.Segments != null)
          foreach (var segment in mi.Segments?.Where(x => x.Keywords != null))
            foreach (var keyword in segment.Keywords.Where(x => keywords.Contains(x)))
              keywords.Remove(keyword);

        if (keywords.Count == 0) break;
      }

      if (keywords.Count == 0) return;

      foreach (var person in _core.PeopleM.All.Where(p => p.Keywords != null))
        foreach (var keyword in person.Keywords)
          keywords.Remove(keyword);

      foreach (var keywordM in keywords)
        ItemDelete(keywordM);
    }

    public List<MediaItem> GetMediaItems(KeywordM keyword, bool recursive) {
      var keywords = new List<KeywordM> { keyword };
      if (recursive) Tree.GetThisAndItemsRecursive(keyword, ref keywords);
      var set = new HashSet<KeywordM>(keywords);

      return _core.MediaItems.All.Cast<MediaItem>()
        .Where(mi => mi.Keywords != null && mi.Keywords.Any(k => set.Contains(k))).ToList();
    }
  }
}
