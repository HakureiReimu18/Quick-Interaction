QuickInteractions = {}

QuickInteractions.AdditionalHooks = {}

QuickInteractions.EnsurePatch = function(class, method, params, patch, hookType)
    local combinedName = class .. "." .. method

    if QuickInteractions.AdditionalHooks[combinedName] == true then 
        --print(combinedName, " Already patched!")
        return 
    end
    QuickInteractions.AdditionalHooks[combinedName] = true

    Hook.Patch('AdditionalHooks', class, method,params, patch, hookType)
end

dofile(... ..  "/Lua/CUI Hooks.lua")