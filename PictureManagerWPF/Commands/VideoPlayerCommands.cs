using System.IO;
using System.Windows.Input;
using PictureManager.Domain;
using PictureManager.Dialogs;
using PictureManager.Domain.Models;
using PictureManager.Patterns;
using PictureManager.ViewModels;

namespace PictureManager.Commands {
  public class VideoPlayerCommands: Singleton<VideoPlayerCommands> {
    public static RoutedUICommand VideoClipSplitCommand { get; } = CommandsController.CreateCommand("Split", "Split", new KeyGesture(Key.S, ModifierKeys.Alt));
    public static RoutedUICommand VideoClipsGroupCreateCommand { get; } = new RoutedUICommand { Text = "New Group" };
    public static RoutedUICommand VideoClipsGroupDeleteCommand { get; } = new RoutedUICommand { Text = "Delete Group" };
    public static RoutedUICommand VideoClipCreateCommand { get; } = new RoutedUICommand { Text = "New Clip" };
    public static RoutedUICommand VideoClipDeleteCommand { get; } = new RoutedUICommand { Text = "Delete Clip" };

    public void AddCommandBindings(CommandBindingCollection cbc) {
      CommandsController.AddCommandBinding(cbc, VideoClipSplitCommand, VideoClipSplit, VideoSourceIsNotNull);
      CommandsController.AddCommandBinding(cbc, VideoClipsGroupCreateCommand, VideoClipsGroupCreate, VideoSourceIsNotNull);
      CommandsController.AddCommandBinding(cbc, VideoClipsGroupDeleteCommand, VideoClipsGroupDelete, VideoSourceIsNotNull);
      CommandsController.AddCommandBinding(cbc, VideoClipCreateCommand, VideoClipCreate, VideoSourceIsNotNull);
      CommandsController.AddCommandBinding(cbc, VideoClipDeleteCommand, VideoClipDelete, VideoSourceIsNotNull);
    }

    private static bool VideoSourceIsNotNull() {
      return App.WMain.FullMedia.MediaElement.Source != null;
    }

    private void VideoClipSplit() {
      App.WMain.FullMedia.SplitClip();
    }

    private void VideoClipsGroupCreate() {
      var mi = App.Core.Model.MediaItems.ThumbsGrid.Current;
      var result = InputDialog.Open(
        IconName.Folder,
        "New Group",
        "Enter the name of the new group.",
        string.Empty,
        answer => null,
        out var output);

      if (!result) return;
      Core.Instance.VideoClipsGroups.ItemCreate(output, mi);
    }

    private void VideoClipsGroupDelete(object parameter) {
      if (!(parameter is VideoClipsGroup vcg)) return;
      if (!MessageDialog.Show("Delete Confirmation", $"Do you really want to delete {vcg.Name}?", true)) return;
      Core.Instance.VideoClipsGroups.ItemDelete(vcg);
      App.WMain.FullMedia.Groups.Remove(vcg);
    }

    public void VideoClipCreate(object parameter) {
      var mi = App.Core.Model.MediaItems.ThumbsGrid.Current;
      Core.Instance.VideoClips.ItemCreate(mi, parameter as VideoClipsGroup);
    }

    public void VideoClipDelete(object parameter) {
      if (!(parameter is VideoClipViewModel vc)) return;
      if (!MessageDialog.Show("Delete Confirmation", $"Do you really want to delete {vc.Name}?", true)) return;
      Core.Instance.VideoClips.ItemDelete(vc.Clip);
      File.Delete(vc.ThumbPath.LocalPath);
      App.WMain.FullMedia.Clips.Remove(vc);
      for (var i = 0; i < App.WMain.FullMedia.Clips.Count; i++)
        (App.WMain.FullMedia.Clips[i]).Index = i + 1;
    }
  }
}
