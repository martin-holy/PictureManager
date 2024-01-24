using MH.Utils;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Domain.Models.MediaItems;
using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Domain.Extensions;

public static class GetDataExtensions {
  public static IEnumerable<CategoryGroupM> GetCategoryGroups<T>(this T item) where T : ITreeItem =>
    item.Parent is CategoryGroupM cg
      ? cg.GetThisAndParents()
      : Enumerable.Empty<CategoryGroupM>();
}