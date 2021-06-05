using System.Linq;
using PictureManager.Domain.CatTreeViewModels;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class CategoryGroup : CatTreeViewGroup, IRecord, ICatTreeViewTagItem {
    public string[] Csv { get; set; }
    public int Id { get; }
    public Category Category { get; set; }

    public CategoryGroup(int id, string name, Category category) {
      Id = id;
      Title = name;
      Category = category;
    }

    public string ToCsv() {
      // ID|Name|Category|GroupItems
      return string.Join("|",
        Id.ToString(),
        Title,
        (int) Category,
        string.Join(",", Items.Cast<IRecord>().Select(x => x.Id)));
    }
  }
}
