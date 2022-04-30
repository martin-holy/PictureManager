using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public sealed class TreeViewSearchVM {
    public RelayCommand<TreeViewSearchItemM> NavigateToCommand { get; }
    public RelayCommand<object> CloseCommand { get; }
    public TreeViewSearchM Model { get; }

    public TreeViewSearchVM(TreeViewSearchM model) {
      Model = model;
      NavigateToCommand = new(Model.NavigateTo);
      CloseCommand = new(() => Model.IsVisible = false);
    }
  }
}
