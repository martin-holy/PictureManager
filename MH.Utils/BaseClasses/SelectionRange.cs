namespace MH.Utils.BaseClasses {
  public class SelectionRange : ObservableObject {
    private double _min;
    private double _max;
    private double _start;
    private double _end;

    public double Min { get => _min; set { _min = value; OnPropertyChanged(); } }
    public double Max { get => _max; set { _max = value; OnPropertyChanged(); } }
    public double Start { get => _start; set { _start = value; OnPropertyChanged(); } }
    public double End { get => _end; set { _end = value; OnPropertyChanged(); } }

    public SelectionRange() { }

    public void CoerceValues(bool maxSelection) {
      if (maxSelection) {
        Start = Min;
        End = Max;

        return;
      }

      if (Start < Min) Start = Min;
      if (Start > Max) Start = Max;
      if (End < Min) End = Min;
      if (End > Max) End = Max;
      if (End == 0) End = Max;
    }

    public bool Fits(double value) =>
      value >= Start && value <= End;

    public bool MaxSelection() =>
      Min == Start && End == Max;

    public void Zero() {
      Min = 0;
      Max = 0;
      Start = 0;
      End = 0;
    }
  }
}
