using PictureManager.Domain.CatTreeViewModels;
using PictureManager.Domain.Utils;
using SimpleDB;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class Person : CatTreeViewItem, IRecord, ICatTreeViewTagItem, ISelectable {
    private Segment _segment;

    public string[] Csv { get; set; }
    public int Id { get; }
    public List<MediaItem> MediaItems { get; } = new List<MediaItem>();
    public List<VideoClip> VideoClips { get; set; }
    public List<Segment> Segments { get; set; } // Top Segments only
    public List<Keyword> Keywords { get; set; }
    public ObservableCollection<Keyword> DisplayKeywords { get; set; }
    public Segment Segment { get => _segment; set { _segment = value; OnPropertyChanged(); } }

    public Person(int id, string name) {
      Id = id;
      Title = name;
      IconName = IconName.People;
    }

    public MediaItem[] GetMediaItems() =>
      Core.Instance.Segments.All.Cast<Segment>().Where(x => x.PersonId == Id).Select(x => x.MediaItem)
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
      Core.Instance.People.DataAdapter.IsModified = true;
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
