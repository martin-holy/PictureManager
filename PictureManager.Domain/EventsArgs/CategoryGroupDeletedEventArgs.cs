using PictureManager.Domain.Models;

namespace PictureManager.Domain.EventsArgs {
  public class CategoryGroupDeletedEventArgs {
    public CategoryGroupM Group { get; }

    public CategoryGroupDeletedEventArgs(CategoryGroupM group) {
      Group = group;
    }
  }
}
