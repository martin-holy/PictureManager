using MH.Utils;
using MovieManager.Common.Models;
using System.Collections.Generic;

namespace MovieManager.Common.Services;

public sealed class MovieS {
  public Selecting<MovieM> Selected { get; } = new();

  public void Select(List<MovieM> movies, MovieM movie, bool isCtrlOn, bool isShiftOn) {
    Selected.Select(movies, movie, isCtrlOn, isShiftOn);
    Core.VM.OpenMovieDetail(movie);
  }
}