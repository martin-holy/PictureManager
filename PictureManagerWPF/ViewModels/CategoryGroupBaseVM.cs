﻿using System.Collections.ObjectModel;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public sealed class CategoryGroupBaseVM : ITreeBranch {
    #region ITreeBranch implementation
    public ITreeBranch Parent { get; set; }
    public ObservableCollection<ITreeLeaf> Items { get; set; } = new();
    #endregion

    public CategoryGroupM Model { get; }

    public CategoryGroupBaseVM(CategoryGroupM model, ITreeBranch parent) {
      Model = model;
      Parent = parent;
    }
  }
}
