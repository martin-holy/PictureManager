﻿using MH.Utils.Interfaces;

namespace PictureManager.Common.Models; 

public class DriveM(int id, string name, ITreeItem parent, string sn) : FolderM(id, name, parent) {
  public string SerialNumber { get; set; } = sn;
}