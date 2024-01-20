using System.Collections.Generic;

namespace MH.UI.HelperClasses;

public record VfsVideo(string FilePath, int Rotation, List<VfsFrame> Frames);