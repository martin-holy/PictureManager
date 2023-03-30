using MH.Utils.BaseClasses;
using MH.Utils.Dialogs;
using MH.Utils.Interfaces;
using PictureManager.Domain.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PictureManager.Domain.Dialogs {
  public sealed class FolderKeywordsDialogM : ObservableObject, IDialog {
    private readonly Core _core;
    private string _title;
    private int _result = -1;

    public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
    public int Result { get => _result; set { _result = value; OnPropertyChanged(); } }
    public ObservableCollection<FolderM> Items { get; } = new();

    public RelayCommand<object> CloseCommand { get; }
    public RelayCommand<FolderM> RemoveCommand { get; }
    public static RelayCommand<object> OpenCommand { get; } = new(
      () => Core.DialogHostShow(new FolderKeywordsDialogM(Core.Instance)));

    public FolderKeywordsDialogM(Core core) {
      _core = core;
      Title = "Folder Keywords";

      CloseCommand = new(() => Result = 0);
      RemoveCommand = new(Remove);

      foreach (var folder in _core.FoldersM.DataAdapter.All.Values
        .Where(x => x.IsFolderKeyword)
        .OrderBy(x => x.FullPath)) {
        Items.Add(folder);
      }
    }

    private void Remove(FolderM folder) {
      if (folder == null) return;
      if (Core.DialogHostShow(new MessageDialog("Remove Confirmation", "Are you sure?", Res.IconQuestion, true)) != 0) return;

      folder.IsFolderKeyword = false;
      Items.Remove(folder);

      _core.FoldersM.DataAdapter.IsModified = true;
      _core.FolderKeywordsM.Load(_core.FoldersM.DataAdapter.All.Values);
    }
  }
}
