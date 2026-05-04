-- It works, but i think it's too slow
-- I think i should prefer in game performance to load time
QuickInteractions.EnsurePatch("Barotrauma.Character", "CanInteractWith", {
    "Barotrauma.Item",
    "System.Single&",
    "System.Boolean"
  }, 
  function(instance, ptable) 
    local result = Hook.Call("Character_CanInteractWith_Item_Postfix")
    if result == true then 
      ptable.ReturnValue = true
    end
  end, 
  Hook.HookMethodType.After
)

        -- Mod.Harmony.Patch(
        --   original: typeof(Character).GetMethod("CanInteractWith", AccessTools.all, new Type[]{
        --     typeof(Item),
        --     typeof(float).MakeByRefType(),
        --     typeof(bool),
        --   }),
        --   postfix: new HarmonyMethod(typeof(CanInteractWith).GetMethod("Character_CanInteractWith_Postfix"))
        -- );

        -- Mod.Harmony.Patch(
        --   original: typeof(Character).GetMethod("CanInteractWith", AccessTools.all, new Type[]{
        --   typeof(Character),
        --   typeof(float),
        --   typeof(bool),
        --   typeof(bool),
        --   }),
        --   prefix: new HarmonyMethod(typeof(CanInteractWith).GetMethod("Character_CanInteractWith_Prefix"))
        -- );