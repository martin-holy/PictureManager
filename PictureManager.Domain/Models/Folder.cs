using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using SimpleDB;

namespace PictureManager.Domain.Models {
  public sealed class Folder : BaseTreeViewTagItem, IRecord, IEquatable<Folder> {
    public string[] Csv { get; set; }
    public int Id { get; }
    public List<MediaItem> MediaItems { get; } = new List<MediaItem>();
    public FolderKeyword FolderKeyword { get; set; }

    private bool _isAccessible;
    private bool _isHidden;
    private bool _isFolderKeyword;

    public bool IsFolderKeyword {
      get => _isFolderKeyword;
      set {
        _isFolderKeyword = value;
        IconName = value ? IconName.FolderPuzzle : IconName.Folder;
      }
    }
    public bool IsAccessible { get => _isAccessible; set { _isAccessible = value; OnPropertyChanged(); } }
    public bool IsHidden { get => _isHidden; set { _isHidden = value; OnPropertyChanged(); } }
    public string FullPath => GetFullPath(Path.DirectorySeparatorChar.ToString());
    public string FullPathCache => FullPath.Replace(Path.VolumeSeparatorChar.ToString(), Core.Instance.CachePath);
    public override bool IsExpanded {
      get => base.IsExpanded;
      set {
        base.IsExpanded = value;
        if (value) LoadSubFolders(false);
        if (Parent != null && !IsFolderKeyword) // not Drive Folder and not FolderKeyword
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

    #region IEquatable implementation

    public bool Equals(Folder other) {
      return Id == other?.Id;
    }

    public override bool Equals(object obj) {
      return Equals(obj as Folder);
    }

    public override int GetHashCode() {
      return Id;
    }

    public static bool operator ==(Folder f1, Folder f2) {
      return f1?.Equals(f2) ?? ReferenceEquals(f2, null);
    }

    public static bool operator !=(Folder f1, Folder f2) {
      return !(f1 == f2);
    }

    #endregion

    public void Rename(string newName) {
      Directory.Move(FullPath, Extensions.PathCombine(((Folder) Parent).FullPath, newName));
      if (Directory.Exists(FullPathCache))
        Directory.Move(FullPathCache, Extensions.PathCombine(((Folder) Parent).FullPathCache, newName));
      Title = newName;
      Core.Instance.Folders.Helper.IsModified = true;

      // sort
      var newIndex = ((Folder) Parent).Items.OrderBy(x => x.Title).ToList().IndexOf(this);
      var oldIndex = ((Folder) Parent).Items.IndexOf(this);
      ((Folder) Parent).Items.Move(oldIndex, newIndex);
    }

    public void CopyTo(Folder destFolder, ref HashSet<string> skipped, ref Dictionary<string, string> renamed) {
      // reload destFolder so that new folder is added
      destFolder.LoadSubFolders(false);

      // get target folder
      var targetFolder = destFolder.GetByPath(Title);
      if (targetFolder == null) return; // if folder doesn't exists => nothing was copied

      // Copy all MediaItems to target folder
      foreach (var mi in MediaItems) {
        var filePath = mi.FilePath;
        var fileName = mi.FileName;

        // skip if this file was skipped
        if (skipped.Remove(filePath)) continue;

        // change the file name if the file was renamed
        if (renamed.TryGetValue(filePath, out var newFileName)) {
          fileName = newFileName;
          renamed.Remove(filePath);
        }

        mi.CopyTo(targetFolder, fileName);
      }

      // Copy all subFolders
      foreach (var subFolder in Items.OfType<Folder>()) {
        subFolder.CopyTo(targetFolder, ref skipped, ref renamed);
      }

      // if srcFolder have subFolders and targetFolder not => add place holder
      if (Items.Count > 0 && targetFolder.Items.Count == 0)
        targetFolder.Items.Add(new BaseTreeViewItem());
    }

    public void MoveTo(Folder destFolder, ref HashSet<string> skipped) {
      // get target folder without reload!
      var targetFolder = (Folder) destFolder.Items.SingleOrDefault(x => x.Title.Equals(Title));
      var srcExists = Directory.Exists(FullPath);
      var deleteThis = !srcExists && targetFolder != null;

      // if nothing was skipped and folder with the same name doesn't exist in destination
      if (!srcExists && targetFolder == null) {
        Parent.Items.Remove(this);
        Parent = destFolder;

        // add folder to the tree if destination is empty
        if (destFolder.Items.Count == 1 && destFolder.Items[0].Title == null) {
          destFolder.Items.Clear();
          destFolder.Items.Add(this);
          return;
        }

        // insert folder to the tree in sort order
        var folder = destFolder.Items.Cast<Folder>().FirstOrDefault(
          f => string.Compare(f.Title, Title, StringComparison.OrdinalIgnoreCase) >= 0);
        destFolder.Items.Insert(folder == null ? destFolder.Items.Count : destFolder.Items.IndexOf(folder), this);

        return;
      }

      if (targetFolder == null) {
        // reload destFolder so that new folder is added
        destFolder.LoadSubFolders(false);
        targetFolder = destFolder.GetByPath(Title);
      }
      if (targetFolder == null) throw new DirectoryNotFoundException();

      // Move all MediaItems to target folder
      foreach (var mi in MediaItems.ToList()) {
        // skip if this file was skipped
        if (skipped.Remove(mi.FilePath)) continue;

        mi.MoveTo(targetFolder, mi.FileName);
      }

      // Move all subFolders
      foreach (var subFolder in Items.OfType<Folder>().ToList()) {
        subFolder.MoveTo(targetFolder, ref skipped);
      }

      // if srcFolder have subFolders and targetFolder not => add place holder
      if (Items.Count > 0 && targetFolder.Items.Count == 0)
        targetFolder.Items.Add(new BaseTreeViewItem());

      // delete if this folder was moved completely and the target folder was already in DB
      if (deleteThis)
        Core.Instance.Folders.DeleteRecord(this);
    }

    public void LoadSubFolders(bool recursive) {
      // remove placeholder
      if (Items.Count == 1 && Items[0].Title == null) Items.Clear();

      var dirNames = new HashSet<string>();
      var fullPath = FullPath + Path.DirectorySeparatorChar;

      if (!Directory.Exists(fullPath)) return;

      foreach (var dir in Directory.EnumerateDirectories(fullPath)) {
        var isNew = false;
        var dirName = dir.Substring(fullPath.Length);
        dirNames.Add(dirName);

        // get existing Folder in the tree
        if (!(Items.SingleOrDefault(x => x.Title.Equals(dirName)) is Folder folder)) {
          isNew = true;
          // add new Folder to the database
          folder = new Folder(Core.Instance.Folders.Helper.GetNextId(), dirName, this);
          Core.Instance.Folders.AddRecord(folder);
        }

        // if Viewer can't see this Folder set it as hidden and continue
        if (!Core.Instance.CanViewerSeeThisFolder(folder)) {
          if (!isNew) folder.IsHidden = true;
          continue;
        }

        if (!isNew) continue;

        // add new Folder to the tree
        Items.Add(folder);

        if (FolderKeyword == null) continue;

        // remove placeholder
        if (FolderKeyword.Items.Count == 1 && FolderKeyword.Items[0].Title == null)
          FolderKeyword.Items.Clear();

        if (!(FolderKeyword.Items.SingleOrDefault(x => x.Title.Equals(folder.Title)) is FolderKeyword fk)) {
          fk = new FolderKeyword {Title = folder.Title, Parent = FolderKeyword};
          FolderKeyword.Items.Add(fk);
          FolderKeyword.Items.Sort(x => x.Title);
        }
        fk.Folders.Add(folder);
        folder.FolderKeyword = fk;
      }

      // remove Folders deleted outside of this application
      foreach (var item in Items.ToList()) {
        if (dirNames.Contains(item.Title)) continue;
        Core.Instance.Folders.DeleteRecord((Folder) item);
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
              item.Items.Add(new BaseTreeViewItem());
              item.FolderKeyword?.Items.Add(new BaseTreeViewItem());
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

      // create Folder
      Directory.CreateDirectory(Extensions.PathCombine(FullPath, folderName));
      var item = new Folder(Core.Instance.Folders.Helper.GetNextId(), folderName, this) { IsAccessible = true };

      // add new Folder to the database
      Core.Instance.Folders.AddRecord(item);

      // add new Folder to the tree
      var folder = Items.FirstOrDefault(f => string.Compare(f.Title, folderName, StringComparison.OrdinalIgnoreCase) >= 0);
      Items.Insert(folder == null ? Items.Count : Items.IndexOf(folder), item);

      return item;
    }

    public string ValidateNewFolderName(string name, bool rename) {
      // check if folder already exists
      if (Directory.Exists(Extensions.PathCombine(rename ? ((Folder)Parent).FullPath : FullPath, name))) {
        return "Folder already exists!";
      }

      // check if is correct folder name
      if (Path.GetInvalidPathChars().Any(name.Contains)) {
        return "New folder's name contains incorrect character(s)!";
      }

      return null;
    }

    public bool HasThisParent(Folder parent) {
      var p = Parent as Folder;
      while (p != null) {
        if (p.Id.Equals(parent.Id))
          return true;
        p = p.Parent as Folder;
      }

      return false;
    }

    /// <param name="path">full or partial folder path with no directory separator on the end</param>
    public Folder GetByPath(string path) {
      if (string.IsNullOrEmpty(path)) return null;
      if (FullPath.Equals(path)) return this;

      var root = this;
      var pathParts = (path.StartsWith(FullPath) 
        ? path.Substring(FullPath.Length + 1) 
        : path)
        .Split(Path.DirectorySeparatorChar);

      foreach (var pathPart in pathParts) {
        var folder = root.Items.SingleOrDefault(x => x.Title.Equals(pathPart));
        if (folder == null) return null;
        root = (Folder) folder;
      }

      return root;
    }

    public MediaItem GetMediaItemByPath(string path) {
      var lioSep = path.LastIndexOf(Path.DirectorySeparatorChar);
      var folderPath = path.Substring(0, lioSep);
      var fileName = path.Substring(lioSep + 1);
      var folder = GetByPath(folderPath);
      return folder?.MediaItems.SingleOrDefault(x => x.FileName.Equals(fileName));
    }

    public List<MediaItem> GetMediaItems(bool recursive) {
      if (!recursive) return MediaItems;

      // get all Folders
      var folders = new List<BaseTreeViewItem>();
      GetThisAndItemsRecursive(ref folders);

      // get all MediaItems from folders
      var mis = new List<MediaItem>();
      foreach (var f in folders.Cast<Folder>())
        mis.AddRange(f.MediaItems);

      return mis;
    }

    /// <summary>
    /// Hidden folder with not hidden subfolders is not supported
    /// </summary>
    /// <returns></returns>
    public bool IsThisOrParentHidden() {
      var f = this;
      do {
        if (f.IsHidden) return true;
        f = f.Parent as Folder;
      } while (f != null);

      return false;
    }

    public static List<Folder> GetFolders(List<Folder> roots, bool recursive) {
      if (!recursive) return roots;

      var output = new List<BaseTreeViewItem>();
      foreach (var root in roots) {
        root.LoadSubFolders(true);
        root.GetThisAndItemsRecursive(ref output);
      }

      return output.Cast<Folder>().ToList();
    }
  }
}
