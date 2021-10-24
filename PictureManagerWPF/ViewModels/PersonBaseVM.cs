using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PictureManager.Domain.Extensions;
using PictureManager.Domain.Interfaces;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;

namespace PictureManager.ViewModels {
  public class PersonBaseVM : ObservableObject, ISelectable, ITreeLeaf {
    #region ISelectable implementation
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    #endregion

    #region ITreeLeaf implementation
    public ITreeBranch Parent { get; set; }
    #endregion

    public PersonM Model { get; }
    public ObservableCollection<KeywordM> DisplayKeywords { get; set; }
    
    public PersonBaseVM(PersonM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }

    public void UpdateDisplayKeywords() {
      DisplayKeywords?.Clear();

      if (Model.Keywords == null) {
        DisplayKeywords = null;
        OnPropertyChanged(nameof(DisplayKeywords));
        return;
      }

      DisplayKeywords ??= new();
      OnPropertyChanged(nameof(DisplayKeywords));
      var allKeywords = new List<KeywordM>();

      foreach (var keyword in Model.Keywords)
        Domain.Utils.Tree.GetThisAndItemsRecursive(keyword, ref allKeywords);

      foreach (var keyword in allKeywords.Distinct().OrderBy(x => x.FullName))
        DisplayKeywords.Add(keyword);
    }
  }
}
