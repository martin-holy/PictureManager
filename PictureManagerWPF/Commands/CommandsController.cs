using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using PictureManager.Patterns;

namespace PictureManager.Commands {
  public class CommandsController: Singleton<CommandsController> {
    public MediaItemsCommands MediaItemsCommands => MediaItemsCommands.Instance;
    public TreeViewCommands TreeViewCommands => TreeViewCommands.Instance;
    public CatTreeViewCommands CatTreeViewCommands => CatTreeViewCommands.Instance;
    public MetadataCommands MetadataCommands => MetadataCommands.Instance;
    public WindowCommands WindowCommands => WindowCommands.Instance;

    public void AddCommandBindings(CommandBindingCollection cbc) {
      MediaItemsCommands.AddCommandBindings(cbc);
      TreeViewCommands.AddCommandBindings(cbc);
      CatTreeViewCommands.AddCommandBindings(cbc);
      MetadataCommands.AddCommandBindings(cbc);
      WindowCommands.AddCommandBindings(cbc);
    }

    public void AddInputBindings() {
      MediaCommands.TogglePlayPause.InputGestures.Add(new KeyGesture(Key.Space));
      MediaCommands.TogglePlayPause.InputGestures.Add(new MouseGesture(MouseAction.LeftClick));

      SetTargetToCommand(MediaCommands.TogglePlayPause, App.WMain.FullMedia);
      SetTargetToCommand(MediaItemsCommands.SelectAllCommand, App.WMain.ThumbnailsTabs);
    }

    private void SetTargetToCommand(RoutedCommand command, IInputElement commandTarget) {
      foreach (InputGesture ig in command.InputGestures)
        App.WMain.InputBindings.Add(new InputBinding(command, ig) { CommandTarget = commandTarget });
    }

    public static RoutedUICommand CreateCommand(string text, string name, InputGesture inputGesture) {
      return new RoutedUICommand(text, name, typeof(CommandsController),
        inputGesture == null ? null : new InputGestureCollection(new List<InputGesture> { inputGesture }));
    }

    public static void AddCommandBinding(CommandBindingCollection elementCommandBindings, ICommand command, Action executed, Func<bool> canExecute) {
      elementCommandBindings.Add(new CommandBinding(command, HandleExecute(executed), HandleCanExecute(canExecute)));
    }

    public static void AddCommandBinding(CommandBindingCollection elementCommandBindings, ICommand command, Action executed) {
      elementCommandBindings.Add(new CommandBinding(command, HandleExecute(executed)));
    }

    public static void AddCommandBinding(CommandBindingCollection elementCommandBindings, ICommand command, Action<object> executed, Func<object, bool> canExecute) {
      elementCommandBindings.Add(new CommandBinding(command, HandleExecute(executed), HandleCanExecute(canExecute)));
    }

    public static void AddCommandBinding(CommandBindingCollection elementCommandBindings, ICommand command, Action<object> executed, Func<bool> canExecute) {
      elementCommandBindings.Add(new CommandBinding(command, HandleExecute(executed), HandleCanExecute(canExecute)));
    }

    public static void AddCommandBinding(CommandBindingCollection elementCommandBindings, ICommand command, Action<object> executed) {
      elementCommandBindings.Add(new CommandBinding(command, HandleExecute(executed)));
    }

    private static ExecutedRoutedEventHandler HandleExecute(Action action) {
      return (o, e) => {
        action();
        e.Handled = true;
      };
    }

    private static ExecutedRoutedEventHandler HandleExecute(Action<object> action) {
      return (o, e) => {
        action(e.Parameter);
        e.Handled = true;
      };
    }

    private static CanExecuteRoutedEventHandler HandleCanExecute(Func<bool> canExecute) {
      return (o, e) => {
        e.CanExecute = canExecute();
        e.Handled = true;
      };
    }

    private static CanExecuteRoutedEventHandler HandleCanExecute(Func<object, bool> canExecute) {
      return (o, e) => {
        e.CanExecute = canExecute(e.Parameter);
        e.Handled = true;
      };
    }
  }
}