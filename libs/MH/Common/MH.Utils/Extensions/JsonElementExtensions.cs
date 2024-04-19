using System;
using System.Linq;
using System.Text.Json;

namespace MH.Utils.Extensions;

public static class JsonElementExtensions {
  public static bool TryGetProperty(this JsonElement element, string[] propertyNames, out JsonElement value) {
    var elm = element;

    foreach (var propName in propertyNames) {
      if (elm.TryGetProperty(propName, out elm)) continue;
      value = default;
      return false;
    }

    value = elm;
    return true;
  }

  public static string TryGetString(this JsonElement element, string propertyName, string ifNull = null) =>
    element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
      ? prop.GetString() : ifNull;

  public static string TryGetString(this JsonElement element, string[] propertyNames, string ifNull = null) =>
    element.TryGetProperty(propertyNames, out var prop) && prop.ValueKind == JsonValueKind.String
      ? prop.GetString() : ifNull;

  public static int TryGetInt32(this JsonElement element, string propertyName, int ifNull = 0) =>
    element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number
      ? prop.TryGetInt32(out var value) ? value : ifNull : ifNull;

  public static int TryGetInt32(this JsonElement element, string[] propertyNames, int ifNull = 0) =>
    element.TryGetProperty(propertyNames, out var prop) && prop.ValueKind == JsonValueKind.Number
      ? prop.TryGetInt32(out var value) ? value : ifNull : ifNull;

  public static double TryGetDouble(this JsonElement element, string propertyName, double ifNull = 0.0) =>
    element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number
      ? prop.TryGetDouble(out var value) ? value : ifNull : ifNull;

  public static double TryGetDouble(this JsonElement element, string[] propertyNames, double ifNull = 0.0) =>
    element.TryGetProperty(propertyNames, out var prop) && prop.ValueKind == JsonValueKind.Number
      ? prop.TryGetDouble(out var value) ? value : ifNull : ifNull;

  public static T[] TryGetArray<T>(this JsonElement element, string propertyName, Func<JsonElement, T> parseElement) =>
    element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array
      ? prop.EnumerateArray().Select(parseElement).ToArray() : [];

  public static T[] TryGetArray<T>(this JsonElement element, string[] propertyNames, Func<JsonElement, T> parseElement) =>
    element.TryGetProperty(propertyNames, out var prop) && prop.ValueKind == JsonValueKind.Array
      ? prop.EnumerateArray().Select(parseElement).ToArray() : [];
}