using UnityEngine;

namespace Utility
{
	[CreateAssetMenu(fileName = "TorchData", menuName = "Torch Data")]
	public class TorchData : ScriptableObject
	{
		[Header("Intensity Ranges")] [Range(0.1f, 1f)]
		public float minIntensity;

		[Range(0.1f, 1.5f)] public float maxIntensity;

		[Header("Fixed Intensities")] public float defaultIntensity = 1.0f;

		public float dimIntensity;
		public float normalIntensity;

		[Header("Transitions")] public float transitionSpeed;

		public float flickTime;
		public float flickBackTime;
		public Vector2 flickCooldown;

		[Header("Flicker")]
		public Vector2 flickRange
		{
			get => new(minIntensity, maxIntensity);
			set
			{
				minIntensity = value.x;
				maxIntensity = value.y;
			}
		}
	}
}