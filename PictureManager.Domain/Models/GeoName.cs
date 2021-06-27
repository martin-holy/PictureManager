﻿using PictureManager.Domain.CatTreeViewModels;
using SimpleDB;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Models {
  public sealed class GeoName : CatTreeViewItem, IRecord, ICatTreeViewTagItem {
    public string[] Csv { get; set; }
    public int Id { get; } // this is GeoNameId not just DB Id
    public string ToponymName { get; set; }
    public string Fcode { get; set; }
    public List<MediaItem> MediaItems { get; } = new();

    public GeoName(int id, string name, string toponymName, string fCode, ICatTreeViewItem parent) {
      Id = id;
      Title = name;
      ToponymName = toponymName;
      Fcode = fCode;
      Parent = parent;
      IconName = IconName.LocationCheckin;
    }

    // ID|Name|ToponymName|FCode|Parent
    public string ToCsv() => string.Join("|", Id.ToString(), Title, ToponymName, Fcode, (Parent as GeoName)?.Id.ToString());

    public MediaItem[] GetMediaItems(bool recursive) => recursive ? GetMediaItemsRecursive() : MediaItems.ToArray();

    public MediaItem[] GetMediaItemsRecursive() {
      // get all GeoNames
      var geoNames = new List<ICatTreeViewItem>();
      CatTreeViewUtils.GetThisAndItemsRecursive(this, ref geoNames);

      // get all MediaItems from geoNames
      var mis = new List<MediaItem>();
      foreach (var gn in geoNames.Cast<GeoName>())
        mis.AddRange(gn.MediaItems);

      return mis.Distinct().ToArray();
    }
  }
}
