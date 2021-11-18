using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MH.Utils.BaseClasses;
using PictureManager.Domain.Models;

namespace PictureManager.ViewModels {
  public class MediaItemBaseVM : ObservableObject {
    public MediaItemM Model { get; }
    public ObservableCollection<string> InfoBoxThumb { get; set; }
    public ObservableCollection<string> InfoBoxPeople { get; set; }
    public ObservableCollection<string> InfoBoxKeywords { get; set; }
    public string Dimensions => $"{Model.Width}x{Model.Height}";

    public MediaItemBaseVM(MediaItemM model) {
      Model = model;
    }

    public void SetInfoBox() {
      InfoBoxPeople?.Clear();
      InfoBoxPeople = null;
      InfoBoxKeywords?.Clear();
      InfoBoxKeywords = null;
      InfoBoxThumb?.Clear();
      InfoBoxThumb = new();

      if (Model.Rating != 0)
        InfoBoxThumb.Add(Model.Rating.ToString());

      if (!string.IsNullOrEmpty(Model.Comment))
        InfoBoxThumb.Add(Model.Comment);

      if (Model.GeoName != null)
        InfoBoxThumb.Add(Model.GeoName.Name);

      if (Model.People != null || Model.Segments != null) {
        var people = (
            Model.People == null
              ? Array.Empty<string>()
              : Model.People.Select(x => x.Name))
          .Concat(
            Model.Segments == null
              ? Array.Empty<string>()
              : Model.Segments.Where(x => x.Person != null).Select(x => x.Person.Name)).ToArray();

        if (people.Any()) {
          InfoBoxPeople = new();

          foreach (var p in people.Distinct().OrderBy(x => x)) {
            InfoBoxPeople.Add(p);
            InfoBoxThumb.Add(p);
          }
        }
      }

      if (Model.Keywords != null) {
        InfoBoxKeywords = new();
        var allKeywords = new List<KeywordM>();

        foreach (var keyword in Model.Keywords)
          MH.Utils.Tree.GetThisAndParentRecursive(keyword, ref allKeywords);

        foreach (var keyword in allKeywords.Distinct().OrderBy(x => x.FullName)) {
          InfoBoxKeywords.Add(keyword.Name);
          InfoBoxThumb.Add(keyword.Name);
        }
      }

      if (InfoBoxThumb.Count == 0)
        InfoBoxThumb = null;

      OnPropertyChanged(nameof(InfoBoxThumb));
      OnPropertyChanged(nameof(InfoBoxPeople));
      OnPropertyChanged(nameof(InfoBoxKeywords));
    }
  }
}
