﻿using MH.UI.BaseClasses;
using System.Linq;

namespace PictureManager.Common.Features.Rating;

public sealed class RatingTreeCategory : TreeCategory {
  public RatingTreeCategory() : base(new(), Res.IconStar, "Ratings", (int)Category.Ratings) {
    _load();
  }

  protected override void _onItemSelected(object o) =>
    _ = Core.VM.ToggleDialog.Toggle(o as RatingTreeM);

  private void _load() {
    for (var i = 0; i < 6; i++)
      Items.Add(new RatingTreeM(i));
  }

  public RatingTreeM GetRatingByValue(int value) =>
    Items.Cast<RatingTreeM>().Single(x => x.Rating.Value == value);
}
