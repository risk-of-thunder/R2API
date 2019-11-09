using R2API.Utils;
using RoR2;
using System;
using System.Collections.ObjectModel;
using UnityEngine;

namespace R2API {
    [R2APISubmodule]
    public class DifficultyAPI{

        private static bool difficultyAlreadyAdded = false;

        public static event EventHandler difficultyCatalogReady;

        public static ObservableCollection<DifficultyDef> difficultyDefinitions = new ObservableCollection<DifficultyDef>();
        /// <summary>
        /// Add a DifficultyDef to the list of available difficulties.
        /// This must be called before the DifficultyCatalog inits, so before plugin.Start()
        /// Value for DifficultyDef.index is set by r2api, so, you'll get your new index returned.
        /// If this is called after the DifficultyCatalog inits then this will return -1 and ignore the difficulty
        /// </summary>
        /// <param name="difficulty">The difficulty to add.</param>
        /// <returns>-1 if it fails. Your index otherwise.</returns>
        public static int AddDifficulty(DifficultyDef difficulty) {
            if (difficultyAlreadyAdded) {
                R2API.Logger.LogError($"Tried to add difficulty: {difficulty.nameToken} after survivor list was created");
                return -1;
            }

            difficultyDefinitions.Add(difficulty);

            return 2+difficultyDefinitions.Count;
        }


        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            difficultyCatalogReady?.Invoke(null, null);
            On.RoR2.DifficultyCatalog.GetDifficultyDef += DifficultyCatalog_GetDifficultyDef;
            On.RoR2.RuleDef.FromDifficulty += RuleDef_FromDifficulty;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.DifficultyCatalog.GetDifficultyDef -= DifficultyCatalog_GetDifficultyDef;
            On.RoR2.RuleDef.FromDifficulty -= RuleDef_FromDifficulty;
        }

        private static DifficultyDef DifficultyCatalog_GetDifficultyDef(On.RoR2.DifficultyCatalog.orig_GetDifficultyDef orig, DifficultyIndex difficultyIndex)
        {
            int index = (int) difficultyIndex;
            int length = DifficultyCatalog.difficultyDefs.Length;
            if (index >= length){
                return difficultyDefinitions[index-length];
            }
            return orig(difficultyIndex);
        }
        private static RuleDef RuleDef_FromDifficulty(On.RoR2.RuleDef.orig_FromDifficulty orig)
        {
            RuleDef ruleChoices = orig();
            difficultyAlreadyAdded = true;
            for ( int i =0; i<difficultyDefinitions.Count;i++){
                DifficultyDef difficultyDef = difficultyDefinitions[i];
                RuleChoiceDef choice = ruleChoices.AddChoice(Language.GetString(difficultyDef.nameToken), null, false);
                choice.spritePath = difficultyDef.iconPath;
                choice.tooltipNameToken = difficultyDef.nameToken;
                choice.tooltipNameColor = difficultyDef.color;
                choice.tooltipBodyToken = difficultyDef.descriptionToken;
                choice.difficultyIndex = (DifficultyIndex) i+3;
                }

            float getScalingValue(RuleChoiceDef rule) {
                if(rule.difficultyIndex <= DifficultyIndex.Hard) {
                    return (float) rule.difficultyIndex + 1f;
                }
                else {
                    return difficultyDefinitions[rule.difficultyIndex - DifficultyIndex.Count].scalingValue;
                }
            }
            ruleChoices.choices.Sort(delegate(RuleChoiceDef x, RuleChoiceDef y){
                var xDiffValue = getScalingValue(x);
                var yDiffValue = getScalingValue(y);
                return xDiffValue.CompareTo(yDiffValue);            
            });
            return ruleChoices;
        }
    }
}
