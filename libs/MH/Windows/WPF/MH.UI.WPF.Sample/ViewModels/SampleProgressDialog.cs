using System.Threading;
using System.Threading.Tasks;
using MH.UI.Dialogs;

namespace MH.UI.WPF.Sample.ViewModels;

public sealed class SampleProgressDialog: ProgressDialog<string> {
  public SampleProgressDialog(string[] items) : base("Sample Progress Dialog", Res.IconImage, items) {
    AutoRun();
  }

  protected override Task Do(string item, CancellationToken token) {
    ReportProgress(item);
    return Task.Delay(1000, token);
  }
}