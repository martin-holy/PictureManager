using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Common.Utils;

public static class StringUtils {
  private static readonly HashSet<char> _commentAllowedCharacters = new("@#$€_&+-()*'.:;!?=<>% ");

  public static string? NormalizeComment(string? comment) =>
    string.IsNullOrEmpty(comment)
      ? null
      : new string(comment.Where(x => char.IsLetterOrDigit(x) || _commentAllowedCharacters.Contains(x)).ToArray());
}