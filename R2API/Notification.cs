using RoR2;
using RoR2.UI;
using System;
using R2API.Utils;
using UnityEngine;

namespace R2API {
    public class Notification : MonoBehaviour {
        public GameObject? RootObject { get; set; }
        public GenericNotification? GenericNotification { get; set; }
        public Func<string>? GetTitle { get; set; }
        public Func<string>? GetDescription { get; set; }
        public Transform? Parent { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "MonoBehaviour")]
        private void Awake() {
            // TODO: Figure out HUD transform for canvas and scaling
            Parent = RoR2Application.instance.mainCanvas.transform;
            RootObject = Instantiate(Resources.Load<GameObject>("Prefabs/NotificationPanel2"));
            GenericNotification = RootObject.GetComponent<GenericNotification>();
            GenericNotification.transform.SetParent(Parent);
            GenericNotification.iconImage.enabled = false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "MonoBehaviour")]
        private void Update() {
            if (GenericNotification == null) {
                Destroy(this);
                return;
            }

            GenericNotification.titleText.SetFieldValue("resolvedString", GetTitle());
            GenericNotification.titleText.InvokeMethod("UpdateLabel");

            GenericNotification.descriptionText.SetFieldValue("resolvedString", GetDescription());
            GenericNotification.descriptionText.InvokeMethod("UpdateLabel");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "MonoBehaviour")]
        private void OnDestroy() {
            Destroy(GenericNotification);
            Destroy(RootObject);
        }

        public void SetIcon(Texture? texture) {
            GenericNotification.iconImage.enabled = true;
            GenericNotification.iconImage.texture = texture;
        }

        public void SetPosition(Vector3 position) {
            RootObject.transform.position = position;
        }
    }
}
