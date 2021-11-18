using System;
using System.Collections.Generic;
using System.Linq;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|MediaItem|TimeStart|TimeEnd|Name|Volume|Speed|Rating|Comment|People|Keywords
  /// </summary>
  public class VideoClipsDataAdapter : DataAdapter {
    private readonly Core _core;
    private readonly VideoClipsM _model;

    public VideoClipsDataAdapter(Core core, VideoClipsM model) : base("VideoClips", core.Sdb) {
      _core = core;
      _model = model;
    }

    public override void Load() {
      _model.All.Clear();
      _model.AllDic = new Dictionary<int, VideoClipM>();
      LoadFromFile();
    }

    public override void Save() => SaveToFile(_model.All.Cast<VideoClipM>(), ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 11) throw new ArgumentException("Incorrect number of values.", csv);
      var vc = new VideoClipM(int.Parse(props[0]), null) {
        TimeStart = props[2].IntParseOrDefault(0),
        TimeEnd = props[3].IntParseOrDefault(0),
        Name = string.IsNullOrEmpty(props[4]) ? null : props[4],
        Csv = props,
        Volume = props[5].IntParseOrDefault(50) / 100.0,
        Speed = props[6].IntParseOrDefault(10) / 10.0,
        Rating = props[7].IntParseOrDefault(0),
        Comment = string.IsNullOrEmpty(props[8]) ? null : props[8]
      };

      _model.All.Add(vc);
      _model.AllDic.Add(vc.Id, vc);
    }

    public static string ToCsv(VideoClipM videoClip) =>
      string.Join("|",
        videoClip.Id.ToString(),
        videoClip.MediaItem.Id.ToString(),
        videoClip.TimeStart.ToString(),
        videoClip.TimeEnd.ToString(),
        videoClip.Name ?? string.Empty,
        ((int)(videoClip.Volume * 100)).ToString(),
        ((int)(videoClip.Speed * 10)).ToString(),
        videoClip.Rating == 0 ? string.Empty : videoClip.Rating.ToString(),
        videoClip.Comment ?? string.Empty,
        videoClip.People == null ? string.Empty : string.Join(",", videoClip.People.Select(x => x.Id)),
        videoClip.Keywords == null ? string.Empty : string.Join(",", videoClip.Keywords.Select(x => x.Id)));

    public override void LinkReferences() {
      foreach (var vc in _model.All.Cast<VideoClipM>()) {
        // reference to MediaItem and back reference from MediaItem to VideoClip without group
        vc.MediaItem = _core.MediaItemsM.AllDic[int.Parse(vc.Csv[1])];
        if (vc.Group == null)
          VideoClipsM.VideoClipAdd(vc.MediaItem, vc);

        // reference to People and back reference from Person to VideoClip
        if (!string.IsNullOrEmpty(vc.Csv[9])) {
          var ids = vc.Csv[9].Split(',');
          vc.People = new(ids.Length);
          foreach (var personId in ids) 
            vc.People.Add(_core.PeopleM.AllDic[int.Parse(personId)]);
        }

        // reference to Keywords
        if (!string.IsNullOrEmpty(vc.Csv[10])) {
          var ids = vc.Csv[10].Split(',');
          vc.Keywords = new(ids.Length);
          foreach (var keywordId in ids)
            vc.Keywords.Add(_core.KeywordsM.AllDic[int.Parse(keywordId)]);
        }

        // csv array is not needed any more
        vc.Csv = null;
      }
    }
  }
}
