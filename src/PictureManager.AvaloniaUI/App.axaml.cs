﻿using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml;
using PictureManager.AvaloniaUI.Views;
using PictureManager.AvaloniaUI.Views.Layout;
using PictureManager.Common;
using System;
using System.Threading.Tasks;
using AUI = MH.UI.AvaloniaUI;

namespace PictureManager.AvaloniaUI;

public partial class App : Application {
  public static Core Core { get; private set; } = null!;
  public static CoreUI CoreUI { get; private set; } = null!;

  public override void Initialize() {
    AvaloniaXamlLoader.Load(this);

    AUI.Utils.Init.LoadDataTemplates(DataTemplates);
    AUI.Utils.Init.UseRelayCommandIconAndText();
    AUI.Utils.Init.ControlsExtensions();
    AUI.Utils.ColorHelper.AddColorsToResources();
    _loadDataTemplates();
  }

  public override void OnFrameworkInitializationCompleted() {
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
      _onDesktopStartup(desktop);
    else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
      _onSingleViewStartup(singleViewPlatform);

    base.OnFrameworkInitializationCompleted();
  }

  private static async void _onDesktopStartup(IClassicDesktopStyleApplicationLifetime desktop) {
    var splashScreen = new SplashScreenV();
    desktop.MainWindow = splashScreen;
    desktop.MainWindow.Show();

    await _initCore(splashScreen.ProgressMessage);

    //ShutdownMode = ShutdownMode.OnMainWindowClose; // TODO PORT
    desktop.MainWindow = new MainWindowV();
    desktop.MainWindow.Show();

    splashScreen.Close();
  }

  private static async void _onSingleViewStartup(ISingleViewApplicationLifetime singleViewPlatform) {
    // TODO PORT progress
    var progress = new Progress<string>();
    await _initCore(progress);
    singleViewPlatform.MainView = new MainView { DataContext = Core.VM.MainWindow };
  }

  private static async Task _initCore(IProgress<string> progress) {
    var drives = Environment.GetLogicalDrives();
    await Core.Inst.InitAsync(progress, AppDomain.CurrentDomain.BaseDirectory);

    Core = Core.Inst;
    CoreUI = new();
    Core.AfterInit(CoreUI);
    CoreUI.AfterInit();
  }

  private void _loadDataTemplates() {
    // include files in *.csproj as AvaloniaResource too
    var files = new[] {
      "Dialogs/AboutDialog",
      "Dialogs/CompressImagesDialog",
      "Dialogs/ErrorDialog",
      "Dialogs/ExportSegmentsDialog",
      "Dialogs/FileOperationCollisionDialog",
      "Dialogs/FileOperationDialog",
      "Dialogs/FolderBrowserDialog",
      "Dialogs/FolderKeywordsDialog",
      "Dialogs/LogDialog",
      "Dialogs/MergePeopleDialog",
      "Dialogs/ImageResizeDialog",
      "Dialogs/RotationDialog",
      "Entities/RatingV",
      "Entities/SegmentRectV",
      "Entities/TreeItemV",
      "Layout/MainTabsV",
      "Layout/MiddleContentV",
      "Layout/StatusBarV",
      "Layout/ToolBarV",
      "Layout/ToolsTabsV",
      "Layout/TreeViewCategoriesV",
      "Sections/MediaItemsViewV",
      "Sections/MediaViewerV",
      "Sections/PeopleToolsTabV",
      "Sections/PeopleV",
      "Sections/PersonDetailV",
      "Sections/SegmentsDrawerV",
      "Sections/SegmentsViewsV",
      "Sections/SettingsV",
      "Sections/TreeViewSearchV",
      "Sections/WhatIsNewV"
    };

    foreach (var file in files) {
      var uri = new Uri($"avares://PictureManager.AvaloniaUI/Views/{file}.axaml");
      if (AvaloniaXamlLoader.Load(uri) is DataTemplates dts)
        DataTemplates.AddRange(dts);
    }
  }
}
