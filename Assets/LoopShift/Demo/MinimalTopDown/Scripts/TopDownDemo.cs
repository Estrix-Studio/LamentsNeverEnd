using LoopShift.Runtime;
using UnityEngine;

namespace LoopShift.Samples
{
    /// <summary>Dependency-free demo controls; replace with your game's input and camera systems.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class TopDownDemo : MonoBehaviour
    {
        [SerializeField] private EndlessLevelRunner runner;
        [SerializeField] private Camera followCamera;
        [SerializeField] private float speed = 4;
        private Rigidbody2D _body;
        private Vector2 _direction;
        private Vector2 _buttonDirection;
        private bool _north, _south, _east, _west;
        private bool _autoWalk;
        private int _visits;
        private string _room = "";
        public int Visits => _visits;

        private void Awake() => _body = GetComponent<Rigidbody2D>();
        private void OnEnable()
        {
            if (runner != null) runner.ZoneEntered += OnEntered;
        }
        private void OnDisable()
        {
            if (runner != null) runner.ZoneEntered -= OnEntered;
            ClearInput();
        }
        private void OnEntered(LevelZoneEvent context) { _visits++; _room = context.ZoneId; }
        private void OnApplicationFocus(bool focused) { if (!focused) ClearInput(); }
        private void ClearInput() { _north = _south = _east = _west = false; _buttonDirection = Vector2.zero; }
        private void FixedUpdate()
        {
            if (runner == null || !runner.IsRunning) return;
            _direction = _autoWalk ? Vector2.right : new Vector2((_east ? 1 : 0) - (_west ? 1 : 0), (_north ? 1 : 0) - (_south ? 1 : 0));
            if (!_autoWalk && _buttonDirection != Vector2.zero) _direction = _buttonDirection;
            _body.MovePosition(_body.position + _direction.normalized * (speed * Time.fixedDeltaTime));
        }
        private void LateUpdate()
        {
            if (followCamera != null) followCamera.transform.position = new Vector3(transform.position.x, transform.position.y, -10);
        }
        private void OnGUI()
        {
            HandleKeyboard(Event.current);
            GUILayout.BeginArea(new Rect(16, 16, 300, 260), GUI.skin.box);
            GUILayout.Label("LOOPSHIFT / Endless rooms");
            GUILayout.Label("WASD / arrows, or hold a direction below");
            _buttonDirection = Vector2.zero;
            if (GUILayout.RepeatButton("North")) _buttonDirection = Vector2.up;
            GUILayout.BeginHorizontal();
            if (GUILayout.RepeatButton("West")) _buttonDirection = Vector2.left;
            if (GUILayout.RepeatButton("East")) _buttonDirection = Vector2.right;
            GUILayout.EndHorizontal();
            if (GUILayout.RepeatButton("South")) _buttonDirection = Vector2.down;
            _autoWalk = GUILayout.Toggle(_autoWalk, "Auto-walk east");
            if (runner != null)
            {
                GUILayout.Label("Room: " + (string.IsNullOrEmpty(_room) ? runner.Graph.StartZoneId : _room));
                GUILayout.Label("Transitions: " + _visits + " | Active: " + runner.ActiveZones.Count + " | Pooled: " + runner.PooledZoneCount);
                if (GUILayout.Button("Restart seeded run"))
                {
                    runner.StopRunner();
                    _body.position = Vector2.zero;
                    transform.position = Vector3.zero;
                    _visits = 0;
                    _room = "";
                    runner.StartRunner();
                }
            }
            GUILayout.EndArea();
        }
        private void HandleKeyboard(Event input)
        {
            if (input.type != EventType.KeyDown && input.type != EventType.KeyUp) return;
            var down = input.type == EventType.KeyDown;
            switch (input.keyCode)
            {
                case KeyCode.W: case KeyCode.UpArrow: _north = down; break;
                case KeyCode.S: case KeyCode.DownArrow: _south = down; break;
                case KeyCode.D: case KeyCode.RightArrow: _east = down; break;
                case KeyCode.A: case KeyCode.LeftArrow: _west = down; break;
                default: return;
            }
            input.Use();
        }
    }
}
