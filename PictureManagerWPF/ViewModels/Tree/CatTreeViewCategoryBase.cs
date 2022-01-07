using System;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Interfaces;
using PictureManager.Dialogs;
using PictureManager.Domain;

namespace PictureManager.ViewModels.Tree {
  public class CatTreeViewCategoryBase : CatTreeViewCategory {
    public event EventHandler OnAfterItemCreate = delegate { };
    public event EventHandler OnAfterItemRename = delegate { };
    public event EventHandler OnAfterItemDelete = delegate { };

    public Category Category { get; }
    public string Name { get; }

    protected CatTreeViewCategoryBase(Category category, string name) {
      Category = category;
      Name = name;
    }

    public virtual string GetTitle(object item) => throw new NotImplementedException();

    protected virtual string ValidateNewItemName(ICatTreeViewItem root, string name) => throw new NotImplementedException();
    protected virtual ICatTreeViewItem ModelItemCreate(ICatTreeViewItem root, string name) => throw new NotImplementedException();
    protected virtual void ModelItemRename(ICatTreeViewItem item, string name) => throw new NotImplementedException();
    protected virtual void ModelItemDelete(ICatTreeViewItem item) => throw new NotImplementedException();
    
    protected virtual string ValidateNewGroupName(ICatTreeViewItem root, string name) => throw new NotImplementedException();
    protected virtual void ModelGroupCreate(ICatTreeViewItem root, string name) => throw new NotImplementedException();
    protected virtual void ModelGroupRename(ICatTreeViewGroup group, string name) => throw new NotImplementedException();
    protected virtual void ModelGroupDelete(ICatTreeViewGroup group) => throw new NotImplementedException();

    public override void ItemCreate(ICatTreeViewItem root) {
      if (!GetNewName(true, string.Empty, out var newName,
        root, ValidateNewItemName, CategoryToIconName(Category))) return;

      try {
        root.IsExpanded = true;
        var item = ModelItemCreate(root, newName);
        OnAfterItemCreate(item, EventArgs.Empty);
      }
      catch (Exception ex) {
        ErrorDialog.Show(ex);
      }
    }

    public override void ItemRename(ICatTreeViewItem item) {
      if (!GetNewName(true, GetTitle(item), out var newName,
        item, ValidateNewItemName, CategoryToIconName(Category))) return;

      try {
        ModelItemRename(item, newName);
        OnAfterItemRename(item, EventArgs.Empty);
      }
      catch (Exception ex) {
        ErrorDialog.Show(ex);
      }
    }

    public override void ItemDelete(ICatTreeViewItem item) {
      if (!DeleteAccepted(GetTitle(item))) return;

      try {
        var parent = item.Parent as ICatTreeViewItem;

        ModelItemDelete(item);

        // collapse parent if doesn't have any sub items
        if (parent != null && parent.Items.Count == 0)
          parent.IsExpanded = false;

        OnAfterItemDelete(item, EventArgs.Empty);
      }
      catch (Exception ex) {
        ErrorDialog.Show(ex);
      }
    }

    public override void GroupCreate(ICatTreeViewItem root) {
      if (!GetNewName(false, string.Empty, out var newName,
        root, ValidateNewGroupName, CategoryToIconName(Category))) return;

      ModelGroupCreate(root, newName);
    }

    public override void GroupRename(ICatTreeViewGroup group) {
      if (!GetNewName(false, GetTitle(group), out var newName,
        group, ValidateNewGroupName, CategoryToIconName(Category))) return;

      ModelGroupRename(group, newName);
    }

    public override void GroupDelete(ICatTreeViewGroup group) {
      if (!DeleteAccepted(GetTitle(group))) return;

      ModelGroupDelete(group);
    }

    private static bool GetNewName(bool forItem, string oldName, out string newName, ICatTreeViewItem item, Func<ICatTreeViewItem, string, string> validator, string icon) {
      var action = string.IsNullOrEmpty(oldName) ? "New" : "Rename";
      var target = forItem ? "Item" : "Group";
      var question = string.IsNullOrEmpty(oldName)
        ? $"Enter the name of the new {target}."
        : $"Enter the new name for the {target}.";
      var result = InputDialog.Open(
        icon,
        $"{action} {target}",
        question,
        oldName,
        answer => validator(item, answer),
        out newName);

      return result;
    }

    private static bool DeleteAccepted(string name) =>
      MessageDialog.Show(
        "Delete Confirmation",
        $"Do you really want to delete '{name}'?",
        true);

    private static string CategoryToIconName(Category category) {
      return category switch {
        Category.FavoriteFolders => "IconFolderStar",
        Category.Folders => "IconFolder",
        Category.Ratings => "IconStar",
        Category.People => "IconPeopleMultiple",
        Category.FolderKeywords => "IconFolder",
        Category.Keywords => "IconTagLabel",
        Category.Viewers => "IconEye",
        Category.VideoClips => "IconMovieClapper",
        _ => "IconNBug"
      };
    }
  }
}
