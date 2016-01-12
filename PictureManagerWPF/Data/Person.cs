namespace PictureManager.Data {
  public class Person: DataBase {
    private int _picCount;
    public virtual int PicCount {
      get { return _picCount; }
      set { _picCount = value; OnPropertyChanged("PicCount"); }
    }
    public int Id { get; set; }
  }
}
