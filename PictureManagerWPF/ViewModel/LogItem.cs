namespace PictureManager.ViewModel {
  public class LogItem {
    public string Title { get; set; }
    public string Detail { get; set; }

    public LogItem(string title, string detail) {
      Title = title;
      Detail = detail;
    }
  }
}
