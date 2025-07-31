using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private float movespeed = 5.0f;
        [SerializeField] DefaultInputActions inputActions;

        private Rigidbody2D rb;

        private void Awake()
        {
            inputActions = new DefaultInputActions();
            inputActions.Enable();
            
            rb = GetComponent<Rigidbody2D>();
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
        }


        private void OnDestroy()
        {
            inputActions.Disable();
        }
    }
}