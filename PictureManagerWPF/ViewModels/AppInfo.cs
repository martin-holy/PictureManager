using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PictureManager.ViewModels {
  public class AppInfo : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string name = null) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private int _progressBarValueA;
    private int _progressBarValueB;
    private int _progressBarMaxA;
    private int _progressBarMaxB;
    private bool _progressBarIsIndeterminate;
    private AppMode _appMode;
    private bool _isThumbInfoVisible = true;

    public int ProgressBarValueA { get => _progressBarValueA; set { _progressBarValueA = value; OnPropertyChanged(); } }
    public int ProgressBarValueB { get => _progressBarValueB; set { _progressBarValueB = value; OnPropertyChanged(); } }
    public int ProgressBarMaxA { get => _progressBarMaxA; set { _progressBarMaxA = value; OnPropertyChanged(); } }
    public int ProgressBarMaxB { get => _progressBarMaxB; set { _progressBarMaxB = value; OnPropertyChanged(); } }
    public bool ProgressBarIsIndeterminate { get => _progressBarIsIndeterminate; set { _progressBarIsIndeterminate = value; OnPropertyChanged(); } }
    public bool IsThumbInfoVisible { get => _isThumbInfoVisible; set { _isThumbInfoVisible = value; OnPropertyChanged(); } }

    public string FilterAndCount => GetActiveFilterCountFor(DisplayFilter.And);
    public string FilterOrCount => GetActiveFilterCountFor(DisplayFilter.Or);
    public string FilterNotCount => GetActiveFilterCountFor(DisplayFilter.Not);

    public AppMode AppMode {
      get => _appMode;
      set {
        _appMode = value;
        OnPropertyChanged();
        App.WMain.StatusPanel.OnPropertyChanged(nameof(App.WMain.StatusPanel.FilePath));
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

    private static string GetActiveFilterCountFor(DisplayFilter displayFilter) {
      var count = App.Ui.ActiveFilterItems.Count(x => x.DisplayFilter == displayFilter);
      return count == 0 ? string.Empty : count.ToString();
    }
  }
}
