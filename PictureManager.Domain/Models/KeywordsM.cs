using PictureManager.Domain.Database;
using PictureManager.Domain.TreeCategories;

namespace PictureManager.Domain.Models;

public sealed class KeywordsM {
  public KeywordsTreeCategory TreeCategory { get; }

  public KeywordsM(KeywordsDA da) {
    TreeCategory = new(da);
  }
}