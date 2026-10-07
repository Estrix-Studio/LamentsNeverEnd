using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LoopShift.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LoopShift.Tests
{
	public class RunnerLifecycleTests
	{
		private readonly List<Object> _owned = new();
		private LevelZone _a;
		private LevelZone _b;
		private LevelGraphAsset _graph;
		private EndlessLevelRunner _runner;
		private BoxCollider2D _actor;

		[SetUp]
		public void SetUp()
		{
			_a = Own(LevelTestFactory.CreateZone("A", new[] { LevelDirection.East, LevelDirection.West }, 2));
			_b = Own(LevelTestFactory.CreateZone("B", new[] { LevelDirection.East, LevelDirection.West }, 2));
			_graph = LevelTestFactory.CreateGraph(_a, _b);
			_owned.Add(_graph);
			SetConnections(new LevelConnection { fromZoneId = "A", toZoneId = "B", direction = LevelDirection.East });
			_runner = LevelTestFactory.CreateRunner(_graph);
			_owned.Add(_runner.gameObject);
			var actorObject = NewObject("Player");
			_actor = actorObject.AddComponent<BoxCollider2D>();
			actorObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
			_runner.AddTarget(actorObject.transform);
		}


        [TestCase(false)]
        [TestCase(true)]
        public void RecenterMovesRoomActorAndNestedCameraExactlyOnce(bool parentTargets)
        {
            _runner.SpawnSettings.recenterOnTransition = true;
            _runner.SpawnSettings.parentTargetsToCurrentZone = parentTargets;
            _runner.transform.position = new Vector3(10, 3, 0);
            var camera = NewObject("Nested camera");
            camera.transform.SetParent(_actor.transform, false);
            camera.transform.localPosition = new Vector3(1, 2, -10);
            _runner.AddTarget(camera.transform);
            _actor.transform.position = new Vector3(12, 3, 0);
            var actorBefore = _actor.transform.position;
            var cameraBefore = camera.transform.position;
            var shifted = Vector3.zero;
            _runner.WorldShifted += value => shifted += value;
            _runner.StartRunner();
            Assert.That(_runner.TryTraverse(CurrentExit(LevelDirection.East), _actor), Is.True);
            Assert.That(shifted, Is.EqualTo(new Vector3(-4, 0, 0)));
            Assert.That(_runner.ActiveZones[_runner.CurrentCell].transform.position, Is.EqualTo(_runner.transform.position));
            Assert.That(_actor.transform.position, Is.EqualTo(actorBefore + shifted));
            Assert.That(camera.transform.position, Is.EqualTo(cameraBefore + shifted));
            Assert.That(camera.transform.parent, Is.EqualTo(_actor.transform));
            Assert.That((Vector2)_actor.transform.position, Is.EqualTo(_actor.attachedRigidbody.position));
        }

        [UnityTest]
        public IEnumerator RecenterKeepsRepeatedTransitionsNearOrigin()
        {
            SetConnections(new LevelConnection { fromZoneId = "A", toZoneId = "A", direction = LevelDirection.East });
            _runner.SpawnSettings.recenterOnTransition = true;
            _runner.SpawnSettings.activeRadius = 0;
            _runner.StartRunner();
            for (var i = 0; i < 100; i++)
            {
                _actor.transform.position = new Vector3(2, 0, 0);
                Assert.That(_runner.TryTraverse(CurrentExit(LevelDirection.East), _actor), Is.True);
                Assert.That(_runner.ActiveZones[_runner.CurrentCell].transform.position, Is.EqualTo(Vector3.zero));
                Assert.That(_actor.transform.position.x, Is.EqualTo(-2));
                Assert.That(_runner.OwnedZoneCount, Is.LessThanOrEqualTo(2));
                yield return null;
            }
            Assert.That(_runner.CurrentCell.x, Is.EqualTo(100));
        }

        [Test]
        public void StopFromWorldShiftCallbackCleansAllRooms()
        {
            _runner.SpawnSettings.recenterOnTransition = true;
            _runner.WorldShifted += _ => _runner.StopRunner();
            _runner.StartRunner();
            Assert.That(_runner.TryTraverse(CurrentExit(LevelDirection.East), _actor), Is.False);
            Assert.That(_runner.IsRunning, Is.False);
            Assert.That(_runner.ActiveZones, Is.Empty);
        }


        [UnityTest]
        public IEnumerator DisablingRunnerGameObjectPreservesParentedActorsAndCanRestart()
        {
            _runner.SpawnSettings.parentTargetsToCurrentZone = true;
            _runner.StartRunner();
            var room = _runner.ActiveZones[Vector2Int.zero];
            _runner.gameObject.SetActive(false);
            Assert.That(_runner.IsRunning, Is.False);
            Assert.That(_runner.ActiveZones, Is.Empty);
            Assert.That(_actor.transform.parent, Is.Null);
            Assert.That(_actor.gameObject.activeInHierarchy, Is.True);
            Assert.That(room.gameObject.activeSelf, Is.False);
            yield return null;
            _runner.gameObject.SetActive(true);
            _runner.StartRunner();
            Assert.That(_runner.IsRunning, Is.True);
            Assert.That(_runner.ActiveZones[Vector2Int.zero], Is.SameAs(room));
        }


        [UnityTest]
        public IEnumerator MissingPoolContainerIsNotRecreatedDuringStop()
        {
            _runner.StartRunner();
            var room = _runner.ActiveZones[Vector2Int.zero];
            _runner.StopRunner();
            var poolRoot = room.transform.parent;
            _runner.StartRunner();
            Object.Destroy(poolRoot.gameObject);
            yield return null;
            _runner.StopRunner();
            Assert.That(_runner.PooledZoneCount, Is.Zero);
            Assert.That(_runner.OwnedZoneCount, Is.Zero);
            yield return null;
            Assert.That(room == null, Is.True);
        }

        [UnityTest]
        public IEnumerator UnloadingRunnerSceneDisposesItsContainers()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.CreateScene("LoopShift unload regression");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_runner.gameObject, scene);
            _runner.StartRunner();
            var activeRoot = _runner.ActiveZones[Vector2Int.zero].transform.parent;
            yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            yield return null;
            Assert.That(_runner == null, Is.True);
            Assert.That(activeRoot == null, Is.True);
            Assert.That(_actor != null, Is.True);
        }

        [UnityTearDown]
		public IEnumerator TearDown()
		{
			for (var i = _owned.Count - 1; i >= 0; i--)
				if (_owned[i] != null)
					Object.Destroy(_owned[i]);
			_owned.Clear();
			yield return null;
		}

		[Test]
		public void StopRestoresTargetParentAndWorldPoseBeforePooling()
		{
			var parent = NewObject("Original parent");
			_actor.transform.SetParent(parent.transform);
			_actor.transform.position = new Vector3(0.3f, 0.4f, 0);
			_runner.SpawnSettings.parentTargetsToCurrentZone = true;
			_runner.StartRunner();
			Assert.That(_actor.transform.parent, Is.EqualTo(_runner.ActiveZones[Vector2Int.zero].transform));
			_runner.StopRunner();
			Assert.That(_actor.transform.parent, Is.EqualTo(parent.transform));
			Assert.That(_actor.transform.position, Is.EqualTo(new Vector3(0.3f, 0.4f, 0)));
			Assert.That(_actor.gameObject.activeInHierarchy, Is.True);
			Assert.That(_runner.IsRunning, Is.False);
		}

		[Test]
		public void NestedTargetsPreserveTheirHierarchy()
		{
			var camera = NewObject("Camera target");
			camera.transform.SetParent(_actor.transform, false);
			_runner.AddTarget(camera.transform);
			_runner.SpawnSettings.parentTargetsToCurrentZone = true;
			_runner.StartRunner();
			Assert.That(camera.transform.parent, Is.EqualTo(_actor.transform));
			_runner.StopRunner();
			Assert.That(camera.transform.parent, Is.EqualTo(_actor.transform));
		}

		[Test]
		public void RemovingAParentedTargetRestoresItsOriginalParent()
		{
			_runner.SpawnSettings.parentTargetsToCurrentZone = true;
			_runner.StartRunner();
			_runner.RemoveTarget(_actor.transform);
			Assert.That(_actor.transform.parent, Is.Null);
			Assert.That(_runner.TryTraverse(CurrentExit(LevelDirection.East), _actor), Is.False);
		}

		[UnityTest]
		public IEnumerator DestroyingRunnerCleansExternalInstancesButPreservesTargets()
		{
			var external = NewObject("External room parent");
			_runner.SpawnSettings.runtimeParent = external.transform;
			_runner.SpawnSettings.parentTargetsToCurrentZone = true;
			_runner.StartRunner();
			Assert.That(external.transform.childCount, Is.EqualTo(1));
			Object.Destroy(_runner.gameObject);
			yield return null;
			yield return null;
			Assert.That(external.transform.childCount, Is.Zero);
			Assert.That(_actor != null, Is.True);
			Assert.That(_actor.transform.parent, Is.Null);
			Assert.That(_actor.gameObject.activeInHierarchy, Is.True);
		}

		[Test]
		public void StopFromSpawnCallbackAbortsCleanly()
		{
			_runner.ZoneSpawned += _ => _runner.StopRunner();
			_runner.StartRunner();
			Assert.That(_runner.ActiveZones, Is.Empty);
			Assert.That(_runner.IsRunning, Is.False);
		}

		[UnityTest]
		public IEnumerator StartFromCallbackIsDeferredAndDoesNotRecurse()
		{
			var calls = 0;
			_runner.ZoneSpawned += _ =>
			{
				calls++;
				if (calls == 1) _runner.StartRunner();
			};
			_runner.StartRunner();
			Assert.That(calls, Is.EqualTo(1));
			yield return null;
			yield return null;
			Assert.That(calls, Is.EqualTo(2));
			Assert.That(_runner.IsRunning, Is.True);
			Assert.That(_runner.ActiveZones.Count, Is.EqualTo(1));
		}

		[Test]
		public void ThrowingSubscriberStopsWithoutLeakingActiveRooms()
		{
			LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: Expected subscriber failure"));
			_runner.ZoneSpawned += _ => throw new InvalidOperationException("Expected subscriber failure");
			_runner.StartRunner();
			Assert.That(_runner.ActiveZones, Is.Empty);
			Assert.That(_runner.IsRunning, Is.False);
		}

		[Test]
		public void NoTargetsDoesNotAcceptArbitraryColliders()
		{
			_runner.RemoveTarget(_actor.transform);
			_runner.StartRunner();
			Assert.That(_runner.TryTraverse(CurrentExit(LevelDirection.East), _actor), Is.False);
		}

		[Test]
		public void AncestorColliderIsNotAcceptedForARegisteredChild()
		{
			_runner.RemoveTarget(_actor.transform);
			var child = NewObject("Registered child");
			child.transform.SetParent(_actor.transform);
			_runner.AddTarget(child.transform);
			_runner.StartRunner();
			Assert.That(_runner.TryTraverse(CurrentExit(LevelDirection.East), _actor), Is.False);
		}

		[Test]
		public void ActivationSeesFinalAlignedPositionAndLifecycleResetsPooledState()
		{
			_b.gameObject.AddComponent<PoolLifecycleProbe>();
			_runner.StartRunner();
			Assert.That(_runner.TryTraverse(CurrentExit(LevelDirection.East), _actor), Is.True);
			var zone = _runner.ActiveZones[Vector2Int.right];
			var probe = zone.GetComponent<PoolLifecycleProbe>();
			Assert.That(probe.SpawnSawAwake, Is.True);
			Assert.That(probe.EnablePosition, Is.EqualTo(new Vector3(4, 0, 0)));
			probe.State = 42;
			_runner.StopRunner();
			Assert.That(probe.State, Is.Zero);
			Assert.That(probe.DespawnCount, Is.EqualTo(1));
			Assert.That(probe.gameObject.activeInHierarchy, Is.False);
		}

		[UnityTest]
		public IEnumerator PooledInstanceGetsAnotherLifecycleCallOnReuse()
		{
			_a.gameObject.AddComponent<PoolLifecycleProbe>();
			_runner.StartRunner();
			var first = _runner.ActiveZones[Vector2Int.zero];
			_runner.StopRunner();
			yield return null;
			_runner.StartRunner();
			Assert.That(_runner.ActiveZones[Vector2Int.zero], Is.SameAs(first));
			Assert.That(first.GetComponent<PoolLifecycleProbe>().SpawnCount, Is.EqualTo(2));
		}

		[Test]
		public void GlobalPoolLimitAppliesAcrossDifferentPrefabs()
		{
			_runner.SpawnSettings.maxPooledInstances = 1;
			_runner.StartRunner();
			Assert.That(_runner.TryTraverse(CurrentExit(LevelDirection.East), _actor), Is.True);
			_runner.StopRunner();
			Assert.That(_runner.PooledZoneCount, Is.EqualTo(1));
			Assert.That(_runner.OwnedZoneCount, Is.EqualTo(1));
		}

		[Test]
		public void DisabledComponentStopsAndRestoresTargets()
		{
			_runner.SpawnSettings.parentTargetsToCurrentZone = true;
			_runner.StartRunner();
			_runner.enabled = false;
			Assert.That(_runner.IsRunning, Is.False);
			Assert.That(_runner.ActiveZones, Is.Empty);
			Assert.That(_actor.transform.parent, Is.Null);
		}

		[UnityTest]
		public IEnumerator OccupiedCellNeverOverridesAnAuthoredRoute()
		{
			var c = Own(LevelTestFactory.CreateZone("C", new[] { LevelDirection.East }, 2));
			LevelTestFactory.Set(_graph, "zones", new List<LevelZoneEntry>
			{
				new() { prefab = _a }, new() { prefab = _b }, new() { prefab = c }
			});
			SetConnections(
				new LevelConnection { fromZoneId = "A", toZoneId = "B", direction = LevelDirection.East, bidirectional = false },
				new LevelConnection { fromZoneId = "B", toZoneId = "C", direction = LevelDirection.West, bidirectional = false });
			_runner.StartRunner();
			Assert.That(_runner.TryTraverse(CurrentExit(LevelDirection.East), _actor), Is.True);
			yield return new WaitForFixedUpdate(); // Actor is away from the landing seam, releasing its gate.
			yield return null;
			Assert.That(_runner.TryTraverse(CurrentExit(LevelDirection.West), _actor), Is.True);
			Assert.That(_runner.ActiveZones[Vector2Int.zero].ZoneId, Is.EqualTo("C"));
		}

		[UnityTest]
		public IEnumerator LandingSeamBlocksBounceUntilAllActorCollidersLeave()
		{
			_runner.StartRunner();
			_actor.transform.position = new Vector3(2, 0, 0);
			Physics2D.SyncTransforms();
			Assert.That(_runner.TryTraverse(CurrentExit(LevelDirection.East), _actor), Is.True);
			var landing = CurrentExit(LevelDirection.West);
			yield return new WaitForFixedUpdate();
			yield return null;
			Assert.That(_runner.TryTraverse(landing, _actor), Is.False);
			_actor.transform.position = new Vector3(4, 0, 0);
			Physics2D.SyncTransforms();
			yield return new WaitForFixedUpdate();
			yield return null;
			Assert.That(_runner.TryTraverse(landing, _actor), Is.True);
		}


		[UnityTest]
		public IEnumerator PhysicsTriggerAutomaticallyTraverses()
		{
			_runner.StartRunner();
			_actor.attachedRigidbody.position = new Vector2(2, 0);
			Physics2D.SyncTransforms();
			yield return new WaitForFixedUpdate();
			yield return new WaitForFixedUpdate();
			Assert.That(_runner.CurrentCell, Is.EqualTo(Vector2Int.right));
		}

		[UnityTest]
		public IEnumerator CompoundActorMustFullyLeaveTheArrivalSeam()
		{
			var child = NewObject("Second player collider");
			child.transform.SetParent(_actor.transform, false);
			child.transform.localPosition = new Vector3(-1.2f, 0, 0);
			child.AddComponent<BoxCollider2D>();
			_runner.StartRunner();
			_actor.transform.position = new Vector3(2, 0, 0);
			Physics2D.SyncTransforms();
			Assert.That(_runner.TryTraverse(CurrentExit(LevelDirection.East), _actor), Is.True);
			var landing = CurrentExit(LevelDirection.West);
			_actor.transform.position = new Vector3(3.2f, 0, 0);
			Physics2D.SyncTransforms();
			yield return new WaitForFixedUpdate();
			yield return null;
			Assert.That(_runner.TryTraverse(landing, _actor), Is.False, "The second collider is still in the seam.");
			_actor.transform.position = new Vector3(5, 0, 0);
			Physics2D.SyncTransforms();
			yield return new WaitForFixedUpdate();
			yield return null;
			Assert.That(_runner.TryTraverse(landing, _actor), Is.True);
		}

		[UnityTest]
		public IEnumerator WarmedCompleteTransitionsAllocateNoManagedMemory()
		{
			SetConnections(new LevelConnection { fromZoneId = "A", toZoneId = "A", direction = LevelDirection.East });
			_runner.SpawnSettings.activeRadius = 0;
			_runner.StartRunner();
			long allocated = 0;
			long elapsedTicks = 0;
			for (var i = 0; i < 144; i++)
			{
				yield return null;
				var exit = CurrentExit(LevelDirection.East);
				var before = GC.GetAllocatedBytesForCurrentThread();
				var start = System.Diagnostics.Stopwatch.GetTimestamp();
				var traversed = _runner.TryTraverse(exit, _actor);
				var ticks = System.Diagnostics.Stopwatch.GetTimestamp() - start;
				var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
				Assert.That(traversed, Is.True);
				_actor.transform.position = _runner.ActiveZones[_runner.CurrentCell].transform.position;
				Physics2D.SyncTransforms();
				if (i < 16)
					continue;
				allocated += bytes;
				elapsedTicks += ticks;
			}
			TestContext.WriteLine("128 warmed transitions: " + allocated + " managed bytes; mean " +
				elapsedTicks * 1000000.0 / System.Diagnostics.Stopwatch.Frequency / 128 + " microseconds.");
			Assert.That(allocated, Is.Zero);
			Assert.That(_runner.ActiveZones.Count, Is.EqualTo(1));
			Assert.That(_runner.OwnedZoneCount, Is.EqualTo(2), "Two instances suffice for this zero-radius loop.");
		}
        [UnityTest]
        public IEnumerator SquareTraversalKeepsPreloadedRoomsBoundedAcrossRadii()
        {
            var directions = new[] { LevelDirection.East, LevelDirection.North, LevelDirection.West, LevelDirection.South };
            var zone = Own(LevelTestFactory.CreateZone("Grid", directions, 2));
            var graph = LevelTestFactory.CreateGraph(zone);
            _owned.Add(graph);
            var connections = new List<LevelConnection>();
            foreach (var direction in directions)
                connections.Add(new LevelConnection { fromZoneId = "Grid", toZoneId = "Grid", direction = direction, bidirectional = false });
            LevelTestFactory.Set(graph, "fixedConnections", connections);
            LevelTestFactory.Set(_runner, "graph", graph);
            _runner.SpawnSettings.recenterOnTransition = true;
            _runner.SpawnSettings.maxPoolInstancesPerPrefab = 16;
            _runner.SpawnSettings.maxPooledInstances = 16;
            for (var radius = 0; radius <= 2; radius++)
            {
                _runner.SpawnSettings.activeRadius = radius;
                _runner.SpawnSettings.preloadRadius = radius;
                _runner.StartRunner();
                var activeLimit = (2 * radius + 1) * (2 * radius + 1);
                for (var step = 0; step < 256; step++)
                {
                    yield return null;
                    var direction = directions[(step / 8) % directions.Length];
                    Assert.That(_runner.TryTraverse(CurrentExit(direction), _actor), Is.True, "radius=" + radius + ", step=" + step);
                    var current = _runner.ActiveZones[_runner.CurrentCell];
                    _actor.transform.position = current.transform.position;
                    Physics2D.SyncTransforms();
                    Assert.That(current.transform.position.sqrMagnitude, Is.LessThan(0.0001f));
                    Assert.That(_runner.ActiveZones.Count, Is.LessThanOrEqualTo(activeLimit));
                    Assert.That(_runner.PooledZoneCount, Is.LessThanOrEqualTo(16));
                    Assert.That(_runner.OwnedZoneCount, Is.LessThanOrEqualTo(activeLimit + 16));
                }
                Assert.That(_runner.CurrentCell, Is.EqualTo(Vector2Int.zero), "Eight square circuits return to the starting logical cell.");
                _runner.StopRunner();
                Assert.That(_runner.ActiveZones, Is.Empty);
                Assert.That(_runner.OwnedZoneCount, Is.LessThanOrEqualTo(16));
            }
        }
		private LevelExit CurrentExit(LevelDirection direction)
		{
			_runner.ActiveZones[_runner.CurrentCell].TryGetExit(direction, null, out var exit);
			return exit;
		}

		private void SetConnections(params LevelConnection[] connections) =>
			LevelTestFactory.Set(_graph, "fixedConnections", connections.ToList());

		private LevelZone Own(LevelZone zone) { _owned.Add(zone.gameObject); return zone; }
		private GameObject NewObject(string name) { var value = new GameObject(name); _owned.Add(value); return value; }
	}
}