using PictureManager.Domain.Utils;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;

namespace PictureManager.Domain.Models {
  public class Faces : ObservableObject, ITable {
    #region ITable Properties
    public TableHelper Helper { get; set; }
    public List<IRecord> All { get; } = new();
    #endregion

    private int _faceSize = 100;

    public List<Face> Loaded { get; } = new();
    public List<Face> Selected { get; } = new();
    public List<MediaItem> WithoutFaces { get; } = new();
    public int FaceSize { get => _faceSize; set { _faceSize = value; OnPropertyChanged(); } }
    public int FaceBoxExpand { get; set; } = 40;

    #region ITable Methods
    public void LoadFromFile() {
      All.Clear();
      Helper.LoadFromFile();
    }

    public void NewFromCsv(string csv) {
      // ID|MediaItemId|PersonId|FaceBox|AvgHash
      var props = csv.Split('|');
      if (props.Length != 5) throw new ArgumentException("Incorrect number of values.", csv);
      var rect = props[3].Split(',');
      var face = new Face(
        int.Parse(props[0]),
        int.Parse(props[2]),
        new Int32Rect(int.Parse(rect[0]), int.Parse(rect[1]), int.Parse(rect[2]), int.Parse(rect[3])),
        long.Parse(props[4])) { Csv = props };

      All.Add(face);
    }

    public void LinkReferences() {
      // AllDic are destroyed after load
      var mediaItems = Core.Instance.MediaItems.All.Cast<MediaItem>().ToDictionary(x => x.Id);
      var people = Core.Instance.People.All.Cast<Person>().ToDictionary(x => x.Id);
      var withoutMediaItem = new List<Face>();
      
      foreach (var face in All.Cast<Face>()) {
        if (mediaItems.TryGetValue(int.Parse(face.Csv[1]), out var mi)) {
          face.MediaItem = mi;

          if (face.PersonId > 0 && people.TryGetValue(face.PersonId, out var person))
            face.Person = person;
        } else
          withoutMediaItem.Add(face);

        // csv array is not needed any more
        face.Csv = null;
      }

      // in case MediaItem was deleted
      foreach (var face in withoutMediaItem)
        All.Remove(face);

      // Table Properties
      WithoutFaces.Clear();
      if (Helper.TableProps == null) return;
      if (Helper.TableProps.TryGetValue(nameof(WithoutFaces), out var withoutFaces))
        foreach (var miId in withoutFaces.Split(','))
          if (mediaItems.TryGetValue(int.Parse(miId), out var mi))
            WithoutFaces.Add(mi);
      if (Helper.TableProps.TryGetValue(nameof(FaceSize), out var faceSize))
        FaceSize = int.Parse(faceSize);
      if (Helper.TableProps.TryGetValue(nameof(FaceBoxExpand), out var faceBoxExpand))
        FaceBoxExpand = int.Parse(faceBoxExpand);

      // table props are not needed any more
      Helper.TableProps.Clear();
      Helper.TableProps = null;
    }

    public void TablePropsToCsv() {
      if (WithoutFaces.Count == 0) return;
      Helper.TableProps = new();
      Helper.TableProps.Add(nameof(WithoutFaces), string.Join(",", WithoutFaces.Select(x => x.Id)));
      Helper.TableProps.Add(nameof(FaceSize), FaceSize.ToString());
    }
    #endregion

    public void Select(bool isCtrlOn, bool isShiftOn, Face face) {
      // single select
      if (!isCtrlOn && !isShiftOn) {
        DeselectAll();
        SetSelected(face, true);
        return;
      }

      // single invert select
      if (isCtrlOn) {
        SetSelected(face, !face.IsSelected);
        return;
      }

      // multi select
      if (isShiftOn) {
        var indexOfFace = Loaded.IndexOf(face);
        var fromFace = Loaded.FirstOrDefault(x => x.IsSelected);
        var from = fromFace == null ? 0 : Loaded.IndexOf(fromFace);
        var to = indexOfFace;

        if (from > to) {
          to = from;
          from = indexOfFace;
        }

        for (var i = from; i < to + 1; i++)
          SetSelected(Loaded[i], true);
      }
    }

    public void DeselectAll() {
      foreach (var mi in Selected.ToArray())
        SetSelected(mi, false);
    }

    public void SetSelected(Face face, bool value) {
      if (face.IsSelected == value) return;
      face.IsSelected = value;
      if (value) Selected.Add(face);
      else Selected.Remove(face);
    }

    public void SortLoaded() {
      var sorted = Imaging.GetSimilarImages(Loaded.ToDictionary(k => (object)k, v => v.AvgHash), -1);
      DeselectAll();
      Loaded.Clear();
      Loaded.AddRange(sorted.Cast<Face>());
    }

    public async IAsyncEnumerable<Face> GetFacesAsync(List<MediaItem> mediaItems, IProgress<int> progress, [EnumeratorCancellation] CancellationToken token = default) {
      DeselectAll();
      Loaded.Clear();

      var detectedFaces = All.Cast<Face>().Where(x => mediaItems.Contains(x.MediaItem)).ToArray();
      var notRecognizedFaces = detectedFaces.Where(x => x.PersonId == 0);
      var mediaItemsToDetect = mediaItems.Except(detectedFaces.Select(x => x.MediaItem).Distinct()).Except(WithoutFaces);
      var done = mediaItems.Count - mediaItemsToDetect.Count();

      progress.Report(Convert.ToInt32((double)done / mediaItems.Count * 100));

      foreach (var face in notRecognizedFaces) {
        if (token.IsCancellationRequested) yield break;
        
        face.SetPicture(FaceSize);
        Loaded.Add(face);

        yield return face;
      }

      foreach (var mi in mediaItemsToDetect) {
        if (token.IsCancellationRequested) yield break;

        progress.Report(Convert.ToInt32((double)++done / mediaItems.Count * 100));
        var filePath = mi.MediaType == MediaType.Image ? mi.FilePath : mi.FilePathCache;
        IList<Int32Rect> faceRects = null;

        try {
          faceRects = await Imaging.DetectFaces(filePath, FaceBoxExpand);
        }
        catch (Exception ex) {
          Core.Instance.Logger.LogError(ex, filePath);
        }

        if (faceRects.Count == 0) {
          WithoutFaces.Add(mi);
          Helper.AreTablePropsModified = true;
          continue;
        }

        foreach (var faceRect in faceRects) {
          var avgHash = await Imaging.GetAvgHashAsync(filePath, faceRect);
          var newFace = new Face(Helper.GetNextId(), 0, faceRect, avgHash) { MediaItem = mi };

          newFace.SetPicture(FaceSize);
          Loaded.Add(newFace);
          All.Add(newFace);

          yield return newFace;
        }
      }
    }

  }
}
