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
  public class QuickTalkButton : CUIHorizontalList
  {
    [Dependency] public static Debugger Debugger { get; set; }
    public static float MotinorScale = 1.0f;
    public static Point IconTextureSize = new Point(139, 75);
    public static float IconProportions = (float)IconTextureSize.X / (float)IconTextureSize.Y;
    public static float ScreenHeightToIconHeight = (float)IconTextureSize.Y / 3072.0f * MotinorScale;
    public static CUINullRect IconSize => new CUINullRect(null, null,
      (float)Math.Round(CUI.GameScreenSize.Y * ScreenHeightToIconHeight * IconProportions),
      (float)Math.Round(CUI.GameScreenSize.Y * ScreenHeightToIconHeight)
    );

    public static float TextScale => CUI.GameScreenSize.Y / 840.0f * MotinorScale;

    public static Color GetButtonColor(Character character)
    {
      if (character.IsDead) return new Color(255, 0, 0);

      Color cl = character.CampaignInteractionType switch
      {
        CampaignMode.InteractionType.Talk => new Color(255, 255, 255),
        CampaignMode.InteractionType.Examine => character.HumanPrefab?.Identifier.Value switch
        {
          "jestmaster" => new Color(255, 64, 255),
          "huskcultecclesiast" => new Color(80, 80, 255),
          _ => new Color(255, 255, 255),
        },
        CampaignMode.InteractionType.Crew => new Color(198, 211, 242),
        CampaignMode.InteractionType.Store => character.HumanPrefab?.Identifier.Value switch
        {
          "merchantnightclub" => new Color(255, 0, 80),
          "merchantmedical" => new Color(255, 130, 130),
          "merchantengineering" => new Color(255, 255, 130),
          "merchantarmory" => new Color(200, 200, 200),
          "merchantclown" => new Color(255, 64, 255),
          "merchanthusk" => new Color(80, 80, 255),
          _ => new Color(255, 200, 170),
        },
        CampaignMode.InteractionType.Upgrade => new Color(106, 250, 115),
        CampaignMode.InteractionType.PurchaseSub => new Color(169, 212, 187),
        CampaignMode.InteractionType.MedicalClinic => new Color(255, 130, 130),
        _ => Color.White,
      };

      // Ye,ok, doesn't work in multiplayer
      Debugger.Log($"Interaction: {character.CampaignInteractionType} Id:{character.HumanPrefab?.Identifier.Value} Color:{character}", DebugLevel.ButtonColor);

      return cl;
    }

    public static CUISprite GetIcon(Character character)
    {
      return character.CampaignInteractionType switch
      {
        CampaignMode.InteractionType.Talk => GetIcon(2),
        CampaignMode.InteractionType.Examine => GetIcon(2),
        CampaignMode.InteractionType.Crew => GetIcon(3),
        CampaignMode.InteractionType.Store => GetIcon(0),
        CampaignMode.InteractionType.Upgrade => GetIcon(4),
        CampaignMode.InteractionType.PurchaseSub => GetIcon(5),
        CampaignMode.InteractionType.MedicalClinic => GetIcon(1),
        _ => GetIcon(2),
      };
    }

    public static CUISprite GetIcon(int x, int y = 0)
    {
      return new CUISprite("Interaction icons.png", new Rectangle(x * IconTextureSize.X, y * IconTextureSize.Y, IconTextureSize.X, IconTextureSize.Y));
    }

    public static string GetInteractionText(Character character)
    {
      string InteractionText = TextManager.Get("CampaignInteraction." + character.CampaignInteractionType).ToString().Replace("[[key]]", "");


      if (character.ActiveConversation != null && character.ActiveConversation != null)
      {
        if (character.ActiveConversation.ParentEvent.Prefab.Identifier.Value.Contains("unlockpath"))
        {
          InteractionText = "Unlock Path";
        }
        if (character.ActiveConversation.ParentEvent.Prefab.Identifier.Value.Contains("missionevent"))
        {
          InteractionText = TextManager.Get("mission").ToString();
        }
      }

      if (TrackTriggerAction.Mapping.ContainsKey(character))
      {
        InteractionText = TrackTriggerAction.Mapping[character].Prefab.Identifier.Value;
      }

      string pname = character.HumanPrefab?.Identifier.Value;
      bool isAManager = pname != null && pname.Contains("outpostmanager");

      LocalizedString name = character.Info?.DisplayName;
      if (character.Info?.Title != "") name = character.Info?.Title;
      if (isAManager) name = TextManager.Get("npctitle.outpostmanager");

      return $"{name} - {InteractionText}";
    }

    public Character character { get; set; }

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

    public QuickTalkButton(Character character, CUIDirection direction) : base()
    {
      FitContent = new CUIBool2(true, true);
      Direction = direction;
      //ResizeToHostHeight = false;

      //To place it in center and prevent rescale
      this["icon"] = Icon = new CUIButton()
      {
        Text = "",
        Border = new CUIBorder(),
        BackgroundSprite = GetIcon(character),
        MasterColorOpaque = GetButtonColor(character),
        //ResizeToSprite = true,
        Absolute = IconSize,
        //this will make it keep proportions, but it won't fit in the gap between crew list and left border, at least on 100% hud scale
        // Relative = new CUINullRect(h:1),
        // CrossRelative = new CUINullRect(w: IconProportions),
      };

      Icon.OnMouseDown += (e) =>
      {
        DispatchUp(new CUICommand("interact", character));
      };

      this["text"] = Text = new CUITextBlock("")
      {
        TextAlign = CUIAnchor.CenterLeft,
        Text = GetInteractionText(character),
        TextScale = TextScale,
        Revealed = false,
        Ghost = new CUIBool2(true, false),
        Absolute = new CUINullRect(null, null, null, 0),
      };

      this.character = character;

    }

  }
}