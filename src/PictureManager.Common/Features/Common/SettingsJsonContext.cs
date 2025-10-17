using System.Text.Json.Serialization;
using PictureManager.Common.Features.Common;

namespace PictureManager.Common.Features.Common;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Settings))]
[JsonSerializable(typeof(CommonSettings))]
[JsonSerializable(typeof(GeoNameSettings))]
[JsonSerializable(typeof(ImagesToVideoSettings))]
[JsonSerializable(typeof(MediaItemSettings))]
[JsonSerializable(typeof(SegmentSettings))]
[JsonSerializable(typeof(MediaViewerSettings))]
internal partial class SettingsJsonContext : JsonSerializerContext { }