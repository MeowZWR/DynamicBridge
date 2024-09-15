using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Core
{
    public static class CharacterStateChecker
    {
        static readonly Dictionary<CharacterState, Func<bool>> States = new()
        {
            [CharacterState.游泳] = () => Svc.Condition[ConditionFlag.Swimming] && Utils.IsMoving,
            [CharacterState.浮水] = () => Svc.Condition[ConditionFlag.Swimming] && !Utils.IsMoving,
            [CharacterState.地面坐骑] = () => Svc.Condition[ConditionFlag.Mounted] && !Svc.Condition[ConditionFlag.InFlight] && !Svc.Condition[ConditionFlag.Diving],
            [CharacterState.水下坐骑] = () => Svc.Condition[ConditionFlag.Mounted] && Svc.Condition[ConditionFlag.Diving],
            [CharacterState.空中坐骑] = () => Svc.Condition[ConditionFlag.Mounted] && Svc.Condition[ConditionFlag.InFlight],
            [CharacterState.潜水] = () => Svc.Condition[ConditionFlag.Diving] && !Svc.Condition[ConditionFlag.Mounted],
            [CharacterState.涉水] = () => !Svc.Condition[ConditionFlag.Diving] && !Svc.Condition[ConditionFlag.Swimming] && Utils.IsInWater,
            [CharacterState.观看过场动画] = () => Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]
                || Svc.Condition[ConditionFlag.WatchingCutscene78],
            [CharacterState.战斗中] = () => Svc.Condition[ConditionFlag.InCombat],
        };

        public static bool Check(this CharacterState state)
        {
            if (States.TryGetValue(state, out var func))
            {
                return func();
            }
            if (EzThrottler.Throttle("ErrorReport", 10000)) DuoLog.Error($"Cound not find checker for state {state}. Please report this error with logs.");
            return false;
        }
    }
}
