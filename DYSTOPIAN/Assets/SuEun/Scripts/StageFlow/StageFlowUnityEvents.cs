using System;
using UnityEngine.Events;

namespace Dystopian.SuEun.StageFlow
{
    [Serializable]
    public sealed class BattleZoneEvent : UnityEvent<BattleZone>
    {
    }

    [Serializable]
    public sealed class BattleZoneEnemyCountEvent : UnityEvent<BattleZone, int>
    {
    }
}
