using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using SimpleDB;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|Category|GroupItems
  /// </summary>
  public sealed class CategoryGroupM : TreeItem, ITreeGroup, IRecord {
    public int Id { get; }
    public Category Category { get; }

    public CategoryGroupM(int id, string name, Category category, string iconName) : base(iconName, name) {
      Id = id;
      Category = category;
    }
  }
}
