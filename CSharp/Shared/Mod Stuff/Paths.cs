using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;

using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;

namespace QuickInteractions
{
  public class ModPaths
  {
    private string modName; public string ModName
    {
      get => modName;
      set
      {
        modName = value;
        FindModDir();
      }
    }
    private string modDir = "";
    public string ModDir
    {
      get => modDir;
      set
      {
        modDir = value;
        if (!string.IsNullOrEmpty(modDir))
        {
          AssetsFolder = Path.Combine(ModDir, "Assets");
          Data = Path.Combine(ModDir, "Data");
          DataUI = Path.Combine(Data, "UI");
          IsInLocalMods = modDir.Contains("LocalMods");
        }
        else
        {
          AssetsFolder = string.Empty;
          Data = string.Empty;
          DataUI = string.Empty;
          IsInLocalMods = false;
        }
      }
    }
    public string AssetsFolder { get; set; }
    public string Data { get; set; }
    public string DataUI { get; set; }
    public bool IsInLocalMods { get; set; }

    public override string ToString() => $"ModDir: {ModDir}\nAssetsFolder: {AssetsFolder}\nData: {Data}\nDataUI: {DataUI}\nIsInLocalMods: {IsInLocalMods}";

    public void FindModDir()
    {
      ContentPackage package = ContentPackageManager.EnabledPackages.All.ToList().Find(
        p => p.Name.Contains(ModName)
      );

      if (package != null) ModDir = Path.GetFullPath(package.Dir);
    }

    public ModPaths() { }
    public ModPaths(string modName) => ModName = modName;

  }

}