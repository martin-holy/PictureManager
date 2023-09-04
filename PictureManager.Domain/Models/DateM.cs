using MH.Utils.BaseClasses;

namespace PictureManager.Domain.Models {
  public class DateM : ListItem {
    public string Raw { get; set; }

    public DateM(string icon, string text, string raw) : base(icon, text) {
      Raw = raw;
    }
  }
}
