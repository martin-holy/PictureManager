using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Patterns;
using PictureManager.Utils;
using PictureManager.ViewModels;

namespace PictureManager.Commands {
  public class MetadataCommands : Singleton<MetadataCommands> {
    public static RoutedUICommand EditCommand { get; } = CommandsController.CreateCommand("Edit", "MetadataEdit", new KeyGesture(Key.E, ModifierKeys.Control));
    public static RoutedUICommand SaveCommand { get; } = CommandsController.CreateCommand("Save", "MetadataSave", new KeyGesture(Key.S, ModifierKeys.Control));
    public static RoutedUICommand CancelCommand { get; } = CommandsController.CreateCommand("Cancel", "MetadataCancel", new KeyGesture(Key.Q, ModifierKeys.Control));
    public static RoutedUICommand CommentCommand { get; } = CommandsController.CreateCommand("Comment", "MetadataComment", new KeyGesture(Key.K, ModifierKeys.Control));
    public static RoutedUICommand ReloadCommand { get; } = new RoutedUICommand { Text = "Reload" };
    public static RoutedUICommand Reload2Command { get; } = new RoutedUICommand { Text = "Reload Metadata" };

    public void AddCommandBindings(CommandBindingCollection cbc) {
      CommandsController.AddCommandBinding(cbc, EditCommand, Edit, CanEdit);
      CommandsController.AddCommandBinding(cbc, SaveCommand, Save, CanSave);
      CommandsController.AddCommandBinding(cbc, CancelCommand, Cancel, CanCancel);
      CommandsController.AddCommandBinding(cbc, CommentCommand, Comment, CanComment);
      CommandsController.AddCommandBinding(cbc, ReloadCommand, Reload, CanReload);
      CommandsController.AddCommandBinding(cbc, Reload2Command, Reload, CanReload);
    }

    private static bool CanEdit() {
      return !App.Core.Model.MediaItems.IsEditModeOn && App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Count > 0;
    }

    private static void Edit() {
      App.Core.Model.MediaItems.IsEditModeOn = true;
    }

    private static bool CanSave() {
      return App.Core.Model.MediaItems.IsEditModeOn && App.Core.Model.MediaItems.ModifiedItems.Count > 0;
    }

    public void Save() {
      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Saving metadata ...");
      progress.AddEvents(
        App.Core.Model.MediaItems.ModifiedItems.ToArray(),
        null,
        // action
        delegate (MediaItem mi) {
          MediaItemsViewModel.TryWriteMetadata(mi);

          Application.Current.Dispatcher?.Invoke(delegate {
            App.Core.Model.MediaItems.SetModified(mi, false);
          });
        },
        mi => mi.FilePath,
        // onCompleted
        delegate (object sender, RunWorkerCompletedEventArgs e) {
          if (e.Cancelled)
            Cancel();
          else
            App.Core.Model.MediaItems.IsEditModeOn = false;

          App.Core.AppInfo.OnPropertyChanged(nameof(App.Core.AppInfo.FileSize));
        });

      progress.StartDialog();
    }

    private static bool CanCancel() {
      return App.Core.Model.MediaItems.IsEditModeOn;
    }

    private void Cancel() {
      var progress = new ProgressBarDialog(App.WMain, false, Environment.ProcessorCount, "Reloading metadata ...");
      progress.AddEvents(
        App.Core.Model.MediaItems.ModifiedItems.ToArray(),
        null,
        // action
        delegate (MediaItem mi) {
          MediaItemsViewModel.ReadMetadata(mi);

          Application.Current.Dispatcher?.Invoke(delegate {
            App.Core.Model.MediaItems.SetModified(mi, false);
            mi.SetInfoBox();
          });
        },
        mi => mi.FilePath,
        // onCompleted
        delegate {
          App.Core.Model.Sdb.SaveAllTables();
          App.Core.Model.MarkUsedKeywordsAndPeople();
          App.Core.Model.MediaItems.IsEditModeOn = false;
        });

      progress.StartDialog();
    }

    private static bool CanComment() {
      return App.Core.Model.MediaItems.ThumbsGrid.Current != null;
    }

    private void Comment() {
      var current = App.Core.Model.MediaItems.ThumbsGrid.Current;
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
      Core.Instance.Sdb.SetModified<MediaItems>();
    }

    private static bool CanReload(object parameter) {
      return parameter is Folder || App.Core.Model.MediaItems.ThumbsGrid.FilteredItems.Count > 0;
    }

    private void Reload(object parameter) {
      var recursive = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var folder = parameter as Folder;
      var mediaItems = folder != null
        ? folder.GetMediaItems(recursive)
        : App.Core.Model.MediaItems.ThumbsGrid.GetSelectedOrAll();

      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Reloading metadata ...");
      progress.AddEvents(
        mediaItems.ToArray(),
        null,
        // action
        delegate (MediaItem mi) {
          MediaItemsViewModel.ReadMetadata(mi);

          // set info box just for loaded media items
          if (folder == null)
            Application.Current.Dispatcher?.Invoke(mi.SetInfoBox);
        },
        mi => mi.FilePath,
        // onCompleted
        delegate {
          App.Core.Model.MarkUsedKeywordsAndPeople();
        });

      progress.Start();
    }

  }
}
