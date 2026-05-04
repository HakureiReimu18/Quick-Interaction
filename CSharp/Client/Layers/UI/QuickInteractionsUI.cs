using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.IO;

using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using QICrabUI;
using QIDependencyInjection;

namespace QuickInteractions
{
  public class QuickInteractionsUI : CUIFrame
  {
    public static string SavePath => Mod.Instance.Paths != null && !string.IsNullOrEmpty(Mod.Instance.Paths.DataUI) ? Path.Combine(Mod.Instance.Paths.DataUI, "QuickInteractionsUI.xml") : string.Empty;
    [Dependency] public GameStageTracker GameStageTracker { get; set; }
    [Dependency] public Logger Logger { get; set; }
    [Dependency] public Debugger Debugger { get; set; }
    [Dependency] public QuickTalk QuickTalk { get; set; }
    [Dependency] public Fabricators Fabricators { get; set; }
    [Dependency] public Debouncer Debouncer { get; set; }

    private bool textVisible;
    public bool TextVisible
    {
      get => textVisible;
      set
      {
        if (blocked) return;
        if (textVisible == value) return;
        textVisible = value;

        //BackgroundColor = value ? new Color(0, 0, 0, 150) : Color.Transparent;

        UpdateAnchor();
        this["layout"].Children.ForEach(c =>
        {
          if (c is QuickTalkButton qtb) qtb.TextVisible = value;
          if (c is FabricatorButton fb) fb.TextVisible = value;
        });
      }
    }

    public CUIDirection ButtonDirection => Real.Left < CUI.GameScreenSize.X / 2.0f ? CUIDirection.Straight : CUIDirection.Reverse;

    public void UpdateAnchor()
    {
      bool onTheLeft = Real.Left < CUI.GameScreenSize.X / 2.0f;
      if (onTheLeft)
      {
        if (Anchor == CUIAnchor.BottomRight)
        {
          Anchor = CUIAnchor.BottomLeft;
          Absolute = Absolute with { Left = Real.Left };
        }
        BackgroundSprite.Effects = SpriteEffects.None;
      }
      else
      {
        if (Anchor == CUIAnchor.BottomLeft)
        {
          Anchor = CUIAnchor.BottomRight;
          Absolute = Absolute with { Left = (Real.Left + Real.Width) - CUI.GameScreenSize.X };
        }
        BackgroundSprite.Effects = SpriteEffects.FlipHorizontally;
      }
    }
    public void CreateUI()
    {
      try
      {
        OutlineColor = Color.Transparent;
        Absolute = new CUINullRect(y: -50);
        Anchor = CUIAnchor.BottomLeft;
        Relative = new CUINullRect(-0.5f, 0);
        Resizible = false;
        FitContent = new CUIBool2(true, true);
        //DragHandle.DragRelative = true;
        DragHandle.OutputRealPos = true;
        
        // 使用 UpdateBackgroundVisibility 来正确设置初始背景
        UpdateBackgroundVisibility();

        this["layout"] = new CUIVerticalList()
        {
          Relative = new CUINullRect(0, 0, 1, 1),
          FitContent = new CUIBool2(true, true),
          Scrollable = true,
          BreakSerialization = true,
          BottomGap = 0,
        };

        //SaveToFile(SavePath);
        LoadSelfFromFile(SavePath);
        
        // 添加切换按钮（在加载保存的UI后添加，避免被覆盖）
        CreateToggleButton();
      }
      catch (Exception ex)
      {
        // 捕获初始化异常，避免影响模组加载
        System.Diagnostics.Debug.WriteLine($"[QuickInteractionsUI] CreateUI error: {ex.Message}");
      }
    }


    /// <summary>
    /// Prevent TextVisible = true until you move the mouse out of the frame
    /// </summary>
    public bool blocked;
    public override void Hydrate()
    {
      OnDrag += (x, y) =>
      {
        bool onTheLeft = x < CUI.GameScreenSize.X / 2.0f;

        if (onTheLeft && BackgroundSprite.Effects == SpriteEffects.FlipHorizontally)
        {
          BackgroundSprite.Effects = SpriteEffects.None;
        }
        if (!onTheLeft && BackgroundSprite.Effects == SpriteEffects.None)
        {
          BackgroundSprite.Effects = SpriteEffects.FlipHorizontally;
        }

        //UpdateAnchor();
        this["layout"].Children.ForEach(c =>
        {
          if (c is CUIHorizontalList button)
          {
            button.Direction = onTheLeft ? CUIDirection.Straight : CUIDirection.Reverse;
          }
        });
      };

      OnMouseLeave += (e) => blocked = false;
      OnMouseOn += (e) => TextVisible = MouseOver;
      OnMouseOff += (e) => TextVisible = MouseOver;

      AddCommand("interact", (o) =>
      {
        TextVisible = false;
        blocked = true;

        if (o is Character character)
        {
          QuickTalk.InteractWith(character);
        }

        if (o is Item item)
        {
          Fabricators.SelectItem(item);
        }
      });
    }

    private bool wasPlayerInPlayerSub = false;
    private bool wasPlayerInOutpost = false;
    private bool iconsVisible = true; // 控制图标显示/隐藏的状态
    
    public void AfterInject()
    {
      // 先创建UI
      CreateUI();
      
      // 检查是否在潜艇编辑器中，如果是则直接显示UI
      if (GameMain.SubEditorScreen != null && Screen.Selected == GameMain.SubEditorScreen)
      {
        Revealed = true;
        ScheduleRefresh(500);
      }
      
      Mod.Instance.OnPluginLoad += () => { Revealed = true; ScheduleRefresh(500); RestartPlayerLocationChecker(); };
      Mod.Instance.OnPluginUnload += () => { SaveToFile(SavePath); };

      GameStageTracker.OnRoundStart += () => { Revealed = true; ScheduleRefresh(500); RestartPlayerLocationChecker(); };
      GameStageTracker.OnRoundStartOrInitialize += () => { Revealed = true; ScheduleRefresh(500); RestartPlayerLocationChecker(); };
      GameStageTracker.OnRoundEnd += () => Revealed = false;

      QuickTalk.CharacterStatusUpdated += (c) => Refresh();
      
      // 延迟启动实时检测，确保所有系统都准备好了
      LuaCsSetup.Instance.Timer.Wait((object[] args) => CheckPlayerLocation(), 1000);
    }

    // 重新启动玩家位置检测定时器
    public void RestartPlayerLocationChecker()
    {
      // 重置状态跟踪变量
      wasPlayerInPlayerSub = false;
      wasPlayerInOutpost = false;
      
      // 重新启动定时器
      CheckPlayerLocation();
    }
    
    // 检查玩家位置是否发生变化，使用递归定时器实现持续检测
    public void CheckPlayerLocation()
    {
      try
      {
        if (!Revealed) return;
        
        bool isPlayerInPlayerSub = Character.Controlled?.Submarine != null && Character.Controlled.Submarine.TeamID == CharacterTeamType.Team1;
        bool isPlayerInOutpost = Level.Loaded != null && Level.Loaded.StartOutpost != null && Character.Controlled?.Submarine == Level.Loaded.StartOutpost;
        
        // 如果玩家位置状态发生变化，刷新UI
        if (isPlayerInPlayerSub != wasPlayerInPlayerSub || isPlayerInOutpost != wasPlayerInOutpost)
        {
          wasPlayerInPlayerSub = isPlayerInPlayerSub;
          wasPlayerInOutpost = isPlayerInOutpost;
          Refresh();
        }
      }
      catch (Exception ex)
      {
        // 捕获位置检测异常
        System.Diagnostics.Debug.WriteLine($"[QuickInteractionsUI] CheckPlayerLocation error: {ex.Message}");
      }
      
      // 使用Wait方法实现持续检测，每500毫秒检查一次
      if (LuaCsSetup.Instance?.Timer != null)
      {
        LuaCsSetup.Instance.Timer.Wait((object[] args) => CheckPlayerLocation(), 500);
      }
    }

    public void ScheduleRefresh(int delay = 100)
    {
      Debugger.Log("ScheduleRefresh", DebugLevel.UIRefresh);
      LuaCsSetup.Instance.Timer.Wait((object[] args) => Refresh(), delay);
    }

    // 创建切换按钮
    private CUIButton toggleButton;
    
    public void CreateToggleButton()
    {
      toggleButton = new CUIButton()
      {
        Text = "-",
        Absolute = new CUINullRect(0, 0, 30, 30),
        BackgroundColor = new Color(100, 100, 100, 200),
        TextColor = Color.White,
        TextScale = 1.5f,
        // 关键：忽略父容器的 IgnoreEvents 设置，保持按钮始终可点击
        IgnoreParentEventIgnorance = true,
      };

      toggleButton.OnClick += (e) =>
      {
        iconsVisible = !iconsVisible;
        toggleButton.Text = iconsVisible ? "-" : "+";
        UpdateBackgroundVisibility();
        Refresh();
      };

      // 将切换按钮添加到主框架，而不是layout中
      this.Append(toggleButton);
    }

    // 更新背景可见性
    public void UpdateBackgroundVisibility()
    {
      try
      {
        if (iconsVisible)
        {
          // 显示背景和布局
          BackgroundColor = new Color(255, 255, 255, 255);
          if (CUI.TextureManager != null)
          {
            BackgroundSprite = CUI.TextureManager.GetCUISprite(4, 1);
          }
          // 恢复容器的交互性
          IgnoreEvents = false;
          if (this["layout"] != null)
          {
            this["layout"].IgnoreEvents = false;
          }
        }
        else
        {
          // 隐藏背景
          BackgroundColor = Color.Transparent;
          BackgroundSprite = null;
          // 禁用主容器的交互性，防止拦截鼠标事件（但 toggleButton 通过 IgnoreParentEventIgnorance 保持可交互）
          IgnoreEvents = true;
          if (this["layout"] != null)
          {
            this["layout"].IgnoreEvents = true;
          }
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"[QuickInteractionsUI] UpdateBackgroundVisibility error: {ex.Message}");
      }
    }

    public void Refresh()
    {
      if (!Revealed) return;

      Debouncer.Debounce("refresh", 100, () =>
      {
        try
        {
          // 如果布局不存在，重新创建
          if (this["layout"] == null)
          {
            this["layout"] = new CUIVerticalList()
            {
              Relative = new CUINullRect(0, 0, 1, 1),
              FitContent = new CUIBool2(true, true),
              Scrollable = true,
              BreakSerialization = true,
              BottomGap = 0,
            };
          }

          if (GUI.DisableHUD)
          {
            ScheduleRefresh(500);
            return;
          }
          Debugger.Log("Refresh", DebugLevel.UIRefresh);

          // 更新背景可见性
          UpdateBackgroundVisibility();

          bool onTheLeft = Real.Left < CUI.GameScreenSize.X / 2.0f;

          this["layout"].RemoveAllChildren();

          // 如果图标被隐藏，则不显示任何内容（除了切换按钮）
          if (!iconsVisible)
          {
            return;
          }

          // 检查是否在潜艇编辑器中
          bool isInSubEditor = GameMain.SubEditorScreen != null && Screen.Selected == GameMain.SubEditorScreen;
          
          // 检查玩家是否在友好位置（起始哨站或信标站）
          bool playerInFriendlyLocation = Utils.IsPlayerInFriendlyLocation;
          // 检查玩家是否在玩家潜艇内
          bool playerInPlayerSub = Character.Controlled?.Submarine != null && Character.Controlled.Submarine.TeamID == CharacterTeamType.Team1;

          // 只有当满足以下条件之一时，才显示NPC按钮：
          // 1. 玩家在友好位置且不在玩家潜艇内（原始条件）
          // 2. 或者玩家在玩家潜艇内（新增例外：支持巡回中玩家潜艇上的NPC）
          if (!isInSubEditor && ((playerInFriendlyLocation && !playerInPlayerSub) || playerInPlayerSub))
          {
            foreach (Character character in QuickTalk.WantToTalk)
            {
              this["layout"].Append(new QuickTalkButton(character, ButtonDirection));
            }

            foreach (Character character in QuickTalk.Merchants)
            {
              this["layout"].Append(new QuickTalkButton(character, ButtonDirection));
            }
          }

          // 收集所有可显示的设备
          List<Item> fabricators = new List<Item>();
          List<Item> deconstructors = new List<Item>();

          foreach (Item item in Item.ItemList)
          {
            if (item?.Prefab?.Identifier != null && item.Submarine != null && 
                (item.Prefab.Identifier.Value == "fabricator" || 
                 item.Prefab.Identifier.Value == "deconstructor" || 
                 item.Prefab.Identifier.Value == "medicalfabricator" ||
                 item.Prefab.Identifier.Value == "deep_general_fabricator" ||
                 item.Prefab.Identifier.Value == "ore_refining_machine" ||
                 item.Prefab.Identifier.Value == "ammoreload_machine" ||
                 item.Prefab.Identifier.Value == "deep_plate_repairtable" ||
                 item.Prefab.Identifier.Value == "tsm_fabricator_ammo" ||
                 item.Prefab.Identifier.Value == "tsm_fabricator" ||
                 item.Prefab.Identifier.Value == "tsm_fabricator_quality" ||
                 item.Prefab.Identifier.Value == "tsm_fabricator_skin" ||
                 item.Prefab.Identifier.Value == "scp_portableammofabricator" ||
                 item.Prefab.Identifier.Value == "scp_portableweaponfabricator" ||
                 item.Prefab.Identifier.Value == "scp_advportableweaponfabricator" ||
                 item.Prefab.Identifier.Value == "scp_chemistrystation" ||
                 item.Prefab.Identifier.Value == "scp_ammofabricator" ||
                 item.Prefab.Identifier.Value == "scp_weaponfabricator" ||
                 item.Prefab.Identifier.Value == "ekutility_placeablefabricator" ||
                 item.Prefab.Identifier.Value == "ekutility_placeablefabricator_adaptive" ||
                 item.Prefab.Identifier.Value == "ekutility_placeablemedicalfabricator" ||
                 item.Prefab.Identifier.Value == "ekutility_placeabledeconstructor" ||
                 item.Prefab.Identifier.Value == "he-cuttingboard" ||
                 item.Prefab.Identifier.Value == "he-oven" ||
                 item.Prefab.Identifier.Value == "he-stove"))
            {
              // 检查设备是否有NonInteractable属性，如果有则忽略
              if (item.NonInteractable)
              {
                continue;
              }
              
              // 检查设备是否在友好位置（起始哨站或信标站）
              bool isInFriendlyLocation = Utils.IsSubmarineInFriendlyLocation(item.Submarine);
              // 检查设备是否在玩家潜艇
              bool isInPlayerSub = item.Submarine.TeamID == CharacterTeamType.Team1;

              // 设备显示规则（严格按位置控制）：
              // - 玩家在自己潜艇上时 → 只显示自己船上的设备
              // - 玩家在哨站时 → 只显示哨站上的设备
              // - 潜艇编辑器中 → 显示所有设备
              if (isInSubEditor || (isInPlayerSub && playerInPlayerSub) || (isInFriendlyLocation && !playerInPlayerSub && playerInFriendlyLocation))
              {
                if (item.Prefab.Identifier.Value == "deconstructor")
                {
                  deconstructors.Add(item);
                }
                else
                {
                  fabricators.Add(item);
                }
              }
            }
          }

          // 先显示所有加工台（fabricator, medicalfabricator, deep_general_fabricator）
          foreach (Item item in fabricators)
          {
            this["layout"].Append(new FabricatorButton(item, ButtonDirection));
          }

          // 再显示所有解构机
          foreach (Item item in deconstructors)
          {
            this["layout"].Append(new FabricatorButton(item, ButtonDirection));
          }
        }
        catch (Exception e)
        {

        }
      });
    }
  }
}
