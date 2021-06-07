using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PictureManager.Dialogs;
using PictureManager.Domain;
using PictureManager.Domain.Models;
using PictureManager.Properties;
using PictureManager.Utils;
using PictureManager.Models;
using PictureManager.ViewModels;

namespace PictureManager {
  public class Tests {

    public void Run() {
      var wtest = new WTest();
      wtest.Show();
      //GetByPathTest();
      /*foreach (var mi in aCore.MediaItems.All.Where(x => x.MediaType == MediaType.Image && x.Width == 0)) {
        mi.ReadMetadata();
      }
      aCore.Sdb.SaveAllTables();*/
      //var count = aCore.MediaItems.All.Where(x => x.InfoBoxThumb.Count > 0);

      //var result = Dialogs.MessageDialog.Show("Test title", "Test Message", false);
      //var result2 = Dialogs.MessageDialog.Show("Test title", "Test Message", true);
      //CommentChars();
      //var x = $"appbar{Regex.Replace(IconName.DriveError.ToString(), @"([A-Z])", "_$1").ToLower()}";

      /*
      var test = Text2Path("⤸");
      var test2 = Text2Path("⤺");
      var test3 = Text2Path("⤺", false, true);*/

      /*foreach (var mi in App.Ui.MediaItems.Items) {
        mi.ReloadThumbnail();
      }*/


      //ErrorDialog.Show(new ArgumentNullException("message 1", new AggregateException()), "Test");
      //BackdoorManipulations();
      //ResizeTest();
      //ProgressBarTest();

      //ChangeDate();

      //TestThumbnails();
      //ResizeToPhoneAndWeb();
      //CreateThumbnailAsyncTest();
      //TestFolderBrowserDialog();
     //RemoveKeywordsFromAutoAddedCategory();
    }

    private void RemoveKeywordsFromAutoAddedCategory() {
      var keywords = App.Core.CategoryGroups.All.Cast<CategoryGroup>().Single(x => x.Title.Equals("Auto Added")).Items.Cast<Keyword>().ToArray();
      foreach (var keyword in keywords) {
        App.Core.Keywords.ItemDelete(keyword);
      }
      App.Db.SaveAllTables();
    }

    private void TestFolderBrowserDialog() {
      var fbd = new FolderBrowserDialog(App.WMain);
      if (!(fbd.ShowDialog() ?? true)) return;
      var path = fbd.SelectedPath;
    }

    private void CreateThumbnailAsyncTest() {
      var folder = App.Core.Folders.GetByPath(@"D:\!test");
      var items = folder.GetMediaItems(true).Where(x => x.MediaType == MediaType.Video).ToArray();
      var index = 0;

      Task.WhenAll(
        from partition in Partitioner.Create(items).GetPartitions(2)
        select Task.Run(async delegate {
          using (partition) {
            while (partition.MoveNext()) {
              Console.WriteLine("loop before");
              var mi = partition.Current;
              await Imaging.CreateThumbnailAsync(mi.FilePath, mi.FilePathCache, Settings.Default.ThumbnailSize, mi.RotationAngle);
              Console.WriteLine("loop after");
            }
          }
        }));

      /*Parallel.ForEach(items, new ParallelOptions {MaxDegreeOfParallelism = 1}, async mi => {
        Console.WriteLine("loop before");
        await App.Ui.CreateThumbnailAsync(mi.FilePath, mi.FilePathCache, Settings.Default.ThumbnailSize);
        Console.WriteLine("loop after");
      });*/

    }

    private void ChangeDate() {
      var progress = new ProgressBarDialog(App.WMain, true, 1, "Change date");
      progress.AddEvents(
        Directory.GetFiles(@"d:\fotos", "*.jpg", SearchOption.AllDirectories),
        null,
        delegate(string path) {
          var fi = new FileInfo(path);
          var match = Regex.Match(fi.Name, "[0-9]{8}_[0-9]{6}");
          if (match.Success && DateTime.TryParseExact(match.Value, "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date)) {
            fi.LastWriteTime = date;
            fi.CreationTime = date;
          }
          else {
            Console.WriteLine($"Date not recognized {path}");
          }
        },
        x => x,
        null);
      progress.StartDialog();
    }

    private void ProgressBarTest() {
      //var folder = App.Ui.Folders.GetByPath(@"D:\Pictures\01 Digital_Foto\-=Hotovo\2018");
      //var items = folder.GetMediaItems(true).Where(x => x.MediaType == MediaType.Image && x.Rating > 2).ToArray();

      var progress = new ProgressBarDialog(App.WMain, true, 1, "Test");
      progress.AddEvents(
        new []{1,2,3,4,5,6,7},
        () => true,
        delegate (int i) { System.Threading.Thread.Sleep(1000); },
        mi => mi.ToString(),
        null);

      progress.StartDialog();
    }

    private void ResizeToPhoneAndWeb() {
      var src = "D:\\Pictures\\01 Digital_Foto\\-=Sklad";
      var destPhone = "D:\\Pictures\\01 Digital_Foto\\-=Sklad\\ToPhone";
      var destWeb = "D:\\Pictures\\01 Digital_Foto\\-=Sklad\\CarryOnTheRoad";

      Directory.CreateDirectory(destPhone);
      Directory.CreateDirectory(destWeb);

      var folder = App.Core.Folders.GetByPath(src);

      //var items = folder.GetMediaItems(true).Where(x => x.MediaType == MediaType.Image && x.Rating > 2).ToArray();
      //App.WMain.MediaItemsResize(items, 2500000, destPhone, true, false);

      var items = folder.GetMediaItems(true).Where(x => x.MediaType == MediaType.Image && x.Rating > 3).ToArray();
      App.WMain.CommandsController.MediaItemsCommands.Resize(items, 1500000, destWeb, false, false);
    }

    private void ResizeTest() {
      //App.WMain.MediaItemsResize(new List<MediaItem> {App.Ui.MediaItems.All.Single(x => x.Id == 1861) }, 1500000, @"D:\", true, false);

      //, 2010, 2011, 2012, 2013, 2014, 2015, 2016, 2017, 2018, 2019

      try {
        /*var year = 2010;
        var folder = App.Ui.Folders.GetByPath($"D:\\Pictures\\01 Digital_Foto\\-=Hotovo\\{year}");
        var items = folder.GetMediaItems(true).Where(x => x.MediaType == MediaType.Image && x.Rating > 2).OrderBy(x => x.FilePath).ToArray();
        App.WMain.MediaItemsResize(items, 2500000, $"D:\\{year}", true, false);*/

        Directory.CreateDirectory(@"D:\000");

        Imaging.ResizeJpg(@"D:\Pictures\01 Digital_Foto\-=Hotovo\2018\2018_01_01+ - Isi & Bettina\20180108_201028_Martin.jpg", @"D:\000\20180108_201028_Martin.jpg", 2500000, true, false);
        //MediaItems.Resize(@"D:\Pictures\01 Digital_Foto\-=Hotovo\2018\2018_01_01+ - Isi & Bettina\20180101_133736_Martin.jpg", @"D:\000\20180101_133736_Martin.jpg", 2500000, true, false);
      }
      catch (Exception ex) {
        App.Ui.LogError(ex);
      }
      
    }

    private void BackdoorManipulations() {
      var items = App.Core.MediaItems.All.Cast<MediaItem>().Where(x => x.MediaType == MediaType.Video && (x.Width == 0 || x.Height == 0));
      var progress = new ProgressBarDialog(App.WMain, true, Environment.ProcessorCount, "Reloading metadata ...");
      progress.AddEvents(
        items.ToArray(),
        null,
        // action
        delegate (MediaItem mi) {
          MediaItemsViewModel.ReadMetadata(mi);
        },
        mi => mi.FilePath,
        // onCompleted
        delegate {
          App.Core.MediaItems.Helper.IsModified = true;
          App.Db.SaveAllTables();
        });

      progress.Start();

      /*var items = App.Ui.MediaItems.All.Where(x => x.MediaType == MediaType.Video).ToList();
      foreach (var mi in items) {
        mi.SetThumbSize();
      }
      App.WMain.MediaItemsRebuildThumbnails(items);*/
    }

    public void LogTest() {
      App.Ui.Log.Clear();
      App.Ui.Log.Add(new LogItem("1 App.Ui.MediaItems.All.Where(x => x.IsSelected).ToArray()", "1 foreach (var mi in aCore.MediaItems.All\n.Where(x => x.MediaType == \nMediaType.Image && x.Width == 0))"));
      App.Ui.Log.Add(new LogItem("2 App.Ui.MediaItems.All.Where(x => x.IsSelected).ToArray()", "2 foreach (var mi in aCore.MediaItems.All.Where(x => x.MediaType == MediaType.Image && x.Width == 0))"));
      App.Ui.Log.Add(new LogItem("3 App.Ui.MediaItems.All.Where(x => x.IsSelected).ToArray()", "3 foreach (var mi in aCore.MediaItems.All.Where(x => x.MediaType == MediaType.Image && x.Width == 0))"));
      App.Ui.Log.Add(new LogItem("4 App.Ui.MediaItems.All.Where(x => x.IsSelected).ToArray()", "4 foreach (var mi in aCore.MediaItems.All.Where(x => x.MediaType == MediaType.Image && x.Width == 0))"));
      App.Ui.Log.Add(new LogItem("5 App.Ui.MediaItems.All.Where(x => x.IsSelected).ToArray()", "5 foreach (var mi in aCore.MediaItems.All.Where(x => x.MediaType == MediaType.Image && x.Width == 0))"));
      App.Ui.Log.Add(new LogItem("6 App.Ui.MediaItems.All.Where(x => x.IsSelected).ToArray()", "6 foreach (var mi in aCore.MediaItems.All.Where(x => x.MediaType == MediaType.Image && x.Width == 0))"));

      var dlg = new LogDialog();
      dlg.ShowDialog();
       
    }

    public void CommentChars() {
      var text = "Nějaký text @&#_+-$():;!?=% \" |/";
      var commentAllowedChars = new HashSet<char>("@#$€_&+-()*':;!?=<>% ");
      var comment = new string(text.Where(x => char.IsLetterOrDigit(x) || commentAllowedChars.Contains(x)).ToArray());

    }

    public void GetByPathTest() {
      var f = App.Core.Folders.GetByPath(@"D:\!test2");
      var f2 = f.GetByPath(@"D:\!test2\2019");
    }

    public void OldTests() {


      var mkv = @"d:\!test2\vid\20190324_145306_Kos a veverka_lq.mkv";
      var mp4 = @"d:\!test2\vid\20190715_105711.mp4";

      //var fileInfoMkv = ShellStuff.FileInformation.GetFileIdInfo(mkv);
      //var fileInfoMp4 = ShellStuff.FileInformation.GetFileIdInfo(mp4);
      //var metadataMkv = ShellStuff.FileInformation.GetVideoMetadata(mkv);
      //var metadataMp4 = ShellStuff.FileInformation.GetVideoMetadata(mp4);
      //var getAllMp4 = ShellStuff.FileInformation.GetAllVideoMetadata(mp4);
      //var getAllMkv = ShellStuff.FileInformation.GetAllVideoMetadata(mkv);

      Console.WriteLine("bla");

    }
  }
}
