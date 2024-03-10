using System;
using System.Linq;

namespace MH.Utils.Extensions {
  public static class EventHandlerExtensions {
    public static bool IsRegistered(this EventHandler e, object target) =>
      e.GetInvocationList().Any(x => x.Target?.GetType() == target.GetType());
  }
}
