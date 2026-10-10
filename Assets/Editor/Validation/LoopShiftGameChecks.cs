using System;
using Gameplay;
using LoopShift.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LamentsNeverEnd.Editor
{
    /// <summary>Batch integration checks for the real migrated scene; no test-assembly dependency on Assembly-CSharp.</summary>
    [InitializeOnLoad]
    public static class LoopShiftGameChecks
    {
        private const string Pending = "LoopShift.GameChecks.Pending";
        private const string Failure = "LoopShift.GameChecks.Failure";
        private static readonly LevelDirection[] Path =
        {
            LevelDirection.East, LevelDirection.East, LevelDirection.East, LevelDirection.North,
            LevelDirection.North, LevelDirection.North, LevelDirection.South, LevelDirection.South, LevelDirection.East,
            LevelDirection.North, LevelDirection.South, LevelDirection.East, LevelDirection.East,
            LevelDirection.South, LevelDirection.North, LevelDirection.West, LevelDirection.North
        };
        private static readonly string[] Rooms =
        {
            "Room1_3", "Room1_2", "Room1_1", "Room1_2", "Penyok2", "Room2_1", "Penyok2", "Room2_3",
            "Room2_2", "Room2_4", "Penyok3", "Room3_1", "Room3_2", "Room3_3", "Room3_2", "Room3_4", "Room3_5"
        };
        private static EndlessLevelRunner _runner;
        private static Player _player;
        private static Collider2D _actor;
        private static GameObject _inactiveFade;
        private static GameObject _inactiveInteraction;
        private static int _step, _events, _checks;
        private static float _nextStep;
        private static bool _started, _crossing;
        private static int _preparedStep = -1;
        private static Vector2Int _crossingCell;
        private static int _crossingEvents;
        private static float _crossingDeadline;

        static LoopShiftGameChecks()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += OnLog;
        }

        public static void Run()
        {
            SessionState.SetBool(Pending, true);
            SessionState.SetString(Failure, "");
            SessionState.SetString("LoopShift.GameChecks.Start", DateTime.UtcNow.ToString("O"));
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            GameData.instance.Reset();
            EditorApplication.EnterPlaymode();
        }

        private static void OnLog(string message, string stackTrace, LogType type)
        {
            if (SessionState.GetBool(Pending, false) && (type == LogType.Exception || type == LogType.Error))
                SessionState.SetString(Failure, message + "\n" + stackTrace);
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Pending, false)) return;
            try
            {
                var error = SessionState.GetString(Failure, "");
                if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                if (DateTimeOffset.UtcNow - DateTimeOffset.Parse(SessionState.GetString("LoopShift.GameChecks.Start", "")) > TimeSpan.FromMinutes(3))
                    throw new TimeoutException("Game integration checks timed out.");
                if (!Application.isPlaying || Time.frameCount < 3 || Time.realtimeSinceStartup < _nextStep) return;
                _nextStep = Time.realtimeSinceStartup + 0.25f;
                if (!_started) { BeginChecks(); return; }
                if (_step < Path.Length) { Traverse(); return; }
                if (_step == Path.Length) { CheckPooledState(); _step++; return; }
                if (_step == Path.Length + 1) { EnterVictoryTrigger(); _step++; return; }
                CheckRestartCycle();
            }
            catch (Exception exception) { Finish(1, "LOOPSHIFT GAME CHECKS FAILED: " + exception); }
        }

        private static void BeginChecks()
        {
            if (SceneManager.GetActiveScene().name == "MainMenu")
            {
                var menu = Object.FindFirstObjectByType<UI.MainMenu>();
                Check(menu != null, "Initial main menu exists");
                menu.GoToGameplay();
                return;
            }
            _runner = Object.FindFirstObjectByType<EndlessLevelRunner>();
            _player = Object.FindFirstObjectByType<Player>();
            Check(_runner != null && _runner.IsRunning, "Migrated runner starts");
            Check(_player != null, "Player exists");
            Check(Object.FindFirstObjectByType<LevelTraversal>() == null, "Legacy traversal is absent");
            Check(_runner.Graph.Zones.Count == 15 && _runner.Graph.FixedConnections.Count == 43, "All authored rooms and routes migrated");
            Check(_runner.ActiveZones[_runner.CurrentCell].ZoneId == "Penyok1", "Correct starting room");
            _player.enabled = false;
            _actor = _player.GetComponent<Collider2D>();
            Check(_actor != null, "Registered player collider exists");
            // Each route check enables physics for a local approach to its authored doorway.
            _actor.enabled = false;
            foreach (var zone in _runner.ActiveZones.Values) IgnoreDoorwayEnemies(zone);
            _runner.ZoneSpawned += context => IgnoreDoorwayEnemies(context.Zone);
            GameData.instance.OnZoneEntered += _ => _events++;
            _started = true;
        }

        private static void IgnoreDoorwayEnemies(LevelZone zone)
        {
            foreach (var enemy in zone.GetComponentsInChildren<Enemy>(true))
                foreach (var collider in enemy.GetComponentsInChildren<Collider2D>(true))
                    Physics2D.IgnoreCollision(_actor, collider);
        }
        private static Vector2 FindDoorwayApproach(LevelExit exit, Vector2 direction)
        {
            var tangent = new Vector2(-direction.y, direction.x);
            _actor.enabled = true;
            Physics2D.SyncTransforms();
            var size = (Vector2)_actor.bounds.size;
            var offset = (Vector2)(_actor.bounds.center - _actor.transform.position);
            var triggerBounds = exit.GetComponent<Collider2D>().bounds;
            var halfSpan = Mathf.Abs(tangent.x) * triggerBounds.extents.x + Mathf.Abs(tangent.y) * triggerBounds.extents.y;
            var actorHalfSpan = (Mathf.Abs(tangent.x) * size.x + Mathf.Abs(tangent.y) * size.y) * 0.5f;
            var samples = Mathf.Max(0, Mathf.FloorToInt((halfSpan - actorHalfSpan - 0.05f) / 0.25f));
            for (var index = 0; index <= samples * 2; index++)
            {
                var lateral = index == 0 ? 0 : ((index + 1) / 2) * 0.25f * (index % 2 == 0 ? -1 : 1);
                var start = (Vector2)exit.SpawnAnchor.position + tangent * lateral - direction * 2;
                var hits = Physics2D.BoxCastAll(start + offset, size, 0, direction, 3);
                var blocked = false;
                foreach (var hit in hits)
                {
                    if (hit.collider.isTrigger || hit.collider.attachedRigidbody == _actor.attachedRigidbody ||
                        hit.collider.GetComponentInParent<Enemy>() != null) continue;
                    blocked = true;
                    break;
                }
                if (!blocked) return start;
            }
            var room = _runner.ActiveZones[_runner.CurrentCell];
            foreach (var legacy in Resources.LoadAll<CyclingZone>("Zones"))
            {
                if (legacy.ZoneID.ToString() != room.ZoneId) continue;
                foreach (var trigger in legacy.GetComponentsInChildren<TriggerZone>(true))
                    Debug.Log("DOORWAY ORIGINAL: " + trigger.name + " local=" + legacy.transform.InverseTransformPoint(trigger.transform.position) + " collider=" + trigger.GetComponent<Collider2D>());
            }
            for (var y = 12; y >= 6; y--)
            {
                var row = "";
                for (var x = -19; x <= 19; x++)
                {
                    var solids = Physics2D.OverlapBoxAll((Vector2)room.transform.position + new Vector2(x * 0.5f, y), new Vector2(0.2f, 0.2f), 0);
                    var marker = '.';
                    foreach (var solid in solids)
                    {
                        if (solid.isTrigger || solid.attachedRigidbody == _actor.attachedRigidbody || solid.GetComponentInParent<Enemy>() != null) continue;
                        marker = solid.GetComponentInParent<LevelZone>() == room ? '#' : 'N';
                        break;
                    }
                    row += marker;
                }
                Debug.Log("DOORWAY MAP y=" + y + ": " + row);
            }
            foreach (var collider in room.GetComponentsInChildren<Collider2D>())
            {
                if (collider.isTrigger) continue;
                Debug.Log("DOORWAY SOLID: " + collider.name + " bounds=" + collider.bounds);
            }
            throw new InvalidOperationException("No clear local approach along authored exit " + exit.name + " at step " + _step);
        }
        private static void Traverse()
        {
            Check(_runner != null && _runner.IsRunning, "Doorway probe requires a running Gameplay scene");
            var room = _runner.ActiveZones[_runner.CurrentCell];
            if (!_crossing && _preparedStep != _step)
            {
                _preparedStep = _step;
                foreach (var interaction in room.GetComponentsInChildren<InteractableObject>())
                {
                    var data = new SerializedObject(interaction);
                    var completion = (EventName)data.FindProperty("eventName").intValue;
                    interaction.Interact();
                    Check(completion != EventName.None && GameData.instance.IsEventCompleted(completion), "Authored interaction unlocks progression in " + room.ZoneId);
                }
                return; // Allow authored gate destruction to finish before querying physics.
            }
            if (!_crossing)
            {
                Check(room.TryGetExit(Path[_step], "Default", out var exit), "Exit exists at step " + _step);
                _crossingCell = _runner.CurrentCell;
                _crossingEvents = _events;
                _crossingDeadline = Time.realtimeSinceStartup + 5;
                var direction = (Vector2)LevelDirectionUtility.ToCellOffset(Path[_step]);
                var start = FindDoorwayApproach(exit, direction);
                _player.transform.position = start;
                _actor.attachedRigidbody.position = start;
                _actor.enabled = true;
                _actor.attachedRigidbody.linearVelocity = direction * 4;
                Physics2D.SyncTransforms();
                _crossing = true;
                return;
            }
            if (_runner.CurrentCell == _crossingCell)
            {
                if (Time.realtimeSinceStartup >= _crossingDeadline)
                {
                    var contacts = new ContactPoint2D[16];
                    var count = _actor.GetContacts(contacts);
                    var details = "";
                    for (var i = 0; i < count; i++)
                        details += " [" + contacts[i].collider.name + "/" + contacts[i].otherCollider.name + " at " + contacts[i].point + "]";
                    throw new InvalidOperationException("Physical doorway crossing timed out at step " + _step +
                        ", room=" + room.ZoneId + ", position=" + _actor.attachedRigidbody.position +
                        ", velocity=" + _actor.attachedRigidbody.linearVelocity + ", contacts=" + details);
                }
                return;
            }
            _actor.attachedRigidbody.linearVelocity = Vector2.zero;
            _actor.enabled = false;
            Check(room.ZoneId == Rooms[_step], "Expected destination " + Rooms[_step]);
            Check(_events == _crossingEvents + 1, "Exactly one progression notification");
            Check(room.transform.position.sqrMagnitude < 0.001f, "Room stays at origin");
            Check(_runner.ActiveZones.Count <= 9 && _runner.PooledZoneCount <= 16, "Room counts bounded");
            _player.transform.position = Vector3.zero;
            _actor.attachedRigidbody.position = Vector2.zero;
            _crossing = false;
            _step++;
        }
        private static void CheckPooledState()
        {
            var probe = new GameObject("Enemy lifecycle probe");
            var enemy = probe.AddComponent<Enemy>();
            enemy.SetFollowPlayer(_player);
            enemy.transform.localPosition = new Vector3(8, 4, 0);
            enemy.OnZoneDespawned(default);
            enemy.OnZoneSpawned(default);
            Check(enemy.transform.localPosition == Vector3.zero, "Pooled enemy restores spawn position");
            Object.Destroy(probe);
            _inactiveFade = new GameObject("Inactive dialogue completion");
            var dialogue = _inactiveFade.AddComponent<Krabik>();
            _inactiveFade.SetActive(false);
            dialogue.CompleteEvent();
            _inactiveInteraction = new GameObject("Inactive interaction completion");
            var interaction = _inactiveInteraction.AddComponent<InteractableObject>();
            _inactiveInteraction.SetActive(false);
            interaction.CompleteEvent();
        }

        private static void EnterVictoryTrigger()
        {
            Check(_inactiveFade == null && _inactiveInteraction == null, "Inactive event completion destroys objects without a coroutine");
            var win = _runner.ActiveZones[_runner.CurrentCell].GetComponentInChildren<WinCollider>(true);
            Check(win != null, "Terminal room retains victory component");
            var trigger = win.GetComponent<Collider2D>();
            Check(trigger != null && trigger.enabled && trigger.isTrigger, "Victory collider is active");
            _player.transform.position = trigger.bounds.center;
            _actor.attachedRigidbody.position = trigger.bounds.center;
            _actor.enabled = true;
            Physics2D.SyncTransforms();
        }


        private static void CheckRestartCycle()
        {
            switch (_step - Path.Length)
            {
                case 2:
                    Check(SceneManager.GetActiveScene().name == "WinScreen", "Victory trigger loads WinScreen");
                    GameData.instance.CompleteEvent(EventName.Dialog_1Completed);
                    SceneManager.LoadScene("MainMenu");
                    break;
                case 3:
                case 6:
                    var menu = Object.FindFirstObjectByType<UI.MainMenu>();
                    Check(menu != null, "Main menu is available for restart");
                    menu.GoToGameplay();
                    break;
                case 4:
                    CheckFreshRun();
                    CheckTorchAndEnemyCollision();
                    break;
                case 5:
                    Check(SceneManager.GetActiveScene().name == "LossScreen", "Loss loads LossScreen");
                    SceneManager.LoadScene("MainMenu");
                    break;
                case 7:
                    CheckFreshRun();
                    Finish(0, "LOOPSHIFT GAME CHECKS PASSED: " + _checks);
                    return;
            }
            _step++;
        }

        private static void CheckTorchAndEnemyCollision()
        {
            var player = Object.FindFirstObjectByType<Player>();
            Check(player != null, "Fresh player exists for interaction checks");
            var torch = player.GetComponentInChildren<Utility.TorchController>(true);
            Check(torch != null && torch.isLit, "Player torch starts lit");
            var lights = torch.GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>(true);
            Check(lights.Length > 0, "Torch has authored lights");
            torch.Toggle();
            Check(!torch.isLit && !player.isTorchLit, "Torch off state reaches player");
            foreach (var light in lights) Check(!light.enabled, "Torch light switches off");
            torch.Toggle();
            Check(torch.isLit && player.isTorchLit, "Torch on state reaches player");
            foreach (var light in lights) Check(light.enabled, "Torch light switches on");

            CheckAuthoredDialogue();
            var room = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LoopShiftGame/Room1_1.prefab");
            var authoredEnemy = room.GetComponentInChildren<Enemy>(true);
            Check(authoredEnemy != null, "Authored room contains an enemy");
            var enemy = Object.Instantiate(authoredEnemy.gameObject);
            enemy.SetActive(true);
            var collider = enemy.GetComponent<Collider2D>();
            Check(collider != null && collider.enabled && !collider.isTrigger, "Authored enemy has a solid collision shape");
            enemy.transform.position = player.transform.position;
            Physics2D.SyncTransforms();
        }
        private static void CheckAuthoredDialogue()
        {
            var actor = Object.FindFirstObjectByType<Krabik>();
            var controller = UI.DialogController.instance;
            Check(actor != null && controller != null, "Authored dialogue actor and controller are active");
            Check(controller.nextButton != null && controller.dialogPanel != null, "Dialogue UI references exist");
            var serialized = new SerializedObject(actor);
            var completion = (EventName)serialized.FindProperty("eventName").intValue;
            Check(completion != EventName.None, "Authored dialogue has a progression event");
            var starts = 0;
            var ends = 0;
            Action onStart = () => starts++;
            Action onEnd = () => ends++;
            controller.OnDialogStart += onStart;
            controller.OnDialogEnd += onEnd;
            try
            {
                actor.Interact();
                Check(starts == 1 && controller.dialogPanel.activeSelf, "Interaction opens authored dialogue");
                for (var i = 0; i < 100 && ends == 0; i++) controller.nextButton.onClick.Invoke();
                Check(ends == 1 && !controller.dialogPanel.activeSelf, "Next callback completes and closes dialogue");
                Check(GameData.instance.IsEventCompleted(completion), "Dialogue completion advances progression");
                controller.nextButton.onClick.Invoke();
                Check(ends == 1, "Next after completion does not repeat the end event");
            }
            finally
            {
                controller.OnDialogStart -= onStart;
                controller.OnDialogEnd -= onEnd;
            }
        }
        private static void CheckFreshRun()
        {
            var runner = Object.FindFirstObjectByType<EndlessLevelRunner>();
            Check(SceneManager.GetActiveScene().name == "Gameplay", "Menu loads existing Gameplay build index");
            Check(runner != null && runner.IsRunning, "Runner starts after scene reload");
            Check(runner.ActiveZones[runner.CurrentCell].ZoneId == "Penyok1", "Restart restores first room");
            Check(!GameData.instance.IsEventCompleted(EventName.Dialog_1Completed), "Restart resets progression");
        }

        private static void Check(bool passed, string label)
        {
            if (!passed) throw new InvalidOperationException(label);
            _checks++;
        }

        private static void Finish(int code, string message)
        {
            SessionState.SetBool(Pending, false);
            Debug.Log(message);
            EditorApplication.Exit(code);
        }
    }
}
