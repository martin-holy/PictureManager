using MH.Utils.Interfaces;
using PictureManager.Interfaces.Models;
using System.Collections.Generic;

namespace PictureManager.Interfaces.Repositories;

public interface IKeywordR : IRepository<IKeywordM> {
  public IKeywordM GetByFullPath(string fullPath, IEnumerable<ITreeItem> src = null, ITreeItem rootForNew = null);
  public ITreeItem GetCategoryGroup(string name);
}