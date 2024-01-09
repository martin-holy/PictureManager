using System.Collections.Generic;

namespace PictureManager.Domain.Models;

public interface IHaveKeywords {
  public List<KeywordM> Keywords { get; set; }
}