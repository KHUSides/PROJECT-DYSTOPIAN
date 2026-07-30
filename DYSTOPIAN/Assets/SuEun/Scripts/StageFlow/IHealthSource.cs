using System;

namespace Dystopian.SuEun.StageFlow
{
    /// <summary>
    /// HUD가 체력 구현체와 분리되어 구독할 수 있는 공통 계약입니다.
    /// </summary>
    public interface IHealthSource
    {
        int CurrentHealth { get; }
        int MaxHealth { get; }
        event Action<int, int> HealthChanged;
    }
}
