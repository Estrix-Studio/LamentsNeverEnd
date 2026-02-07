using System;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using Utility;

namespace Gameplay
{
	public class Player : MonoBehaviour
	{
		private static readonly int WalkingFront = Animator.StringToHash("Walking_Front");
		private static readonly int WalkingBack = Animator.StringToHash("Walking_Back");
		private static readonly int WalkingLeft = Animator.StringToHash("Walking_Left");
		private static readonly int WalkingRight = Animator.StringToHash("Walking_Right");

		[SerializeField] private float movespeed = 5.0f;

		[SerializeField] private GameObject interactionPrompt;

		private readonly List<IInteractableObject> _currentInteractables = new();

		private Animator _animator;

		private InputAction _interactAction;

		private bool _isWalking = true;
		private InputAction _toggleAction;

		private TorchController _torch;
		private InputAction _uiAction;
		[SerializeField] private InputSystem_Actions _inputActions;

		private ZoneSide _lastAnim;

		private Rigidbody2D _rb;

		public bool isTorchLit => _torch.isLit;

		private void Awake()
		{
			_inputActions = new InputSystem_Actions();
			_inputActions.Enable();

			_interactAction = _inputActions.Interact.TriggerObject;
			_interactAction.performed += InteractActionOnperformed;

			_toggleAction = _inputActions.Torch.Toggle;
			_toggleAction.Enable();
			_toggleAction.performed += ToggleActionOnperformed;

			_inputActions.UI.Click.performed += UiActionOnperformed;
			_inputActions.UI.Submit.performed += UiActionOnperformed;
			_inputActions.UI.RightClick.performed += UiActionOnperformed;
			_inputActions.UI.MiddleClick.performed += UiActionOnperformed;
			_inputActions.UI.Cancel.performed += UiActionOnperformed;

			_rb = GetComponent<Rigidbody2D>();
			_animator = GetComponent<Animator>();

			UpdateInteractionPrompt();
		}

		private void Start()
		{
			_torch = FindFirstObjectByType<TorchController>();
			DialogController.instance.OnDialogEnd += InstanceOnOnDialogEnd;
			DialogController.instance.OnDialogStart += InstanceOnOnDialogStart;
		}

		private void FixedUpdate()
		{
			if (_isWalking)
			{
				var input = _inputActions.Player.Move.ReadValue<Vector2>();
				var dir = input.normalized * movespeed;

				_rb.MovePosition(_rb.position + dir * Time.fixedDeltaTime);
				UpdateAnimation(input);
			}
			else
			{
				UpdateAnimation(Vector2.zero);
			}
		}

		private void OnDestroy()
		{
			_toggleAction.performed -= ToggleActionOnperformed;
			_toggleAction.Disable();
			_interactAction.performed -= InteractActionOnperformed;
			_inputActions.Disable();

			_inputActions.UI.Click.performed -= UiActionOnperformed;
			_inputActions.UI.Click.Disable();
			_inputActions.UI.Submit.performed -= UiActionOnperformed;
			_inputActions.UI.Submit.Disable();
			_inputActions.UI.RightClick.performed -= UiActionOnperformed;
			_inputActions.UI.RightClick.Disable();
			_inputActions.UI.MiddleClick.performed -= UiActionOnperformed;
			_inputActions.UI.MiddleClick.Disable();
			_inputActions.UI.Cancel.performed -= UiActionOnperformed;
			_inputActions.UI.Cancel.Disable();
		}

		private void OnCollisionEnter2D(Collision2D collision)
		{
			if (collision.gameObject.TryGetComponent<Enemy>(out var enemy)) GameData.instance.RestartGame();
		}

		private void OnTriggerEnter2D(Collider2D collision)
		{
			// Debug.Log($"Triggered by {collision.gameObject.name}");

			if (collision.TryGetComponent<IInteractableObject>(out var interactable))
			{
				_currentInteractables.Add(interactable);
				UpdateInteractionPrompt();
			}
		}

		private void OnTriggerExit2D(Collider2D other)
		{
			if (other.TryGetComponent<IInteractableObject>(out var interactable))
			{
				_currentInteractables.Remove(interactable);
				UpdateInteractionPrompt();
			}
		}

		private void UiActionOnperformed(InputAction.CallbackContext obj)
		{
			if (!_isWalking) DialogController.instance.DisplayNextPhrase();
		}

		private void InstanceOnOnDialogStart()
		{
			_isWalking = false;
		}

		private void InstanceOnOnDialogEnd()
		{
			_isWalking = true;
		}

		private void InteractActionOnperformed(InputAction.CallbackContext obj)
		{
			if (!_isWalking)
				return;
			foreach (var action in _currentInteractables) action.Interact();
		}

		private void UpdateAnimation(Vector2 input)
		{
			ZoneSide newState;
			if (input is { x: > 0, y: > 0 })
			{
				newState = _lastAnim == ZoneSide.Top ? ZoneSide.Top : ZoneSide.Right;
			}
			else
				newState = input.x switch
				{
					< 0 when input.y > 0 => _lastAnim == ZoneSide.Top ? ZoneSide.Top : ZoneSide.Left,
					> 0 when input.y < 0 => _lastAnim == ZoneSide.Bottom ? ZoneSide.Bottom : ZoneSide.Right,
					< 0 when input.y < 0 => _lastAnim == ZoneSide.Bottom ? ZoneSide.Bottom : ZoneSide.Left,
					> 0 => ZoneSide.Right,
					< 0 => ZoneSide.Left,
					_ => input.y switch
					{
						> 0 => ZoneSide.Top,
						< 0 => ZoneSide.Bottom,
						_ => ZoneSide.None
					}
				};

			if (newState == _lastAnim)
				return;

			_lastAnim = newState;
			switch (_lastAnim)
			{
				case ZoneSide.None:
				case ZoneSide.Center:
					_animator.SetBool(WalkingRight, false);
					_animator.SetBool(WalkingLeft, false);
					_animator.SetBool(WalkingBack, false);
					_animator.SetBool(WalkingFront, false);
					break;
				case ZoneSide.Right:
					_animator.SetBool(WalkingRight, true);
					_animator.SetBool(WalkingLeft, false);
					_animator.SetBool(WalkingBack, false);
					_animator.SetBool(WalkingFront, false);
					break;
				case ZoneSide.Bottom:
					_animator.SetBool(WalkingRight, false);
					_animator.SetBool(WalkingLeft, false);
					_animator.SetBool(WalkingBack, false);
					_animator.SetBool(WalkingFront, true);
					break;
				case ZoneSide.Top:
					_animator.SetBool(WalkingRight, false);
					_animator.SetBool(WalkingLeft, false);
					_animator.SetBool(WalkingBack, true);
					_animator.SetBool(WalkingFront, false);
					break;
				case ZoneSide.Left:
					_animator.SetBool(WalkingRight, false);
					_animator.SetBool(WalkingLeft, true);
					_animator.SetBool(WalkingBack, false);
					_animator.SetBool(WalkingFront, false);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void UpdateInteractionPrompt()
		{
			if (_currentInteractables.Count > 0)
			{
				if (interactionPrompt)
					interactionPrompt.SetActive(true);
			}
			else
			{
				if (interactionPrompt)
					interactionPrompt?.SetActive(false);
			}
		}

		private void ToggleActionOnperformed(InputAction.CallbackContext obj)
		{
			if (!_isWalking)
				return;
			_torch.Toggle();
		}
	}
}