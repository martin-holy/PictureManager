using PictureManager.Domain.Utils;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PictureManager.Domain.Models {
  public class Faces : ObservableObject, ITable {
    #region ITable Properties
    public TableHelper Helper { get; set; }
    public List<IRecord> All { get; } = new();
    #endregion

    private int _faceSize = 100;
    private int _compareFaceSize = 32;
    private int _similarityLimit = 90;
    private int _similarityLimitMin = 80;
    private Dictionary<Face, Dictionary<Face, double>> _loadedSimilar = new();

    public List<Face> Loaded { get; } = new();
    public List<List<Face>> LoadedInGroups { get; } = new();
    public List<Face> Selected { get; } = new();
    public List<MediaItem> WithoutFaces { get; } = new();
    public int FaceSize { get => _faceSize; set { _faceSize = value; OnPropertyChanged(); } }
    public int CompareFaceSize { get => _compareFaceSize; set { _compareFaceSize = value; OnPropertyChanged(); } }
    public int FaceBoxExpand { get; set; } = 40;
    public int SimilarityLimit { get => _similarityLimit; set { _similarityLimit = value; OnPropertyChanged(); } }
    public int SimilarityLimitMin { get => _similarityLimitMin; set { _similarityLimitMin = value; OnPropertyChanged(); } }
    public int SelectedCount => Selected.Count;

    #region ITable Methods
    public void LoadFromFile() {
      All.Clear();
      Helper.LoadFromFile();
    }

    public void NewFromCsv(string csv) {
      // ID|MediaItemId|PersonId|FaceBox
      var props = csv.Split('|');
      if (props.Length != 4) throw new ArgumentException("Incorrect number of values.", csv);
      var rect = props[3].Split(',');
      var face = new Face(
        int.Parse(props[0]),
        int.Parse(props[2]),
        new Int32Rect(int.Parse(rect[0]), int.Parse(rect[1]), int.Parse(rect[2]), int.Parse(rect[3]))) { Csv = props };

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
        }
        else
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
      if (Helper.TableProps.TryGetValue(nameof(CompareFaceSize), out var fompareFaceSize))
        CompareFaceSize = int.Parse(fompareFaceSize);
      if (Helper.TableProps.TryGetValue(nameof(FaceBoxExpand), out var faceBoxExpand))
        FaceBoxExpand = int.Parse(faceBoxExpand);
      if (Helper.TableProps.TryGetValue(nameof(SimilarityLimit), out var similarityLimit))
        SimilarityLimit = int.Parse(similarityLimit);
      if (Helper.TableProps.TryGetValue(nameof(SimilarityLimitMin), out var similarityLimitMin))
        SimilarityLimitMin = int.Parse(similarityLimitMin);

      // table props are not needed any more
      Helper.TableProps.Clear();
      Helper.TableProps = null;
    }

    public void TablePropsToCsv() {
      if (WithoutFaces.Count == 0) return;
      Helper.TableProps = new();
      Helper.TableProps.Add(nameof(WithoutFaces), string.Join(",", WithoutFaces.Select(x => x.Id)));
      Helper.TableProps.Add(nameof(FaceSize), FaceSize.ToString());
      Helper.TableProps.Add(nameof(CompareFaceSize), CompareFaceSize.ToString());
      Helper.TableProps.Add(nameof(FaceBoxExpand), FaceBoxExpand.ToString());
      Helper.TableProps.Add(nameof(SimilarityLimit), SimilarityLimit.ToString());
      Helper.TableProps.Add(nameof(SimilarityLimitMin), SimilarityLimitMin.ToString());
    }
    #endregion

    public void Select(bool isCtrlOn, bool isShiftOn, Face face) {
      // single select
      if (!isCtrlOn && !isShiftOn) {
        DeselectAll();
        SetSelected(face, true);
        OnPropertyChanged(nameof(SelectedCount));
        return;
      }

      // single invert select
      if (isCtrlOn) {
        SetSelected(face, !face.IsSelected);
        OnPropertyChanged(nameof(SelectedCount));
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

        OnPropertyChanged(nameof(SelectedCount));
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

    private void ResetBeforeNewLoad() {
      DeselectAll();
      Loaded.Clear();
      LoadedInGroups.ForEach(x => x.Clear());
      LoadedInGroups.Clear();
    }

    public async IAsyncEnumerable<Face> GetFacesAsync(List<MediaItem> mediaItems, IProgress<int> progress, [EnumeratorCancellation] CancellationToken token = default) {
      ResetBeforeNewLoad();

      var detectedFaces = All.Cast<Face>().Where(x => mediaItems.Contains(x.MediaItem)).ToArray();
      var misWithNotRecognizedFaces = detectedFaces.Where(x => x.PersonId < 1).GroupBy(x => x.MediaItem);
      var mediaItemsToDetect = mediaItems.Except(detectedFaces.Select(x => x.MediaItem).Distinct()).Except(WithoutFaces);
      var done = mediaItems.Count - misWithNotRecognizedFaces.Count() - mediaItemsToDetect.Count();

      progress.Report(done);

      foreach (var miGroup in misWithNotRecognizedFaces) {
        foreach (var face in miGroup) {
          if (token.IsCancellationRequested) yield break;

          await face.SetPictureAsync(FaceSize);
          await face.SetComparePictureAsync(CompareFaceSize);
          Loaded.Add(face);

          yield return face;
        }

        progress.Report(++done);
      }

      foreach (var mi in mediaItemsToDetect) {
        if (token.IsCancellationRequested) yield break;

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
          progress.Report(++done);
          continue;
        }

        foreach (var faceRect in faceRects) {
          var newFace = new Face(Helper.GetNextId(), 0, faceRect) { MediaItem = mi };

          await newFace.SetPictureAsync(FaceSize);
          await newFace.SetComparePictureAsync(CompareFaceSize);
          Loaded.Add(newFace);
          All.Add(newFace);

          yield return newFace;
        }

        progress.Report(++done);
      }
    }

    public void FindSimilarities(IProgress<int> progress) {
      // clear previous loaded similar
      Loaded.ForEach(x => x.Similar?.Clear());

      var tm = new Accord.Imaging.ExhaustiveTemplateMatching(0);
      var done = 0;
      var unknown = Loaded.Where(x => x.PersonId == 0);
      var samePerson = Loaded.Where(x => x.PersonId != 0).GroupBy(x => x.PersonId);

      void Compare(IEnumerable<Face> enumA, IEnumerable<Face> enumB) {
        foreach (var faceA in enumA) {
          faceA.Similar ??= new();
          foreach (var faceB in enumB) {
            if (faceA.Id == faceB.Id) continue;
            var matchings = tm.ProcessImage(faceB.ComparePicture, faceA.ComparePicture);
            var diff = Math.Round(matchings.Max(x => x.Similarity) * 100, 1);
            if (diff < SimilarityLimitMin) continue;
            faceA.Similar.Add((faceB, diff));
          }

          progress.Report(++done);
        }
      }

      foreach (var faces in samePerson)
        Compare(faces, unknown);

      Compare(unknown, unknown);
    }

    public async Task SortLoadedAsync() {
      await Task.Run(() => {
        LoadedInGroups.ForEach(x => x.Clear());
        LoadedInGroups.Clear();

        var set = new HashSet<int>();
        var toLoad = new List<Face>();

        void AddIfToLoad(Face face, double sim) {
          if (set.Add(face.Id)) {
            toLoad.Add(face);
            LoadedInGroups[^1].Add(face);
            face.Sim = sim;
          }
        }

        void AddToLoad(IEnumerable<Face> faces, bool isSamePerson) {
          var withSimilar = faces.Where(x => x.Similar.Count > 0).OrderByDescending(x => x.Similar.Max(y => y.similarity));
          var withoutSimilar = faces.Except(withSimilar);

          LoadedInGroups.Add(new());

          if (isSamePerson)
            foreach (var face in faces)
              AddIfToLoad(face, 100);

          foreach (var face in withSimilar) {
            foreach (var simFace in face.Similar.Where(x => x.face.PersonId == 0 && x.similarity >= SimilarityLimit).OrderByDescending(x => x.similarity)) {
              if (simFace.similarity < simFace.face.Similar.Max(x => x.similarity)) continue;

              AddIfToLoad(face, simFace.similarity);
              AddIfToLoad(simFace.face, simFace.similarity);
            }
          }

          foreach(var face in withoutSimilar)
            AddIfToLoad(face, 0);
        }

        var samePerson = Loaded.Where(x => x.PersonId != 0).GroupBy(x => x.PersonId);
        var unknown = Loaded.Where(x => x.PersonId == 0);

        foreach (var faces in samePerson)
          AddToLoad(faces, true);

        AddToLoad(unknown, false);

        var notSimilar = Loaded.Except(toLoad);
        var newLoaded = toLoad.Concat(notSimilar).ToList();

        foreach (var face in notSimilar)
          LoadedInGroups[^1].Add(face);

        DeselectAll();
        Loaded.Clear();
        Loaded.AddRange(newLoaded);
      });
    }

    public async Task SortLoadedAsyncOld() {
      await Task.Run(() => {
        LoadedInGroups.ForEach(x => x.Clear());
        LoadedInGroups.Clear();

        var set = new HashSet<int>();
        var toLoad = new List<Face>();

        void AddIfToLoad(Face face, double sim) {
          if (set.Add(face.Id)) {
            toLoad.Add(face);
            LoadedInGroups[^1].Add(face);
            face.Sim = sim;
          }
        }

        void AddToLoad(IEnumerable<KeyValuePair<Face, Dictionary<Face, double>>> faces, bool isSamePerson) {
          var withSimilar = faces.Where(x => x.Value != null).OrderByDescending(x => x.Value.Max(y => y.Value));
          var withoutSimilar = faces.Except(withSimilar);

          if (isSamePerson) {
            LoadedInGroups.Add(new());
            foreach (var face in withSimilar)
              AddIfToLoad(face.Key, 100);
          }

          foreach (var face in withSimilar) {
            foreach (var simFace in face.Value.Where(x => x.Value >= SimilarityLimit).OrderByDescending(x => x.Value)) {
              var simFaceSim = _loadedSimilar[simFace.Key];
              var simFaceBestMatch = simFaceSim == null ? 0 : simFaceSim.Max(x => x.Value);
              if (simFace.Value < simFaceBestMatch) continue;

              AddIfToLoad(face.Key, simFace.Value);
              AddIfToLoad(simFace.Key, simFace.Value);
            }
          }

          foreach(var face in withoutSimilar)
            AddIfToLoad(face.Key, 0);
        }

        var samePerson = _loadedSimilar.Where(x => x.Key.PersonId != 0).GroupBy(x => x.Key.PersonId);
        var unknown = _loadedSimilar.Where(x => x.Key.PersonId == 0);

        foreach (var faces in samePerson)
          AddToLoad(faces, true);

        LoadedInGroups.Add(new());
        AddToLoad(unknown, false);

        var notSimilar = Loaded.Except(toLoad);
        var newLoaded = toLoad.Concat(notSimilar).ToList();

        foreach (var face in notSimilar)
          LoadedInGroups[^1].Add(face);

        DeselectAll();
        Loaded.Clear();
        Loaded.AddRange(newLoaded);
      });
    }

    /// <summary>
    /// Sets new PersonId to all Faces that are selected or that have the same PersonId as some of the selected.
    /// The new PersonId is the lowest from the selected or the lowest -1 from all faces.
    /// </summary>
    public void SetSelectedAsSamePerson() {
      var selectedPeopleIds = Selected.Where(x => x.PersonId < 0).Select(x => x.PersonId).Distinct().ToArray();
      var allWithSameId = All.Cast<Face>().Where(x => selectedPeopleIds.Contains(x.PersonId));
      var toUpdate = allWithSameId.Concat(Selected.Where(x => x.PersonId == 0));
      var newId = Selected.Min(x => x.PersonId);
      if (newId == 0) newId = All.Cast<Face>().Min(x => x.PersonId) - 1;

      foreach (var face in toUpdate) {
        face.PersonId = newId;
        Core.Instance.Sdb.SetModified<Faces>();
      }
    }

    public void SetSelectedAsNotThisPerson() {
      Selected.ForEach(x => x.PersonId = 0);
      SetSelectedAsSamePerson();
    }
  }
}