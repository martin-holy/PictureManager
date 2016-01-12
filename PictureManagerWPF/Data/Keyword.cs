namespace PictureManager.Data {
  public class Keyword: DataBase {
    public Keyword Parent { get; set; }
    private int _picCount;
    public virtual int PicCount {
      get { return _picCount; }
      set { _picCount = value; OnPropertyChanged("PicCount"); }
    }
    public int Id { get; set; }
    public int Index { get; set; }
  }
}
