using System.Collections.Generic;
using System.Collections.ObjectModel;
using MH.Utils.BaseClasses;
using static MH.Utils.DragDropHelper;

namespace MH.Utils.Dialogs {
  public class GroupByDialog : Dialog {
    public ObservableCollection<object> Available { get; } = new();
    public ObservableCollection<object> Chosen { get; } = new();
    public CanDropFunc CanDropFunc { get; }
    public DoDropAction DoDropAction { get; }

    public GroupByDialog(string title, string icon) : base(title, icon) {
      Buttons = new DialogButton[] {
        new("Ok", "IconCheckMark", YesOkCommand, true),
        new("Cancel", "IconXCross", CloseCommand, false, true) };

      CanDropFunc = CanDrop;
      DoDropAction = DoDrop;
    }

    public void SetAvailable(IEnumerable<object> items) {
      Available.Clear();
      
      foreach (var item in items)
        Available.Add(item);
    }

    private DragDropEffects CanDrop(object target, object data, bool haveSameOrigin) {
      if (!haveSameOrigin && !Chosen.Contains(data))
        return DragDropEffects.Copy;
      if (haveSameOrigin && data != target)
        return DragDropEffects.Move;

      return DragDropEffects.None;
    }

    private void DoDrop(object data, bool haveSameOrigin) {
      if (!Chosen.Remove(data))
        Chosen.Add(data);
    }
  }
}
