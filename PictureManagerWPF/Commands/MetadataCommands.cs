using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using PictureManager.Database;
using PictureManager.Dialogs;
using PictureManager.Patterns;
using PictureManager.Utils;

namespace PictureManager.Commands {
  public class MetadataCommands : SingletonBase<MetadataCommands> {
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
      return !App.Core.MediaItems.IsEditModeOn && App.Core.MediaItems.FilteredItems.Count > 0;
    }

    private void Edit() {
      Application.Current.Properties[nameof(AppProperty.EditMetadataFromFolders)] = App.WMain.TabFolders.IsSelected;
      App.WMain.TabKeywords.IsSelected = true;
      App.Core.MediaItems.IsEditModeOn = true;
    }

    private static bool CanSave() {
      return App.Core.MediaItems.IsEditModeOn && App.Core.MediaItems.ModifiedItems.Count > 0;
    }

    public void Save() {
      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Saving metadata ...");
      progress.AddEvents(
        App.Core.MediaItems.ModifiedItems.ToArray(),
        null,
        // action
        delegate (MediaItem mi) {
          mi.TryWriteMetadata();

          Application.Current.Dispatcher?.Invoke(delegate {
            App.Core.MediaItems.SetModified(mi, false);
          });
        },
        mi => mi.FilePath,
        // onCompleted
        delegate (object sender, RunWorkerCompletedEventArgs e) {
          if (e.Cancelled) {
            Cancel();
          }
          else {
            if ((bool)Application.Current.Properties[nameof(AppProperty.EditMetadataFromFolders)])
              App.WMain.TabFolders.IsSelected = true;

            App.Core.MediaItems.IsEditModeOn = false;
          }
        });

      progress.StartDialog();
    }

    private static bool CanCancel() {
      return App.Core.MediaItems.IsEditModeOn;
    }

    private void Cancel() {
      var progress = new ProgressBarDialog(App.WMain, false, Environment.ProcessorCount, "Reloading metadata ...");
      progress.AddEvents(
        App.Core.MediaItems.ModifiedItems.ToArray(),
        null,
        // action
        delegate (MediaItem mi) {
          mi.ReadMetadata();

          Application.Current.Dispatcher?.Invoke(delegate {
            App.Core.MediaItems.SetModified(mi, false);
            mi.SetInfoBox();
          });
        },
        mi => mi.FilePath,
        // onCompleted
        delegate {
          App.Core.Sdb.SaveAllTables();
          App.Core.MarkUsedKeywordsAndPeople();
          App.Core.MediaItems.IsEditModeOn = false;
          if ((bool)Application.Current.Properties[nameof(AppProperty.EditMetadataFromFolders)])
            App.WMain.TabFolders.IsSelected = true;
        });

      progress.StartDialog();
    }

    private static bool CanComment() {
      return App.Core.MediaItems.Current != null;
    }

    private void Comment() {
      var current = App.Core.MediaItems.Current;
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
      current.TryWriteMetadata();
      current.SetInfoBox();
      current.OnPropertyChanged(nameof(current.Comment));
      App.Core.AppInfo.OnPropertyChanged(nameof(App.Core.AppInfo.IsCommentVisible));
    }

    private static bool CanReload(object parameter) {
      return parameter is Folder || App.Core.MediaItems.FilteredItems.Count > 0;
    }

    private void Reload(object parameter) {
      var recursive = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
      var folder = parameter as Folder;
      var mediaItems = folder != null
        ? folder.GetMediaItems(recursive)
        : App.Core.MediaItems.GetSelectedOrAll();

      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Reloading metadata ...");
      progress.AddEvents(
        mediaItems.ToArray(),
        null,
        // action
        delegate (MediaItem mi) {
          mi.ReadMetadata();

          // set info box just for loaded media items
          if (folder == null)
            Application.Current.Dispatcher?.Invoke(mi.SetInfoBox);
        },
        mi => mi.FilePath,
        // onCompleted
        delegate {
          App.Core.MediaItems.Helper.IsModified = true;
          App.Core.Sdb.SaveAllTables();
        });

      progress.Start();
    }

  }
}
