﻿using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Utils;
using SimpleDB;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class Person : CatTreeViewItem, IRecord, ICatTreeViewTagItem, ISelectable {
    private Face _face;

    public string[] Csv { get; set; }
    public int Id { get; }
    public List<MediaItem> MediaItems { get; } = new List<MediaItem>();
    public List<VideoClip> VideoClips { get; set; }
    public ObservableCollection<Face> Faces { get; set; } // Top Faces only
    public List<Keyword> Keywords { get; set; }
    public ObservableCollection<Keyword> DisplayKeywords { get; set; }
    public Face Face { get => _face; set { _face = value; OnPropertyChanged(); } }

    public Person(int id, string name) {
      Id = id;
      Title = name;
      IconName = IconName.People;
    }

    // ID|Name|Faces|Keywords
    public string ToCsv() => string.Join("|",
      Id.ToString(),
      Title,
      Faces == null ? string.Empty : string.Join(",", Faces.Select(x => x.Id)),
      Keywords == null ? string.Empty : string.Join(",", Keywords.Select(x => x.Id)));

    public MediaItem[] GetMediaItems() =>
      Core.Instance.Faces.All.Cast<Face>().Where(x => x.PersonId == Id).Select(x => x.MediaItem)
      .Concat(MediaItems).Distinct().OrderBy(x => x.FileName).ToArray();

    public void ToggleKeyword(Keyword keyword) {
      if (Keywords?.Remove(keyword) == true) {
        if (!Keywords.Any())
          Keywords = null;
      }
      else {
        Keywords ??= new();
        Keywords.Add(keyword);
      }

      UpdateDisplayKeywords();
      Core.Instance.Sdb.SetModified<People>();
    }

    public void UpdateDisplayKeywords() {
      DisplayKeywords?.Clear();

      if (Keywords == null) {
        DisplayKeywords = null;
        OnPropertyChanged(nameof(DisplayKeywords));
        return;
      }

      DisplayKeywords ??= new();
      OnPropertyChanged(nameof(DisplayKeywords));
      var allKeywords = new List<ICatTreeViewItem>();

      foreach (var keyword in Keywords)
        CatTreeViewUtils.GetThisAndParentRecursive(keyword, ref allKeywords);

      foreach (var keyword in allKeywords.OfType<Keyword>().Distinct().OrderBy(x => x.FullPath))
        DisplayKeywords.Add(keyword);
    }
  }
}
