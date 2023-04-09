using MH.Utils;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|MediaItem|TimeStart|TimeEnd|Name|Volume|Speed|Rating|Comment|People|Keywords
  /// </summary>
  public class VideoClipsDataAdapter : DataAdapter<VideoClipM> {
    private readonly VideoClipsM _model;
    private readonly MediaItemsM _mediaItemsM;
    private readonly KeywordsM _keywordsM;
    private readonly PeopleM _peopleM;

    public VideoClipsDataAdapter(VideoClipsM model, MediaItemsM mi, KeywordsM k, PeopleM p) : base("VideoClips", 11) {
      _model = model;
      _mediaItemsM = mi;
      _keywordsM = k;
      _peopleM = p;
    }

    public IEnumerable<VideoClipM> GetAll() {
      foreach (var item in _mediaItemsM.MediaItemVideoClips.Values.SelectMany(x => x))
        switch (item) {
          case VideoClipsGroupM vcg:
            foreach (var vc in vcg.Items.Cast<VideoClipM>())
              yield return vc;
            break;

          case VideoClipM vc:
            yield return vc;
            break;
        }
    }

    public override void Save() =>
      SaveDriveRelated(GetAll()
        .GroupBy(x => Tree.GetTopParent(x.MediaItem.Folder))
        .ToDictionary(x => x.Key.Name, x => x.AsEnumerable()));

    public override VideoClipM FromCsv(string[] csv) =>
      new(int.Parse(csv[0]), null) {
        TimeStart = csv[2].IntParseOrDefault(0),
        TimeEnd = csv[3].IntParseOrDefault(0),
        Name = string.IsNullOrEmpty(csv[4]) ? null : csv[4],
        Volume = csv[5].IntParseOrDefault(50) / 100.0,
        Speed = csv[6].IntParseOrDefault(10) / 10.0,
        Rating = csv[7].IntParseOrDefault(0),
        Comment = string.IsNullOrEmpty(csv[8]) ? null : csv[8]
      };

    public override string ToCsv(VideoClipM vc) =>
      string.Join("|",
        vc.Id.ToString(),
        vc.MediaItem.Id.ToString(),
        vc.TimeStart.ToString(),
        vc.TimeEnd.ToString(),
        vc.Name ?? string.Empty,
        ((int)(vc.Volume * 100)).ToString(),
        ((int)(vc.Speed * 10)).ToString(),
        vc.Rating == 0
          ? string.Empty
          : vc.Rating.ToString(),
        vc.Comment ?? string.Empty,
        vc.People == null
          ? string.Empty
          : string.Join(",", vc.People.Select(x => x.Id)),
        vc.Keywords == null
          ? string.Empty
          : string.Join(",", vc.Keywords.Select(x => x.Id)));

    public override void LinkReferences() {
      foreach (var (vc, csv) in AllCsv) {
        // reference to MediaItem
        vc.MediaItem = _mediaItemsM.DataAdapter.All[int.Parse(csv[1])];
        vc.MediaItem.HasVideoClips = true;

        // set parent for clips not in an group
        if (vc.Parent == null) {
          vc.Parent = _model;

          if (!_mediaItemsM.MediaItemVideoClips.ContainsKey(vc.MediaItem))
            _mediaItemsM.MediaItemVideoClips.Add(vc.MediaItem, new());
          _mediaItemsM.MediaItemVideoClips[vc.MediaItem].Add(vc);
        }

        // reference to People
        vc.People = LinkList(csv[9], _peopleM.DataAdapter.All);

        // reference to Keywords
        vc.Keywords = LinkList(csv[10], _keywordsM.DataAdapter.All);
      }
    }
  }
}
