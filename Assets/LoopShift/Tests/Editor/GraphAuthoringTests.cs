using System;
using LoopShift.Editor;
using LoopShift.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LoopShift.Tests
{
    public class GraphAuthoringTests
    {
        private string _folder;
        private LevelGraphAsset _graph;
        private LevelZone _prefab;

        [SetUp]
        public void SetUp()
        {
            var name = "__LoopShiftTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", name);
            _folder = "Assets/" + name;
            var zone = LevelTestFactory.CreateZone("Room", new[] { LevelDirection.East, LevelDirection.West });
            _prefab = PrefabUtility.SaveAsPrefabAsset(zone.gameObject, _folder + "/Room.prefab").GetComponent<LevelZone>();
            Object.DestroyImmediate(zone.gameObject);
            _graph = ScriptableObject.CreateInstance<LevelGraphAsset>();
            AssetDatabase.CreateAsset(_graph, _folder + "/Graph.asset");
        }


        [Test]
        public void GraphFactoryUsesSharedAliasesAndDoesNotModifyPrefabs()
        {
            var originalId = _prefab.ZoneId;
            var created = LevelGraphAuthoring.CreateGraph(_folder, "Created", new[] { _prefab, _prefab });
            Assert.That(created.StartZoneId, Is.EqualTo("Room"));
            Assert.That(created.Zones[1].ZoneId, Is.EqualTo("Room_2"));
            Assert.That(created.Zones[1].editorPosition, Is.EqualTo(new Vector2(290, 0)));
            Assert.That(_prefab.ZoneId, Is.EqualTo(originalId));
            Assert.That(AssetDatabase.LoadAssetAtPath<LevelGraphAsset>(_folder + "/Created.asset"), Is.SameAs(created));
        }

        [TestCase("Packages/ReadOnly", "Graph")]
        [TestCase("Assets/../Packages", "Graph")]
        [TestCase("Assets", "../Graph")]
        [TestCase("Assets", "")]
        public void GraphFactoryRejectsInvalidOutput(string folder, string name)
        {
            Assert.Throws<ArgumentException>(() => LevelGraphAuthoring.CreateGraph(folder, name, Array.Empty<LevelZone>()));
        }

        [Test]
        public void GraphFactoryRejectsSceneRoomsBeforeCreatingAsset()
        {
            var sceneRoom = LevelTestFactory.CreateZone("Scene", Array.Empty<LevelDirection>());
            try
            {
                Assert.Throws<ArgumentException>(() => LevelGraphAuthoring.CreateGraph(_folder, "Rejected", new[] { _prefab, sceneRoom }));
                Assert.That(AssetDatabase.LoadAssetAtPath<LevelGraphAsset>(_folder + "/Rejected.asset"), Is.Null);
            }
            finally { Object.DestroyImmediate(sceneRoom.gameObject); }
        }


        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator CanvasDragUndoAndPortConnectionUseAuthoringState()
        {
            LevelGraphAuthoring.AddZone(_graph, _prefab, Vector2.zero);
            LevelGraphAuthoring.AddZone(_graph, _prefab, new Vector2(290, 0));
            LevelGraphWindow.Open(_graph);
            var window = EditorWindow.GetWindow<LevelGraphWindow>();
            window.position = new Rect(40, 40, 1100, 700);
            using (var state = new SerializedObject(window))
            {
                state.FindProperty("pan").vector2Value = new Vector2(45, 45);
                state.FindProperty("zoom").floatValue = 1;
                state.ApplyModifiedPropertiesWithoutUndo();
            }
            try
            {
                yield return null;
                window.SendEvent(new Event { type = EventType.Layout });
                window.SendEvent(new Event { type = EventType.Repaint });
                // SendEvent uses host coordinates; account for Unity's tab/header above the content.
                var contentOffset = window.rootVisualElement.worldBound.position;
                var fields = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = contentOffset + new Vector2(90, 80) });
                Assert.That(typeof(LevelGraphWindow).GetField("_dragging", fields).GetValue(window), Is.Not.Null, "Mouse down must select a node before dragging");
                window.SendEvent(new Event { type = EventType.MouseDrag, button = 0, mousePosition = contentOffset + new Vector2(190, 120), delta = new Vector2(100, 40) });
                window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = contentOffset + new Vector2(190, 120) });
                Assert.That(_graph.Zones[0].editorPosition, Is.EqualTo(new Vector2(100, 40)));
                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                Assert.That(_graph.Zones[0].editorPosition, Is.EqualTo(Vector2.zero));
                yield return null;
                // At default pan/zoom: East port of the first room, West port of the second.
                window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = contentOffset + new Vector2(275, 136) });
                window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = contentOffset + new Vector2(275, 136) });
                window.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = contentOffset + new Vector2(335, 159) });
                window.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = contentOffset + new Vector2(335, 159) });
                Assert.That(_graph.FixedConnections.Count, Is.EqualTo(1));
                Assert.That(_graph.FixedConnections[0].fromZoneId, Is.EqualTo("Room"));
                Assert.That(_graph.FixedConnections[0].toZoneId, Is.EqualTo("Room_2"));
                Assert.That(_graph.FixedConnections[0].direction, Is.EqualTo(LevelDirection.East));
            }
            finally { window.Close(); }
        }

        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator IdleWindowKeepsItsCompiledViewAndRefreshesExternalEdits()
        {
            LevelGraphAuthoring.AddZone(_graph, _prefab, Vector2.zero);
            LevelGraphWindow.Open(_graph);
            var window = EditorWindow.GetWindow<LevelGraphWindow>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var update = typeof(LevelGraphWindow).GetMethod("OnInspectorUpdate", flags);
            var nodesField = typeof(LevelGraphWindow).GetField("_nodes", flags);
            try
            {
                yield return null;
                window.SendEvent(new Event { type = EventType.Layout });
                window.SendEvent(new Event { type = EventType.Repaint });
                var nodes = (System.Collections.IList)nodesField.GetValue(window);
                var firstNode = nodes[0];
                for (var i = 0; i < 3; i++)
                {
                    update.Invoke(window, null);
                    window.SendEvent(new Event { type = EventType.Repaint });
                    Assert.That(nodes[0], Is.SameAs(firstNode), "Idle updates must not recompile the graph view.");
                }
                LevelGraphAuthoring.AddZone(_graph, _prefab, new Vector2(290, 0));
                update.Invoke(window, null);
                window.SendEvent(new Event { type = EventType.Layout });
                window.SendEvent(new Event { type = EventType.Repaint });
                Assert.That(nodes.Count, Is.EqualTo(2), "External serialized graph edits must refresh the view.");
            }
            finally { window.Close(); }
        }
        [Test]
        public void DeferredRefreshSurvivesDraggingAndSwitchingGraphsCancelsTheGesture()
        {
            LevelGraphAuthoring.AddZone(_graph, _prefab, Vector2.zero);
            var window = ScriptableObject.CreateInstance<LevelGraphWindow>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var type = typeof(LevelGraphWindow);
            var rebuild = type.GetMethod("Rebuild", flags);
            var setGraph = type.GetMethod("SetGraph", flags);
            var dragging = type.GetField("_dragging", flags);
            var dirty = type.GetField("_dirty", flags);
            try
            {
                setGraph.Invoke(window, new object[] { _graph });
                rebuild.Invoke(window, null);
                var nodes = (System.Collections.IList)type.GetField("_nodes", flags).GetValue(window);
                dragging.SetValue(window, nodes[0]);
                type.GetMethod("Refresh", flags).Invoke(window, null);
                rebuild.Invoke(window, null);
                Assert.That(dirty.GetValue(window), Is.True, "A gesture must defer rather than discard a requested refresh.");
                setGraph.Invoke(window, new object[] { null });
                Assert.That(dragging.GetValue(window), Is.Null, "Switching graphs must cancel the old graph's drag.");
                rebuild.Invoke(window, null);
                Assert.That(nodes.Count, Is.Zero, "The new graph must not retain nodes from the previous gesture.");
                Assert.That(dirty.GetValue(window), Is.False);
            }
            finally { Object.DestroyImmediate(window); }
        }
        [TearDown]
        public void TearDown()
        {
            Undo.ClearAll();
            AssetDatabase.DeleteAsset(_folder);
        }

        [Test]
        public void AddRoomAssignsUniqueIdsAndStart()
        {
            var first = LevelGraphAuthoring.AddZone(_graph, _prefab, new Vector2(12, 34));
            LevelGraphAuthoring.AddZone(_graph, _prefab, Vector2.zero);
            Assert.That(_graph.StartZoneId, Is.EqualTo("Room"));
            Assert.That(_graph.Zones[1].ZoneId, Is.EqualTo("Room_2"));
            Assert.That(first.editorPosition, Is.EqualTo(new Vector2(12, 34)));
            Assert.That(first.hasEditorPosition, Is.True);
        }

        [Test]
        public void SceneObjectIsRejectedWithoutChangingGraph()
        {
            var zone = LevelTestFactory.CreateZone("Scene", Array.Empty<LevelDirection>());
            try
            {
                Assert.Throws<ArgumentException>(() => LevelGraphAuthoring.AddZone(_graph, zone, Vector2.zero));
                Assert.That(_graph.Zones, Is.Empty);
            }
            finally { Object.DestroyImmediate(zone.gameObject); }
        }

        [Test]
        public void RenameAndDeleteMaintainReferences()
        {
            LevelGraphAuthoring.AddZone(_graph, _prefab, Vector2.zero);
            LevelGraphAuthoring.AddZone(_graph, _prefab, Vector2.one);
            LevelGraphAuthoring.AddPool(_graph, Vector2.zero);
            LevelGraphAuthoring.AddToPool(_graph, "Pool", "Room");
            LevelGraphAuthoring.Connect(_graph, "Room", LevelDirection.East, "", "Room_2", "", true);
            LevelGraphAuthoring.RenameZone(_graph, "Room", "Start");
            Assert.That(_graph.StartZoneId, Is.EqualTo("Start"));
            Assert.That(_graph.Pools[0].entries[0].ZoneId, Is.EqualTo("Start"));
            Assert.That(_graph.FixedConnections[0].fromZoneId, Is.EqualTo("Start"));
            LevelGraphAuthoring.RemoveZone(_graph, "Start");
            Assert.That(_graph.FixedConnections, Is.Empty);
            Assert.That(_graph.Pools[0].entries, Is.Empty);
            Assert.That(_graph.StartZoneId, Is.Empty);
        }

        [Test]
        public void ReverseRouteCannotBeOverwrittenAndConnectionSupportsUndo()
        {
            LevelGraphAuthoring.AddZone(_graph, _prefab, Vector2.zero);
            LevelGraphAuthoring.AddZone(_graph, _prefab, Vector2.one);
            Undo.IncrementCurrentGroup();
            LevelGraphAuthoring.Connect(_graph, "Room", LevelDirection.East, "", "Room_2", "", true);
            Assert.Throws<ArgumentException>(() => LevelGraphAuthoring.Connect(
                _graph, "Room_2", LevelDirection.West, "", "Room", "", false));
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            Assert.That(_graph.FixedConnections, Is.Empty);
            Undo.PerformRedo();
            Assert.That(_graph.FixedConnections.Count, Is.EqualTo(1));
        }

        [Test]
        public void PoolRenameAndRemovalMaintainGlobalRules()
        {
            LevelGraphAuthoring.AddPool(_graph, Vector2.zero);
            LevelGraphAuthoring.AddPoolRule(_graph, "Pool", LevelDirection.East, "");
            LevelGraphAuthoring.RenamePool(_graph, "Pool", "Corridors");
            Assert.That(_graph.PoolRules[0].poolId, Is.EqualTo("Corridors"));
            LevelGraphAuthoring.RemovePool(_graph, "Corridors");
            Assert.That(_graph.PoolRules, Is.Empty);
            Assert.That(_graph.Pools, Is.Empty);
        }
    }
}
