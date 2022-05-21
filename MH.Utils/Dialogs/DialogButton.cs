namespace MH.Utils.Dialogs {
  public class DialogButton {
    public string Text { get; }
    public int Result { get; }
    public string Icon { get; }
    public bool IsDefault { get; }
    public bool IsCancel { get; }

    public DialogButton(string text, int result, string icon = null, bool isDefault = false, bool isCancel = false) {
      Text = text;
      Result = result;
      Icon = icon;
      IsDefault = isDefault;
      IsCancel = isCancel;
    }
  }
}
