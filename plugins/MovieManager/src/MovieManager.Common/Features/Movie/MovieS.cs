using MH.Utils;
using System.Collections.Generic;
using System.Linq;

namespace MovieManager.Common.Features.Movie;

public sealed class MovieS {
  public Selecting<MovieM> Selected { get; } = new();

  public void Select(List<MovieM> movies, MovieM movie, bool isCtrlOn, bool isShiftOn) {
    Selected.Select(movies, movie, isCtrlOn, isShiftOn);
    Core.VM.OpenMovieDetail(Selected.Items.FirstOrDefault());
  }
}