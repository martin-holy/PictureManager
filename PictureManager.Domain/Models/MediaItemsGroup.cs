﻿namespace PictureManager.Domain.Models {
  public class MediaItemsGroup : ObservableObject {
    private string _folderFullPath;
    private string _folder;
    private string _date;
    private int _itemsCount;

    public string FolderFullPath { get => _folderFullPath; set { _folderFullPath = value; OnPropertyChanged(); } }
    public string Folder { get => _folder; set { _folder = value; OnPropertyChanged(); } }
    public string Date { get => _date; set { _date = value; OnPropertyChanged(); } }
    public int ItemsCount { get => _itemsCount; set { _itemsCount = value; OnPropertyChanged(); } }
  }
}
