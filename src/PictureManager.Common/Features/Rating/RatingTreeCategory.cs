﻿using MH.UI.BaseClasses;
using System.Linq;

namespace PictureManager.Common.Features.Rating;

public sealed class RatingTreeCategory : TreeCategory {
  public RatingTreeCategory() : base(Res.IconStar, "Ratings", (int)Category.Ratings) {
    Load();
  }

  public override void OnItemSelected(object o) =>
    Core.VM.ToggleDialog.Toggle(o as RatingTreeM);

  private void Load() {
    for (var i = 0; i < 6; i++)
      Items.Add(new RatingTreeM(i));
  }

  public RatingTreeM GetRatingByValue(int value) =>
    Items.Cast<RatingTreeM>().Single(x => x.Rating.Value == value);
}