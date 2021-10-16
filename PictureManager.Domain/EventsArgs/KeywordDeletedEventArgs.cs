using System;
using PictureManager.Domain.Models;

namespace PictureManager.Domain.EventsArgs {
  public class KeywordDeletedEventArgs : EventArgs {
    public KeywordM Keyword { get; }

    public KeywordDeletedEventArgs(KeywordM keyword) {
      Keyword = keyword;
    }
  }
}
