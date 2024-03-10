namespace MH.Utils.BaseClasses {
  public class HeaderedListItem<T, TH> : ListItem<T> {
    private TH _contentHeader;

    public TH ContentHeader { get => _contentHeader; set { _contentHeader = value; OnPropertyChanged(); } }

    public HeaderedListItem(T content, TH contentHeader) : base(content) {
      ContentHeader = contentHeader;
    }
  }
}
