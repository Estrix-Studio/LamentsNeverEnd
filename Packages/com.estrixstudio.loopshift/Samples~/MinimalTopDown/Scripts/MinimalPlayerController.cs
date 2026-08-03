using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LoopShift.Samples.MinimalTopDown
{
	[RequireComponent(typeof(Rigidbody2D))]
	public sealed class MinimalPlayerController : MonoBehaviour
	{
		[SerializeField, Min(0f)] private float speed = 6f;

		private Rigidbody2D _body;

		private void Awake()
		{
			_body = GetComponent<Rigidbody2D>();
		}

		private void FixedUpdate()
		{
			_body.velocity = ReadMovement() * speed;
		}

		private static Vector2 ReadMovement()
		{
#if ENABLE_INPUT_SYSTEM
			var keyboard = Keyboard.current;
			if (keyboard == null)
				return Vector2.zero;

			var movement = Vector2.zero;
			if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
				movement.x -= 1f;
			if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
				movement.x += 1f;
			if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
				movement.y -= 1f;
			if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
				movement.y += 1f;
			return movement.normalized;
#else
			return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
#endif
		}
	}
}
