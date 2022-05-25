using System.Linq;
using PictureManager.Domain.Models;
using SimpleDB;

namespace PictureManager.Domain.DataAdapters {
  /// <summary>
  /// DB fields: ID|Name|Parent
  /// </summary>
  public class KeywordsDataAdapter : DataAdapter<KeywordM> {
    private readonly KeywordsM _model;
    private readonly CategoryGroupsM _categoryGroupsM;

    public KeywordsDataAdapter(KeywordsM model, CategoryGroupsM cg) : base("Keywords", 3) {
      _model = model;
      _categoryGroupsM = cg;
    }

    public override KeywordM FromCsv(string[] csv) =>
      new(int.Parse(csv[0]), csv[1], null);

    public override string ToCsv(KeywordM keyword) =>
      string.Join("|",
        keyword.Id.ToString(),
        keyword.Name,
        (keyword.Parent as KeywordM)?.Id.ToString());

    public override void LinkReferences() {
      // clear done in CategoryGroups
      //_model.Items.Clear();

      // link hierarchical keywords
      foreach (var (keyword, csv) in AllCsv) {
        // reference to parent and back reference to children
        if (!string.IsNullOrEmpty(csv[2])) {
          keyword.Parent = All[int.Parse(csv[2])];
          keyword.Parent.Items.Add(keyword);
        }
      }

      // add loose keywords
      foreach (var keywordM in All.Values.Where(x => x.Parent == null)) {
        keywordM.Parent = _model;
        _model.Items.Add(keywordM);
      }

      // group for keywords automatically added from MediaItems metadata
      _model.AutoAddedGroup = _model.Items
        .OfType<CategoryGroupM>()
        .SingleOrDefault(x => x.Name.Equals("Auto Added"))
        ?? _categoryGroupsM.GroupCreate("Auto Added", Category.Keywords);
    }
  }
}
