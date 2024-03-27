﻿namespace MovieManager.Plugins.Common.Interfaces;

public interface ISearchMovie {
  public IImage Image { get; }
  public string Id { get; }
  public string Name { get; }
  public string Type { get; }
  public int Year { get; }
}