using MH.UI.Controls;

namespace MH.UI.WPF.Sample.ViewModels.Layout;

public sealed class MiddleContentSlotVM;

public sealed class MiddleContentVM() : TabControl(new(Dock.Left, Dock.Top, new MiddleContentSlotVM()) { RotationAngle = 270 });