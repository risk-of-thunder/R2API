using RoR2;
using RoR2.UI;
using System;
using System.Reflection;
using R2API.Utils;
using UnityEngine;

namespace R2API {
    public class Notification : MonoBehaviour {
        public GameObject RootObject { get; set; }
        public GenericNotification GenericNotification { get; set; }
        public Func<string> GetTitle { get; set; }
        public Func<string> GetDescription { get; set; }
        public Transform Parent { get; set; }

        private static MethodInfo UpdateLabel =
            typeof(LanguageTextMeshController).GetMethodCached("UpdateLabel");

        private void Awake() {
            // TODO: Figure out HUD transform for canvas and scaling
            Parent = RoR2Application.instance.mainCanvas.transform;
            RootObject = Instantiate(Resources.Load<GameObject>("Prefabs/NotificationPanel2"));
            GenericNotification = RootObject.GetComponent<GenericNotification>();
            GenericNotification.transform.SetParent(Parent);
            GenericNotification.iconImage.enabled = false;
        }

        private void Update() {
            if (GenericNotification == null) {
                Destroy(this);
                return;
            }

            GenericNotification.titleText.SetFieldValue("resolvedString", GetTitle());
            UpdateLabel.Invoke(GenericNotification.titleText, null);

            GenericNotification.descriptionText.SetFieldValue("resolvedString", GetDescription());
            UpdateLabel.Invoke(GenericNotification.descriptionText, null);
        }

        private void OnDestroy() {
            Destroy(GenericNotification);
            Destroy(RootObject);
        }

        public void SetIcon(Texture texture) {
            GenericNotification.iconImage.enabled = true;
            GenericNotification.iconImage.texture = texture;
        }

        public void SetPosition(Vector3 position) {
            RootObject.transform.position = position;
        }
    }
}
