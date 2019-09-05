using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using PictureManager.Dialogs;
using PictureManager.ViewModel;

namespace PictureManager {
  public class Tests {
    public AppCore ACore;

    public Tests(AppCore aCore) {
      ACore = aCore;

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

      foreach (var mi in App.Core.MediaItems.Items) {
        mi.ReloadThumbnail();
      }


      //ErrorDialog.Show(new ArgumentNullException("message 1", new AggregateException()), "Test");
    }

    public void LogTest() {
      App.Core.Log.Clear();
      App.Core.Log.Add(new LogItem("1 App.Core.MediaItems.All.Where(x => x.IsSelected).ToArray()", "1 foreach (var mi in aCore.MediaItems.All\n.Where(x => x.MediaType == \nMediaType.Image && x.Width == 0))"));
      App.Core.Log.Add(new LogItem("2 App.Core.MediaItems.All.Where(x => x.IsSelected).ToArray()", "2 foreach (var mi in aCore.MediaItems.All.Where(x => x.MediaType == MediaType.Image && x.Width == 0))"));
      App.Core.Log.Add(new LogItem("3 App.Core.MediaItems.All.Where(x => x.IsSelected).ToArray()", "3 foreach (var mi in aCore.MediaItems.All.Where(x => x.MediaType == MediaType.Image && x.Width == 0))"));
      App.Core.Log.Add(new LogItem("4 App.Core.MediaItems.All.Where(x => x.IsSelected).ToArray()", "4 foreach (var mi in aCore.MediaItems.All.Where(x => x.MediaType == MediaType.Image && x.Width == 0))"));
      App.Core.Log.Add(new LogItem("5 App.Core.MediaItems.All.Where(x => x.IsSelected).ToArray()", "5 foreach (var mi in aCore.MediaItems.All.Where(x => x.MediaType == MediaType.Image && x.Width == 0))"));
      App.Core.Log.Add(new LogItem("6 App.Core.MediaItems.All.Where(x => x.IsSelected).ToArray()", "6 foreach (var mi in aCore.MediaItems.All.Where(x => x.MediaType == MediaType.Image && x.Width == 0))"));

      var dlg = new LogDialog();
      dlg.ShowDialog();
       
    }

    public string Text2Path(string text, bool flipVertically = false, bool flipHorizontally = false) {
      var formattedText = new FormattedText(text,
        CultureInfo.GetCultureInfo("en-us"),
        FlowDirection.LeftToRight,
        new Typeface(
          new FontFamily("Segoe UI Symbol"),
          FontStyles.Normal,
          FontWeights.Bold,
          FontStretches.Normal),
        16, Brushes.Black);

      var geometry = formattedText.BuildGeometry(new Point(0, 0));
      var gb = geometry.Bounds;

      if (flipVertically)
        geometry.Transform = new ScaleTransform(1, -1, 0, (gb.Bottom - gb.Top) / 2.0);
      if (flipHorizontally)
        geometry.Transform = new ScaleTransform(-1, 1, 0, (gb.Right - gb.Left) / 2.0);

      var data = geometry.GetFlattenedPathGeometry().ToString().Replace(",", ".").Replace(";", ",");

      return $"<Path Width=\"{(int) gb.Width}\" Height=\"{(int) gb.Height}\" Canvas.Left=\"{(int) gb.Left}\" Canvas.Top=\"{(int) gb.Top}\" Stretch=\"Fill\" Fill=\"{{DynamicResource BlackBrush}}\" Data=\"{data}\" />";
    }

    public void CommentChars() {
      var text = "Nějaký text @&#_+-$():;!?=% \" |/";
      var commentAllowedChars = new HashSet<char>("@#$€_&+-()*':;!?=<>% ");
      var comment = new string(text.Where(x => char.IsLetterOrDigit(x) || commentAllowedChars.Contains(x)).ToArray());

    }

    public void GetByPathTest() {
      var f = ACore.Folders.GetByPath(@"D:\!test2");
      var f2 = f.GetByPath(@"D:\!test2\2019");
    }

    public void OldTests() {

      /*ACore.FileOperationDelete(new List<string> {@"d:\!test2\deltest1"}, true, true);*/
      //Directory.Delete(@"d:\!test2\deltest2", true);

      //File.Move(@"d:\!test2\20140206_175827_Martin.jpg", @"d:\!test2\aaa\20140206_175827_Martin.jpg");
      //AppCore.MoveDirectory(@"d:\!test2\2019", @"d:\!test2\newfolder\2019");

      //AppCore.MoveDirectory(@"d:\!test2\2019", @"d:\!test2\newF\blabla\bleble\2019");

      //File.Move(@"d:\!test2\deltest1\20140206_175827_Martin.jpg", @"d:\!test2\deltest1\20140206_175827_Martin.jpg");

      /*var paths = new List<string>();
      for (var i = 0; i < 20; i++) {
        paths.Add($"d:\\!test2\\RecycleBinTest\\file{i}.txt");
      }
      foreach (var path in paths) {
        using (var file = new StreamWriter(path)) {
          file.WriteLine("Delete to Recycle Bin test");
        }
      }

      ACore.FileOperationDelete(paths, true, true);*/

      //var focd = new FileOperationCollisionDialog(@"d:\!test2\20150410_220526_Martin.jpg", @"d:\!test2\aaa\20150410_220526_Martin.jpg") {Owner = this};
      //focd.ShowDialog();

      var acore = ACore;

#pragma warning disable CS0219 // The variable 'mkv' is assigned but its value is never used
      var mkv = @"d:\!test2\vid\20190324_145306_Kos a veverka_lq.mkv";
#pragma warning restore CS0219 // The variable 'mkv' is assigned but its value is never used
#pragma warning disable CS0219 // The variable 'mp4' is assigned but its value is never used
      var mp4 = @"d:\!test2\vid\20190715_105711.mp4";
#pragma warning restore CS0219 // The variable 'mp4' is assigned but its value is never used

      //var fileInfoMkv = ShellStuff.FileInformation.GetFileIdInfo(mkv);
      //var fileInfoMp4 = ShellStuff.FileInformation.GetFileIdInfo(mp4);

      //var metadataMkv = ShellStuff.FileInformation.GetVideoMetadata(mkv);
      //var metadataMp4 = ShellStuff.FileInformation.GetVideoMetadata(mp4);



      //var getAllMp4 = ShellStuff.FileInformation.GetAllVideoMetadata(mp4);
      //var getAllMkv = ShellStuff.FileInformation.GetAllVideoMetadata(mkv);

      Console.WriteLine("bla");





      //var file1 = ShellStuff.FileInformation.GetFileIdInfo(@"d:\video.mp4");
      //var x = GetFileProps(@"d:\video.mp4");
      //var xx = ShellStuff.FileInformation.GetVideoMetadata(@"d:\video.mp4");

      /*var file1 = ShellStuff.FileInformation.GetFileIdInfo(@"c:\20150831_114319_Martin.jpg");
      var file2 = ShellStuff.FileInformation.GetFileIdInfo(@"d:\!test\20150831_114319_Martin.jpg");
      var file3 = ShellStuff.FileInformation.GetFileIdInfo(@"d:\Temp\20150831_114319_Martin.jpg");
      //3659174697441353
      var filePath = @"d:\!test\20150831_114319_Martin.jpg";
      var fileInfo = new FileInfo(filePath);*/

      /*var formattedText = new FormattedText(
        "\U0001F4CF",
        CultureInfo.GetCultureInfo("en-us"),
        FlowDirection.LeftToRight,
        new Typeface("Segoe UI Symbol"),
        32,
        Brushes.Black);
      var buildGeometry = formattedText.BuildGeometry(new Point(0, 0));
      var p = buildGeometry.GetFlattenedPathGeometry();*/



    }
  }
}
