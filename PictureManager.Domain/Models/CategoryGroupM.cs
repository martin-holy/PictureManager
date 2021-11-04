using System.Collections.ObjectModel;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using SimpleDB;

namespace PictureManager.Domain.Models {
  /// <summary>
  /// DB fields: ID|Name|Category|GroupItems
  /// </summary>
  public sealed class CategoryGroupM : ObservableObject, IRecord, ITreeBranch {
    #region IRecord implementation
    public int Id { get; }
    public string[] Csv { get; set; }
    #endregion

    #region ITreeBranch implementation
    public ITreeBranch Parent { get; set; }
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    #endregion

    private string _name;

    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public Category Category { get; }

    public CategoryGroupM(int id, string name, Category category) {
      Id = id;
      Name = name;
      Category = category;
    }
  }
}
