using LoopShift.Runtime;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LoopShift.Samples.MinimalTopDown
{
	public sealed class SampleTutorialUI : MonoBehaviour
	{
		[SerializeField] private EndlessLevelRunner runner;
		[SerializeField] private Transform player;

		private int _enteredCount;
		private int _spawnedCount;
		private int _despawnedCount;
		private string _currentZoneId = "Starting...";
		private Vector2Int _currentCell;
		private GUIStyle _headerStyle;
		private GUIStyle _bodyStyle;
		private GUIStyle _successStyle;

		private void OnEnable()
		{
			if (runner == null)
				return;

			runner.ZoneSpawned += OnZoneSpawned;
			runner.ZoneEntered += OnZoneEntered;
			runner.ZoneBecameCenter += OnZoneBecameCenter;
			runner.ZoneDespawned += OnZoneDespawned;
		}

		private void OnDisable()
		{
			if (runner == null)
				return;

			runner.ZoneSpawned -= OnZoneSpawned;
			runner.ZoneEntered -= OnZoneEntered;
			runner.ZoneBecameCenter -= OnZoneBecameCenter;
			runner.ZoneDespawned -= OnZoneDespawned;
		}

		private void Update()
		{
			if (RestartPressed())
				RestartSample();
		}

		private void OnGUI()
		{
			BuildStyles();
			var width = Mathf.Min(470f, Screen.width - 32f);
			GUILayout.BeginArea(new Rect(16f, 16f, width, Screen.height - 32f), GUI.skin.box);

			GUILayout.Label("LOOPSHIFT — GUIDED SAMPLE", _headerStyle);
			GUILayout.Space(6f);
			GUILayout.Label(
				"This scene shows the three core pieces: a LevelZone prefab, LevelExit triggers, and an EndlessLevelRunner driven by a LevelGraphAsset.",
				_bodyStyle);

			GUILayout.Space(10f);
			GUILayout.Label("TRY IT", _headerStyle);
			GUILayout.Label("1. Move with WASD or the arrow keys.\n2. Cross either cyan EXIT strip.\n3. Watch the runner center the next zone and stream another one ahead.", _bodyStyle);

			GUILayout.Space(10f);
			if (_enteredCount == 0)
			{
				GUILayout.Label("Next goal: cross an exit to trigger your first transition.", _bodyStyle);
			}
			else
			{
				GUILayout.Label("Transition complete! The old outer zone is returned to the prefab pool and can be reused.", _successStyle);
			}

			GUILayout.Space(10f);
			GUILayout.Label("LIVE RUNNER STATE", _headerStyle);
			GUILayout.Label(
				$"Current zone: {_currentZoneId}\nCurrent grid cell: {_currentCell}\nActive zones: {(runner != null ? runner.ActiveZones.Count : 0)}\nEntered: {_enteredCount}   Spawn events: {_spawnedCount}   Despawn events: {_despawnedCount}",
				_bodyStyle);

			GUILayout.Space(10f);
			GUILayout.Label("After Play mode, inspect SampleZone.prefab, MinimalTopDownGraph, and LoopShift Runner in that order.", _bodyStyle);
			GUILayout.Space(6f);
			if (GUILayout.Button("Restart sample (R)"))
				RestartSample();

			GUILayout.EndArea();
		}

		private void OnZoneSpawned(LevelZoneEvent evt)
		{
			_spawnedCount++;
		}

		private void OnZoneEntered(LevelZoneEvent evt)
		{
			_enteredCount++;
		}

		private void OnZoneBecameCenter(LevelZoneEvent evt)
		{
			_currentZoneId = evt.ZoneId;
			_currentCell = evt.Cell;
		}

		private void OnZoneDespawned(LevelZoneEvent evt)
		{
			_despawnedCount++;
		}

		private void RestartSample()
		{
			if (runner == null)
				return;

			runner.StopRunner();
			if (player != null)
			{
				player.position = runner.transform.position;
				var body = player.GetComponent<Rigidbody2D>();
				if (body != null)
#if UNITY_6000_0_OR_NEWER
					body.linearVelocity = Vector2.zero;
#else
					body.velocity = Vector2.zero;
#endif
			}

			_enteredCount = 0;
			_spawnedCount = 0;
			_despawnedCount = 0;
			_currentZoneId = "Starting...";
			_currentCell = Vector2Int.zero;
			runner.StartRunner();
		}

		private void BuildStyles()
		{
			if (_headerStyle != null)
				return;

			_headerStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 15,
				fontStyle = FontStyle.Bold,
				wordWrap = true
			};
			_bodyStyle = new GUIStyle(GUI.skin.label)
			{
				fontSize = 13,
				wordWrap = true
			};
			_successStyle = new GUIStyle(_bodyStyle);
			_successStyle.normal.textColor = new Color(0.35f, 1f, 0.65f);
			_successStyle.fontStyle = FontStyle.Bold;
		}

		private static bool RestartPressed()
		{
#if ENABLE_INPUT_SYSTEM
			return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
			return Input.GetKeyDown(KeyCode.R);
#endif
		}
	}
}
