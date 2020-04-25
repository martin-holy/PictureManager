using System.Collections.Generic;
using System.Linq;

namespace PictureManager.Utils {
  public static class StringUtils {
    private static readonly HashSet<char> CommentAllowedCharacters = new HashSet<char>("@#$€_&+-()*':;!?=<>% ");

    public static string NormalizeComment(string comment) {
      return string.IsNullOrEmpty(comment)
        ? null
        : new string(comment.Where(x => char.IsLetterOrDigit(x) || CommentAllowedCharacters.Contains(x)).ToArray());
    }
  }
}
