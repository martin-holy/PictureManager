using System;
using System.Linq;
using MH.Utils.Extensions;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Folder|FileName|Width|Height|Orientation|Rating|Comment|GeoName|People|Keywords|IsOnlyInDb
  /// </summary>
  public class MediaItemsDataAdapter : DataAdapter {
    private readonly MediaItemsM _model;
    private readonly FoldersM _foldersM;
    private readonly PeopleM _peopleM;
    private readonly KeywordsM _keywordsM;
    private readonly GeoNamesM _geoNamesM;

    public MediaItemsDataAdapter(SimpleDB.SimpleDB db, MediaItemsM model, FoldersM f, PeopleM p, KeywordsM k, GeoNamesM g)
      : base("MediaItems", db) {
      _model = model;
      _foldersM = f;
      _peopleM = p;
      _keywordsM = k;
      _geoNamesM = g;
    }

    public override void Load() {
      _model.All.Clear();
      _model.AllDic = new();
      LoadFromFile();
      _model.OnPropertyChanged(nameof(_model.MediaItemsCount));
    }

    public override void Save() =>
      SaveToFile(_model.All, ToCsv);

    public override void FromCsv(string csv) {
      var props = csv.Split('|');
      if (props.Length != 12) throw new ArgumentException("Incorrect number of values.", csv);
      var mi = new MediaItemM(int.Parse(props[0]), null, props[2]) {
        Csv = props,
        Width = props[3].IntParseOrDefault(0),
        Height = props[4].IntParseOrDefault(0),
        Orientation = props[5].IntParseOrDefault(1),
        Rating = props[6].IntParseOrDefault(0),
        Comment = string.IsNullOrEmpty(props[7]) ? null : props[7],
        IsOnlyInDb = props[11] == "1"
      };
      _model.All.Add(mi);
      _model.AllDic.Add(mi.Id, mi);
    }

    public static string ToCsv(MediaItemM mediaItem) =>
      string.Join("|",
        mediaItem.Id.ToString(),
        mediaItem.Folder.Id.ToString(),
        mediaItem.FileName,
        mediaItem.Width.ToString(),
        mediaItem.Height.ToString(),
        mediaItem.Orientation.ToString(),
        mediaItem.Rating.ToString(),
        mediaItem.Comment ?? string.Empty,
        mediaItem.GeoName?.Id.ToString(),
        mediaItem.People == null
          ? string.Empty
          : string.Join(",", mediaItem.People.Select(x => x.Id)),
        mediaItem.Keywords == null
          ? string.Empty
          : string.Join(",", mediaItem.Keywords.Select(x => x.Id)),
        mediaItem.IsOnlyInDb
          ? "1"
          : string.Empty);

    public override void LinkReferences() {
      foreach (var mi in _model.All.Cast<MediaItemM>()) {
        // reference to Folder and back reference from Folder to MediaItems
        mi.Folder = _foldersM.AllDic[int.Parse(mi.Csv[1])];
        mi.Folder.MediaItems.Add(mi);

        // reference to People
        if (!string.IsNullOrEmpty(mi.Csv[9])) {
          var ids = mi.Csv[9].Split(',');
          mi.People = new(ids.Length);
          foreach (var id in ids)
            mi.People.Add(_peopleM.AllDic[int.Parse(id)]);
        }

        // reference to Keywords
        if (!string.IsNullOrEmpty(mi.Csv[10])) {
          var ids = mi.Csv[10].Split(',');
          mi.Keywords = new(ids.Length);
          foreach (var id in ids)
            mi.Keywords.Add(_keywordsM.AllDic[int.Parse(id)]);
        }

        // reference to GeoName
        if (!string.IsNullOrEmpty(mi.Csv[8]))
          mi.GeoName = _geoNamesM.AllDic[int.Parse(mi.Csv[8])];

        // CSV array is not needed any more
        mi.Csv = null;
      }
    }
  }
}
