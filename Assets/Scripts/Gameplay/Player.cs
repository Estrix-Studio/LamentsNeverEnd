using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private float movespeed = 5.0f;
        [SerializeField] DefaultInputActions inputActions;

        private Rigidbody2D rb;

        private List<IInteractableObject> _currentInteractables = new List<IInteractableObject>();

        private InputAction _interactAction;

        [SerializeField] private GameObject InteractionPrompt;
        
        private void Awake()
        {
            inputActions = new DefaultInputActions();
            inputActions.Enable();
            
            _interactAction = inputActions.FindAction("Interact/Interact");
            _interactAction.performed += InteractActionOnperformed;  
            
            rb = GetComponent<Rigidbody2D>();
        }

        private void InteractActionOnperformed(InputAction.CallbackContext obj)
        {
            foreach (var action in _currentInteractables)   
            {
                action.Interact();
            }
        }

        private void FixedUpdate()
        {
            Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
            Vector2 dir = input.normalized * movespeed;
            
            rb.MovePosition(rb.position + dir * Time.fixedDeltaTime);
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
                InteractionPrompt.SetActive(true);
            }
            else
            {
                InteractionPrompt.SetActive(false);
            }
        }


        private void OnDestroy()
        {
            _interactAction.performed -= InteractActionOnperformed;
            inputActions.Disable();
        }
    }
}