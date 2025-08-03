using System;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay
{
    public class Player : MonoBehaviour
    {
        private static readonly int WalkingFront = Animator.StringToHash("Walking_Front");
        private static readonly int WalkingBack = Animator.StringToHash("Walking_Back");
        private static readonly int WalkingLeft = Animator.StringToHash("Walking_Left");
        private static readonly int WalkingRight = Animator.StringToHash("Walking_Right");
        
        [SerializeField] private float movespeed = 5.0f;
        [SerializeField] InputSystem_Actions inputActions;

        private Rigidbody2D rb;

        private List<IInteractableObject> _currentInteractables = new List<IInteractableObject>();

        private InputAction _interactAction;

        [SerializeField] private GameObject InteractionPrompt;

        private Animator _animator;

        private void Awake()
        {
            inputActions = new InputSystem_Actions();
            inputActions.Enable();
            
            _interactAction = inputActions.FindAction("Interact/TriggerObject");
            _interactAction.performed += InteractActionOnperformed;  
            
            rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            
            UpdateInteractionPrompt();
        }

        private void InteractActionOnperformed(InputAction.CallbackContext obj)
        {
            foreach (var action in _currentInteractables)   
            {
                action.Interact();
            }
        }

        private ZoneSide lastAnim;
        
        private void FixedUpdate()
        {
            Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
            Vector2 dir = input.normalized * movespeed;
            
            rb.MovePosition(rb.position + dir * Time.fixedDeltaTime);
            UpdateAnimation(input);
        }

        private void UpdateAnimation(Vector2 input)
        {
            var newState = ZoneSide.Center;
            if (input.x > 0 && input.y > 0)
            {
                if (lastAnim == ZoneSide.Top)
                    newState = ZoneSide.Top;
                else
                    newState = ZoneSide.Right;
            }
            else if (input.x < 0 && input.y > 0)
            {
                if (lastAnim == ZoneSide.Top)
                    newState = ZoneSide.Top;
                else 
                    newState = ZoneSide.Left;
            }
            else if (input.x > 0 && input.y < 0)
            {
                if (lastAnim == ZoneSide.Bottom)
                    newState = ZoneSide.Bottom;
                else
                    newState = ZoneSide.Right;
            }
            else if (input.x < 0 && input.y < 0)
            {
                if (lastAnim == ZoneSide.Bottom)
                    newState = ZoneSide.Bottom;
                else
                    newState = ZoneSide.Left;
            }
            else if (input.x > 0)
            {
                newState = ZoneSide.Right;
            }
            else if (input.x < 0)
            {
                newState = ZoneSide.Left;
            }else if (input.y > 0)
            {
                newState = ZoneSide.Top;
            }else if (input.y < 0)
            {
                newState = ZoneSide.Bottom;
            }
            else
            {
                newState = ZoneSide.None;
            }

            if (newState == lastAnim)
                return;

            lastAnim = newState;
            switch (lastAnim)
            {
                case ZoneSide.None:
                    _animator.SetBool(WalkingRight, false);
                    _animator.SetBool(WalkingLeft, false);
                    _animator.SetBool(WalkingBack, false);
                    _animator.SetBool(WalkingFront, false);
                    break;
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

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Debug.Log($"Collided with {collision.gameObject.name}");
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Debug.Log($"Triggered by {collision.gameObject.name}");

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

        private void UpdateInteractionPrompt()
        {
            if (_currentInteractables.Count > 0)
            {
                if (InteractionPrompt)
                    InteractionPrompt.SetActive(true);
            }
            else
            {
                if (InteractionPrompt)
                    InteractionPrompt?.SetActive(false);
            }
        }
        private void OnDestroy()
        {
            _interactAction.performed -= InteractActionOnperformed;
            inputActions.Disable();
        }
    }
}