using UnityEngine;

namespace TOP.Core
{
    public interface IInteractable
    {
        float InteractionRange { get; }
        void Interact(uint interactorId);
        void ShowInteractionUI();
        void HideInteractionUI();
    }
}
