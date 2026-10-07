using UnityEngine;

namespace Level
{
	// Keep this component in the runtime assembly so scene references survive player builds.
	// Mesh authoring and Undo support live in Assets/Editor/Level.
	[RequireComponent(typeof(CompositeCollider2D))]
	public class ShadowCaster : MonoBehaviour
	{
		[SerializeField] private bool selfShadows = true;
		public bool SelfShadows => selfShadows;
	}
}