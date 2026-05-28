using Save;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LevelObjects.Interactable
{
    public class OpenShop : Interactable
    {
        [SerializeField] private GameObject Shop;

        public override void Interact()
        {
            if (!Shop.activeSelf)
            {
                Shop.SetActive(true);
            }
            else
            {
                Shop.SetActive(false);
            }
        }

        public override InteractableObjectState SaveState()
        {
            return new InteractableObjectState();
            throw new NotImplementedException();
        }

        public override void LoadState(InteractableObjectState state)
        {
            return;
            throw new NotImplementedException();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") && Shop != null)
            {
                Shop.SetActive(false);
            }
        }
    }
}
