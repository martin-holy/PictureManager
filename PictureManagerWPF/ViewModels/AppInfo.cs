using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PictureManager.ViewModels {
  public class AppInfo : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged = delegate { };
    public void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged.Invoke(this, new(name));

    private int _progressBarValueA;
    private int _progressBarValueB;
    private int _progressBarMaxA;
    private int _progressBarMaxB;
    private bool _progressBarIsIndeterminate;
    private AppMode _appMode;

    public int ProgressBarValueA { get => _progressBarValueA; set { _progressBarValueA = value; OnPropertyChanged(); } }
    public int ProgressBarValueB { get => _progressBarValueB; set { _progressBarValueB = value; OnPropertyChanged(); } }
    public int ProgressBarMaxA { get => _progressBarMaxA; set { _progressBarMaxA = value; OnPropertyChanged(); } }
    public int ProgressBarMaxB { get => _progressBarMaxB; set { _progressBarMaxB = value; OnPropertyChanged(); } }
    // TODO check that ProgressBar is not leaved in state of IsIndeterminate
    public bool ProgressBarIsIndeterminate { get => _progressBarIsIndeterminate; set { _progressBarIsIndeterminate = value; OnPropertyChanged(); } }

    public AppMode AppMode {
      get => _appMode;
      set {
        _appMode = value;
        OnPropertyChanged(); ;
        App.Ui.StatusPanelVM.OnPropertyChanged(nameof(App.Ui.StatusPanelVM.FilePath));
      }
    }

    public void ResetProgressBars(int max) {
      ResetProgressBarA(max);
      ResetProgressBarB(max);
    }

    public void ResetProgressBarA(int max) {
      ProgressBarValueA = 0;
      ProgressBarMaxA = max;
    }

    public void ResetProgressBarB(int max) {
      ProgressBarValueB = 0;
      ProgressBarMaxB = max;
    }
  }
}
