using MH.Utils.BaseClasses;
using MH.Utils.Extensions;
using PictureManager.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PictureManager {
  public class Tests {

    public static void Run() {
      //var wtest = new WTest();
      //wtest.Show();
      //App.Db.SetModified<Viewers>();
      //TestTryParseDoubleUniversal();

      //var x = PictureManager.Domain.Core.DialogHostShow(new MessageDialog("test", "test message která je delší a delší a delší", Res.IconPeople, true));
      //var x = PictureManager.Domain.Core.DialogHostShow(new MH.Utils.Dialogs.InputDialog("Test title", "Test message bla bla bla la la la", Res.IconEye, "bla bla", (s) => { return "Error message"; }));
      //App.Core.FoldersM.DataAdapter.Save();

      //App.Core.VideoClipsM.DataAdapter.Save();
    }


    private static void TestTryParseDoubleUniversal() {
      "1 234.56 kč".TryParseDoubleUniversal(out var a);
      "1,234.56 kč".TryParseDoubleUniversal(out var b);
      "$1 234,56".TryParseDoubleUniversal(out var c);
      "$1.234,56".TryParseDoubleUniversal(out var d);
      "-1 234.56 kč".TryParseDoubleUniversal(out var e);
      "-1,234.56 kč".TryParseDoubleUniversal(out var f);
      "$-1 234,56".TryParseDoubleUniversal(out var g);
      "$-1.234,56".TryParseDoubleUniversal(out var h);

      var list = new List<double>() { a, b, c, d, e, f, g, h };
    }

    private static void ChangeDate() {
      var progress = new MH.Utils.Dialogs.ProgressBarDialog("Change date", Res.IconCalendar, true, 1);
      progress.AddEvents(
        Directory.GetFiles(@"d:\fotos", "*.jpg", SearchOption.AllDirectories),
        null,
        delegate (string path) {
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
      progress.Start();
      Dialog.Show(progress);
    }

    public static void CommentChars() {
      var text = "Nějaký text @&#_+-$():;!?=% \" |/";
      var commentAllowedChars = new HashSet<char>("@#$€_&+-()*':;!?=<>% ");
      var comment = new string(text.Where(x => char.IsLetterOrDigit(x) || commentAllowedChars.Contains(x)).ToArray());
    }
  }
}
