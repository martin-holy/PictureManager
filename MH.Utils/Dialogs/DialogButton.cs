namespace MH.Utils.Dialogs {
  public class DialogButton {
    public string Text { get; }
    public bool IsDefault { get; }
    public bool IsCancel { get; }

    public DialogButton(string text, bool isDefault = false, bool isCancel = false) {
      Text = text;
      IsDefault = isDefault;
      IsCancel = isCancel;
    }
  }
}
