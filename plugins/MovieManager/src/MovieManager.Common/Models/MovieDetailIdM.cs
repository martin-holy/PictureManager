namespace MovieManager.Common.Models;

public class MovieDetailIdM(int id, string detailId, string detailName, MovieM movie) : BaseDetailIdM(id, detailId, detailName) {
  public MovieM Movie { get; set; } = movie;
}