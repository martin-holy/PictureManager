using MH.UI.BaseClasses;
using PictureManager.Domain.Models;
using System.Linq;
using PictureManager.Domain.Dialogs;

namespace PictureManager.Domain.TreeCategories;

public sealed class RatingsTreeCategory : TreeCategory {
  public RatingsTreeCategory() : base(Res.IconStar, "Ratings", (int)Category.Ratings) {
    Load();
  }

  public override void OnItemSelected(object o) =>
    ToggleDialogM.SetRating(o as RatingTreeM);

  private void Load() {
    for (var i = 0; i < 6; i++)
      Items.Add(new RatingTreeM(i));
  }

  public RatingTreeM GetRatingByValue(int value) =>
    Items.Cast<RatingTreeM>().Single(x => x.Rating.Value == value);
}