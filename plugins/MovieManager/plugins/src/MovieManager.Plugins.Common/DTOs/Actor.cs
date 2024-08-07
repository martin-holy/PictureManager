﻿namespace MovieManager.Plugins.Common.DTOs;

public class Actor(DetailId detailId, string name, Image? image) {
  public DetailId DetailId { get; } = detailId;
  public string Name { get; } = name;
  public Image? Image { get; } = image;
}