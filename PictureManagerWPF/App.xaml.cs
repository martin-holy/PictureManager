using MH.Utils.BaseClasses;
using PictureManager.Domain;
using PictureManager.Domain.Dialogs;
using PictureManager.Views;
using System;
using System.Windows;
using System.Windows.Input;

namespace PictureManager {
  public partial class App {
    public static Core Core => (Core)Current.Properties[nameof(AppProperty.Core)];
    public static AppCore Ui => (AppCore)Current.Properties[nameof(AppProperty.Ui)];
    
    protected override async void OnStartup(StartupEventArgs e) {
      base.OnStartup(e);

      AppDomain.CurrentDomain.UnhandledException += delegate (object _, UnhandledExceptionEventArgs args) {
        Dialog.Show(new ErrorDialogM((Exception)args.ExceptionObject));
      };

      CommandManager.RequerySuggested += RelayCommand.InvokeCanExecuteChanged;

      var splashScreen = new SplashScreenV();
      MainWindow = splashScreen;
      MainWindow.Show();

      await Core.Inst.InitAsync(splashScreen.ProgressMessage);

      Current.Properties[nameof(AppProperty.Core)] = Core.Inst;
      Current.Properties[nameof(AppProperty.Ui)] = new AppCore();

      Core.AfterInit();
      Ui.AfterInit();

      ShutdownMode = ShutdownMode.OnMainWindowClose;
      MainWindow = new MainWindowV();
      MainWindow.Show();

      splashScreen.Close();
    }
  }
}
