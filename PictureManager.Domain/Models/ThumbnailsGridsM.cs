﻿using System.Collections.ObjectModel;
using MH.Utils.BaseClasses;

namespace PictureManager.Domain.Models {
  public sealed class ThumbnailsGridsM : ObservableObject {
    private readonly Core _core;
    private ThumbnailsGridM _current;

    public ObservableCollection<ThumbnailsGridM> All { get; } = new();
    public ThumbnailsGridM Current { get => _current; set { _current = value; OnPropertyChanged(); } }

    public ThumbnailsGridsM(Core core) {
      _core = core;
    }

    public ThumbnailsGridM AddThumbnailsGrid() {
      var grid = new ThumbnailsGridM(_core);
      All.Add(grid);
      Current = ThumbnailsGridM.ActivateThumbnailsGrid(Current, grid);

      return grid;
    }
  }
}
