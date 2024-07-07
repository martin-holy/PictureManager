using System;
using System.Linq;
using System.Text.Json;

namespace MH.Utils.Extensions;

public static class JsonElementExtensions {
  public static bool TryGetPropertySafe(this JsonElement element, string propName, out JsonElement value) {
    if (element.ValueKind == JsonValueKind.Object)
      return element.TryGetProperty(propName, out value);

    value = default;
    return false;
  }

  public static T[] TryGetArray<T>(this JsonElement element, string propName, Func<JsonElement, T> parseElement) =>
    element.TryGetPropertySafe(propName, out var prop) && prop.ValueKind == JsonValueKind.Array
      ? prop.EnumerateArray().Select(parseElement).ToArray() : [];

  public static T[] TryGetArray<T>(this JsonElement element, string propName1, string propName2, Func<JsonElement, T> parseElement) =>
    element.TryGetPropertySafe(propName1, out var prop)
      ? prop.TryGetArray(propName2, parseElement) : [];

  public static bool? TryGetBool(this JsonElement element, string propName, bool? ifNull = null) =>
    element.TryGetPropertySafe(propName, out var prop) && prop.ValueKind is JsonValueKind.True or JsonValueKind.False
      ? prop.GetBoolean() : ifNull;

  public static bool? TryGetBool(this JsonElement element, string propName1, string propName2, bool? ifNull = null) =>
    element.TryGetPropertySafe(propName1, out var prop)
      ? prop.TryGetBool(propName2, ifNull) : ifNull;

  public static double TryGetDouble(this JsonElement element, string propName, double ifNull = 0.0) =>
    element.TryGetPropertySafe(propName, out var prop) && prop.ValueKind == JsonValueKind.Number
      ? prop.TryGetDouble(out var value) ? value : ifNull : ifNull;

  public static double TryGetDouble(this JsonElement element, string propName1, string propName2, double ifNull = 0.0) =>
    element.TryGetPropertySafe(propName1, out var prop)
      ? prop.TryGetDouble(propName2, ifNull) : ifNull;

  public static int TryGetInt32(this JsonElement element, string propName, int ifNull = 0) =>
    element.TryGetPropertySafe(propName, out var prop) && prop.ValueKind == JsonValueKind.Number
      ? prop.TryGetInt32(out var value) ? value : ifNull : ifNull;

  public static int TryGetInt32(this JsonElement element, string propName1, string propName2, int ifNull = 0) =>
    element.TryGetPropertySafe(propName1, out var prop)
      ? prop.TryGetInt32(propName2, ifNull) : ifNull;

  public static T? TryGetObject<T>(this JsonElement element, string propName, Func<JsonElement, T> parseElement) =>
    element.TryGetPropertySafe(propName, out var prop) && prop.ValueKind == JsonValueKind.Object
      ? parseElement(prop) : default;

  public static T? TryGetObject<T>(this JsonElement element, string propName1, string propName2, Func<JsonElement, T> parseElement) =>
    element.TryGetPropertySafe(propName1, out var prop)
      ? prop.TryGetObject(propName2, parseElement) : default;

  public static string? TryGetString(this JsonElement element, string propName) =>
    element.TryGetPropertySafe(propName, out var prop) && prop.ValueKind == JsonValueKind.String
      ? prop.GetString() : null;

  public static string? TryGetString(this JsonElement element, string propName1, string propName2) =>
    element.TryGetPropertySafe(propName1, out var prop)
      ? prop.TryGetString(propName2) : null;

  public static string? TryGetString(this JsonElement element, string propName1, string propName2, string propName3) =>
    element.TryGetPropertySafe(propName1, out var prop)
      ? prop.TryGetString(propName2, propName3) : null;
}