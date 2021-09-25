using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace PictureManagerUno.Skia.Tizen {
  class Program {
    static void Main(string[] args) {
      var host = new TizenHost(() => new PictureManagerUno.App(), args);
      host.Run();
    }
  }
}
