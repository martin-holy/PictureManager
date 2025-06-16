using Android.Widget;
using MH.UI.MAUI.Controls;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using PictureManager.Common.Layout;
using PictureManager.MAUI.Droid.Views;

namespace PictureManager.MAUI.Droid.Handlers;

public class MyShellHandler : ViewHandler<MyShell, FrameLayout> {
  public static IPropertyMapper<MyShell, MyShellHandler> PropertyMapper =
    new PropertyMapper<MyShell, MyShellHandler>(ViewMapper);

  public MyShellHandler() : base(PropertyMapper) { }

  protected override FrameLayout CreatePlatformView() => new(Context);

  protected override void ConnectHandler(FrameLayout platformView) {
    base.ConnectHandler(platformView);

    if (VirtualView.BindingContext is MainWindowVM viewModel)
      platformView.AddView(new MainWindowV(Context) { ViewModel = viewModel });
  }

  protected override void DisconnectHandler(FrameLayout platformView) {
    platformView.RemoveAllViews();
    base.DisconnectHandler(platformView);
  }
}