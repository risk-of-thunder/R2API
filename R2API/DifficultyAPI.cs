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

        private const DifficultyIndex VanillaFinalIndex = DifficultyIndex.Hard;//We want to replace this 

        public static ObservableCollection<DifficultyDef> difficultyDefinitions = new ObservableCollection<DifficultyDef>();
        /// <summary>
        /// Add a DifficultyDef to the list of available difficulties.
        /// This must be called before the DifficultyCatalog inits, so before plugin.Start()
        /// You'll get your new index returned that you can work with for comparing to Run.Instance.selectedDifficulty.
        /// If this is called after the DifficultyCatalog inits then this will return -1/DifficultyIndex.Invalid and ignore the difficulty
        /// </summary>
        /// <param name="difficulty">The difficulty to add.</param>
        /// <returns>DifficultyIndex.Invalid if it fails. Your index otherwise.</returns>
        public static DifficultyIndex AddDifficulty(DifficultyDef difficulty) {
            if (difficultyAlreadyAdded) {
                R2API.Logger.LogError($"Tried to add difficulty: {difficulty.nameToken} after difficulty list was created");
                return DifficultyIndex.Invalid;
            }
            difficultyDefinitions.Add(difficulty);
            
            return VanillaFinalIndex + difficultyDefinitions.Count;
        }


        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            difficultyCatalogReady?.Invoke(null, null);
            On.RoR2.DifficultyCatalog.GetDifficultyDef += GetExtendedDifficultyDef;
            On.RoR2.RuleDef.FromDifficulty += InitialiseRuleBookAndFinalizeList;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.DifficultyCatalog.GetDifficultyDef -= GetExtendedDifficultyDef;
            On.RoR2.RuleDef.FromDifficulty -= InitialiseRuleBookAndFinalizeList;
        }

        private static DifficultyDef GetExtendedDifficultyDef(On.RoR2.DifficultyCatalog.orig_GetDifficultyDef orig, DifficultyIndex difficultyIndex)
        {
            if(difficultyAlreadyAdded)
                return difficultyDefinitions[(int) difficultyIndex];
            return orig(difficultyIndex);
        }
        private static RuleDef InitialiseRuleBookAndFinalizeList(On.RoR2.RuleDef.orig_FromDifficulty orig)
        {
            RuleDef ruleChoices = orig();
            var vanillaDefs = DifficultyCatalog.difficultyDefs;
            if (difficultyAlreadyAdded == false) {//Technically this function we are hooking is only called once, but in the weird case it's called multiple times, we don't want to add the definitions again.
                difficultyAlreadyAdded = true;
                for (int i = 0; i < vanillaDefs.Length; i++) {
                    difficultyDefinitions.Insert(i, vanillaDefs[i]);
                }
            }

            for ( int i=vanillaDefs.Length; i<difficultyDefinitions.Count;i++){//This basically replicates what the orig does, but that uses the hardcoded enum.Count to end it's loop, instead of the actual array length.
                DifficultyDef difficultyDef = difficultyDefinitions[i];
                RuleChoiceDef choice = ruleChoices.AddChoice(Language.GetString(difficultyDef.nameToken), null, false);
                choice.spritePath = difficultyDef.iconPath;
                choice.tooltipNameToken = difficultyDef.nameToken;
                choice.tooltipNameColor = difficultyDef.color;
                choice.tooltipBodyToken = difficultyDef.descriptionToken;
                choice.difficultyIndex =  (DifficultyIndex) i;
            }

            ruleChoices.choices.Sort(delegate(RuleChoiceDef x, RuleChoiceDef y){
                var xDiffValue = DifficultyCatalog.GetDifficultyDef(x.difficultyIndex).scalingValue;
                var yDiffValue = DifficultyCatalog.GetDifficultyDef(y.difficultyIndex).scalingValue;
                return xDiffValue.CompareTo(yDiffValue);            
            });

            return ruleChoices;
        }
    }
}
