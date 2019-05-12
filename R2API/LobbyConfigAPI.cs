using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    public static class LobbyConfigAPI {
        #region Reflection

        private static readonly MethodInfo _addCategory =
            typeof(RuleCatalog).GetMethodCached("AddCategory"
                , new[] {typeof(string), typeof(Color), typeof(string), typeof(Func<bool>)});

        private static readonly MethodInfo _addRule =
            typeof(RuleCatalog).GetMethodCached("AddRule"
                , new[] {typeof(RuleDef)});

        private static readonly List<RuleChoiceDef> _ruleChoices = (List<RuleChoiceDef>)
            typeof(RuleCatalog).GetFieldCached("allChoicesDefs")
                .GetValue(null);

        #endregion

        internal static void InitHooks() {
            // late hook.
            On.RoR2.PreGameController.Awake += (origAwake, selfPreGameController) => {
                origAwake(selfPreGameController);

                On.RoR2.RuleBook.ApplyChoice += (orig, self, def) => {
                    orig(self, def);

                    UpdateValues?.Invoke(self, null);
                };
            };

            On.RoR2.UI.RuleCategoryController.Awake += (orig, self) => {
                self.SetCollapsed(true);
                orig(self);
            };
        }

        private static event EventHandler UpdateValues;

        /// <summary>
        /// Adds a rule category to the lobby. If a category with the same title already exists, will return that.
        /// </summary>
        /// <param name="title">The category's title.</param>
        /// <param name="color">The category's color</param>
        /// <param name="emptyDescription">Should the category be empty, you can show this description.</param>
        /// <returns>The RuleCategoryDef, keep if you want to add rules.</returns>
        public static RuleCategoryDef AddCategory(string title, Color color, string emptyDescription = null) {
            var category = RuleCatalog.allCategoryDefs.FirstOrDefault(x => x.displayToken == title);
            if (category != null)
                return category;

            _addCategory.Invoke(null, new object[] {title, color, emptyDescription, null});
            return RuleCatalog.allCategoryDefs.Last();
        }

        /// <summary>
        /// Adds a rule to the category. DO NOT ADD CHOICES AFTER THIS.
        /// </summary>
        /// <param name="category">The category to add this rule to.</param>
        /// <param name="rule">The rule to add.</param>
        /// <typeparam name="T">The type of value this rule holds.</typeparam>
        public static void AddToCategory<T>(RuleCategoryDef category, LobbyRule<T> rule) {
            rule._ruleDef.globalIndex = RuleCatalog.ruleCount;
            _addRule.Invoke(null, new object[] {rule._ruleDef});

            RuleCatalog.allCategoryDefs.Last()?.children.Remove(rule._ruleDef);
            RuleCatalog.allCategoryDefs.FirstOrDefault(x => x == category)?.children.Add(rule._ruleDef);

            rule._ruleDef.category = category;
            rule._ruleDef.choices.ForEach(x => {
                x.globalIndex = RuleCatalog.choiceCount;
                _ruleChoices.Add(x);
            });

            Array.Resize(ref PreGameRuleVoteController.votesForEachChoice, RuleCatalog.choiceCount);

            UpdateValues += (sender, args) => rule.UpdateValue((RuleBook) sender);
        }

        private static int _globalNameSequence;
        internal static string GlobalNameSequence() => $"R2API_LCAPI_{++_globalNameSequence:D5}";

        public class LobbyRule<T> {
            private object _value = default(T);
            internal readonly RuleDef _ruleDef;

            /// <summary>
            /// Gets invoked if the rule is added to a category and the value changed. Sender is 'this', args are 'null'.
            /// </summary>
            public event EventHandler ValueChanged;

            public T Value => (T) _value;

            public LobbyRule() {
                _ruleDef = new RuleDef(GlobalNameSequence(), "R2API_RULE");
            }

            private int _localNameSequence;
            internal string LocalNameSequence() => $"{_ruleDef.globalName}_{++_localNameSequence:D3}";

            /// <summary>
            /// Adds a choice to the rule.
            /// </summary>
            /// <param name="value">The value this choice represents.</param>
            /// <param name="title">Tooltip title.</param>
            /// <param name="description">Tooltip description.</param>
            /// <param name="titleColor"></param>
            /// <param name="descriptionColor"></param>
            /// <param name="sprite">A path to the sprite for this choice.</param>
            /// <param name="name">An internal name for this choice.</param>
            /// <returns>'this', for chaining.</returns>
            public LobbyRule<T> AddChoice(T value
                , string title, string description
                , Color titleColor, Color descriptionColor
                , string sprite = "", string name = "") {
                var choice = _ruleDef.AddChoice(name == "" ? LocalNameSequence() : name, value);

                choice.tooltipNameToken = title;
                choice.tooltipNameColor = titleColor;
                choice.tooltipBodyToken = description;
                choice.tooltipBodyColor = descriptionColor;

                choice.spritePath = sprite;

                return this;
            }

            /// <summary>
            /// Set the default value for this rule.
            /// </summary>
            /// <param name="value">The value for which the choice will be marked as default.</param>
            /// <returns>'this', for chaining.</returns>
            public LobbyRule<T> MakeValueDefault(T value) {
                var choice = _ruleDef.choices.FirstOrDefault(x => ((T) x.extraData).Equals(value));

                if (choice != null)
                    _ruleDef.defaultChoiceIndex = choice.localIndex;

                return this;
            }

            internal void UpdateValue(RuleBook ruleBook) {
                var choice = ruleBook.GetRuleChoice(_ruleDef);

                if (Value.Equals(choice.extraData))
                    return;

                _value = choice.extraData;
                ValueChanged?.Invoke(this, null);
            }
        }
    }
}
