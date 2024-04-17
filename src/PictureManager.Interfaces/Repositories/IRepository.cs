using MH.Utils.Interfaces;
using System.Collections.Generic;

namespace PictureManager.Interfaces.Repositories;

public interface IRepository<T> {
  public T GetById(string id, bool nullable = false);
  public List<T> Link(string csv, IDataAdapter seeker);
}