using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PictureManager {
  public class Tests {
    public AppCore ACore;

    public Tests(AppCore aCore) {
      ACore = aCore;

      //TestListVsArray();
      GetByPathTest();


    }

    public void GetByPathTest() {
      var f = ACore.Folders.GetByPath(@"D:\!test2");
      var f2 = f.GetByPath(@"D:\!test2\2019");
    }

    public void TestListVsArray() {
      var data = new List<int>(1000000);
      for (var i = 0; i < 1000000; i++) {
        data.Add(i);
      }

      var idx = 0;
      foreach (var i in data.ToArray()) {
        idx = i;
      }

      idx = 0;
      foreach (var i in data.ToList()) {
        idx = i;
      }
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

      var mkv = @"d:\!test2\vid\20190324_145306_Kos a veverka_lq.mkv";
      var mp4 = @"d:\!test2\vid\20190715_105711.mp4";

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
