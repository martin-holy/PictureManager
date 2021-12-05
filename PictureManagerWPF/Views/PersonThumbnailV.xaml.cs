using System;
using System.Windows;
using MH.UI.WPF.Converters;
using PictureManager.ViewModels;

namespace PictureManager.Views {
  public partial class PersonThumbnailV {
    public static readonly DependencyProperty PersonProperty = 
      DependencyProperty.Register(nameof(Person), typeof(PersonBaseVM), typeof(PersonThumbnailV));

    public PersonBaseVM Person {
      get => (PersonBaseVM)GetValue(PersonProperty);
      set => SetValue(PersonProperty, value);
    }

    public EventHandler<ClickEventArgs> SelectedEventHandler { get; set; } = delegate { };

    public PersonThumbnailV() {
      InitializeComponent();
    }

    public void OnSelected(object o, ClickEventArgs e) {
      e.DataContext = this;
      SelectedEventHandler.Invoke(this, e);
    }
  }
}
