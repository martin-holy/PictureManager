using System;
using System.Collections.Generic;
using System.Windows;

namespace MH.UI.WPF.Utils {
  public class CachedResourceDictionary : ResourceDictionary {
    private static readonly Dictionary<Uri, WeakReference> _cache = new();
    private Uri _source;

    public new Uri Source {
      get => _source;
      set {
        _source = value;

        if (!_cache.TryGetValue(_source, out var weakReference))
          AddToCache();
        else if (weakReference is { IsAlive: true })
          MergedDictionaries.Add((ResourceDictionary)weakReference.Target);
        else
          AddToCache();
      }
    }

    private void AddToCache() {
      base.Source = _source;

      if (_cache.ContainsKey(_source))
        _cache.Remove(_source);

      _cache.Add(_source, new(this, false));
    }
  }
}
