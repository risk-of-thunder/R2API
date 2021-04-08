using RoR2.ContentManagement;
using System;
using System.Collections;

namespace R2API {
    public class R2APIContentPackProvider : IContentPackProvider {
        public string identifier => "R2API";

        internal static ContentPack ContentPack = new ContentPack();

        internal static Action<ContentPack> WhenContentPackReady;

        internal static void Init() {
            ContentManager.collectContentPackProviders += AddCustomContent;
        }

        private static void AddCustomContent(ContentManager.AddContentPackProviderDelegate addContentPackProvider) {
            if (WhenContentPackReady != null) {
                foreach (Action<ContentPack> @event in WhenContentPackReady.GetInvocationList()) {
                    try {
                        @event(ContentPack);
                    }
                    catch (Exception e) {
                        R2API.Logger.LogError(e);
                    }
                }
            }

            addContentPackProvider(new R2APIContentPackProvider());
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args) {
            args.ReportProgress(1);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args) {
            ContentPack.Copy(ContentPack, args.output);

            args.ReportProgress(1);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args) {
            args.ReportProgress(1);
            yield break;
        }
    }
}
