using MH.Utils;
using MH.Utils.Extensions;
using PictureManager.Common.Features.Person;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace PictureManager.Common.Features.MediaItem.Image;

public sealed class ImageS(ImageR r) {
  private static readonly XNamespace _nsX = "adobe:ns:meta/";
  private static readonly XNamespace _nsRdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
  private static readonly XNamespace _nsXmp = "http://ns.adobe.com/xap/1.0/";
  private static readonly XNamespace _nsDc = "http://purl.org/dc/elements/1.1/";
  private static readonly XNamespace _nsGn = "http://www.geonames.org/ontology#"; // TODO check the url for correct schema
  private static readonly XNamespace _nsMp = "http://ns.microsoft.com/photo/1.2/";
  private static readonly XNamespace _nsMpReg = "http://ns.microsoft.com/photo/1.2/t/Region#";
  private static readonly XNamespace _nsMpRi = "http://ns.microsoft.com/photo/1.2/t/RegionInfo#";

  public static Func<ImageM, int, bool> WriteMetadata { get; set; } = null!;

  public bool TryWriteMetadata(ImageM img, int quality) {
    try {
      var xmp = BuildXmp(null, img);
      if (!WriteMetadata(img, quality)) throw new("Error writing metadata");
      img.IsOnlyInDb = false;
    }
    catch (Exception ex) {
      Log.Error(ex, $"Metadata will be saved just in database. {img.FilePath}");
      img.IsOnlyInDb = true;
    }

    r.IsModified = true;
    return !img.IsOnlyInDb;
  }

  public static string? BuildXmp(string? existingXmp, ImageM img) {
    // TODO do compare instead and include rating as well
    if (img.Keywords == null &&
        img.GeoLocation?.GeoName == null &&
        img.People == null &&
        img.Segments == null)
      return null;

    var doc =
      new XDocument(
        new XElement(_nsX + "xmpmeta", new XAttribute(XNamespace.Xmlns + "x", _nsX),
          new XElement(_nsRdf + "RDF", new XAttribute(XNamespace.Xmlns + "rdf", _nsRdf),
            new XElement(_nsRdf + "Description",
              _buildRating(img),
              _buildKeywords(img),
              _buildGeoName(img),
              _buildPeople(img)))));

    return doc.ToString(SaveOptions.DisableFormatting);
  }

  private static XElement? _buildRating(ImageM img) =>
    new XElement(_nsXmp + "Rating", new XAttribute(XNamespace.Xmlns + "xmp", _nsXmp), img.Rating);

  private static XElement? _buildKeywords(ImageM img) {
    var keywords = img.Keywords?.Select(k => k.FullName).ToArray();
    if (keywords == null || keywords.Length == 0) return null;

    return new XElement(_nsDc + "subject", new XAttribute(XNamespace.Xmlns + "dc", _nsDc),
      new XElement(_nsRdf + "Bag",
        keywords.Select(k => new XElement(_nsRdf + "li", k))));
  }

  private static XElement? _buildGeoName(ImageM img) {
    var id = img.GeoLocation?.GeoName?.Id.ToString();
    return id == null
      ? null
      : new XElement(_nsGn + "GeoNameId", new XAttribute(XNamespace.Xmlns + "GeoNames", _nsGn), id);
  }

  private static XElement? _buildPeople(ImageM img) {
    var peopleRects = GetPeopleSegmentsKeywords(img);
    if (peopleRects == null || peopleRects.Count == 0) return null;

    return new XElement(_nsMp + "RegionInfo",
      new XAttribute(XNamespace.Xmlns + "MP", _nsMp),
      new XAttribute(XNamespace.Xmlns + "MPRI", _nsMpRi),
      new XAttribute(XNamespace.Xmlns + "MPReg", _nsMpReg),
      new XElement(_nsMpRi + "Regions",
        new XElement(_nsRdf + "Bag",
          peopleRects.Select(pr =>
            new XElement(_nsRdf + "li",
              new XElement(_nsRdf + "Description",
                pr.Item1 == null ? null : new XElement(_nsMpReg + "PersonDisplayName", pr.Item1.Name),
                pr.Item2 == null ? null : new XElement(_nsMpReg + "Rectangle", pr.Item2),
                pr.Item3 == null ? null : new XElement(_nsMpReg + "RectangleKeywords",
                  new XElement(_nsRdf + "Bag", pr.Item3.Select(k => new XElement(_nsRdf + "li", k))))
              )
            )
          )
        )
      )
    );
  }

  public static List<Tuple<PersonM?, string?, string[]?>>? GetPeopleSegmentsKeywords(ImageM img) {
    var peopleOnSegments = img.Segments.EmptyIfNull().Select(x => x.Person).Distinct().ToHashSet();

    return img.Segments?
      .Select(x => new Tuple<PersonM?, string?, string[]?>(
        x.Person,
        x.ToMsRect(),
        x.Keywords?.Select(k => k.FullName).ToArray()))
      .Concat(img.People
        .EmptyIfNull()
        .Where(x => !peopleOnSegments.Contains(x))
        .Select(x => new Tuple<PersonM?, string?, string[]?>(x, null, null)))
      .ToList();
  }
}