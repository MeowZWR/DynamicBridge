using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Core;
public unsafe static class ETimeChecker
{
    internal static long* ET = &CSFramework.Instance()->ClientTime.EorzeaTime;

    public readonly static Dictionary<ETime, string> Names = new()
    {
        [ETime.下午] = "12pm - 5pm",
        [ETime.夜晚] = "10pm - 5am",
        [ETime.黎明] = "5am - 7am",
        [ETime.黄昏] = "5pm - 7pm",
        [ETime.早晨] = "7am - 12pm",
        [ETime.傍晚] = "7pm - 10am",
    };

    public static ETime GetEorzeanTimeInterval() => GetTimeInterval(*ET);

    public static ETime GetTimeInterval(long time)
    {
        var date = DateTimeOffset.FromUnixTimeSeconds(time);
        if (date.Hour < 5) return ETime.夜晚;
        if (date.Hour < 7) return ETime.黎明;
        if (date.Hour < 12) return ETime.早晨;
        if (date.Hour < 17) return ETime.下午;
        if (date.Hour < 19) return ETime.黄昏;
        if (date.Hour < 22) return ETime.傍晚;
        return ETime.夜晚;
    }
}
