using System;
using LoopShift.Editor;
using LoopShift.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LoopShift.Tests
{
    public class ZoneAuthoringTests
    {
        private LevelZone _zone;
        [SetUp]
        public void SetUp() => _zone = LevelTestFactory.CreateZone("Room", Array.Empty<LevelDirection>());
        [TearDown]
        public void TearDown() { Undo.ClearAll(); if (_zone != null) Object.DestroyImmediate(_zone.gameObject); }

        [Test]
        public void CardinalExitCreationUsesOffsetBoundsAndIsIdempotent()
        {
            LevelZoneAuthoring.SetManualBounds(_zone, new Vector2(3, -2), new Vector2(12, 8));
            LevelZoneAuthoring.AddMissingCardinalExits(_zone);
            LevelZoneAuthoring.AddMissingCardinalExits(_zone);
            Assert.That(_zone.Exits.Count, Is.EqualTo(4));
            Assert.That(_zone.TryGetExit(LevelDirection.East, "Default", out var east), Is.True);
            Assert.That(east.transform.localPosition, Is.EqualTo(new Vector3(9, -2, 0)));
            Assert.That(east.GetComponent<Collider2D>().isTrigger, Is.True);
            Assert.That(_zone.TryGetExit(LevelDirection.North, "Default", out var north), Is.True);
            Assert.That(north.transform.localPosition, Is.EqualTo(new Vector3(3, 2, 0)));
        }

        [Test]
        public void ExitCreationSupportsUndoAndRedo()
        {
            LevelZoneAuthoring.AddMissingCardinalExits(_zone);
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            _zone.RefreshExits();
            Assert.That(_zone.Exits, Is.Empty);
            Undo.PerformRedo();
            _zone.RefreshExits();
            Assert.That(_zone.Exits.Count, Is.EqualTo(4));
        }

        [Test]
        public void ExitPositionUsesWorldCoordinatesWithRotatedScaledParent()
        {
            _zone.transform.SetPositionAndRotation(new Vector3(4, 5, 0), Quaternion.Euler(0, 0, 35));
            _zone.transform.localScale = new Vector3(2, 3, 1);
            var exit = LevelZoneAuthoring.AddExit(_zone, LevelDirection.East);
            LevelZoneAuthoring.SetExitPosition(_zone, exit, new Vector3(12, 9, 0));
            Assert.That(Vector3.Distance(exit.SpawnAnchor.position, new Vector3(12, 9, 0)), Is.LessThan(0.0001f));
        }

        [Test]
        public void InvalidGeometryAndNonCardinalExitsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => LevelZoneAuthoring.SetManualBounds(_zone, Vector2.zero, new Vector2(0, 2)));
            Assert.Throws<ArgumentException>(() => LevelZoneAuthoring.SetManualBounds(_zone, new Vector2(float.NaN, 0), Vector2.one));
            Assert.Throws<ArgumentException>(() => LevelZoneAuthoring.AddExit(_zone, LevelDirection.None));
            Assert.That(_zone.Exits, Is.Empty);
        }

        [TestCase(-2f, 3f, 0f)]
        [TestCase(2f, 3f, 37f)]
        public void AutomaticRendererBoundsAreIndependentOfRoomTransform(float scaleX, float scaleY, float angle)
        {
            using (var state = new SerializedObject(_zone))
            {
                state.FindProperty("boundsMode").enumValueIndex = (int)LevelBoundsMode.Auto;
                state.ApplyModifiedPropertiesWithoutUndo();
            }
            _zone.transform.SetPositionAndRotation(new Vector3(40, -12, 0), Quaternion.Euler(0, 0, angle));
            _zone.transform.localScale = new Vector3(scaleX, scaleY, 1);
            var content = new GameObject("Geometry");
            content.transform.SetParent(_zone.transform, false);
            content.transform.localPosition = new Vector3(3, -2, 0);
            var renderer = content.AddComponent<MeshRenderer>();
            renderer.localBounds = new Bounds(Vector3.zero, new Vector3(12, 8, 1));
            _zone.RefreshCachedData();
            Assert.That(Vector3.Distance(_zone.LocalBounds.center, new Vector3(3, -2, 0)), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(_zone.LocalBounds.size, new Vector3(12, 8, 1)), Is.LessThan(0.001f));
        }

        [Test]
        public void AutomaticBoundsIgnoreNestedRoomsAndRetainFallbackOffset()
        {
            LevelZoneAuthoring.SetManualBounds(_zone, new Vector2(3, -2), new Vector2(12, 8));
            using (var state = new SerializedObject(_zone))
            {
                state.FindProperty("boundsMode").enumValueIndex = (int)LevelBoundsMode.Auto;
                state.ApplyModifiedPropertiesWithoutUndo();
            }
            var child = new GameObject("Other room");
            child.transform.SetParent(_zone.transform, false);
            child.AddComponent<LevelZone>();
            child.AddComponent<MeshRenderer>().localBounds = new Bounds(Vector3.zero, Vector3.one * 1000);
            _zone.RefreshCachedData();
            Assert.That(_zone.LocalBounds.center, Is.EqualTo(new Vector3(3, -2, 0)));
            Assert.That(_zone.LocalBounds.size, Is.EqualTo(new Vector3(12, 8, 1)));
        }
        [Test]
        public void NestedRoomExitDoesNotSuppressParentExitCreation()
        {
            var child = new GameObject("Nested");
            child.transform.SetParent(_zone.transform, false);
            var nested = child.AddComponent<LevelZone>();
            var nestedExit = LevelZoneAuthoring.AddExit(nested, LevelDirection.East);
            var parentExit = LevelZoneAuthoring.AddExit(_zone, LevelDirection.East);
            Assert.That(parentExit, Is.Not.SameAs(nestedExit));
            Assert.That(parentExit.GetComponentInParent<LevelZone>(true), Is.SameAs(_zone));
            Assert.That(_zone.Exits.Count, Is.EqualTo(1));
        }
    }
}
