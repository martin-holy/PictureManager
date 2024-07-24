using PictureManager.Common.Features.CategoryGroup;

namespace PictureManager.Common.Features.Keyword;

public class KeywordCategoryGroupM(int id, string name, Category category, string icon)
  : CategoryGroupM(id, name, category, icon);