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
    public TableHelper Helper { get; set; }
    public List<IRecord> All { get; } = new();
    public Dictionary<int, Face> AllDic { get; set; }

    private int _faceSize = 100;
    private int _compareFaceSize = 32;
    private int _similarityLimit = 90;
    private int _similarityLimitMin = 80;
    private int _maxFacesInGroup = 5;
    private bool _groupFaces;
    private bool _groupConfirmedFaces;

    public List<Face> Loaded { get; } = new();
    public List<List<Face>> LoadedInGroups { get; } = new();
    public List<Face> Selected { get; } = new();
    public List<MediaItem> WithoutFaces { get; } = new();
    public List<(int personId, Face face, List<(int personId, Face face, double sim)> similar)> ConfirmedFaces { get; } = new();
    public int FaceSize { get => _faceSize; set { _faceSize = value; OnPropertyChanged(); } }
    public int CompareFaceSize { get => _compareFaceSize; set { _compareFaceSize = value; OnPropertyChanged(); } }
    public int FaceBoxExpand { get; set; } = 40;
    public int SimilarityLimit { get => _similarityLimit; set { _similarityLimit = value; OnPropertyChanged(); } }
    public int SimilarityLimitMin { get => _similarityLimitMin; set { _similarityLimitMin = value; OnPropertyChanged(); } }
    public int MaxFacesInGroup { get => _maxFacesInGroup; set { _maxFacesInGroup = value; OnPropertyChanged(); } }
    public int SelectedCount => Selected.Count;
    public bool GroupFaces { get => _groupFaces; set { _groupFaces = value; OnPropertyChanged(); } }
    public bool GroupConfirmedFaces { get => _groupConfirmedFaces; set { _groupConfirmedFaces = value; OnPropertyChanged(); } }

    #region ITable Methods
    public void LoadFromFile() {
      All.Clear();
      AllDic = new Dictionary<int, Face>();
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
      AllDic.Add(face.Id, face);
    }

    public void LinkReferences() {
      var mediaItems = Core.Instance.MediaItems.AllDic;
      var people = Core.Instance.People.AllDic;
      var faces = All.Cast<Face>().ToDictionary(x => x.Id);
      var withoutMediaItem = new List<Face>();

      foreach (var face in All.Cast<Face>()) {
        if (mediaItems.TryGetValue(int.Parse(face.Csv[1]), out var mi)) {
          face.MediaItem = mi;

          if (face.PersonId > 0 && people.TryGetValue(face.PersonId, out var person)) {
            face.Person = person;
            if (person.Face == null) {
              person.Face = face;
              _ = face.SetPictureAsync(FaceSize);
            }
          }
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
      if (Helper.TableProps.TryGetValue(nameof(MaxFacesInGroup), out var maxFacesInGroup))
        MaxFacesInGroup = int.Parse(maxFacesInGroup);

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
      Helper.TableProps.Add(nameof(MaxFacesInGroup), MaxFacesInGroup.ToString());
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
        var list = GroupFaces ? LoadedInGroups.Single(x => x.Contains(face)) : Loaded;
        var indexOfFace = list.IndexOf(face);
        var fromFace = list.FirstOrDefault(x => x.IsSelected);
        var from = fromFace == null ? 0 : list.IndexOf(fromFace);
        var to = indexOfFace;

        if (from > to) {
          to = from;
          from = indexOfFace;
        }

        for (var i = from; i < to + 1; i++)
          SetSelected(list[i], true);
      }
    }

    public void DeselectAll() {
      foreach (var face in Selected.ToArray())
        SetSelected(face, false);
    }

    public void SetSelected(Face face, bool value) {
      if (face.IsSelected == value) return;
      face.IsSelected = value;
      if (value) Selected.Add(face);
      else Selected.Remove(face);
      OnPropertyChanged(nameof(SelectedCount));
    }

    private void ResetBeforeNewLoad() {
      DeselectAll();
      Loaded.Clear();
      LoadedInGroups.ForEach(x => x.Clear());
      LoadedInGroups.Clear();
    }

    private static IEnumerable<Face> GetSomeFacesForEachPerson(IEnumerable<Face> faces, int count) {
      var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
      var facesA = faces.Where(x => x.PersonId != 0).GroupBy(x => x.PersonId).Select(x => x.OrderBy(y => random.Next()).Take(count));
      var facesB = facesA.Any() ? facesA.Aggregate((all, next) => all.Concat(next)) : Enumerable.Empty<Face>();
      return facesB;
    }

    public async IAsyncEnumerable<Face> GetFacesAsync(List<MediaItem> mediaItems, bool detectNewFaces, IProgress<int> progress, [EnumeratorCancellation] CancellationToken token = default) {
      ResetBeforeNewLoad();

      var detectedFaces = All.Cast<Face>().Where(x => mediaItems.Contains(x.MediaItem)).ToArray();
      var misWithDetectedFaces = detectedFaces.GroupBy(x => x.MediaItem);
      var mediaItemsToDetect = Array.Empty<MediaItem>();

      if (detectNewFaces)
        mediaItemsToDetect = mediaItems.Except(detectedFaces.Select(x => x.MediaItem).Distinct()).Except(WithoutFaces).ToArray();

      var done = mediaItems.Count - misWithDetectedFaces.Count() - mediaItemsToDetect.Length;

      progress.Report(done);

      // load max {MaxFacesInGroup} faces from each PersonId
      var knownFaces = GetSomeFacesForEachPerson(detectedFaces, MaxFacesInGroup);
      var faces = knownFaces.Concat(detectedFaces.Where(x => x.PersonId == 0)).OrderBy(x => x.MediaItem.FileName).ToArray();
      var doneFaces = 0.0;
      var doneFacesVsMediaItems = (double)misWithDetectedFaces.Count() / faces.Length;

      foreach (var face in faces) {
        if (token.IsCancellationRequested) yield break;

        await face.SetPictureAsync(FaceSize);
        Loaded.Add(face);

        yield return face;

        doneFaces += doneFacesVsMediaItems;
        progress.Report(done + (int)doneFaces);
      }

      done += (int)doneFaces;

      // load all
      /*foreach (var miGroup in misWithDetectedFaces) {
        foreach (var face in miGroup) {
          if (token.IsCancellationRequested) yield break;

          await face.SetPictureAsync(FaceSize);
          Loaded.Add(face);

          yield return face;
        }

        progress.Report(++done);
      }*/

      if (!detectNewFaces) yield break;

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
          Loaded.Add(newFace);
          All.Add(newFace);

          yield return newFace;
        }

        progress.Report(++done);
      }
    }

    public Task FindSimilaritiesAsync(IProgress<int> progress, CancellationToken token) {
      return Task.Run(async () => {
        // clear previous loaded similar
        foreach (var face in Loaded) {
          face.Similar?.Clear();
          face.SimMax = 0;
        }

        var tm = new Accord.Imaging.ExhaustiveTemplateMatching(0);
        var done = 0;

        foreach (var faceA in Loaded) {
          if (token.IsCancellationRequested) break;
          await faceA.SetComparePictureAsync(CompareFaceSize);
          if (faceA.ComparePicture == null) continue;

          faceA.Similar ??= new();
          foreach (var faceB in Loaded) {
            if (token.IsCancellationRequested) break;
            if (faceA.Id == faceB.Id || (faceA.PersonId != 0 && faceA.PersonId == faceB.PersonId)) continue;

            await faceB.SetComparePictureAsync(CompareFaceSize);
            if (faceB.ComparePicture == null) continue;

            var matchings = tm.ProcessImage(faceB.ComparePicture, faceA.ComparePicture);
            var sim = Math.Round(matchings.Max(x => x.Similarity) * 100, 1);
            if (sim < SimilarityLimitMin) continue;

            faceA.Similar.Add(faceB, sim);
            if (faceA.SimMax < sim) faceA.SimMax = sim;
          }

          if (faceA.Similar.Count == 0) {
            faceA.Similar = null;
            faceA.SimMax = 0;
          }

          progress.Report(++done);
        }
      }, token);
    }

    public async IAsyncEnumerable<Face> GetAllFacesAsync(IProgress<int> progress, [EnumeratorCancellation] CancellationToken token = default) {
      ResetBeforeNewLoad();
      var done = 0;

      foreach (var face in GetSomeFacesForEachPerson(All.Cast<Face>(), MaxFacesInGroup)) {
        if (token.IsCancellationRequested) yield break;

        await face.SetPictureAsync(FaceSize);
        face.MediaItem.SetThumbSize();
        Loaded.Add(face);

        yield return face;

        progress.Report(++done);
      }
    }

    /// <summary>
    /// Compares faces with same person id to other faces with same person id 
    /// and select random face from each group for display
    /// </summary>
    public void ReloadConfirmedFaces() {
      var groupsA = Loaded.Where(x => x.PersonId > 0).GroupBy(x => x.PersonId).OrderBy(x => x.First().Person.Title);
      var groupsB = Loaded.Where(x => x.PersonId < 0).GroupBy(x => x.PersonId).OrderByDescending(x => x.Key);
      var groups = groupsA.Concat(groupsB);
      var similarity = new List<(IGrouping<int, Face> gA, double sim, IGrouping<int, Face> gB)>();
      var set = new HashSet<int>();
      var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);

      ConfirmedFaces.Clear();

      foreach (var gA in groups) {
        (int personId, Face face, List<(int personId, Face face, double sim)> similar) confirmedFace = new() {
          personId = gA.Key,
          face = gA.ToArray()[random.Next(gA.Count() - 1)],
          similar = new()
        };

        foreach (var gB in groups) {
          if (gA.Key == gB.Key) continue;

          var sims = new List<double>();

          foreach (var faceA in gA)
            foreach (var faceB in gB)
              if (faceA.Similar != null && faceA.Similar.TryGetValue(faceB, out var sim) && sim >= SimilarityLimit)
                sims.Add(sim);

          if (sims.Count == 0) continue;

          var simMedian = sims.OrderBy(x => x).ToArray()[sims.Count / 2];
          confirmedFace.similar.Add(new(gB.Key, gB.ToArray()[random.Next(gB.Count() - 1)], simMedian));
        }

        ConfirmedFaces.Add(confirmedFace);
      }
    }

    public Task ReloadLoadedInGroupsAsync() {
      return Task.Run(() => {
        // clear
        foreach (var group in LoadedInGroups)
          group.Clear();
        LoadedInGroups.Clear();

        List<Face> facesGroup;

        // add faces with PersonId != 0 with all similar with PersonId == 0
        var groupsA = Loaded.Where(x => x.PersonId > 0).GroupBy(x => x.PersonId).OrderBy(x => x.First().Person.Title);
        var groupsB = Loaded.Where(x => x.PersonId < 0).GroupBy(x => x.PersonId).OrderByDescending(x => x.Key);
        var samePerson = groupsA.Concat(groupsB);
        foreach (var faces in samePerson) {
          var sims = new List<(Face face, double sim)>();
          facesGroup = new List<Face>();

          foreach (var face in faces) {
            facesGroup.Add(face);
            if (face.Similar == null) continue;
            sims.AddRange(face.Similar.Where(x => x.Key.PersonId == 0 && x.Value >= SimilarityLimit).Select(x => (x.Key, x.Value)));
          }

          // order by number of similar than by similarity
          foreach (var face in sims.GroupBy(x => x.face).OrderByDescending(g => g.Count())
                                   .Select(g => g.OrderByDescending(x => x.sim).First().face)) {
            facesGroup.Add(face);
          }

          if (facesGroup.Count != 0)
            LoadedInGroups.Add(facesGroup);
        }

        // add faces with PersonId == 0 ordered by similar
        var unknown = Loaded.Where(x => x.PersonId == 0);
        var withSimilar = unknown.Where(x => x.Similar != null).OrderByDescending(x => x.SimMax);
        var withoutSimilar = unknown.Except(withSimilar);
        var set = new HashSet<int>();
        facesGroup = new List<Face>();

        foreach (var face in withSimilar) {
          var simFaces = face.Similar.Where(x => x.Key.PersonId == 0 && x.Value >= SimilarityLimit).OrderByDescending(x => x.Value);
          foreach (var simFace in simFaces) {
            if (set.Add(face.Id)) facesGroup.Add(face);
            if (set.Add(simFace.Key.Id)) facesGroup.Add(simFace.Key);
          }
        }

        // add rest of the faces
        foreach (var face in unknown.Where(x => !set.Contains(x.Id)))
          facesGroup.Add(face);

        if (facesGroup.Count != 0)
          LoadedInGroups.Add(facesGroup);
      });
    }

    /// <summary>
    /// Sets new PersonId to all Faces that are selected or that have the same PersonId (not 0) as some of the selected.
    /// The new PersonId is the highest PersonId from the selected or highest unused negative id if PersonsIds are 0.
    /// </summary>
    public void SetSelectedAsSamePerson() {
      if (Selected.Count == 0) return;

      var personsIds = Selected.Select(x => x.PersonId).Distinct().OrderByDescending(x => x).ToArray();
      // prefer known person id (id > 0)
      var newId = personsIds[0] != 0 ? personsIds[0] : personsIds.Length > 1 ? personsIds[1] : 0;

      if (newId == 0) { // get unused min ID
        var usedIds = All.Cast<Face>().Where(x => x.PersonId < 0).
          Select(x => x.PersonId).Distinct().OrderByDescending(x => x).ToArray();
        for (var i = -1; i > usedIds.Min() - 2; i--) {
          if (usedIds.Contains(i)) continue;
          newId = i;
          break;
        }
      }

      Face[] toUpdate;

      if (personsIds.Length == 1 && personsIds[0] == 0)
        toUpdate = Selected.ToArray();
      else {
        // take just faces with unknown people
        var allWithSameId = All.Cast<Face>().
          Where(x => x.PersonId != 0 && x.PersonId != newId && personsIds.Contains(x.PersonId));
        toUpdate = allWithSameId.Concat(Selected.Where(x => x.PersonId == 0)).ToArray();
      }

      var person = newId < 1 ? null : Core.Instance.People.All.FirstOrDefault(x => x.Id == newId) as Person;

      foreach (var face in toUpdate) {
        face.PersonId = newId;
        face.Person = person;
        Core.Instance.Sdb.SetModified<Faces>();
      }

      return;
    }

    public void SetSelectedAsAnotherPerson() {
      foreach (var face in Selected)
        face.PersonId = 0;
      SetSelectedAsSamePerson();
    }

    public void ChangePerson(int personId, Person person) {
      foreach (var face in All.Cast<Face>().Where(x => x.PersonId == personId))
        ChangePerson(face, person);
    }

    public static void ChangePerson(Face face, Person person) {
      face.PersonId = person.Id;
      face.Person = person;
      person.Face ??= face;

      Core.Instance.Sdb.SetModified<Faces>();
    }

    public void DeleteSelected() {
      foreach (var face in Selected.ToArray()) {
        SetSelected(face, false);
        if (face.Person != null) {
          face.Person.Face = null;
          face.Person = null;
        }

        face.Similar?.Clear();
        face.Picture = null;
        All.Remove(face);
        Loaded.Remove(face);

        foreach (var simFace in Loaded)
          simFace.Similar?.Remove(face);

        Core.Instance.Sdb.SetModified<Faces>();
      }
    }
  }
}