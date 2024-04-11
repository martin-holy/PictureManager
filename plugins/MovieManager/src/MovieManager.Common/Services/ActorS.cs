using MH.Utils;
using MovieManager.Common.Models;

namespace MovieManager.Common.Services;

public class ActorS {
  public Selecting<ActorM> Selected { get; } = new();
}