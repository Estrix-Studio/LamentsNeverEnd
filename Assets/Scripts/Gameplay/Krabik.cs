using Gameplay;
using UnityEngine;
using Utility;

public class Krabik : MonoBehaviour, IInteractableObject
{
    [SerializeField] private EventName EventName;

    [SerializeField] private DialogInfo DialogInfo;
    
    public void Interact()
    {
        Debug.Log("Interact with krabik!");
    }
}
