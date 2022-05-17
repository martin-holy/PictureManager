using System;
using System.Linq;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|MediaItem|TimeStart|TimeEnd|Name|Volume|Speed|Rating|Comment|People|Keywords
  /// </summary>
  public class VideoClipsDataAdapter : DataAdapter<VideoClipM> {
    private readonly VideoClipsM _model;
    private readonly MediaItemsM _mediaItemsM;
    private readonly KeywordsM _keywordsM;
    private readonly PeopleM _peopleM;

    public VideoClipsDataAdapter(SimpleDB.SimpleDB db, VideoClipsM model, MediaItemsM mi, KeywordsM k, PeopleM p)
      : base("VideoClips", db) {
      _model = model;
      _mediaItemsM = mi;
      _keywordsM = k;
      _peopleM = p;
    }

    public override void Load() {
      _model.All.Clear();
      LoadFromFile();
    }

    public override void Save() =>
      SaveToFile(_model.All, ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 11) throw new ArgumentException("Incorrect number of values.", csv);
      var vc = new VideoClipM(int.Parse(props[0]), null) {
        TimeStart = props[2].IntParseOrDefault(0),
        TimeEnd = props[3].IntParseOrDefault(0),
        Name = string.IsNullOrEmpty(props[4]) ? null : props[4],
        Volume = props[5].IntParseOrDefault(50) / 100.0,
        Speed = props[6].IntParseOrDefault(10) / 10.0,
        Rating = props[7].IntParseOrDefault(0),
        Comment = string.IsNullOrEmpty(props[8]) ? null : props[8]
      };

      _model.All.Add(vc);
      AllCsv.Add(vc, props);
      AllId.Add(vc.Id, vc);
    }

    public static string ToCsv(VideoClipM vc) =>
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
        vc.MediaItem = _mediaItemsM.DataAdapter.AllId[int.Parse(csv[1])];
        vc.MediaItem.HasVideoClips = true;

        // set parent for clips not in an group
        vc.Parent ??= _model;

        // reference to People
        if (!string.IsNullOrEmpty(csv[9])) {
          var ids = csv[9].Split(',');
          vc.People = new(ids.Length);
          foreach (var personId in ids) 
            vc.People.Add(_peopleM.DataAdapter.AllId[int.Parse(personId)]);
        }

        // reference to Keywords
        if (!string.IsNullOrEmpty(csv[10])) {
          var ids = csv[10].Split(',');
          vc.Keywords = new(ids.Length);
          foreach (var keywordId in ids)
            vc.Keywords.Add(_keywordsM.DataAdapter.AllId[int.Parse(keywordId)]);
        }
      }
    }
  }
}
