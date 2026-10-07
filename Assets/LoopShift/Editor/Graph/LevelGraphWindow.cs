using System;
using System.Collections.Generic;
using LoopShift.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoopShift.Editor
{
	public partial class LevelGraphWindow : EditorWindow
	{
		private const float ToolbarHeight = 24;
		private const float DetailsWidth = 310;
		private const float NodeWidth = 230;
		private static readonly Color RoomColor = new(0.18f, 0.44f, 0.62f);
		private static readonly Color PoolColor = new(0.51f, 0.35f, 0.66f);

		[SerializeField] private LevelGraphAsset graph;
		[SerializeField] private Vector2 pan = new(45, 45);
		[SerializeField] private float zoom = 1;
		[SerializeField] private bool bidirectional = true;
		private readonly List<Node> _nodes = new();
		private readonly Dictionary<string, Node> _rooms = new(StringComparer.Ordinal);
		private readonly Dictionary<string, Node> _pools = new(StringComparer.Ordinal);
		private IReadOnlyList<LevelValidationMessage> _validation = Array.Empty<LevelValidationMessage>();
		private Node _selected;
		private Node _dragging;
		private Port _pendingPort;
		private int _selectedEdge = -1;
		private int _dragUndoGroup;
		private Vector2 _detailsScroll;
		private Rect _canvas;
		private string _notice;
		private bool _dirty = true;
		private int _observedGraphDirtyCount = -1;

		[MenuItem("Window/LoopShift/Level Graph")]
		public static void Open() => Open(Selection.activeObject as LevelGraphAsset);

		public static void Open(LevelGraphAsset asset)
		{
			var window = GetWindow<LevelGraphWindow>("LoopShift");
			window.minSize = new Vector2(780, 420);
			window.SetGraph(asset != null ? asset : window.graph);
			window.Show();
		}

		private void OnEnable()
		{
			Undo.undoRedoPerformed += Refresh;
			Undo.postprocessModifications += OnAuthoringModified;
			EditorApplication.projectChanged += Refresh;
			Refresh();
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= Refresh;
			Undo.postprocessModifications -= OnAuthoringModified;
			EditorApplication.projectChanged -= Refresh;
		}

		private void OnInspectorUpdate()
		{
			if (graph != null && EditorUtility.GetDirtyCount(graph) != _observedGraphDirtyCount)
				Refresh();
		}

        private UndoPropertyModification[] OnAuthoringModified(UndoPropertyModification[] changes)
        {
            foreach (var change in changes)
            {
                var target = change.currentValue.target;
                if (target != graph && !(target is LevelZone) && !(target is LevelExit))
                    continue;
                Refresh();
                break;
            }
            return changes;
        }

        private void SetGraph(LevelGraphAsset asset)
		{
			graph = asset;
			_dragging = null;
			_selected = null;
			_selectedEdge = -1;
			_pendingPort = null;
			_notice = null;
			Refresh();
		}

		private void Refresh()
		{
			_dirty = true;
			Repaint();
		}

		private void OnGUI()
		{
			if (_dirty)
				Rebuild();
			_canvas = new Rect(0, ToolbarHeight, Mathf.Max(200, position.width - DetailsWidth), position.height - ToolbarHeight);
			DrawToolbar();
			DrawCanvas();
			DrawDetails(new Rect(_canvas.xMax, ToolbarHeight, DetailsWidth, position.height - ToolbarHeight));
		}

		private void DrawToolbar()
		{
			GUILayout.BeginArea(new Rect(0, 0, position.width, ToolbarHeight));
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				var chosen = (LevelGraphAsset)EditorGUILayout.ObjectField(graph, typeof(LevelGraphAsset), false, GUILayout.MinWidth(150));
				if (chosen != graph) SetGraph(chosen);
				if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(42))) NewGraph();
				using (new EditorGUI.DisabledScope(graph == null))
				{
					if (GUILayout.Button("Add selected", EditorStyles.toolbarButton)) AddSelectedPrefabs();
					if (GUILayout.Button("+ Pool", EditorStyles.toolbarButton))
						Perform(() => LevelGraphAuthoring.AddPool(graph, ToGraph(_canvas.size * 0.5f)));
					if (GUILayout.Button("Frame all", EditorStyles.toolbarButton)) FrameAll();
					if (GUILayout.Button("Save", EditorStyles.toolbarButton)) AssetDatabase.SaveAssetIfDirty(graph);
				}
			}
			GUILayout.EndArea();
		}

		private void DrawCanvas()
		{
			EditorGUI.DrawRect(_canvas, new Color(0.105f, 0.12f, 0.145f));
			GUI.BeginGroup(_canvas);
			DrawGrid();
			if (graph == null)
			{
				GUI.Label(new Rect(30, 30, 450, 60), "Choose or create a level graph.\nDrag saved LevelZone prefabs here to add rooms.", EditorStyles.wordWrappedLabel);
				GUI.EndGroup();
				return;
			}
			DrawEdges();
			for (var i = 0; i < _nodes.Count; i++)
				DrawNode(_nodes[i]);
			if (_pendingPort != null)
				DrawWire(PortPoint(_pendingPort), Event.current.mousePosition, Color.yellow, 2);
			HandleCanvasInput(Event.current);
			GUI.EndGroup();
		}

		private void DrawGrid()
		{
			var step = 32 * zoom;
			for (var x = pan.x % step; x < _canvas.width; x += step)
				EditorGUI.DrawRect(new Rect(x, 0, 1, _canvas.height), new Color(1, 1, 1, 0.035f));
			for (var y = pan.y % step; y < _canvas.height; y += step)
				EditorGUI.DrawRect(new Rect(0, y, _canvas.width, 1), new Color(1, 1, 1, 0.035f));
		}

		private void DrawNode(Node node)
		{
			var rect = NodeRect(node);
			if (!rect.Overlaps(new Rect(Vector2.zero, _canvas.size)))
				return;
			var color = node.Pool != null ? PoolColor : RoomColor;
			EditorGUI.DrawRect(rect, node == _selected ? new Color(0.83f, 0.7f, 0.3f) : color);
			EditorGUI.DrawRect(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), new Color(0.17f, 0.19f, 0.23f));
			EditorGUI.DrawRect(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, 28 * zoom), color);
			var title = node.Id + (node.Zone != null && graph.StartZoneId == node.Id ? "   [START]" : "");
			GUI.Label(ScaleRect(node, 10, 4, NodeWidth - 20, 22), title, EditorStyles.boldLabel);
			if (node.Pool != null)
			{
				GUI.Label(ScaleRect(node, 12, 38, NodeWidth - 24, 22), (node.Pool.entries?.Count ?? 0) + " weighted rooms");
				GUI.Label(ScaleRect(node, 12, 60, NodeWidth - 24, 22), "Global exit rules", EditorStyles.miniLabel);
				EditorGUI.DrawRect(new Rect(rect.center.x - 6, rect.y - 6, 12, 12), PoolColor);
				return;
			}
			GUI.Label(ScaleRect(node, 12, 33, NodeWidth - 24, 20),
				node.Zone.prefab != null ? node.Zone.prefab.name : "Missing prefab", EditorStyles.miniLabel);
			for (var i = 0; i < node.Ports.Count; i++)
			{
				var port = node.Ports[i];
				var point = PortPoint(port);
				EditorGUI.DrawRect(new Rect(point.x - 6, point.y - 6, 12, 12),
					_pendingPort == port ? Color.yellow : new Color(0.4f, 0.8f, 0.95f));
				GUI.Label(ScaleRect(node, 16, 57 + i * 23, NodeWidth - 32, 21), port.Direction + " · " + port.Tag);
			}
		}

		private void DrawEdges()
		{
			for (var i = 0; i < graph.FixedConnections.Count; i++)
			{
				var edge = graph.FixedConnections[i];
				if (edge == null || !_rooms.TryGetValue(edge.fromZoneId ?? "", out var source) ||
				    !_rooms.TryGetValue(edge.toZoneId ?? "", out var target))
					continue;
				var a = FindPort(source, edge.direction, edge.fromExitTag);
				var b = FindPort(target, LevelDirectionUtility.Opposite(edge.direction), edge.toExitTag);
				if (a == null || b == null)
					continue;
				var start = PortPoint(a);
				var end = PortPoint(b);
				DrawWire(start, end, _selectedEdge == i ? Color.yellow : new Color(0.45f, 0.78f, 0.9f), 3);
				var middle = (start + end) * 0.5f;
				GUI.Label(new Rect(middle.x - 10, middle.y - 10, 25, 20), edge.bidirectional ? "↔" : "→");
				if (Event.current.type != EventType.MouseDown || Event.current.button != 0 ||
				    HandleUtility.DistancePointBezier(Event.current.mousePosition, start, end,
					    start + Vector2.right * 60, end - Vector2.right * 60) > 8)
					continue;
				_selectedEdge = i;
				_selected = null;
				Event.current.Use();
			}
			foreach (var rule in graph.PoolRules)
			{
				if (rule == null || !_pools.TryGetValue(rule.poolId ?? "", out var pool))
					continue;
				foreach (var node in _rooms.Values)
				{
					var port = FindPort(node, rule.direction, rule.fromExitTag);
					if (port != null)
						DrawWire(PortPoint(port), NodeRect(pool).center, new Color(0.65f, 0.5f, 0.8f, 0.5f), 1);
				}
			}
		}

		private static void DrawWire(Vector2 start, Vector2 end, Color color, float width) =>
			Handles.DrawBezier(start, end, start + Vector2.right * 60, end - Vector2.right * 60, color, null, width);

		private void HandleCanvasInput(Event input)
		{
			var inside = new Rect(Vector2.zero, _canvas.size).Contains(input.mousePosition);
			if (input.type == EventType.KeyDown && !EditorGUIUtility.editingTextField)
			{
				if (input.keyCode == KeyCode.Escape) { _pendingPort = null; input.Use(); }
				else if (input.keyCode == KeyCode.F) { FrameAll(); input.Use(); }
				else if (input.keyCode == KeyCode.Delete || input.keyCode == KeyCode.Backspace)
				{
					DeleteSelection();
					input.Use();
				}
			}
			if (input.type == EventType.MouseUp && _dragging != null)
			{
				Undo.CollapseUndoOperations(_dragUndoGroup);
				_dragging = null;
				input.Use();
				Refresh();
				return;
			}
			if (!inside)
				return;
			if (input.type == EventType.ScrollWheel)
			{
				var anchor = ToGraph(input.mousePosition);
				zoom = Mathf.Clamp(zoom * Mathf.Pow(1.08f, -input.delta.y), 0.4f, 1.6f);
				pan = input.mousePosition - anchor * zoom;
				input.Use();
				Repaint();
				return;
			}
			if (input.type == EventType.MouseDrag && (input.button == 2 || input.alt))
			{
				pan += input.delta;
				input.Use();
				Repaint();
				return;
			}
			if (input.type == EventType.MouseDrag && _dragging != null)
			{
				Undo.RecordObject(graph, "Move graph node");
				_dragging.Position += input.delta / zoom;
				EditorUtility.SetDirty(graph);
				input.Use();
				Repaint();
				return;
			}
			if (input.type == EventType.DragUpdated || input.type == EventType.DragPerform)
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				if (input.type == EventType.DragPerform)
				{
					DragAndDrop.AcceptDrag();
					AddPrefabs(DragAndDrop.objectReferences, ToGraph(input.mousePosition));
				}
				input.Use();
				return;
			}
			if (input.type == EventType.MouseUp && _pendingPort != null)
			{
				var target = HitPort(input.mousePosition);
				if (target != null && target != _pendingPort)
					ConnectPending(target);
				else
				{
					var node = HitNode(input.mousePosition);
					if (node?.Pool != null) ConnectPool(node);
				}
				input.Use();
				return;
			}
			if (input.type != EventType.MouseDown)
				return;
			var hitPort = HitPort(input.mousePosition);
			var hitNode = HitNode(input.mousePosition);
			if (input.button == 1)
			{
				ShowContext(hitNode, ToGraph(input.mousePosition));
				input.Use();
				return;
			}
			if (input.button != 0 || input.alt)
				return;
			if (hitPort != null)
			{
				if (_pendingPort != null && _pendingPort != hitPort) ConnectPending(hitPort);
				else _pendingPort = hitPort;
				input.Use();
				Repaint();
				return;
			}
			if (_pendingPort != null && hitNode?.Pool != null)
			{
				ConnectPool(hitNode);
				input.Use();
				return;
			}
			_selected = hitNode;
			_selectedEdge = -1;
			_pendingPort = null;
			_dragging = hitNode;
			if (_dragging != null)
			{
				Undo.IncrementCurrentGroup();
				_dragUndoGroup = Undo.GetCurrentGroup();
			}
			input.Use();
			Repaint();
		}

		private void ConnectPending(Port target)
		{
			var source = _pendingPort;
			_pendingPort = null;
			if (source == null)
				return;
			Perform(() => LevelGraphAuthoring.Connect(graph, source.Node.Id, source.Direction, source.Tag,
				target.Node.Id, target.Tag, bidirectional),
				LevelDirectionUtility.Opposite(source.Direction) == target.Direction
					? null : "The target exit must face the opposite direction.");
		}

		private void ConnectPool(Node pool)
		{
			var source = _pendingPort;
			_pendingPort = null;
			Perform(() => LevelGraphAuthoring.AddPoolRule(graph, pool.Id, source.Direction, source.Tag));
			_notice = "Pool rules apply to all matching exits. Fixed connections take priority.";
		}

		private void Perform(Action action, string error = null)
		{
			try
			{
				if (error != null) throw new ArgumentException(error);
				action();
				_notice = null;
			}
			catch (ArgumentException exception) { _notice = exception.Message; }
			Refresh();
		}

		private void NewGraph()
		{
			var path = EditorUtility.SaveFilePanelInProject("Create level graph", "LevelGraph", "asset", "Choose a project folder.");
			if (string.IsNullOrEmpty(path))
				return;
			var asset = LevelGraphAuthoring.CreateGraph(System.IO.Path.GetDirectoryName(path),
                System.IO.Path.GetFileNameWithoutExtension(path), Array.Empty<LevelZone>());

			SetGraph(asset);
		}

		private void AddSelectedPrefabs() => AddPrefabs(Selection.objects, ToGraph(_canvas.size * 0.5f));

		private void AddPrefabs(UnityEngine.Object[] objects, Vector2 position)
		{
			var offset = Vector2.zero;
			foreach (var item in objects)
			{
				var prefab = item as GameObject;
				var zone = prefab != null ? prefab.GetComponent<LevelZone>() : item as LevelZone;
				if (zone == null)
					continue;
				var placedAt = position + offset;
				Perform(() => LevelGraphAuthoring.AddZone(graph, zone, placedAt));
				offset += new Vector2(35, 35);
			}
		}

		private void ShowContext(Node node, Vector2 position)
		{
			var menu = new GenericMenu();
			if (node == null)
				menu.AddItem(new GUIContent("Add pool"), false, () => Perform(() => LevelGraphAuthoring.AddPool(graph, position)));
			else
			{
				_selected = node;
				_selectedEdge = -1;
				if (node.Zone != null)
				{
					menu.AddItem(new GUIContent("Set as start"), false, () => Perform(() => LevelGraphAuthoring.SetStart(graph, node.Id)));
					foreach (var pool in graph.Pools)
					{
						if (pool == null) continue;
						var poolId = pool.poolId;
						menu.AddItem(new GUIContent("Add to pool/" + poolId), false,
							() => Perform(() => LevelGraphAuthoring.AddToPool(graph, poolId, node.Id)));
					}
				}
				menu.AddItem(new GUIContent("Delete"), false, DeleteSelection);
			}
			menu.ShowAsContext();
		}

		private void DeleteSelection()
		{
			if (graph == null)
				return;
			if (_selectedEdge >= 0) Perform(() => LevelGraphAuthoring.RemoveConnection(graph, _selectedEdge));
			else if (_selected?.Pool != null) Perform(() => LevelGraphAuthoring.RemovePool(graph, _selected.Id));
			else if (_selected != null) Perform(() => LevelGraphAuthoring.RemoveZone(graph, _selected.Id));
			_selected = null;
			_selectedEdge = -1;
		}

		private void FrameAll()
		{
			if (_nodes.Count == 0)
				return;
			var bounds = new Rect(_nodes[0].Position, new Vector2(NodeWidth, _nodes[0].Height));
			foreach (var node in _nodes)
				bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, node.Position.x), Mathf.Min(bounds.yMin, node.Position.y),
					Mathf.Max(bounds.xMax, node.Position.x + NodeWidth), Mathf.Max(bounds.yMax, node.Position.y + node.Height));
			zoom = Mathf.Clamp(Mathf.Min((_canvas.width - 70) / bounds.width, (_canvas.height - 70) / bounds.height), 0.4f, 1.3f);
			pan = _canvas.size * 0.5f - bounds.center * zoom;
			Repaint();
		}

		private Vector2 ToGraph(Vector2 local) => (local - pan) / zoom;
		private Rect NodeRect(Node node) => new(node.Position * zoom + pan, new Vector2(NodeWidth, node.Height) * zoom);
		private Rect ScaleRect(Node node, float x, float y, float width, float height) =>
			new((node.Position + new Vector2(x, y)) * zoom + pan, new Vector2(width, height) * zoom);
		private Vector2 PortPoint(Port port) => (port.Node.Position + new Vector2(
			port.Direction == LevelDirection.West || port.Direction == LevelDirection.South ? 0 : NodeWidth,
			67 + port.Index * 23)) * zoom + pan;

		private Node HitNode(Vector2 point)
		{
			for (var i = _nodes.Count - 1; i >= 0; i--)
				if (NodeRect(_nodes[i]).Contains(point))
					return _nodes[i];
			return null;
		}

		private Port HitPort(Vector2 point)
		{
			foreach (var node in _nodes)
				foreach (var port in node.Ports)
					if ((PortPoint(port) - point).sqrMagnitude < 100)
						return port;
			return null;
		}

		private static Port FindPort(Node node, LevelDirection direction, string tag)
		{
			foreach (var port in node.Ports)
				if (port.Direction == direction && LevelZone.TagsCompatible(port.Tag, tag))
					return port;
			return null;
		}

		private void Rebuild()
		{
			// Preserve live drag/connection objects until the gesture finishes.
			if (_dragging != null || _pendingPort != null)
				return;
			_dirty = false;
			_observedGraphDirtyCount = graph != null ? EditorUtility.GetDirtyCount(graph) : 0;
			var selectedId = _selected?.Id;
			var selectedPool = _selected?.Pool != null;
			_nodes.Clear();
			_rooms.Clear();
			_pools.Clear();
			if (graph == null)
				return;
			foreach (var zone in graph.Zones) AddRoomNode(zone);
			foreach (var pool in graph.Pools)
			{
				if (pool == null || string.IsNullOrWhiteSpace(pool.poolId))
					continue;
				if (!_pools.ContainsKey(pool.poolId))
				{
					var node = new Node(null, pool, DefaultPosition());
					_nodes.Add(node);
					_pools.Add(pool.poolId, node);
				}
				if (pool.entries == null) continue;
				foreach (var zone in pool.entries) AddRoomNode(zone);
			}
			_selected = null;
			if (selectedId != null)
				(selectedPool ? _pools : _rooms).TryGetValue(selectedId, out _selected);
			_validation = graph.ValidateGraph();
		}

		private void AddRoomNode(LevelZoneEntry entry)
		{
			if (entry == null || string.IsNullOrWhiteSpace(entry.ZoneId) || _rooms.ContainsKey(entry.ZoneId))
				return;
			var node = new Node(entry, null, DefaultPosition());
			_nodes.Add(node);
			_rooms.Add(entry.ZoneId, node);
		}

		private Vector2 DefaultPosition() => new(_nodes.Count % 3 * 290, _nodes.Count / 3 * 225);

		private sealed class Node
		{
			public readonly LevelZoneEntry Zone;
			public readonly LevelZonePool Pool;
			public readonly List<Port> Ports = new();
			private readonly Vector2 _fallback;
			public string Id => Pool != null ? Pool.poolId : Zone.ZoneId;
			public float Height => Pool != null ? 94 : 65 + Mathf.Max(1, Ports.Count) * 23;
			public Vector2 Position
			{
				get => Pool != null ? Pool.hasEditorPosition ? Pool.editorPosition : _fallback :
					Zone.hasEditorPosition ? Zone.editorPosition : _fallback;
				set
				{
					if (Pool != null) { Pool.editorPosition = value; Pool.hasEditorPosition = true; }
					else { Zone.editorPosition = value; Zone.hasEditorPosition = true; }
				}
			}
			public Node(LevelZoneEntry zone, LevelZonePool pool, Vector2 fallback)
			{
				Zone = zone; Pool = pool; _fallback = fallback;
				if (zone?.prefab == null) return;
				foreach (var exit in zone.prefab.Exits)
					if (exit != null && LevelDirectionUtility.IsCardinal(exit.Direction))
						Ports.Add(new Port(this, exit.Direction, exit.ExitTag, Ports.Count));
			}
		}

		private sealed class Port
		{
			public readonly Node Node;
			public readonly LevelDirection Direction;
			public readonly string Tag;
			public readonly int Index;
			public Port(Node node, LevelDirection direction, string tag, int index)
			{ Node = node; Direction = direction; Tag = tag; Index = index; }
		}
	}
}