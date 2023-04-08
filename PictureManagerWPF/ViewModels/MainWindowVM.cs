using MH.Utils.BaseClasses;
using PictureManager.Domain;
using PictureManager.Domain.Dialogs;
using PictureManager.Domain.Models;
using System.Windows;

namespace PictureManager.ViewModels {
  public sealed class MainWindowVM : ObservableObject {
    public Core CoreM { get; }
    public AppCore CoreVM { get; }
    public MainWindowM MainWindowM { get; }

    public RelayCommand<object> TestButtonCommand { get; }
    public RelayCommand<object> LoadedCommand { get; }

    public MainWindowVM(Core coreM, AppCore coreVM, MainWindowM model) {
      CoreM = coreM;
      CoreVM = coreVM;
      MainWindowM = model;

      TestButtonCommand = new(() => Tests.Run());
      LoadedCommand = new(Loaded);
    }

    private void Loaded() {
      var windowsDisplayScale = 1.0;

      if (Application.Current.MainWindow != null)
        windowsDisplayScale = PresentationSource.FromVisual(Application.Current.MainWindow)
        ?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;

      MainWindowM.Loaded(windowsDisplayScale);
    }
  }
}
