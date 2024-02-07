﻿using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain.Models.MediaItems;
using System.Collections.Generic;

namespace PictureManager.Domain.Database;

/// <summary>
/// DB fields: ID|MediaItem|Time|Rating|Comment|People|Keywords
/// </summary>
public class VideoImagesDA : TableDataAdapter<VideoImageM> {
  private readonly Db _db;

  public VideoImagesDA(Db db) : base("VideoImages", 7) {
    _db = db;
    IsDriveRelated = true;
  }

  public override Dictionary<string, IEnumerable<VideoImageM>> GetAsDriveRelated() =>
    Db.GetAsDriveRelated(All, x => x.Folder);

  public override VideoImageM FromCsv(string[] csv) =>
    new(int.Parse(csv[0]), null, csv[2].IntParseOrDefault(0)) {
      Rating = csv[3].IntParseOrDefault(0),
      Comment = string.IsNullOrEmpty(csv[4]) ? null : csv[4]
    };

  public override string ToCsv(VideoImageM vi) =>
    string.Join("|",
      vi.GetHashCode().ToString(),
      vi.Video.GetHashCode().ToString(),
      vi.TimeStart.ToString(),
      vi.Rating.ToString(),
      vi.Comment ?? string.Empty,
      vi.People.ToHashCodes().ToCsv(),
      vi.Keywords.ToHashCodes().ToCsv());

  public override void LinkReferences() {
    foreach (var (vi, csv) in AllCsv) {
      vi.Video = _db.Videos.GetById(csv[1]);
      vi.Video.VideoImages ??= new();
      vi.Video.VideoImages.Add(vi);
      vi.People = _db.People.Link(csv[5], this);
      vi.Keywords = _db.Keywords.Link(csv[6], this);
    }
  }

  public override int GetNextId() =>
    _db.MediaItems.GetNextId();

  public VideoImageM CustomItemCreate(VideoM video, int timeStart) =>
    ItemCreate(new(GetNextId(), video, timeStart));

  protected override void OnItemCreated(VideoImageM item) =>
    item.Video.Toggle(item);

  protected override void OnItemDeleted(VideoImageM item) {
    _db.MediaItems.OnItemDeletedCommon(item);
    item.Video.Toggle(item);
    item.Video = null;
  }
}