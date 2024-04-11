using MH.UI.Controls;
using MH.Utils.BaseClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MH.UI.Dialogs;

public interface IToggleDialogSourceType {
  public Type Type { get; }
  public string Icon { get; }
  public string Title { get; }
  public string Message { get; }
  public List<IToggleDialogOption> Options { get; }
}

public interface IToggleDialogTargetType {
  public object[] Items { get; }
  public Tuple<string, string> Init(object item);
  public void Clear();
}

public interface IToggleDialogOption {
  public IToggleDialogTargetType TargetType { get; }
  public Action<object[], object> SetItems { get; }
}

public class ToggleDialog : Dialog {
  public List<IToggleDialogSourceType> SourceTypes { get; } = new();
  public string Message { get; private set; }
  public ListItem Item { get; private set; }

  public ToggleDialog(string title, string icon) : base(title, icon) { }

  public void Toggle(ListItem item) {
    if (SourceTypes.SingleOrDefault(x => x.Type == item?.GetType()) is not { } st) return;

    var buttons = new List<DialogButton>();
    for (var i = 0; i < st.Options.Count; i++) {
      if (st.Options[i].TargetType.Init(item) is not { } iconText) continue;
      buttons.Add(new(SetResult(i + 1, iconText.Item1, iconText.Item2)));
    }
    if (buttons.Count == 0) return;

    Icon = st.Icon;
    Title = st.Title;
    Message = st.Message;
    Item = item;
    Buttons = buttons.ToArray();

    Show(this);

    if (Result > 0) {
      var opt = st.Options[Result - 1];
      opt.SetItems(opt.TargetType.Items, item);
    }

    st.Options.ForEach(x => x.TargetType.Clear());
  }
}

public class ToggleDialogSourceType<T> : IToggleDialogSourceType {
  public Type Type { get; }
  public string Icon { get; }
  public string Title { get; }
  public string Message { get; }
  public List<IToggleDialogOption> Options { get; } = new();

  public ToggleDialogSourceType(string icon, string title, string message) {
    Type = typeof(T);
    Icon = icon;
    Title = title;
    Message = message;
  }
}

public class ToggleDialogTargetType<TTarget> : IToggleDialogTargetType {
  private readonly string _icon;
  private readonly Func<object, TTarget[]> _getItems;
  private readonly Func<int, string> _getButtonText;

  public TTarget[] Items { get; private set; }
  object[] IToggleDialogTargetType.Items => Array.ConvertAll(Items, item => (object)item);

  public ToggleDialogTargetType(string icon, Func<object, TTarget[]> getItems, Func<int, string> getButtonText) {
    _icon = icon;
    _getItems = getItems;
    _getButtonText = getButtonText;
  }

  public Tuple<string, string> Init(object item) {
    Items = _getItems(item);
    return Items.Length == 0 ? null : new(_icon, _getButtonText(Items.Length));
  }

  public void Clear() {
    Items = null;
  }
}

public class ToggleDialogOption<TSource, TTarget> : IToggleDialogOption where TSource : class {
  public ToggleDialogTargetType<TTarget> TargetType { get; }
  public Action<TTarget[], TSource> SetItems { get; }

  IToggleDialogTargetType IToggleDialogOption.TargetType => TargetType;
  Action<object[], object> IToggleDialogOption.SetItems => (items, item) =>
    SetItems(Array.ConvertAll(items, x => (TTarget)x), (TSource)item);

  public ToggleDialogOption(ToggleDialogTargetType<TTarget> targetType, Action<TTarget[], TSource> setItems) {
    TargetType = targetType;
    SetItems = setItems;
  }
}