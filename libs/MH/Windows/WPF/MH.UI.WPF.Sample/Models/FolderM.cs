using MH.UI.WPF.Sample.Resources;
using MH.Utils;
using MH.Utils.BaseClasses;
using MH.Utils.Interfaces;
using System;
using System.IO;

namespace MH.UI.WPF.Sample.Models;

public class FolderM : TreeItem {
  public string FullPath => this.GetFullName(Path.DirectorySeparatorChar.ToString(), x => x.Name);

  public FolderM(ITreeItem? parent, string name) : base(Icons.Folder, name) {
    Parent = parent;
  }

  protected override void OnIsExpandedChanged(bool value) {
    if (value) LoadSubFolders();
    UpdateIcon();
  }

  private void UpdateIcon() {
    if (Parent != null) // not Drive Folder
      Icon = IsExpanded
        ? Icons.FolderOpen
        : Icons.Folder;
  }

  private void LoadSubFolders() {
    var fullPath = FullPath + Path.DirectorySeparatorChar;
    Items.Clear();

    foreach (var dir in Directory.EnumerateDirectories(fullPath)) {
      var folder = new FolderM(this, dir[fullPath.Length..]);

      try {
        // add placeholder so the folder can be expanded
        using var enumerator = Directory.EnumerateDirectories(folder.FullPath).GetEnumerator();
        if (enumerator.MoveNext())
          folder.Items.Add(new FolderM(null, string.Empty));

        // add new Folder to the tree if is Accessible
        Items.Add(folder);
      }
      catch (UnauthorizedAccessException) { }
    }
  }
}