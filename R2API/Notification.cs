using RoR2;
using RoR2.UI;
using System;
using System.Reflection;
using UnityEngine;

namespace R2API {
    public class Notification : MonoBehaviour {
        public GameObject RootObject { get; set; }
        public GenericNotification GenericNotification { get; set; }
        public Func<string> GetTitle { get; set; }
        public Func<string> GetDescription { get; set; }
        public Transform Parent { get; set; }

        private static FieldInfo ResolvedText =
            typeof(LanguageTextMeshController).GetFieldCached("resolvedString", BindingFlags.Instance | BindingFlags.NonPublic);

        private static MethodInfo UpdateLabel =
            typeof(LanguageTextMeshController).GetMethodCached("UpdateLabel", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

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


            ResolvedText.SetValue(GenericNotification.titleText, GetTitle());
            UpdateLabel.Invoke(GenericNotification.titleText, null);

            ResolvedText.SetValue(GenericNotification.descriptionText, GetDescription());
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
