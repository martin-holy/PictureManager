using MH.UI.Controls;
using MH.Utils.BaseClasses;
using MovieManager.Common.CollectionViews;
using MovieManager.Common.Models;
using System.Linq;

namespace MovieManager.Common.ViewModels;

public sealed class MovieDetailVM : ObservableObject {
  private MovieM _movieM;

  public MovieM MovieM { get => _movieM; set { _movieM = value; OnPropertyChanged(); } }
  public CollectionViewCharacters Characters { get; } = new();

  public void Reload(MovieM movie) {
    MovieM = movie;

    if (MovieM == null) {
      Characters.Root?.Clear();
      return;
    }

    var charSource = Core.R.Character.All.Where(x => ReferenceEquals(x.Movie, movie)).ToList();

    foreach (var character in charSource)
      if (character.Actor?.Person?.Segment is { } segment)
        character.Segment = segment;

    Characters.Reload(charSource, GroupMode.ThenByRecursive, null, true);
  }
}