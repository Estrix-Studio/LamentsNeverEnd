using System;
using System.Linq;
using Gameplay;
using Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace LamentsNeverEnd.Editor
{
	/// <summary>Dependency-free regression and content checks; usable from Unity batch mode.</summary>
	public static class ProjectChecks
	{
		[MenuItem("Tools/Laments Never End/Run Project Checks")]
		public static void Run()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
				throw new InvalidOperationException("Run project checks outside Play mode.");

			// Isolate checks from the real session and its subscribers.
			var data = (GameData)Activator.CreateInstance(typeof(GameData), true);
			var checks = 0;
			Action<bool, string> check = (passed, description) =>
			{
				if (!passed)
					throw new InvalidOperationException("Project check failed: " + description);
				checks++;
				Debug.Log("PASS: " + description);
			};

			var original = ZoneConfig.DefaultZones.First(zone => zone.Zone == data.firstZone);
			data.currentConnections[data.firstZone].Left = CycleZoneID.Finish;
			check(original.Left != CycleZoneID.Finish, "Runtime connections do not mutate defaults");
			data.Reset();
			check(data.currentConnections[data.firstZone].Left == original.Left, "Reset restores room connections");
			check(!ReferenceEquals(data.currentConnections[data.firstZone], original), "Reset creates independent room records");

			check(data.TryRegisterCondition(EventName.Dialog_1Completed), "First condition registers");
			check(!data.TryRegisterCondition(EventName.Dialog_1Completed), "Duplicate condition is rejected");
			check(!data.TryRegisterCondition(EventName.None), "Unconfigured condition is rejected");

			var notifications = 0;
			data.OnEventCompleted += completed =>
			{
				notifications++;
				data.CompleteEvent(completed); // Reentrant completion must be harmless.
			};
			data.CompleteEvent(EventName.Dialog_1Completed);
			data.CompleteEvent(EventName.Dialog_1Completed);
			data.CompleteEvent(EventName.None);
			check(notifications == 1, "Completion is idempotent, including reentrant subscribers");
			check(!data.IsEventCompleted(EventName.None), "None is not a completed event");
			data.spawnedConditions.Clear();
			check(!data.TryRegisterCondition(EventName.Dialog_1Completed), "Completed condition cannot respawn");
			data.Reset();
			check(!data.IsEventCompleted(EventName.Dialog_1Completed) && data.spawnedConditions.Count == 0,
				"Reset clears progression");
			check(data.TryRegisterCondition(EventName.Dialog_1Completed), "Conditions register again after reset");

			var zoneIds = ZoneConfig.DefaultZones.Select(zone => zone.Zone).ToArray();
			check(zoneIds.Distinct().Count() == zoneIds.Length, "Room configuration IDs are unique");
			var terminal = ZoneConfig.DefaultZones.First(zone => zone.Zone == CycleZoneID.Room3_5);
			check(new[] { terminal.Left, terminal.Right, terminal.Top, terminal.Bottom }.All(id => id == CycleZoneID.None),
				"Victory room has no nonexistent outgoing neighbor");
			foreach (var zone in ZoneConfig.DefaultZones)
				foreach (var target in new[] { zone.Left, zone.Right, zone.Top, zone.Bottom })
					check(target == CycleZoneID.None || zoneIds.Contains(target),
						"Connection target exists: " + zone.Zone + " -> " + target);

			var prefabs = Resources.LoadAll<CyclingZone>("Zones");
			foreach (var zoneId in zoneIds)
				check(prefabs.Count(prefab => prefab.ZoneID == zoneId) == 1,
					"Exactly one Resources/Zones prefab for " + zoneId);

			check(prefabs.Single(prefab => prefab.ZoneID == CycleZoneID.Room3_5)
				.GetComponentInChildren<WinCollider>(true) != null, "Victory room contains its win trigger");

			var winTrigger = prefabs.Single(prefab => prefab.ZoneID == CycleZoneID.Room3_5)
                .GetComponentInChildren<WinCollider>(true).GetComponent<Collider2D>();
            check(winTrigger != null && winTrigger.enabled && winTrigger.isTrigger, "Victory collider is enabled and configured as a trigger");

            CheckShadowAuthoring(check);

			var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
			var required = new[] { "MainMenu", "Gameplay", "Options", "Credits", "LossScreen", "WinScreen" };
			foreach (var sceneName in required)
				check(scenes.Any(scene => scene.path == "Assets/Scenes/" + sceneName + ".unity" &&
				                         AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) != null),
					"Enabled scene exists: " + sceneName);
			check(scenes.Length > (int)Scene.Gameplay &&
			      scenes[(int)Scene.Gameplay].path == "Assets/Scenes/Gameplay.unity",
				"Gameplay build index matches Scene enum");

			Debug.Log("PROJECT CHECKS PASSED: " + checks);
		}

		private static void CheckShadowAuthoring(Action<bool, string> check)
		{
			var root = new GameObject("Shadow authoring regression");
			try
			{
				root.transform.position = new Vector3(12, 4, 0);
				root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
				var composite = root.AddComponent<CompositeCollider2D>();
				var box = root.AddComponent<BoxCollider2D>();
				box.compositeOperation = Collider2D.CompositeOperation.Merge;
				composite.GenerateGeometry();
				var creator = root.AddComponent<Level.ShadowCaster>();
				var unrelated = new GameObject("Unrelated child");
				unrelated.transform.SetParent(root.transform, false);

				ShadowCasterEditor.Create(creator);
				var casters = root.GetComponentsInChildren<UnityEngine.Rendering.Universal.ShadowCaster2D>();
				check(casters.Length > 0, "Shadow geometry is generated from composite paths");
				check(casters.All(caster => caster.transform.localPosition == Vector3.zero &&
				                           caster.shapePath.Length >= 3),
					"Shadow paths preserve local alignment under a translated parent");
				ShadowCasterEditor.RemoveGenerated(creator);
				check(unrelated != null && root.transform.childCount == 1,
					"Removing generated shadows preserves unrelated children");
			}
			finally
			{
				UnityEngine.Object.DestroyImmediate(root);
			}
		}
	}
}