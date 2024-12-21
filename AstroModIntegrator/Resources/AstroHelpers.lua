-- Copyright (c) 2024 AstroTechies, atenfyr. See: https://github.com/atenfyr/AstroModLoader-Classic/blob/master/LICENSE.md

local UEHelpers = require("UEHelpers")

local AstroHelpers = {}

function AstroHelpers.GetIntegratorAPI(ForceInvalidateCache)
    return UEHelpers.CacheDefaultObject("/Game/Integrator/IntegratorAPI.Default__IntegratorAPI_C", "AstroHelpers_IntegratorAPI", ForceInvalidateCache)
end

local statics_cache = nil
function AstroHelpers.GetIntegratorStatics(ForceInvalidateCache)
    if ForceInvalidateCache then statics_cache = nil end

    if statics_cache == nil then
        local statics_raw = {}
        AstroHelpers:GetIntegratorAPI():GetIntegratorStatics(UEHelpers:GetWorldContextObject(), statics_raw)
        statics_cache = statics_raw["IntegratorStatics"]
    end
    return statics_cache
end

function AstroHelpers.GetIntegratorVersion()
    return AstroHelpers.GetIntegratorStatics()["IntegratorVersion"]:ToString()
end

function AstroHelpers.GetGameVersion(ForceInvalidateCache)
    return UEHelpers.CacheDefaultObject("/Script/EngineSettings.Default__GeneralProjectSettings", "AstroHelpers_GeneralProjectSettings", ForceInvalidateCache)["ProjectVersion"]:ToString()
end

function AstroHelpers.Sleep(n)
    os.execute("ping -n " .. tonumber(n + 1) .. " localhost > NUL")
end

return AstroHelpers
