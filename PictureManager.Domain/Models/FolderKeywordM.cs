using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Utils;

namespace PictureManager.Domain.Models {
  public sealed class FolderKeywordM : ObservableObject, ITreeBranch {
    #region ITreeBranch implementation
    public ITreeBranch Parent { get; set; }
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    #endregion

    private string _name;
    
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public string FullPath => Tree.GetFullName(this, Path.DirectorySeparatorChar.ToString(), x => x.Name);
    public List<FolderM> Folders { get; } = new();
    public int Id { get; }

    public FolderKeywordM(int id, string name, ITreeBranch parent) {
      Id = id;
      Name = name;
      Parent = parent;
    }
  }
}
