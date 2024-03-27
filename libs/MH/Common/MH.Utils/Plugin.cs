using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace MH.Utils;

public static class Plugin {
  public static T Load<T>(string path) where T : class {
    var asm = LoadAssembly(path);
    if (GetFirstTypeWithInterface(asm.GetTypes(), nameof(T)) is not { } type) return default;
    return Activator.CreateInstance(type) as T;
  }

  private static Type GetFirstTypeWithInterface(IEnumerable<Type> types, string name) =>
    types.FirstOrDefault(type => type.GetInterface(name) is not null);

  public static Assembly LoadAssembly(string path) {
    var loadContext = new PluginLoadContext(path);
    return loadContext.LoadFromAssemblyName(AssemblyName.GetAssemblyName(path));
  }
}

public class PluginLoadContext(string pluginPath) : AssemblyLoadContext {
  private readonly AssemblyDependencyResolver _resolver = new(pluginPath);

  protected override Assembly Load(AssemblyName name) {
    var path = _resolver.ResolveAssemblyToPath(name);
    return path == null ? null : LoadFromAssemblyPath(path);
  }

  protected override IntPtr LoadUnmanagedDll(string name) {
    var path = _resolver.ResolveUnmanagedDllToPath(name);
    return path == null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
  }
}