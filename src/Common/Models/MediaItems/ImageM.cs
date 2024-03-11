namespace PictureManager.Common.Models.MediaItems;

public sealed class ImageM(int id, FolderM folder, string fileName) : RealMediaItemM(id, folder, fileName);