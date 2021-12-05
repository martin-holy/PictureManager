using System;
using System.Windows;
using MH.UI.WPF.BaseClasses;
using MH.UI.WPF.Converters;
using PictureManager.Domain.Models;

namespace PictureManager.Views {
  public partial class SegmentV {

    public static readonly DependencyProperty SegmentProperty =
      DependencyProperty.Register(nameof(Segment), typeof(SegmentM), typeof(SegmentV));

    public static readonly DependencyProperty IsCheckMarkVisibleProperty =
      DependencyProperty.Register(nameof(IsCheckMarkVisible), typeof(bool), typeof(SegmentV), new(false));

    public SegmentM Segment {
      get => (SegmentM)GetValue(SegmentProperty);
      set => SetValue(SegmentProperty, value);
    }

    public bool IsCheckMarkVisible {
      get => (bool)GetValue(IsCheckMarkVisibleProperty);
      set => SetValue(IsCheckMarkVisibleProperty, value);
    }

    public EventHandler<ClickEventArgs> SelectedEventHandler { get; set; } = delegate { };
    public RelayCommand<ClickEventArgs> SelectCommand { get; }

    public SegmentV() {
      InitializeComponent();

      SelectCommand = new(e => SelectedEventHandler.Invoke(this, e));
    }
  }
}
