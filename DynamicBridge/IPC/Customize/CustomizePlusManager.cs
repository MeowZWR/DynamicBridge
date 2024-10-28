﻿using Dalamud.Plugin.Ipc.Exceptions;
using DynamicBridge.Configuration;
using ECommons.ChatMethods;
using ECommons.EzIpcManager;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using System.Collections.Generic;

namespace DynamicBridge.IPC.Customize;
public class CustomizePlusManager
{
    public bool WasSet = false;
    public List<Guid> SavedProfileID = null;
    public Guid LastEnabledProfileID = Guid.Empty;
    public CustomizePlusReflector Reflector;

    [EzIPC("Profile.GetList")] Func<IList<IPCProfileDataTuple>> GetProfileList;
    [EzIPC("Profile.EnableByUniqueId")] Func<Guid, int> EnableProfileByUniqueId;
    [EzIPC("Profile.DisableByUniqueId")] Func<Guid, int> DisableProfileByUniqueId;

    public CustomizePlusManager()
    {
        Reflector = new();
        EzIPC.Init(this, "CustomizePlus", SafeWrapper.AnyException);
    }

    List<PathInfo> PathInfos = null;
    public List<PathInfo> GetCombinedPathes()
    {
        PathInfos ??= Utils.BuildPathes(GetRawPathes());
        return PathInfos;
    }

    public List<string> GetRawPathes()
    {
        var ret = new List<string>();
        try
        {
            foreach (var x in GetProfiles())
            {
                var path = x.VirtualPath;
                if (path != null)
                {
                    ret.Add(path);
                }
            }
        }
        catch (Exception e)
        {
            e.LogInternal();
        }
        return ret;
    }

    IList<IPCProfileDataTuple> Cache = null;
    public IEnumerable<IPCProfileDataTuple> GetProfiles(IEnumerable<string> chara = null)
    {
        Cache ??= GetProfileList();
        var charaSenders = chara?.Select(x => Sender.TryParse(x, out var s) ? s : default);
        foreach (var x in (Cache ?? []))
        {
            if (charaSenders == null ||
                charaSenders.Any(c =>
                    x.Characters.Any(p =>
                        (p.Name == c.Name && p.WorldId == c.HomeWorld) ||
                        (p.Name == c.Name && p.WorldId == 65535))))
            {
                yield return x;
            }
        }
    }


    public void ResetCache()
    {
        Cache = null;
        PathInfos = null;
    }

    public void SetProfile(string profileGuidStr, string charName)
    {
        try
        {
            PluginLog.Information($"Try parse: {charName}");
            if(Sender.TryParse(charName, out var chara))
            {
                var charaProfiles = GetProfiles().Where(x =>
                x.Characters.Any(c =>
                    (c.Name == chara.Name && c.WorldId == chara.HomeWorld) ||
                    (c.Name == chara.Name && c.WorldId == 65535))).ToArray();
                PluginLog.Information($"CharaProfiles: {charaProfiles}");
                if(!WasSet)
                {
                    var enabled = charaProfiles.Where(x => x.IsEnabled);
                    if(enabled.Any())
                    {
                        SavedProfileID = enabled.Select(x => x.UniqueId).ToList();
                    }
                    else
                    {
                        SavedProfileID = null;
                    }
                }
                if(Guid.TryParse(profileGuidStr, out var guid))
                {
                    if(charaProfiles.TryGetFirst(x => x.UniqueId == guid, out var profile))
                    {
                        foreach(var x in charaProfiles)
                        {
                            DisableProfileByUniqueId(x.UniqueId);
                        }
                        EnableProfileByUniqueId(profile.UniqueId);
                        LastEnabledProfileID = profile.UniqueId;
                    }
                }
                else
                {
                    PluginLog.Error($"Could not parse Customize+ profile: {profileGuidStr}. Is customize+ loaded?");
                }
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
        WasSet = true;
    }

    public void RestoreState()
    {
        if (WasSet)
        {
            try
            {
                DisableProfileByUniqueId(LastEnabledProfileID);
                foreach(var x in SavedProfileID)
                {
                    EnableProfileByUniqueId(x);
                }
            }
            catch (Exception e)
            {
                e.Log();
            }
            SavedProfileID = null;
        }
        WasSet = false;
    }



    public string TransformName(string originalName)
    {
        if (Guid.TryParse(originalName, out Guid guid))
        {
            if (GetProfiles().TryGetFirst(x => x.UniqueId == guid, out var entry))
            {
                if (C.GlamourerFullPath)
                {
                    return entry.VirtualPath;
                }
                return entry.Name;
            }
        }
        return originalName;
    }
}
