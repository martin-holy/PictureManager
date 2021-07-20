using PictureManager.Dialogs;
using PictureManager.Domain.Models;
using PictureManager.Domain.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace PictureManager {
  public class Tests {

    public static void Run() {
      //var wtest = new WTest();
      //wtest.Show();
    }

    

    private static void ChangeDate() {
      var progress = new ProgressBarDialog(App.WMain, true, 1, "Change date");
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
      progress.StartDialog();
    }

    public static void CommentChars() {
      var text = "Nějaký text @&#_+-$():;!?=% \" |/";
      var commentAllowedChars = new HashSet<char>("@#$€_&+-()*':;!?=<>% ");
      var comment = new string(text.Where(x => char.IsLetterOrDigit(x) || commentAllowedChars.Contains(x)).ToArray());
    }
  }
}
