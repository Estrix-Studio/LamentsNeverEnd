using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private float movespeed = 5.0f;
        [SerializeField] DefaultInputActions inputActions;

        private void Awake()
        {
            inputActions = new DefaultInputActions();
            inputActions.Enable();
        }

        private void Update()
        {
            Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
            Vector3 dir = new Vector3(input.x, input.y, 0) * movespeed * Time.deltaTime;
            
            transform.Translate(dir, Space.World);
        }


        private void OnDestroy()
        {
            inputActions.Disable();
        }
    }
}