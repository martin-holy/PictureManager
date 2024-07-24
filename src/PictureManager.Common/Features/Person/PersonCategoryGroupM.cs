using PictureManager.Common.Features.CategoryGroup;

namespace PictureManager.Common.Features.Person;

public class PersonCategoryGroupM(int id, string name, Category category, string icon)
  : CategoryGroupM(id, name, category, icon);