﻿namespace PictureManager.Data {
  public class BaseTagItem: BaseItem {
    private bool _isMarked;
    private int _picCount;

    public bool IsMarked { get { return _isMarked; } set { _isMarked = value; OnPropertyChanged(); } }
    public int PicCount { get { return _picCount; } set { _picCount = value; OnPropertyChanged(); } }
    public int Id;
  }
}
