using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;

using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;
using QICrabUI;
using QIDependencyInjection;

namespace QuickInteractions
{
  public class FabricatorButton : CUIHorizontalList
  {
    public static Color GetButtonColor(Item item)
    {
      return item.Prefab.Identifier.Value switch
      {
        "fabricator" => new Color(255, 255, 255),
        "medicalfabricator" => new Color(255, 130, 130),
        "deconstructor" => new Color(255, 255, 130),
        "deep_general_fabricator" => new Color(130, 255, 255),
        "ore_refining_machine" => new Color(255, 200, 100),
        "ammoreload_machine" => new Color(150, 200, 255),
        "deep_plate_repairtable" => new Color(200, 150, 255),
        "tsm_fabricator_ammo" => new Color(255, 180, 100),
        "tsm_fabricator" => new Color(200, 255, 200),
        "tsm_fabricator_quality" => new Color(200, 200, 255),
        "tsm_fabricator_skin" => new Color(255, 200, 200),
        "scp_portableammofabricator" => new Color(180, 220, 180),
        "scp_portableweaponfabricator" => new Color(220, 180, 180),
        "scp_advportableweaponfabricator" => new Color(180, 180, 220),
        "scp_chemistrystation" => new Color(150, 255, 150),
        "scp_ammofabricator" => new Color(255, 220, 150),
        "scp_weaponfabricator" => new Color(220, 150, 255),
        "ekutility_placeablefabricator" => new Color(200, 220, 255),
        "ekutility_placeablefabricator_adaptive" => new Color(220, 255, 200),
        "ekutility_placeablemedicalfabricator" => new Color(255, 200, 220),
        "ekutility_placeabledeconstructor" => new Color(220, 220, 180),
        _ => new Color(255, 255, 255),
      };
    }

    public static CUISprite GetIcon(Item item)
    {
      return item.Prefab.Identifier.Value switch
      {
        "fabricator" => GetIcon(0, 1),
        "medicalfabricator" => GetIcon(0, 1),
        "deconstructor" => GetIcon(1, 1),
        "deep_general_fabricator" => GetIcon(0, 1),
        "ore_refining_machine" => GetIcon(0, 1),
        "ammoreload_machine" => GetIcon(0, 1),
        "deep_plate_repairtable" => GetIcon(0, 1),
        "tsm_fabricator_ammo" => GetIcon(0, 1),
        "tsm_fabricator" => GetIcon(0, 1),
        "tsm_fabricator_quality" => GetIcon(0, 1),
        "tsm_fabricator_skin" => GetIcon(0, 1),
        "scp_portableammofabricator" => GetIcon(0, 1),
        "scp_portableweaponfabricator" => GetIcon(0, 1),
        "scp_advportableweaponfabricator" => GetIcon(0, 1),
        "scp_chemistrystation" => GetIcon(0, 1),
        "scp_ammofabricator" => GetIcon(0, 1),
        "scp_weaponfabricator" => GetIcon(0, 1),
        "ekutility_placeablefabricator" => GetIcon(0, 1),
        "ekutility_placeablefabricator_adaptive" => GetIcon(0, 1),
        "ekutility_placeablemedicalfabricator" => GetIcon(0, 1),
        "ekutility_placeabledeconstructor" => GetIcon(1, 1),
        _ => GetIcon(0, 1),
      };
    }

    public static CUISprite GetIcon(int x, int y) => QuickTalkButton.GetIcon(x, y);

    public static string GetInteractionText(Item item)
    {
      return $"{item.Prefab.Name}";
    }

    public Item item { get; set; }

    public bool TextVisible
    {
      get => Text.Parent != null;
      set
      {
        if (value)
        {
          Text.Absolute = new CUINullRect(null, null, null, null);
          Text.Ghost = new CUIBool2(false, false);
          Text.Revealed = true;
          //if (Text.Parent == null) Append(Text);
        }
        else
        {
          //if (Text.Parent != null) RemoveChild(Text);
          //Text.GhostText = true;
          Text.Revealed = false;
          Text.Ghost = new CUIBool2(true, false);
          Text.Absolute = new CUINullRect(null, null, null, 0);
        }
      }
    }

    public CUIButton Icon;
    public CUITextBlock Text;

    public FabricatorButton(Item item, CUIDirection direction) : base()
    {
      FitContent = new CUIBool2(true, true);
      Direction = direction;

      this["icon"] = Icon = new CUIButton()
      {
        Text = "",
        Border = new CUIBorder(),
        BackgroundSprite = GetIcon(item),
        MasterColorOpaque = GetButtonColor(item),
        Absolute = QuickTalkButton.IconSize,
        //ResizeToSprite = true,
      };

      Icon.OnMouseDown += (e) =>
      {
        DispatchUp(new CUICommand("interact", item));
      };

      this["text"] = Text = new CUITextBlock("")
      {
        TextAlign = CUIAnchor.CenterLeft,
        Text = GetInteractionText(item),
        TextScale = QuickTalkButton.TextScale,
        Revealed = false,
        Ghost = new CUIBool2(true, false),
        Absolute = new CUINullRect(null, null, null, 0),
      };


      this.item = item;
    }

  }
}