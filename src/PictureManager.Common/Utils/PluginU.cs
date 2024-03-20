using MH.Utils;
using PictureManager.Plugins.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace PictureManager.Common.Utils;

public static class PluginU {
  public static IPluginCore GetPluginCore(string pluginName) {
    try {
      var pluginPath = Path.GetFullPath(Path.Combine("plugins", pluginName, $"{pluginName}.Common.dll"));
      var asm = LoadAssembly(pluginPath);
      if (GetFirstTypeWithInterface(asm.GetTypes(), nameof(IPluginCore)) is not { } type) return null;
      return Activator.CreateInstance(type) as IPluginCore;
    }
    catch (Exception ex) {
      Log.Error(ex);
      return null;
    }
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