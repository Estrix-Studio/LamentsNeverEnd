using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Level
{
#if UNITY_EDITOR
    
    [RequireComponent(typeof(CompositeCollider2D))]
    public class ShadowCaster : MonoBehaviour
    {
        private static readonly FieldInfo meshField =
            typeof(ShadowCaster2D).GetField("m_Mesh", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo shapePathField =
            typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo shapePathHashField =
            typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly MethodInfo generateShadowMeshMethod = typeof(ShadowCaster2D)
            .Assembly
            .GetType("UnityEngine.Rendering.Universal.ShadowUtility")
            .GetMethod("GenerateShadowMesh", BindingFlags.Public | BindingFlags.Static);

        [SerializeField] private bool selfShadows = true;

        private CompositeCollider2D tilemapCollider;

        public void Create()
        {
            DestroyOldShadowCasters();
            tilemapCollider = GetComponent<CompositeCollider2D>();

            for (var i = 0; i < tilemapCollider.pathCount; i++)
            {
                var pathVertices = new Vector2[tilemapCollider.GetPathPointCount(i)];
                tilemapCollider.GetPath(i, pathVertices);
                var shadowCaster = new GameObject("shadow_caster_" + i);
                shadowCaster.transform.parent = gameObject.transform;
                var shadowCasterComponent = shadowCaster.AddComponent<ShadowCaster2D>();
                shadowCasterComponent.selfShadows = selfShadows;

                var testPath = new Vector3[pathVertices.Length];
                for (var j = 0; j < pathVertices.Length; j++) testPath[j] = pathVertices[j];

                shapePathField.SetValue(shadowCasterComponent, testPath);
                shapePathHashField.SetValue(shadowCasterComponent, Random.Range(int.MinValue, int.MaxValue));
                meshField.SetValue(shadowCasterComponent, new Mesh());
                generateShadowMeshMethod.Invoke(shadowCasterComponent,
                    new[]
                    {
                        meshField.GetValue(shadowCasterComponent), shapePathField.GetValue(shadowCasterComponent)
                    });
            }
        }

        public void DestroyOldShadowCasters()
        {
            var tempList = transform.Cast<Transform>().ToList();
            foreach (var child in tempList) DestroyImmediate(child.gameObject);
        }
    }

    [CustomEditor(typeof(ShadowCaster))]
    public class ShadowCaster2DTileMapEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create"))
            {
                var creator = (ShadowCaster)target;
                creator.Create();
            }

            if (GUILayout.Button("Remove Shadows"))
            {
                var creator = (ShadowCaster)target;
                creator.DestroyOldShadowCasters();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
    
#endif
}
