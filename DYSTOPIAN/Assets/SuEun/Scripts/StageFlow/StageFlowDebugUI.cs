using UnityEngine;

namespace Dystopian.SuEun.StageFlow
{
    public class StageFlowDebugUI : MonoBehaviour
    {
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private bool showRemainingEnemyUI = true;

        private void OnGUI()
        {
            StageFlowManager manager = StageFlowManager.Instance;
            string activeZoneName = manager != null && manager.ActiveZone != null
                ? manager.ActiveZone.ZoneId
                : "None";

            if (showDebugUI)
            {
                GUILayout.BeginArea(new Rect(16f, 16f, 360f, 140f), GUI.skin.box);
                GUILayout.Label("Stage Flow Prototype");
                GUILayout.Label($"Active Battle: {activeZoneName}");
                GUILayout.Label("B: Start first battle zone");
                GUILayout.Label("K: Defeat one enemy");
                GUILayout.Label("C: Clear active battle zone");
                GUILayout.EndArea();
            }

            if (!showRemainingEnemyUI || manager == null || manager.ActiveZone == null)
                return;

            Rect enemyCountRect = new Rect(Screen.width - 256f, Screen.height - 88f, 240f, 72f);
            GUILayout.BeginArea(enemyCountRect, GUI.skin.box);
            GUILayout.Label("SubStage");
            GUILayout.Label($"Remaining Enemies: {manager.ActiveZone.RemainingEnemyCount}");
            GUILayout.EndArea();
        }
    }
}
