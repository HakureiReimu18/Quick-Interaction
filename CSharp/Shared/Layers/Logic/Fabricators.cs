using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;

using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;
using QIDependencyInjection;
using Barotrauma.Items.Components;

namespace QuickInteractions
{
  [Singleton]
  public partial class Fabricators
  {
    [Dependency] public Logger Logger { get; set; }
    [Dependency] public GameStageTracker GameStageTracker { get; set; }

    // Dirty, but it's much simpler than removing this https://github.com/evilfactory/LuaCsForBarotrauma/blob/6da26ffa93eb1d94b8fec4add1847879e6b1c75d/Barotrauma/BarotraumaShared/SharedSource/Characters/Animation/HumanoidAnimController.cs#L428
    public void MakeUngrabbable(Item item)
    {
      if (item?.Prefab != null)
      {
        item.Prefab.GrabWhenSelected = false;
      }
    }

    public void RestoreGrabability()
    {
      if (OutpostFabricator?.Prefab != null) OutpostFabricator.Prefab.GrabWhenSelected = true;
      if (OutpostMedFabricator?.Prefab != null) OutpostMedFabricator.Prefab.GrabWhenSelected = true;
      if (OutpostDeconstructor?.Prefab != null) OutpostDeconstructor.Prefab.GrabWhenSelected = true;
      if (OutpostDeepGeneralFabricator?.Prefab != null) OutpostDeepGeneralFabricator.Prefab.GrabWhenSelected = true;
      if (OutpostOreRefiningMachine?.Prefab != null) OutpostOreRefiningMachine.Prefab.GrabWhenSelected = true;
      if (OutpostAmmoReloadMachine?.Prefab != null) OutpostAmmoReloadMachine.Prefab.GrabWhenSelected = true;
      if (OutpostDeepPlateRepairtable?.Prefab != null) OutpostDeepPlateRepairtable.Prefab.GrabWhenSelected = true;
      if (OutpostTsmFabricatorAmmo?.Prefab != null) OutpostTsmFabricatorAmmo.Prefab.GrabWhenSelected = true;
      if (OutpostTsmFabricator?.Prefab != null) OutpostTsmFabricator.Prefab.GrabWhenSelected = true;
      if (OutpostTsmFabricatorQuality?.Prefab != null) OutpostTsmFabricatorQuality.Prefab.GrabWhenSelected = true;
      if (OutpostTsmFabricatorSkin?.Prefab != null) OutpostTsmFabricatorSkin.Prefab.GrabWhenSelected = true;
      if (OutpostScpPortableAmmoFabricator?.Prefab != null) OutpostScpPortableAmmoFabricator.Prefab.GrabWhenSelected = true;
      if (OutpostScpPortableWeaponFabricator?.Prefab != null) OutpostScpPortableWeaponFabricator.Prefab.GrabWhenSelected = true;
      if (OutpostScpAdvPortableWeaponFabricator?.Prefab != null) OutpostScpAdvPortableWeaponFabricator.Prefab.GrabWhenSelected = true;
      if (OutpostScpChemistryStation?.Prefab != null) OutpostScpChemistryStation.Prefab.GrabWhenSelected = true;
      if (OutpostScpAmmoFabricator?.Prefab != null) OutpostScpAmmoFabricator.Prefab.GrabWhenSelected = true;
      if (OutpostScpWeaponFabricator?.Prefab != null) OutpostScpWeaponFabricator.Prefab.GrabWhenSelected = true;
      if (OutpostEkuPlaceableFabricator?.Prefab != null) OutpostEkuPlaceableFabricator.Prefab.GrabWhenSelected = true;
      if (OutpostEkuPlaceableFabricatorAdaptive?.Prefab != null) OutpostEkuPlaceableFabricatorAdaptive.Prefab.GrabWhenSelected = true;
      if (OutpostEkuPlaceableMedicalFabricator?.Prefab != null) OutpostEkuPlaceableMedicalFabricator.Prefab.GrabWhenSelected = true;
      if (OutpostEkuPlaceableDeconstructor?.Prefab != null) OutpostEkuPlaceableDeconstructor.Prefab.GrabWhenSelected = true;
      if (HeCuttingboard?.Prefab != null) HeCuttingboard.Prefab.GrabWhenSelected = true;
      if (HeOven?.Prefab != null) HeOven.Prefab.GrabWhenSelected = true;
      if (HeStove?.Prefab != null) HeStove.Prefab.GrabWhenSelected = true;
    }

    private bool searchedThisRound = false;
    // Too lazy to dry it
    public void FindFabricators()
    {
      if (!Utils.RoundIsLive) return;

      if (searchedThisRound) return;
      searchedThisRound = true;

      foreach (Item item in Item.ItemList)
      {
        if (item?.Prefab?.Identifier == null || item.Submarine == null) continue;
        
        // 只查找友好位置（起始哨站或信标站）或玩家潜艇内的设备
#if CLIENT
        bool isInFriendlyLocation = Utils.IsSubmarineInFriendlyLocation(item.Submarine);
#else
        bool isInFriendlyLocation = Level.Loaded != null && item.Submarine == Level.Loaded.StartOutpost;
#endif
        bool isInPlayerSub = item.Submarine.TeamID == CharacterTeamType.Team1;
        
        if (!isInFriendlyLocation && !isInPlayerSub) continue;
        
        if (item.Prefab.Identifier.Value == "fabricator")
        {
          OutpostFabricator = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "medicalfabricator")
        {
          outpostMedFabricator = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "deconstructor")
        {
          outpostDeconstructor = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "deep_general_fabricator")
        {
          outpostDeepGeneralFabricator = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "ore_refining_machine")
        {
          outpostOreRefiningMachine = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "ammoreload_machine")
        {
          outpostAmmoReloadMachine = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "deep_plate_repairtable")
        {
          outpostDeepPlateRepairtable = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "tsm_fabricator_ammo")
        {
          outpostTsmFabricatorAmmo = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "tsm_fabricator")
        {
          outpostTsmFabricator = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "tsm_fabricator_quality")
        {
          outpostTsmFabricatorQuality = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "tsm_fabricator_skin")
        {
          outpostTsmFabricatorSkin = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "scp_portableammofabricator")
        {
          outpostScpPortableAmmoFabricator = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "scp_portableweaponfabricator")
        {
          outpostScpPortableWeaponFabricator = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "scp_advportableweaponfabricator")
        {
          outpostScpAdvPortableWeaponFabricator = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "scp_chemistrystation")
        {
          outpostScpChemistryStation = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "scp_ammofabricator")
        {
          outpostScpAmmoFabricator = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "scp_weaponfabricator")
        {
          outpostScpWeaponFabricator = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "ekutility_placeablefabricator")
        {
          outpostEkuPlaceableFabricator = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "ekutility_placeablefabricator_adaptive")
        {
          outpostEkuPlaceableFabricatorAdaptive = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "ekutility_placeablemedicalfabricator")
        {
          outpostEkuPlaceableMedicalFabricator = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "ekutility_placeabledeconstructor")
        {
          outpostEkuPlaceableDeconstructor = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "he-cuttingboard")
        {
          HeCuttingboard = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "he-oven")
        {
          HeOven = item;
          MakeUngrabbable(item);
        }

        if (item.Prefab.Identifier.Value == "he-stove")
        {
          HeStove = item;
          MakeUngrabbable(item);
        }
      }
    }

    public List<string> ItemsToFind = new List<string>()
    {
      "fabricator",
      "medicalfabricator",
      "deconstructor",
      "deep_general_fabricator",
      "ore_refining_machine",
      "ammoreload_machine",
      "deep_plate_repairtable",
      "tsm_fabricator_ammo",
      "tsm_fabricator",
      "tsm_fabricator_quality",
      "tsm_fabricator_skin",
      "scp_portableammofabricator",
      "scp_portableweaponfabricator",
      "scp_advportableweaponfabricator",
      "scp_chemistrystation",
      "scp_ammofabricator",
      "scp_weaponfabricator",
      "ekutility_placeablefabricator",
      "ekutility_placeablefabricator_adaptive",
      "ekutility_placeablemedicalfabricator",
      "ekutility_placeabledeconstructor",
      "he-cuttingboard",
      "he-oven",
      "he-stove",
    };

    private Item outpostDeconstructor;
    public Item OutpostDeconstructor
    {
      get
      {
        FindFabricators();
        return outpostDeconstructor;
      }
      set => outpostDeconstructor = value;
    }

    private Item outpostFabricator;
    public Item OutpostFabricator
    {
      get
      {
        FindFabricators();
        return outpostFabricator;
      }
      set => outpostFabricator = value;
    }

    private Item outpostMedFabricator;
    public Item OutpostMedFabricator
    {
      get
      {
        FindFabricators();
        return outpostMedFabricator;
      }
      set => outpostMedFabricator = value;
    }

    private Item outpostDeepGeneralFabricator;
    public Item OutpostDeepGeneralFabricator
    {
      get
      {
        FindFabricators();
        return outpostDeepGeneralFabricator;
      }
      set => outpostDeepGeneralFabricator = value;
    }

    private Item outpostOreRefiningMachine;
    public Item OutpostOreRefiningMachine
    {
      get
      {
        FindFabricators();
        return outpostOreRefiningMachine;
      }
      set => outpostOreRefiningMachine = value;
    }

    private Item outpostAmmoReloadMachine;
    public Item OutpostAmmoReloadMachine
    {
      get
      {
        FindFabricators();
        return outpostAmmoReloadMachine;
      }
      set => outpostAmmoReloadMachine = value;
    }

    private Item outpostDeepPlateRepairtable;
    public Item OutpostDeepPlateRepairtable
    {
      get
      {
        FindFabricators();
        return outpostDeepPlateRepairtable;
      }
      set => outpostDeepPlateRepairtable = value;
    }

    private Item outpostTsmFabricatorAmmo;
    public Item OutpostTsmFabricatorAmmo
    {
      get
      {
        FindFabricators();
        return outpostTsmFabricatorAmmo;
      }
      set => outpostTsmFabricatorAmmo = value;
    }

    private Item outpostTsmFabricator;
    public Item OutpostTsmFabricator
    {
      get
      {
        FindFabricators();
        return outpostTsmFabricator;
      }
      set => outpostTsmFabricator = value;
    }

    private Item outpostTsmFabricatorQuality;
    public Item OutpostTsmFabricatorQuality
    {
      get
      {
        FindFabricators();
        return outpostTsmFabricatorQuality;
      }
      set => outpostTsmFabricatorQuality = value;
    }

    private Item outpostTsmFabricatorSkin;
    public Item OutpostTsmFabricatorSkin
    {
      get
      {
        FindFabricators();
        return outpostTsmFabricatorSkin;
      }
      set => outpostTsmFabricatorSkin = value;
    }

    private Item outpostScpPortableAmmoFabricator;
    public Item OutpostScpPortableAmmoFabricator
    {
      get
      {
        FindFabricators();
        return outpostScpPortableAmmoFabricator;
      }
      set => outpostScpPortableAmmoFabricator = value;
    }

    private Item outpostScpPortableWeaponFabricator;
    public Item OutpostScpPortableWeaponFabricator
    {
      get
      {
        FindFabricators();
        return outpostScpPortableWeaponFabricator;
      }
      set => outpostScpPortableWeaponFabricator = value;
    }

    private Item outpostScpAdvPortableWeaponFabricator;
    public Item OutpostScpAdvPortableWeaponFabricator
    {
      get
      {
        FindFabricators();
        return outpostScpAdvPortableWeaponFabricator;
      }
      set => outpostScpAdvPortableWeaponFabricator = value;
    }

    private Item outpostScpChemistryStation;
    public Item OutpostScpChemistryStation
    {
      get
      {
        FindFabricators();
        return outpostScpChemistryStation;
      }
      set => outpostScpChemistryStation = value;
    }

    private Item outpostScpAmmoFabricator;
    public Item OutpostScpAmmoFabricator
    {
      get
      {
        FindFabricators();
        return outpostScpAmmoFabricator;
      }
      set => outpostScpAmmoFabricator = value;
    }

    private Item outpostScpWeaponFabricator;
    public Item OutpostScpWeaponFabricator
    {
      get
      {
        FindFabricators();
        return outpostScpWeaponFabricator;
      }
      set => outpostScpWeaponFabricator = value;
    }

    private Item outpostEkuPlaceableFabricator;
    public Item OutpostEkuPlaceableFabricator
    {
      get
      {
        FindFabricators();
        return outpostEkuPlaceableFabricator;
      }
      set => outpostEkuPlaceableFabricator = value;
    }

    private Item outpostEkuPlaceableFabricatorAdaptive;
    public Item OutpostEkuPlaceableFabricatorAdaptive
    {
      get
      {
        FindFabricators();
        return outpostEkuPlaceableFabricatorAdaptive;
      }
      set => outpostEkuPlaceableFabricatorAdaptive = value;
    }

    private Item outpostEkuPlaceableMedicalFabricator;
    public Item OutpostEkuPlaceableMedicalFabricator
    {
      get
      {
        FindFabricators();
        return outpostEkuPlaceableMedicalFabricator;
      }
      set => outpostEkuPlaceableMedicalFabricator = value;
    }

    private Item outpostEkuPlaceableDeconstructor;
    public Item OutpostEkuPlaceableDeconstructor
    {
      get
      {
        FindFabricators();
        return outpostEkuPlaceableDeconstructor;
      }
      set => outpostEkuPlaceableDeconstructor = value;
    }

    private Item heCuttingboard;
    public Item HeCuttingboard
    {
      get
      {
        FindFabricators();
        return heCuttingboard;
      }
      set => heCuttingboard = value;
    }

    private Item heOven;
    public Item HeOven
    {
      get
      {
        FindFabricators();
        return heOven;
      }
      set => heOven = value;
    }

    private Item heStove;
    public Item HeStove
    {
      get
      {
        FindFabricators();
        return heStove;
      }
      set => heStove = value;
    }

    public void AfterInject()
    {
      Mod.Instance.OnPluginLoad += FindFabricators;
      GameStageTracker.OnRoundStart += () =>
      {
        searchedThisRound = false;
        FindFabricators();
      };
      Mod.Instance.OnPluginUnload += RestoreGrabability;
      GameStageTracker.OnRoundEnd += () =>
      {
        RestoreGrabability();
        searchedThisRound = false;
        OutpostFabricator = null;
        OutpostDeconstructor = null;
        OutpostMedFabricator = null;
        OutpostDeepGeneralFabricator = null;
        OutpostOreRefiningMachine = null;
        OutpostAmmoReloadMachine = null;
        OutpostDeepPlateRepairtable = null;
        OutpostTsmFabricatorAmmo = null;
        OutpostTsmFabricator = null;
        OutpostTsmFabricatorQuality = null;
        OutpostTsmFabricatorSkin = null;
        OutpostScpPortableAmmoFabricator = null;
        OutpostScpPortableWeaponFabricator = null;
        OutpostScpAdvPortableWeaponFabricator = null;
        OutpostScpChemistryStation = null;
        OutpostScpAmmoFabricator = null;
        OutpostScpWeaponFabricator = null;
        OutpostEkuPlaceableFabricator = null;
        OutpostEkuPlaceableFabricatorAdaptive = null;
        OutpostEkuPlaceableMedicalFabricator = null;
        OutpostEkuPlaceableDeconstructor = null;
        HeCuttingboard = null;
        HeOven = null;
        HeStove = null;
      };
    }
  }
}