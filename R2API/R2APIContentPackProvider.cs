using RoR2.ContentManagement;
using System.Collections;

namespace R2API {
    public class R2APIContentPackProvider : IContentPackProvider {
        public string identifier => "R2API";

        internal static ContentPack ContentPack = new ContentPack();

        internal static void Init() {
            ContentManager.collectContentPackProviders += AddContentPackToGame;
        }

        private static void AddContentPackToGame(ContentManager.AddContentPackProviderDelegate addContentPackProvider) {
            addContentPackProvider(new R2APIContentPackProvider());
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args) {
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args) {
            ContentPack.Copy(ContentPack, args.output);

            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args) {
            args.ReportProgress(1f);
            yield break;
        }
    }
}
