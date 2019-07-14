using System.Linq;
using PictureManager.ViewModel;

namespace PictureManager.Database {
  public sealed class CategoryGroup : BaseTreeViewTagItem, IRecord {
    public string[] Csv { get; set; }
    public int Id { get; set; }
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
        string.Join(",", Items.OfType<IRecord>().Select(x => x.Id)));
    }
  }
}
