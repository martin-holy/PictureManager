using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using PictureManager.ViewModel;
using PictureManager.Dialogs;
using PictureManager.Properties;

namespace PictureManager.Database {
  public sealed class Folder : BaseTreeViewItem, IRecord {
    public string[] Csv { get; set; }
    public int Id { get; set; }
    public bool IsFolderKeyword { get; set; }
    public List<BaseMediaItem> MediaItems { get; set; } = new List<BaseMediaItem>();
    public FolderKeyword FolderKeyword { get; set; }

    private bool _isAccessible;
    public bool IsAccessible { get => _isAccessible; set { _isAccessible = value; OnPropertyChanged(); } }
    public string FullPath => GetFullPath();
    public string FullPathCache => FullPath.Replace(":\\", Settings.Default.CachePath);
    public override bool IsExpanded {
      get => base.IsExpanded;
      set {
        base.IsExpanded = value;
        if (value) LoadSubFolders(false);
        if (Parent != null) // not Drive Folder
          IconName = IsExpanded ? IconName.FolderOpen : IconName.Folder;
      }
    }

    public Folder(int id, string name, BaseTreeViewItem parent) {
      Id = id;
      Title = name;
      Parent = parent;
      IconName = IconName.Folder;
    }

    public string ToCsv() {
      // ID|Name|Parent|IsFolderKeyword
      return string.Join("|",
        Id.ToString(),
        Title,
        (Parent as Folder)?.Id.ToString(),
        IsFolderKeyword ? "1" : string.Empty);
    }

    private string GetFullPath() {
      var parent = Parent;
      var names = new List<string> {Title};
      while (parent != null) {
        names.Add(parent.Title);
        parent = parent.Parent as Folder;
      }

      names.Reverse();

      // if just drive => add empty item so the FullPath ends with DirectorySeparatorChar
      if (names.Count == 1)
        names.Add(string.Empty);

      return string.Join(Path.DirectorySeparatorChar.ToString(), names);
    }

    private void Rename(string newName) {
      Directory.Move(FullPath, Path.Combine(((Folder) Parent).FullPath, newName));
      Directory.Move(FullPathCache, Path.Combine(((Folder) Parent).FullPathCache, newName));
      Title = newName;
      ACore.Folders.Helper.IsModifed = true;

      // reload if the folder was selected before
      if (ACore.LastSelectedSource == this)
        ACore.TreeView_Select(this, false, false, ACore.LastSelectedSourceRecursive);
    }

    public void CopyTo(Folder destFolder, ref HashSet<string> skipped, ref Dictionary<string, string> renamed) {
      var targetFolder = destFolder.GetByPath(Title, true);
      if (targetFolder == null) return; // if folder doesn't exists => nothing was copied

      foreach (var mi in MediaItems) {
        var filePath = mi.FilePath;
        var fileName = mi.FileName;

        if (skipped.Remove(filePath)) continue;

        if (renamed.TryGetValue(filePath, out var newFileName)) {
          fileName = newFileName;
          renamed.Remove(filePath);
        }

        mi.CopyTo(targetFolder, fileName);
      }

      foreach (var subFolder in Items.OfType<Folder>()) {
        subFolder.CopyTo(targetFolder, ref skipped, ref renamed);
      }

      // if srcFolder have subFolders and targetFolder not => add place holder
      if (Items.Count > 0 && targetFolder.Items.Count == 0)
        targetFolder.Items.Add(new BaseTreeViewItem { Title = "..." });
    }

    public void MoveTo(Folder destFolder, ref HashSet<string> skipped) {
      var targetFolder = (Folder)destFolder.Items.SingleOrDefault(x => x.Title.Equals(Title));
      var srcExists = Directory.Exists(FullPath);
      var deleteThis = !srcExists && targetFolder != null;

      // if nothing was skipped and folder with the same name doesn't exist in destination
      if (!srcExists && targetFolder == null) {
        Parent.Items.Remove(this);
        Parent = destFolder;

        // insert in sort order
        var folder = destFolder.Items.Cast<Folder>().FirstOrDefault(
          f => string.Compare(f.Title, Title, StringComparison.OrdinalIgnoreCase) >= 0);
        destFolder.Items.Insert(folder == null ? destFolder.Items.Count : destFolder.Items.IndexOf(folder), this);

        return;
      }

      if (targetFolder == null)
        targetFolder = destFolder.GetByPath(Title, true);
      if (targetFolder == null) throw new DirectoryNotFoundException();

      foreach (var mi in MediaItems.ToList()) {
        if (skipped.Remove(mi.FilePath)) continue;
        mi.MoveTo(targetFolder, mi.FileName);
      }

      foreach (var subFolder in Items.OfType<Folder>().ToList()) {
        subFolder.MoveTo(targetFolder, ref skipped);
      }

      // if srcFolder have subFolders and targetFolder not => add place holder
      if (Items.Count > 0 && targetFolder.Items.Count == 0)
        targetFolder.Items.Add(new BaseTreeViewItem { Title = "..." });

      if (deleteThis)
        ACore.Folders.DeleteRecord(this, false);
    }

    public void LoadSubFolders(bool recursive) {
      // remove placeholder
      if (Items.Count == 1 && Items[0].Title.Equals("..."))
        Items.RemoveAt(0);

      var dirNames = new List<string>();
      foreach (var dir in Directory.EnumerateDirectories(FullPath)) {
        var isNew = false;
        var di = new DirectoryInfo(dir);
        dirNames.Add(di.Name);

        // get existing Folder in the tree
        var folder = (Folder) Items.SingleOrDefault(x => x.Title.Equals(di.Name));

        if (folder == null) {
          isNew = true;
          // add new Folder to the database
          folder = new Folder(ACore.Folders.Helper.GetNextId(), di.Name, this);
          ACore.Folders.AddRecord(folder);
        }

        // if Viewer can't see this Folder => remove Folder from the tree and continue
        if (!ACore.CanViewerSeeThisDirectory(folder)) {
          if (!isNew) Items.Remove(folder);
          continue;
        }

        // add new Folder to the tree
        if (isNew) Items.Add(folder);
      }

      // remove Folders deleted outside of this application
      foreach (var item in Items.ToList()) {
        if (dirNames.Any(x => x.Equals(item.Title))) continue;
        ACore.Folders.DeleteRecord((Folder) item, false);
      }

      // add placeholder so the folder can be expanded
      // or if recursive => keep loading
      foreach (var item in Items.Cast<Folder>()) {
        // folder already have some items
        if (!recursive && item.Items.Count > 0) {
          item.IsAccessible = true;
          continue;
        }

        try {
          if (recursive) {
            item.LoadSubFolders(true);
          }
          else {
            if (Directory.EnumerateDirectories(item.FullPath).GetEnumerator().MoveNext()) {
              item.Items.Add(new BaseTreeViewItem {Title = "..."});
            }
            item.IsAccessible = true;
          } 
        }
        catch (UnauthorizedAccessException) {
          item.IconName = IconName.FolderLock;
          item.IsAccessible = false;
        }
      }
      
      // sort Items
      Items.Sort(x => x.Title);
    }

    public Folder New(string folderName) {
      IsExpanded = true;
      try {
        // create Folder
        Directory.CreateDirectory(Path.Combine(FullPath, folderName));
        var item = new Folder(ACore.Folders.Helper.GetNextId(), folderName, this) { IsAccessible = true };

        // add new Folder to the database
        ACore.Folders.AddRecord(item);

        // add new Folder to the tree
        var folder = Items.FirstOrDefault(f => string.Compare(f.Title, folderName, StringComparison.OrdinalIgnoreCase) >= 0);
        Items.Insert(folder == null ? Items.Count : Items.IndexOf(folder), item);

        return item;
      }
      catch (Exception ex) {
        AppCore.ShowErrorDialog(ex);
        return null;
      }
    }

    public void NewOrRename(bool rename) {
      var inputDialog = new InputDialog {
        Owner = AppCore.WMain,
        IconName = IconName.Folder,
        Title = rename ? "Rename Folder" : "New Folder",
        Question = rename ? "Enter the new name for the folder." : "Enter the name of the new folder.",
        Answer = rename ? Title : string.Empty
      };

      inputDialog.BtnDialogOk.Click += delegate {
        // check if folder already exists
        if (Directory.Exists(Path.Combine(rename ? ((Folder)Parent).FullPath : FullPath, inputDialog.TxtAnswer.Text))) {
          inputDialog.ShowErrorMessage("Folder already exists!");
          return;
        }

        // check if is correct folder name
        if (Path.GetInvalidPathChars().Any(inputDialog.TxtAnswer.Text.Contains)) {
          inputDialog.ShowErrorMessage("New folder's name contains incorrect character(s)!");
          return;
        }

        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (!(inputDialog.ShowDialog() ?? true)) return;
      if (rename) Rename(inputDialog.Answer);
      else New(inputDialog.Answer);

      ACore.Sdb.SaveAllTables();
    }

    /// <param name="path">full or partial folder path with no direcotry separator on the end</param>
    /// <param name="withReload">try with reload if not the path was not found</param>
    /// <returns>full folder path</returns>
    public Folder GetByPath(string path, bool withReload = false) {
      if (path.Equals(string.Empty)) return null;
      if (FullPath.Equals(path)) return this;

      var root = this;
      var pathParts = path.Replace(FullPath, string.Empty)
        .TrimStart(Path.DirectorySeparatorChar)
        .Split(Path.DirectorySeparatorChar);

      foreach (var pathPart in pathParts) {
        var folder = root.Items.SingleOrDefault(x => x.Title.Equals(pathPart));
        if (folder == null) {
          if (!withReload) return null;

          // Reload SubFolders and try it again
          root.LoadSubFolders(false);
          folder = root.Items.SingleOrDefault(x => x.Title.Equals(pathPart));
          if (folder == null) return null;
        }
        root = (Folder) folder;
      }

      return root;
    }

    public BaseMediaItem GetMediaItemByPath(string path) {
      var lioSep = path.LastIndexOf(Path.DirectorySeparatorChar);
      var folderPath = path.Substring(0, lioSep);
      var fileName = path.Substring(lioSep + 1);
      var folder = GetByPath(folderPath);
      return folder?.MediaItems.SingleOrDefault(x => x.FileName.Equals(fileName));
    }

    public List<BaseMediaItem> GetMediaItemsRecursive() {
      // get all Folders
      var folders = new List<BaseTreeViewItem>();
      GetThisAndItemsRecursive(ref folders);

      // get all MediaItems from folders
      var mis = new List<BaseMediaItem>();
      foreach (var f in folders.Cast<Folder>())
        mis.AddRange(f.MediaItems);

      return mis;
    }
  }
}
