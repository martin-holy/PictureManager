﻿using MH.UI.Controls;
using MH.UI.Dialogs;
using MH.Utils.BaseClasses;
using PictureManager.Common.Repositories;

namespace PictureManager.Common.ViewModels.Entities;

public sealed class GeoNameVM {
  private readonly GeoNameR _r;

  public static RelayCommand NewGeoNameFromGpsCommand { get; set; } = null!;

  public GeoNameVM(GeoNameR r) {
    _r = r;
    NewGeoNameFromGpsCommand = new(NewGeoNameFromGps, Res.IconLocationCheckin, "New GeoName from GPS");
  }

  public async void NewGeoNameFromGps() {
    var inputDialog = new InputDialog(
      "GeoName latitude and longitude",
      "Enter in format: N36.75847,W3.84609",
      Res.IconLocationCheckin,
      string.Empty,
      answer => {
        var (a, b) = GeoNameR.ParseLatLng(answer);
        return a == 0 && b == 0
          ? "Incorrect format"
          : string.Empty;
      });

    if (Dialog.Show(inputDialog) != 1) return;

    var (lat, lng) = GeoNameR.ParseLatLng(inputDialog.Answer);
    await _r.CreateGeoNameHierarchy(lat, lng);
  }
}