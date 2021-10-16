using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Utils;
using PictureManager.ViewModels;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace PictureManager.Commands {
  public static class MetadataCommands {
    public static RoutedUICommand EditCommand { get; } = CommandsController.CreateCommand("Edit", "MetadataEdit", new KeyGesture(Key.E, ModifierKeys.Control));
    public static RoutedUICommand SaveCommand { get; } = CommandsController.CreateCommand("Save", "MetadataSave", new KeyGesture(Key.S, ModifierKeys.Control));
    public static RoutedUICommand CancelCommand { get; } = CommandsController.CreateCommand("Cancel", "MetadataCancel", new KeyGesture(Key.Q, ModifierKeys.Control));
    public static RoutedUICommand CommentCommand { get; } = CommandsController.CreateCommand("Comment", "MetadataComment", new KeyGesture(Key.K, ModifierKeys.Control));
    public static RoutedUICommand ReloadCommand { get; } = new() { Text = "Reload" };
    public static RoutedUICommand Reload2Command { get; } = new() { Text = "Reload Metadata" };

    public static void AddCommandBindings(CommandBindingCollection cbc) {
      CommandsController.AddCommandBinding(cbc, EditCommand, Edit, CanEdit);
      CommandsController.AddCommandBinding(cbc, SaveCommand, Save, CanSave);
      CommandsController.AddCommandBinding(cbc, CancelCommand, Cancel, CanCancel);
      CommandsController.AddCommandBinding(cbc, CommentCommand, Comment, CanComment);
      CommandsController.AddCommandBinding(cbc, ReloadCommand, Reload, CanReload);
      CommandsController.AddCommandBinding(cbc, Reload2Command, Reload, CanReload);
    }

    private static bool CanEdit() => !App.Core.MediaItems.IsEditModeOn && App.Core.MediaItems.ThumbsGrid?.FilteredItems.Count > 0;

    private static void Edit() => App.Core.MediaItems.IsEditModeOn = true;

    private static bool CanSave() => App.Core.MediaItems.IsEditModeOn && App.Core.MediaItems.ModifiedItems.Count > 0;

    public static void Save() {
      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Saving metadata ...");
      progress.AddEvents(
        App.Core.MediaItems.ModifiedItems.ToArray(),
        null,
        // action
        async (MediaItem mi) => {
          MediaItemsViewModel.TryWriteMetadata(mi);
          await App.Core.RunOnUiThread(() => App.Core.MediaItems.SetModified(mi, false));
        },
        mi => mi.FilePath,
        // onCompleted
        (o, e) => {
          if (e.Cancelled)
            Cancel();
          else
            App.Core.MediaItems.IsEditModeOn = false;

          App.Core.MediaItems.OnPropertyChanged(nameof(App.Core.MediaItems.ActiveFileSize));
        });

      progress.StartDialog();
    }

    private static bool CanCancel() => App.Core.MediaItems.IsEditModeOn;

    private static void Cancel() {
      var progress = new ProgressBarDialog(App.WMain, false, Environment.ProcessorCount, "Reloading metadata ...");
      progress.AddEvents(
        App.Core.MediaItems.ModifiedItems.ToArray(),
        null,
        // action
        async (MediaItem mi) => {
          await MediaItemsViewModel.ReadMetadata(mi);

          await App.Core.RunOnUiThread(() => {
            App.Core.MediaItems.SetModified(mi, false);
            mi.SetInfoBox();
          });
        },
        mi => mi.FilePath,
        // onCompleted
        (o, e) => {
          App.Ui.MarkUsedKeywordsAndPeople();
          App.Core.MediaItems.IsEditModeOn = false;
        });

      progress.StartDialog();
    }

    private static bool CanComment() => App.Core.MediaItems.ThumbsGrid?.Current != null;

    private static void Comment() {
      var current = App.Core.MediaItems.ThumbsGrid.Current;
      var inputDialog = new InputDialog {
        Owner = App.WMain,
        IconName = IconName.Notification,
        Title = "Comment",
        Question = "Add a comment.",
        Answer = current.Comment
      };

      inputDialog.BtnDialogOk.Click += delegate {
        if (inputDialog.TxtAnswer.Text.Length > 256) {
          inputDialog.ShowErrorMessage("Comment is too long!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (!(inputDialog.ShowDialog() ?? true)) return;
      current.Comment = StringUtils.NormalizeComment(inputDialog.TxtAnswer.Text);
      current.SetInfoBox();
      current.OnPropertyChanged(nameof(current.Comment));
      MediaItemsViewModel.TryWriteMetadata(current);
      App.Core.MediaItems.DataAdapter.IsModified = true;
    }

    private static bool CanReload(object parameter) => parameter is Folder || App.Core.MediaItems.ThumbsGrid?.FilteredItems.Count > 0;

    private static void Reload(object parameter) {
      var recursive = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var folder = parameter as Folder;
      var mediaItems = folder != null
        ? folder.GetMediaItems(recursive)
        : App.Core.MediaItems.ThumbsGrid.GetSelectedOrAll();

      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Reloading metadata ...");
      progress.AddEvents(
        mediaItems.ToArray(),
        null,
        // action
        async (MediaItem mi) => {
          await MediaItemsViewModel.ReadMetadata(mi);

          // set info box just for loaded media items
          if (folder == null)
            await App.Core.RunOnUiThread(mi.SetInfoBox);
        },
        mi => mi.FilePath,
        // onCompleted
        (o, e) => App.Ui.MarkUsedKeywordsAndPeople());

      progress.Start();
    }
  }
}
