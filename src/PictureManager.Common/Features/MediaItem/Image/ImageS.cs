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
      var existingXmp = XmpU.ReadFromJpeg(img.FilePath);
      var xmp = BuildXmp(existingXmp, img);
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
    if (_tryParseXmp(existingXmp) is not { } doc)
      doc = new XDocument(new XElement(_nsX + "xmpmeta", new XAttribute(XNamespace.Xmlns + "x", _nsX)));

    if (doc.Descendants(_nsRdf + "RDF").FirstOrDefault() is not { } rdf) {
      rdf = new XElement(_nsRdf + "RDF", new XAttribute(XNamespace.Xmlns + "rdf", _nsRdf));
      doc.Add(rdf);
    }

    _mergeRating(rdf, img.Rating);
    _mergeKeywords(rdf, img);
    _mergeGeoName(rdf, img);
    _mergePeople(rdf, img);

    return doc.ToString(SaveOptions.DisableFormatting);
  }

  private static XDocument? _tryParseXmp(string? xmp) {
    try {
      if (!string.IsNullOrEmpty(xmp))
        return XDocument.Parse(xmp, LoadOptions.PreserveWhitespace);
    }
    catch (Exception ex) {
      Log.Error(ex);
    }

    return null;
  }

  private static XElement? _getDescriptionFor(XElement rdf, XNamespace propertyNs, XName propertyName) =>
    rdf.Elements(_nsRdf + "Description").FirstOrDefault(
      d => d.Element(propertyName) != null ||
      d.Attributes().Any(a => a.IsNamespaceDeclaration && a.Value == propertyNs));

  private static XElement _addDescription(XElement rdf) {
    var desc = new XElement(_nsRdf + "Description");
    rdf.Add(desc);
    return desc;
  }

  private static void _mergeRating(XElement rdf, int rating) {
    var desc = _getDescriptionFor(rdf, _nsXmp, _nsXmp + "Rating") ?? _addDescription(rdf);
    desc.SetAttributeValue(XNamespace.Xmlns + "xmp", _nsXmp);
    desc.Element(_nsXmp + "Rating")?.Remove();
    desc.Add(new XElement(_nsXmp + "Rating", rating));
  }

  private static void _mergeKeywords(XElement rdf, ImageM img) {
    var desc = _getDescriptionFor(rdf, _nsDc, _nsDc + "subject");

    var keywords = img.Keywords?.Select(k => k.FullName).ToArray();
    if (keywords == null || keywords.Length == 0) {
      desc?.Element(_nsDc + "subject")?.Remove();
      return;
    }

    desc ??= _addDescription(rdf);
    desc.SetAttributeValue(XNamespace.Xmlns + "dc", _nsDc);
    desc.Element(_nsDc + "subject")?.Remove();
    desc.Add(new XElement(_nsDc + "subject",
      new XElement(_nsRdf + "Bag",
        keywords.Select(k => new XElement(_nsRdf + "li", k)))));
  }

  private static void _mergeGeoName(XElement rdf, ImageM img) {
    var id = img.GeoLocation?.GeoName?.Id.ToString();
    var desc = _getDescriptionFor(rdf, _nsGn, _nsGn + "GeoNameId");

    if (id == null) {
      desc?.Element(_nsGn + "GeoNameId")?.Remove();
      return;
    }

    desc ??= _addDescription(rdf);
    desc.SetAttributeValue(XNamespace.Xmlns + "GeoNameId", _nsGn);
    desc.Element(_nsGn + "GeoNameId")?.Remove();
    desc.Add(new XElement(_nsXmp + "GeoNameId", id));
  }

  private static void _mergePeople(XElement rdf, ImageM img) {
    var desc = _getDescriptionFor(rdf, _nsMp, _nsMp + "RegionInfo");

    var people = GetPeopleSegmentsKeywords(img);
    if (people == null || people.Count == 0) {
      desc?.Element(_nsMp + "RegionInfo")?.Remove();
      return;
    }

    desc ??= _addDescription(rdf);

    if (desc.Element(_nsMp + "RegionInfo") is not { } regionInfo) {
      regionInfo =
        new XElement(_nsMp + "RegionInfo",
          new XAttribute(XNamespace.Xmlns + "MP", _nsMp),
          new XAttribute(XNamespace.Xmlns + "MPRI", _nsMpRi),
          new XAttribute(XNamespace.Xmlns + "MPReg", _nsMpReg));
      desc.Add(regionInfo);
    }

    if (regionInfo.Element(_nsMpRi + "Regions") is not { } regions) {
      regions = new XElement(_nsMpRi + "Regions");
      regionInfo.Add(regions);
    }

    if (regions.Element(_nsRdf + "Bag") is not { } bag) {
      bag = new XElement(_nsRdf + "Bag");
      regions.Add(bag);
    }

    // WARN person can have multiple regions on one image
    // TODO review from here down

    // Index existing regions by PersonDisplayName
    var existing =
      bag.Elements(_nsRdf + "li")?
         .Select(li => li.Element(_nsRdf + "Description"))
         .Where(d => d != null)
         .ToDictionary(
           d => (string?)d.Element(_nsMpReg + "PersonDisplayName"),
           d => d,
           StringComparer.Ordinal
         );

    foreach (var (person, rect, keywords) in people) {
      var name = person?.Name;
      if (string.IsNullOrWhiteSpace(name))
        continue;

      if (!existing.TryGetValue(name, out var rDesc)) {
        // Create new region
        rDesc = new XElement(_nsRdf + "Description",
          new XElement(_nsMpReg + "PersonDisplayName", name));

        bag.Add(new XElement(_nsRdf + "li", rDesc));
      }

      // Rectangle: update only if provided
      if (rect != null) {
        rDesc.Element(_nsMpReg + "Rectangle")?.Remove();
        rDesc.Add(new XElement(_nsMpReg + "Rectangle", rect));
      }

      // RectangleKeywords: replace entirely if provided
      if (keywords != null) {
        rDesc.Element(_nsMpReg + "RectangleKeywords")?.Remove();

        if (keywords.Length > 0) {
          rDesc.Add(
            new XElement(_nsMpReg + "RectangleKeywords",
              new XElement(_nsRdf + "Bag",
                keywords.Select(k => new XElement(_nsRdf + "li", k)))
            )
          );
        }
      }
    }
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