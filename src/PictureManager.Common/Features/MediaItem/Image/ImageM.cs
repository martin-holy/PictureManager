using PictureManager.Common.Features.Folder;

namespace PictureManager.Common.Features.MediaItem.Image;

public sealed class ImageM(int id, FolderM folder, string fileName) : RealMediaItemM(id, folder, fileName);