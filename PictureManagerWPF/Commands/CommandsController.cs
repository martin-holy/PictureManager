using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace PictureManager.Commands {
  public static class CommandsController {
    public static void AddCommandBindings(CommandBindingCollection cbc) {
      MediaItemsCommands.AddCommandBindings(cbc);
      TreeViewCommands.AddCommandBindings(cbc);
      CatTreeViewCommands.AddCommandBindings(cbc);
      MetadataCommands.AddCommandBindings(cbc);
      WindowCommands.AddCommandBindings(cbc);
    }

    public static void AddInputBindings() {
      MediaCommands.TogglePlayPause.InputGestures.Add(new KeyGesture(Key.Space));
      MediaCommands.TogglePlayPause.InputGestures.Add(new MouseGesture(MouseAction.LeftClick));

      SetTargetToCommand(MediaCommands.TogglePlayPause, App.WMain.FullMedia);
      SetTargetToCommand(MediaItemsCommands.SelectAllCommand, App.WMain.MainTabs);
    }

    private static void SetTargetToCommand(RoutedCommand command, IInputElement commandTarget) {
      foreach (InputGesture ig in command.InputGestures)
        App.WMain.InputBindings.Add(new InputBinding(command, ig) { CommandTarget = commandTarget });
    }

    public static RoutedUICommand CreateCommand(string text, string name, InputGesture inputGesture) =>
      new(text, name, typeof(CommandsController),
        inputGesture == null ? null : new InputGestureCollection(new List<InputGesture> { inputGesture }));

    public static void AddCommandBinding(CommandBindingCollection ecb, ICommand command, Action executed) =>
      ecb.Add(new CommandBinding(command, HandleExecute(executed)));

    public static void AddCommandBinding(CommandBindingCollection ecb, ICommand command, Action executed, Func<bool> canExecute) =>
      ecb.Add(new CommandBinding(command, HandleExecute(executed), HandleCanExecute(canExecute)));

    public static void AddCommandBinding(CommandBindingCollection ecb, ICommand command, Action<object> executed) =>
      ecb.Add(new CommandBinding(command, HandleExecute(executed)));

    public static void AddCommandBinding(CommandBindingCollection ecb, ICommand command, Action<object> executed, Func<bool> canExecute) =>
      ecb.Add(new CommandBinding(command, HandleExecute(executed), HandleCanExecute(canExecute)));

    public static void AddCommandBinding(CommandBindingCollection ecb, ICommand command, Action<object> executed, Func<object, bool> canExecute) =>
      ecb.Add(new CommandBinding(command, HandleExecute(executed), HandleCanExecute(canExecute)));

    private static ExecutedRoutedEventHandler HandleExecute(Action action) =>
      (o, e) => { action(); e.Handled = true; };

    private static ExecutedRoutedEventHandler HandleExecute(Action<object> action) =>
      (o, e) => { action(e.Parameter); e.Handled = true; };

    private static CanExecuteRoutedEventHandler HandleCanExecute(Func<bool> canExecute) =>
      (o, e) => { e.CanExecute = canExecute(); e.Handled = true; };

    private static CanExecuteRoutedEventHandler HandleCanExecute(Func<object, bool> canExecute) =>
      (o, e) => { e.CanExecute = canExecute(e.Parameter); e.Handled = true; };
  }
}