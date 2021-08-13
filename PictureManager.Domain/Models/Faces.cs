using PictureManager.Domain.Utils;
using SimpleDB;
using System;
using System.Collections.Generic;
using System.IO;
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
    private int _maxFacesInGroup = 3;
    private bool _groupFaces;
    private bool _groupConfirmedFaces;

    public List<Face> Loaded { get; } = new();
    public IEnumerable<Face> FacesForComparison { get; set; }
    public List<List<Face>> LoadedGroupedByPerson { get; } = new();
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
      // ID|MediaItemId|PersonId|GroupId|FaceBox
      var props = csv.Split('|');
      if (props.Length != 5) throw new ArgumentException("Incorrect number of values.", csv);
      var rect = props[4].Split(',');
      var face = new Face(int.Parse(props[0]), int.Parse(props[2]), int.Parse(rect[0]), int.Parse(rect[1]), int.Parse(rect[2])) {
        Csv = props,
        GroupId = int.Parse(props[3])
      };

      All.Add(face);
      AllDic.Add(face.Id, face);
    }

    public void LinkReferences() {
      var mediaItems = Core.Instance.MediaItems.AllDic;
      var people = Core.Instance.People.AllDic;
      var withoutMediaItem = new List<Face>();

      foreach (var face in All.Cast<Face>()) {
        if (mediaItems.TryGetValue(int.Parse(face.Csv[1]), out var mi)) {
          face.MediaItem = mi;
          mi.Faces ??= new();
          mi.Faces.Add(face);

          if (face.PersonId > 0 && people.TryGetValue(face.PersonId, out var person)) {
            face.Person = person;
            person.Face ??= face;
          }
        }
        else {
          withoutMediaItem.Add(face);
        }

        // csv array is not needed any more
        face.Csv = null;
      }

      // in case MediaItem was deleted
      foreach (var face in withoutMediaItem)
        _ = All.Remove(face);

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

    public void Select(bool isCtrlOn, bool isShiftOn, List<Face> list, Face face) {
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
        var indexOfFace = list.IndexOf(face);
        var fromFace = list.Find(x => x.IsSelected && x != face);
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
      else _ = Selected.Remove(face);
      OnPropertyChanged(nameof(SelectedCount));
    }

    private void ResetBeforeNewLoad() {
      DeselectAll();
      Loaded.Clear();
      LoadedGroupedByPerson.ForEach(x => x.Clear());
      LoadedGroupedByPerson.Clear();
    }


    // this is test for creating groups of similar faces for each person
    public async Task ReloadFaceGroups(int personId, double similarity) =>
      await ReloadFaceGroups(All.Cast<Face>().Where(x => x.PersonId == personId), similarity);

    public async Task ReloadFaceGroups(IEnumerable<Face> faces, double similarity) {
      var groups = new List<List<Face>>();
      var tm = new Accord.Imaging.ExhaustiveTemplateMatching(0);

      foreach (var face0 in faces) {
        await face0.SetPictureAsync(FaceSize);
        await face0.SetComparePictureAsync(CompareFaceSize);
        if (face0.ComparePicture == null) continue;

        var res = new List<(List<Face> group, Face face, double sim)>();
        foreach (var group in groups) {
          foreach (var face in group) {
            await face.SetPictureAsync(FaceSize);
            await face.SetComparePictureAsync(CompareFaceSize);

            var matchings = tm.ProcessImage(face0.ComparePicture, face.ComparePicture);
            var sim = matchings.Max(x => x.Similarity);
            if (sim < similarity) continue;
            res.Add(new(group, face, sim));
          }
        }

        if (res.Count == 0)
          groups.Add(new List<Face>() { face0 });
        else
          res.OrderByDescending(x => x.sim).First().group.Add(face0);
      }

      for (var i = 0; i < groups.Count; i++)
        foreach (var face in groups[i])
          face.GroupId = i + 1;

      Core.Instance.Sdb.SetModified<Faces>();
    }

    public async IAsyncEnumerable<Face> GetFacesAsync(List<MediaItem> mediaItems, bool detectNewFaces, IProgress<int> progress, [EnumeratorCancellation] CancellationToken token = default) {
      ResetBeforeNewLoad();

      var detectedFaces = All.Cast<Face>().Where(x => mediaItems.Contains(x.MediaItem)).ToArray();
      var misWithDetectedFaces = detectedFaces.GroupBy(x => x.MediaItem);
      var mediaItemsToDetect = detectNewFaces
        ? mediaItems.Except(misWithDetectedFaces.Select(x => x.Key)).Except(WithoutFaces).ToArray()
        : Array.Empty<MediaItem>();
      var done = mediaItems.Count - misWithDetectedFaces.Count() - mediaItemsToDetect.Length;

      progress.Report(done);

      foreach (var miGroup in misWithDetectedFaces) {
        foreach (var face in miGroup) {
          if (token.IsCancellationRequested) yield break;

          await face.SetPictureAsync(FaceSize);
          Loaded.Add(face);

          yield return face;
        }

        progress.Report(++done);
      }

      if (!detectNewFaces) yield break;

      foreach (var mi in mediaItemsToDetect) {
        if (token.IsCancellationRequested) yield break;

        var filePath = mi.MediaType == MediaType.Image ? mi.FilePath : mi.FilePathCache;
        IList<Int32Rect> faceRects = null;

        try {
          faceRects = await Imaging.DetectFaces(filePath, FaceBoxExpand);
        }
        catch (Exception ex) {
          Core.Instance.LogError(ex, filePath);
        }

        if (faceRects.Count == 0) {
          WithoutFaces.Add(mi);
          Helper.AreTablePropsModified = true;
          progress.Report(++done);
          continue;
        }

        foreach (var faceRect in faceRects) {
          var half = faceRect.Width / 2;
          var newFace = await AddNewFace(faceRect.X + half, faceRect.Y + half, half * 2, mi);

          yield return newFace;
        }

        progress.Report(++done);
      }

      if (Helper.AreTablePropsModified)
        Helper.SaveTablePropsToFile();
    }

    public async Task<Face> AddNewFace(int x, int y, int size, MediaItem mediaItem) {
      var newFace = new Face(Helper.GetNextId(), 0, x, y, size) { MediaItem = mediaItem };
      await newFace.SetPictureAsync(FaceSize);
      mediaItem.Faces ??= new();
      mediaItem.Faces.Add(newFace);
      All.Add(newFace);
      Loaded.Add(newFace);
      _ = WithoutFaces.Remove(mediaItem);

      return newFace;
    }

    public async Task SetFacesForComparison() {
      if (FacesForComparison != null) return;

      var peopleFaces = GetSomeFacesForEachPerson(Loaded, MaxFacesInGroup);
      var faces = peopleFaces.Any() ? peopleFaces.Aggregate((all, next) => all.Concat(next)) : Enumerable.Empty<Face>();
      FacesForComparison = Loaded.Where(x => x.PersonId == 0).Concat(faces);

      foreach (var face in FacesForComparison.Except(Loaded).ToArray()) {
        await face.SetPictureAsync(FaceSize);
        face.MediaItem.SetThumbSize();
        Loaded.Add(face);
      }
    }

    public Task FindSimilaritiesAsync(IEnumerable<Face> faces, IProgress<int> progress, CancellationToken token) {
      return Task.Run(async () => {
        // clear previous loaded similar
        // clear needs to be for Loaded!
        foreach (var face in Loaded) {
          face.Similar?.Clear();
          face.SimMax = 0;
        }

        var tm = new Accord.Imaging.ExhaustiveTemplateMatching(0);
        var done = 0;

        foreach (var faceA in faces) {
          if (token.IsCancellationRequested) break;
          await faceA.SetPictureAsync(FaceSize);
          await faceA.SetComparePictureAsync(CompareFaceSize);
          if (faceA.ComparePicture == null) {
            progress.Report(++done);
            Core.Instance.LogError(new Exception($"Picture with unsupported pixel format.\n{faceA.MediaItem.FilePath}"));
            continue;
          }

          faceA.Similar ??= new();
          foreach (var faceB in faces) {
            if (token.IsCancellationRequested) break;
            // do not compare face with it self or with face that have same person
            if (faceA == faceB || (faceA.PersonId != 0 && faceA.PersonId == faceB.PersonId)) continue;

            await faceA.SetPictureAsync(FaceSize);
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

    private static IEnumerable<IEnumerable<Face>> GetSomeFacesForEachPerson(IEnumerable<Face> faces, int count) {
      var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
      return faces.Where(x => x.PersonId != 0).GroupBy(x => x.PersonId).Select(
        x => x.First().Person?.Faces != null ? x.First().Person.Faces : x.OrderBy(y => random.Next()).Take(count));
    }

    public async IAsyncEnumerable<Face> GetAllFacesAsync(IProgress<int> progress, [EnumeratorCancellation] CancellationToken token = default) {
      ResetBeforeNewLoad();
      var done = 0;

      foreach (var personFaces in GetSomeFacesForEachPerson(All.Cast<Face>(), MaxFacesInGroup)) {
        foreach (var face in personFaces) {
          if (token.IsCancellationRequested) yield break;

          await face.SetPictureAsync(FaceSize);
          face.MediaItem.SetThumbSize();
          Loaded.Add(face);

          yield return face;
        }
        progress.Report(++done);
      }
    }

    /// <summary>
    /// Compares faces with same person id to other faces with same person id 
    /// and select random face from each group for display
    /// </summary>
    public async Task ReloadConfirmedFaces() {
      var groupsA = Loaded.Where(x => x.PersonId > 0).GroupBy(x => x.PersonId).OrderBy(x => x.First().Person.Title);
      var groupsB = Loaded.Where(x => x.PersonId < 0).GroupBy(x => x.PersonId).OrderByDescending(x => x.Key);
      var groups = groupsA.Concat(groupsB);
      var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);

      async Task<Face> GetRandomFace(IEnumerable<Face> faces) {
        var _faces = faces.First().Person?.Faces ?? faces;
        var face = _faces.ToArray()[random.Next(_faces.Count() - 1)];
        await face.SetPictureAsync(FaceSize);
        face.MediaItem.SetThumbSize();
        return face;
      }

      ConfirmedFaces.Clear();

      foreach (var gA in groups) {
        (int personId, Face face, List<(int personId, Face face, double sim)> similar) confirmedFace = new() {
          personId = gA.Key,
          face = await GetRandomFace(gA),
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
          confirmedFace.similar.Add(new(gB.Key, await GetRandomFace(gB), simMedian));
        }

        ConfirmedFaces.Add(confirmedFace);
      }
    }

    public Task ReloadLoadedGroupedByPersonAsync() {
      return Task.Run(() => {
        // clear
        foreach (var group in LoadedGroupedByPerson)
          group.Clear();
        LoadedGroupedByPerson.Clear();

        List<Face> facesGroup;

        // add faces with PersonId != 0 with all similar faces with PersonId == 0
        var groupsA = Loaded.Where(x => x.PersonId > 0).GroupBy(x => x.PersonId).OrderBy(x => x.First().Person.Title);
        var groupsB = Loaded.Where(x => x.PersonId < 0).GroupBy(x => x.PersonId).OrderByDescending(x => x.Key);
        var samePerson = groupsA.Concat(groupsB);
        foreach (var faces in samePerson) {
          var sims = new List<(Face face, double sim)>();
          facesGroup = new();

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
            LoadedGroupedByPerson.Add(facesGroup);
        }

        // add faces with PersonId == 0 ordered by similar
        var unknown = Loaded.Where(x => x.PersonId == 0);
        var withSimilar = unknown.Where(x => x.Similar != null).OrderByDescending(x => x.SimMax);
        var set = new HashSet<int>();
        facesGroup = new();

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
          LoadedGroupedByPerson.Add(facesGroup);
      });
    }

    /// <summary>
    /// Sets new Person to all Faces that are selected or that have the same PersonId (< 0) as some of the selected.
    /// </summary>
    /// <param name="person"></param>
    public void SetSelectedAsPerson(Person person) {
      var unknownPeople = Selected.Select(x => x.PersonId).Distinct().Where(x => x < 0).ToDictionary(x => x);
      var faces = Selected.Where(x => x.PersonId >= 0).Concat(All.Cast<Face>().Where(x => unknownPeople.ContainsKey(x.PersonId)));

      foreach (var face in faces)
        ChangePerson(face, person);

      DeselectAll();
    }

    // TODO refactor this
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
        face.MediaItem.SetInfoBox();
        Core.Instance.Sdb.SetModified<Faces>();
      }
    }

    public void SetSelectedAsUnknown() {
      foreach (var face in Selected) {
        RemovePersonFromFace(face);
        face.PersonId = 0;
        face.MediaItem.SetInfoBox();
        Core.Instance.Sdb.SetModified<Faces>();
      }
    }

    private static void RemovePersonFromFace(Face face) {
      if (face?.Person == null) return;
      if (face.Person.Face == face)
        face.Person.Face = null;
      if (face.Person.Faces != null && face.Person.Faces.Remove(face)) {
        if (!face.Person.Faces.Any())
          face.Person.Faces = null;
        Core.Instance.Sdb.SetModified<People>();
      }
      face.Person = null;
    }

    public void ChangePerson(int personId, Person person) {
      foreach (var face in All.Cast<Face>().Where(x => x.PersonId == personId))
        ChangePerson(face, person);
    }

    public static void ChangePerson(Face face, Person person) {
      RemovePersonFromFace(face);
      face.PersonId = person.Id;
      face.Person = person;
      person.Face ??= face;
      face.MediaItem.SetInfoBox();

      Core.Instance.Sdb.SetModified<Faces>();
    }

    public void Delete(Face face) {
      SetSelected(face, false);
      RemovePersonFromFace(face);

      // remove Face from MediaItem
      if (face.MediaItem.Faces.Remove(face) && !face.MediaItem.Faces.Any()) {
        face.MediaItem.Faces = null;
        WithoutFaces.Add(face.MediaItem);
      }

      face.Similar?.Clear();
      face.Picture = null;
      face.ComparePicture = null;

      _ = All.Remove(face);
      _ = Loaded.Remove(face);

      foreach (var simFace in Loaded)
        _ = simFace.Similar?.Remove(face);

      Core.Instance.Sdb.SetModified<Faces>();

      try {
        File.Delete(face.CacheFilePath);
      }
      catch (Exception ex) {
        Core.Instance.LogError(ex);
      }
    }

    public void DeleteSelected() {
      foreach (var face in Selected.ToArray())
        Delete(face);
    }
  }
}