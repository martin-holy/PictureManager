using System;

namespace PictureManager.Patterns {
  public abstract class Singleton<T> where T : class {
    private static T _instance;
    private static readonly object Lock = new();
    public static T Instance {
      get {
        lock (Lock) {
          return _instance ??= Activator.CreateInstance(typeof(T), true) as T;
        }
      }
    }
  }
}
