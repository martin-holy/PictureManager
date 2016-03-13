using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using PictureManager.Dialogs;

namespace PictureManager {
  /// <summary>
  /// Interaction logic for WFilterBuilder.xaml
  /// </summary>
  public partial class WFilterBuilder : INotifyPropertyChanged {
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = "") {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public ViewModel.Filter FiltersTreeItem { get; set; }
    public string FilterTitle { get { return FiltersTreeItem.Title; } set { FiltersTreeItem.Title = value; OnPropertyChanged(); } }
    public ContextMenu MnuFilterGroupOps;
    public ContextMenu MnuFilterConditionProperties;
    public ContextMenu MnuFilterConditionOps;

    public WFilterBuilder(ViewModel.Filter filtersTreeItem) {
      FiltersTreeItem = filtersTreeItem;
      InitializeComponent();

      BuildMnuFilterGroupOps();
      BuildMnuFilterConditionProperties();
      BuildMnuFilterConditionOps();
    }

    private void BuildMnuFilterGroupOps() {
      MnuFilterGroupOps = new ContextMenu();
      
      //Operators
      foreach (var v in Enum.GetValues(typeof(FilterGroupOps))) {
        var menuItem = new MenuItem { Header = Enum.GetName(typeof(FilterGroupOps), v), Tag = v };
        menuItem.Click += delegate (object sender, RoutedEventArgs args) {
          var mi = sender as MenuItem;
          var m = mi?.Parent as ContextMenu;
          var fg = m?.Tag as FilterGroup;
          if (fg == null) return;
          fg.Operator = (FilterGroupOps)mi.Tag;
        };
        MnuFilterGroupOps.Items.Add(menuItem);
      }

      MnuFilterGroupOps.Items.Add(new Separator());
      
      //Add Condition
      var miAddCondition = new MenuItem {Header = "Add Condition"};
      miAddCondition.Click += delegate(object sender, RoutedEventArgs args) {
        var fg = ((ContextMenu) ((MenuItem) sender)?.Parent)?.Tag as FilterGroup;
        if (fg == null) return;
        fg.AddCondition();
      };
      MnuFilterGroupOps.Items.Add(miAddCondition);
      
      //Add Group
      var miAddGroup = new MenuItem { Header = "Add Group" };
      miAddGroup.Click += delegate (object sender, RoutedEventArgs args) {
        var fg = ((ContextMenu)((MenuItem)sender)?.Parent)?.Tag as FilterGroup;
        if (fg == null) return;
        fg.AddGroup();
      };
      MnuFilterGroupOps.Items.Add(miAddGroup);

      MnuFilterGroupOps.Items.Add(new Separator());

      //Remove Group
      var miRemoveGroup = new MenuItem { Header = "Remove Group" };
      miRemoveGroup.Click += delegate (object sender, RoutedEventArgs args) {
        var fg = ((ContextMenu)((MenuItem)sender)?.Parent)?.Tag as FilterGroup;
        if (fg == null) return;
        fg.RemoveGroup();
      };
      MnuFilterGroupOps.Items.Add(miRemoveGroup);
    }

    private void BuildMnuFilterConditionProperties() {
      MnuFilterConditionProperties = new ContextMenu();

      //Properties
      foreach (var v in Enum.GetValues(typeof(FilterConditionProperties))) {
        var menuItem = new MenuItem { Header = Enum.GetName(typeof(FilterConditionProperties), v), Tag = v };
        menuItem.Click += delegate (object sender, RoutedEventArgs args) {
          var mi = sender as MenuItem;
          var m = mi?.Parent as ContextMenu;
          var fc = m?.Tag as FilterCondition;
          if (fc == null) return;
          fc.Property = (FilterConditionProperties)mi.Tag;
        };
        MnuFilterConditionProperties.Items.Add(menuItem);
      }
    }

    private void BuildMnuFilterConditionOps() {
      MnuFilterConditionOps = new ContextMenu();

      //Operators
      foreach (var v in Enum.GetValues(typeof(FilterConditionOps))) {
        var menuItem = new MenuItem { Header = Enum.GetName(typeof(FilterConditionOps), v), Tag = v };
        menuItem.Click += delegate (object sender, RoutedEventArgs args) {
          var mi = sender as MenuItem;
          var m = mi?.Parent as ContextMenu;
          var fc = m?.Tag as FilterCondition;
          if (fc == null) return;
          fc.Operator = (FilterConditionOps)mi.Tag;
        };
        MnuFilterConditionOps.Items.Add(menuItem);
      }
    }

    private void BtnOk_OnClick(object sender, RoutedEventArgs e) {
      DialogResult = true;
    }

    private void WFilterBuilder_OnLoaded(object sender, RoutedEventArgs e) {
      ExpandAll(FiltersTreeItem.FilterData);
      TvFilter.ItemsSource = FiltersTreeItem.FilterData;
    }

    private void FilterGroup_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      var x = sender as TextBlock;
      if (x == null) return;
      MnuFilterGroupOps.Tag = x.DataContext;
      x.ContextMenu = MnuFilterGroupOps;
      x.ContextMenu.IsOpen = true;
    }

    private void FilterConditionProperty_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      var x = sender as TextBlock;
      if (x == null) return;
      MnuFilterConditionProperties.Tag = x.DataContext;
      x.ContextMenu = MnuFilterConditionProperties;
      x.ContextMenu.IsOpen = true;
    }

    private void FilterConditionOperator_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      var x = sender as TextBlock;
      if (x == null) return;
      MnuFilterConditionOps.Tag = x.DataContext;
      x.ContextMenu = MnuFilterConditionOps;
      x.ContextMenu.IsOpen = true;
    }

    private void FilterConditionValue_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
      var x = sender as TextBlock;
      if (x == null) return;
      var fc = (FilterCondition) x.DataContext;
      InputDialog inputDialog = new InputDialog {
        Owner = this,
        IconName = "appbar_filter",
        Title = "Filter condition value",
        Question = "Enter filter condition value.",
        Answer = fc.Value
      };

      inputDialog.BtnDialogOk.Click += delegate {
        inputDialog.DialogResult = true;
      };

      inputDialog.TxtAnswer.SelectAll();

      if (inputDialog.ShowDialog() ?? true) {
        fc.Value = inputDialog.Answer;
      }
    }

    private void TvFilter_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
      var item = TvFilter.SelectedItem as BaseFilterItem;
      if (item == null) return;
      item.IsSelected = false;
    }

    private void ExpandAll(ObservableCollection<BaseFilterItem> items) {
      foreach (var item in items) {
        var group = item as FilterGroup;
        if (group == null) continue;
        group.IsExpanded = true;
        ExpandAll(group.Items);
      }
    }
  }

  [Serializable]
  public enum FilterGroupOps {
    And = 0,
    Or = 1
  }

  [Serializable]
  public enum FilterConditionOps {
    Equals = 0,
    DoesNotEqual = 1,
    IsGreaterThan = 2,
    IsGreaterThanOrEqualTo = 3,
    IsLessThan = 4,
    IsLessThanOrEqualTo = 5,

    Contains = 6,
    DoesNotContain = 7,
    StartsWith = 8,
    DoesNotStartWith = 9,
    EndsWith = 10,
    DoesNotEndWith = 11,
    IsLike = 12,
    IsNotLike = 13,

    IsBlank = 14,
    IsNotBlank = 15
  }

  [Serializable]
  public enum FilterConditionProperties {
    Rating = 0,
    Person = 1,
    Keyword = 2,
    FilePath = 3,
    FileName = 4,
    Comment = 5
  }

  [Serializable]
  public class BaseFilterItem : INotifyPropertyChanged {
    [field:NonSerialized]
    public event PropertyChangedEventHandler PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string name = "") {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public FilterGroup Parent;

    [NonSerialized]
    private bool _isSelected;
    public bool IsSelected { get { return _isSelected; } set { _isSelected = value; OnPropertyChanged(); } }
  }

  [Serializable]
  public class FilterGroup : BaseFilterItem {
    public ObservableCollection<BaseFilterItem> Items { get; set; }

    [NonSerialized]
    private bool _isExpanded;
    public bool IsExpanded { get { return _isExpanded; } set { _isExpanded = value; OnPropertyChanged(); } }

    private FilterGroupOps _operator;
    public FilterGroupOps Operator { get { return _operator; } set { _operator = value; OnPropertyChanged(); } }

    public FilterGroup() {
      Items = new ObservableCollection<BaseFilterItem>();
    }

    public void AddCondition() {
      Items.Add(new FilterCondition {
        Property = FilterConditionProperties.Rating,
        Operator = FilterConditionOps.Equals,
        Value = "<enter a value>",
        Parent = this
      });
      IsExpanded = true;
    }

    public void AddGroup() {
      Items.Add(new FilterGroup {Operator = FilterGroupOps.And, Parent = this});
      IsExpanded = true;
    }

    public void RemoveGroup() {
      if (Parent == null)
        Items.Clear();
      else 
        Parent.Items.Remove(this);
    }
  }

  [Serializable]
  public class FilterCondition : BaseFilterItem {
    private FilterConditionProperties _property;
    public FilterConditionProperties Property { get { return _property; } set { _property = value; OnPropertyChanged(); } }

    private FilterConditionOps _operator;
    public FilterConditionOps Operator { get { return _operator; } set { _operator = value; OnPropertyChanged(); } }

    private string _value;
    public string Value { get { return _value; } set { _value = value; OnPropertyChanged(); } }
  }

  public class FilterGroupOpsConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      return $"[{(FilterGroupOps) value}]";
      /*FilterGroupOps filterGroupOp = (FilterGroupOps) value;
      switch (filterGroupOp) {
        case FilterGroupOps.And: return "And";
        case FilterGroupOps.Or: return "Or";
        case FilterGroupOps.NotAnd: return "Not And";
        case FilterGroupOps.NotOr: return "Not Or";
        default: return string.Empty;
      }*/
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      throw new NotSupportedException();
    }
  }

  public class EnumConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
      return $"[{value}]";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
      throw new NotSupportedException();
    }
  }
}
