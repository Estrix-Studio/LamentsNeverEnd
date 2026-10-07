using System;
using System.Collections;
using Gameplay.Data;
using LoopShift.Runtime;
using UnityEngine;

namespace Gameplay
{
    /// <summary>Game-only progression adapter; reusable streaming stays in LoopShift.</summary>
    [RequireComponent(typeof(EndlessLevelRunner))]
    public sealed class GameLevelRunner : MonoBehaviour
    {
        [SerializeField] private EndlessLevelRunner runner;
        [SerializeField] private Player player;
        [SerializeField] private CameraController cameraController;

        private IEnumerator Start()
        {
            if (runner == null) runner = GetComponent<EndlessLevelRunner>();
            if (player == null) player = FindAnyObjectByType<Player>();
            if (cameraController == null) cameraController = FindAnyObjectByType<CameraController>();
            if (player == null)
            {
                Debug.LogError("Game level runner requires a Player.", this);
                yield break;
            }
            runner.AddTarget(player.transform);
            if (cameraController != null) runner.AddTarget(cameraController.transform);
            runner.ZoneEntered += OnEntered;
            runner.StartRunner();
            // Room conditions initialize in Start before the first progression notification.
            yield return null;
            if (runner.IsRunning) NotifyRoom(runner.Graph.StartZoneId);
        }

        private void OnEntered(LevelZoneEvent context) => NotifyRoom(context.ZoneId);

        private static void NotifyRoom(string id)
        {
            if (Enum.TryParse(id, out CycleZoneID room) && room != CycleZoneID.None)
                GameData.instance.TriggerOnZoneEntered(room);
        }

        private void OnDestroy()
        {
            if (runner != null) runner.ZoneEntered -= OnEntered;
        }
    }
}
