using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace MH.Utils;

public static class Plugin {
  public static T LoadPlugin<T>(string path) {
    var asm = Assembly.LoadFrom(path);
    var targetType = typeof(T);
    var pluginType = asm.GetTypes().FirstOrDefault(targetType.IsAssignableFrom);

    return pluginType == null ? default : (T)Activator.CreateInstance(pluginType);
  }

  // TODO I can't get this to work
  public static Assembly LoadAssembly(string path) =>
    new PluginLoadContext(path).LoadFromAssemblyName(AssemblyName.GetAssemblyName(path));
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