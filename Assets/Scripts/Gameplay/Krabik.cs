using UI;
using UnityEngine;
using Utility;

namespace Gameplay
{
    public class Krabik : MonoBehaviour, IInteractableObject
    {
        [SerializeField] private EventName EventName;

        [SerializeField] private DialogInfo DialogInfo;

        private bool hasBeenInteracted = false;
    
        public void Interact()
        {
            if (hasBeenInteracted)
                return;
            Debug.Log("Start Dialog");
            hasBeenInteracted = true;
            DialogController.Instance.OnDialogEnd += InstanceOnOnDialogEnd;
            DialogController.Instance.StartDialog(DialogInfo.Phrases);
        }

        private void OnDestroy()
        {
            DialogController.Instance.OnDialogEnd -= InstanceOnOnDialogEnd;
        }

        private void InstanceOnOnDialogEnd()
        {
            GameData.Instance.CompleteEvent(EventName);
        }
    }
}
