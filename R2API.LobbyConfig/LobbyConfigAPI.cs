using System;
using System.Collections.Generic;
using System.Linq;
using R2API.AutoVersionGen;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace R2API;

// ReSharper disable once InconsistentNaming
#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class LobbyConfigAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".lobbyconfig";
    public const string PluginName = R2API.PluginName + ".LobbyConfig";

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;

    private static int _ruleNameSequence;

    internal static string RuleNameSequence() => $"LCAPI_{++_ruleNameSequence:D5}";

    private static readonly List<RuleChoiceDef> InternalRuleChoices =
        typeof(RuleCatalog).GetFieldValue<List<RuleChoiceDef>>("allChoicesDefs");

    private static List<RuleCategoryController> _controllers;

    private static event EventHandler<RuleChoiceDef> UpdateValues;

    private static event EventHandler<RuleCategoryController> CollapseCategory;

    public class LobbyCategory
    {

        /// <summary>
        /// Adds a rule category to the lobby.
        /// </summary>
        /// <param name="title">The category's title.</param>
        /// <param name="color">The category's color</param>
        /// <param name="description">Should the category be empty, you can show this description.</param>
        public LobbyCategory(string? title, Color color, string? description)
        {
            LobbyConfigAPI.SetHooks();
            typeof(RuleCatalog).GetMethodCached("AddCategory"
                    , new[] { typeof(string), typeof(Color), typeof(string), typeof(Func<bool>) })
                .Invoke(null, new object[] { title, color, description, null });

            Def = RuleCatalog.allCategoryDefs.Last();
        }

        /// <summary>
        /// Wraps a rule category for the lobby.
        /// </summary>
        /// <param name="category">The category.</param>
        public LobbyCategory(RuleCategoryDef? category)
        {
            LobbyConfigAPI.SetHooks();
            Def = category;
        }

        /// <summary>
        /// Adds a rule to the category. You cannot add choices to that rule after pushing it.
        /// </summary>
        /// <param name="rule">The rule to add.</param>
        /// <typeparam name="T">The type of value this rule holds.</typeparam>
        /// <returns>'this', for chaining.</returns>
        public LobbyCategory PushRule<T>(LobbyRule<T> rule)
        {
            LobbyConfigAPI.SetHooks();
            rule.Pushed = true;

            rule.Def.globalIndex = RuleCatalog.ruleCount;
            typeof(RuleCatalog).GetMethodCached("AddRule"
                    , new[] { typeof(RuleDef) })
                .Invoke(null, new object[] { rule.Def });

            RuleCatalog.allCategoryDefs.Last()?.children.Remove(rule.Def);
            RuleCatalog.allCategoryDefs.FirstOrDefault(x => x == Def)?.children.Add(rule.Def);

            rule.Def.category = Def;
            rule.Def.choices.ForEach(x =>
            {
                x.globalIndex = RuleCatalog.choiceCount;
                InternalRuleChoices.Add(x);
            });

            Array.Resize(ref PreGameRuleVoteController.votesForEachChoice, RuleCatalog.choiceCount);

            UpdateValues += rule.UpdateValue;
            return this;
        }

        /// <summary>
        /// Adds a child to this category.
        /// The child will get hidden should the parent be collapsed.
        /// </summary>
        /// <param name="category">The child.</param>
        /// <returns>'this', for chaining.</returns>
        public LobbyCategory AddChildCategory(LobbyCategory? category)
        {
            LobbyConfigAPI.SetHooks();
            if (category == this)
                throw new ArgumentException("Cannot be own parent.");

            if (_children.Count == 0)
                CollapseCategory += HideChildren;

            _children.Add(category);

            return this;
        }

        #region internal

        internal readonly RuleCategoryDef Def;
        private readonly List<LobbyCategory> _children = new List<LobbyCategory>();

        private void HideChildren(object sender, RuleCategoryController category)
        {
            if (Def != category.GetFieldValue<RuleCategoryDef>("currentCategory"))
                return;

            _children.ForEach(child =>
            {
                var controller = _controllers?.FirstOrDefault(c => c.GetFieldValue<RuleCategoryDef>("currentCategory") == child.Def);

                controller.SetFieldValue("collapsed", true);

                if (controller)
                    controller.gameObject.SetActive(!category.GetFieldValue<bool>("collapsed"));

                child.HideChildren(null, controller);
            });
        }

        #endregion internal
    }

    public class LobbyRule<T>
    {

        /// <summary>
        /// Value of the current choice of the rule.
        /// </summary>
        public T Value => (T)_val;

        /// <summary>
        /// Gets invoked if the rule is added to a category and the value changed.
        /// Sender is 'this', args is 'this.Value'.
        /// </summary>
        public event EventHandler<T>? ValueChanged;

        /// <summary>
        /// Construct a rule. Does not affect the game until you push the rule to a category.
        /// </summary>
        public LobbyRule()
        {
            LobbyConfigAPI.SetHooks();
            Def = new RuleDef(RuleNameSequence(), "R2API_RULE");
        }

        /// <summary>
        /// Adds a choice to the rule.
        /// </summary>
        /// <param name="value">The value this choice represents.</param>
        /// <param name="title">Tooltip title.</param>
        /// <param name="description">Tooltip description.</param>
        /// <param name="titleColor"></param>
        /// <param name="descriptionColor"></param>
        /// <param name="sprite">A path to the sprite for this choice.</param>
        /// <returns>'this', for chaining.</returns>
        public LobbyRule<T> AddChoice(T value, string? title, string? description
            , Color titleColor, Color descriptionColor, string? sprite = "")
        {
            LobbyConfigAPI.SetHooks();
            if (Pushed)
                throw new NotSupportedException("Cannot add choice after rule has been pushed.");

            var choice = Def.AddChoice(ChoiceNameSequence(), value);

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
        public LobbyRule<T> MakeValueDefault(T value)
        {
            LobbyConfigAPI.SetHooks();
            var choice = Def.choices.FirstOrDefault(x => value.Equals(x.extraData));

            if (choice != null)
                Def.defaultChoiceIndex = choice.localIndex;
            return this;
        }

        #region internal

        internal readonly RuleDef Def;
        internal bool Pushed;
        private object _val;

        private int _choiceNameSequence;

        private string ChoiceNameSequence() => $"{Def.globalName}_{++_choiceNameSequence:D3}";

        internal void UpdateValue(object _, RuleChoiceDef choiceDef)
        {
            if (choiceDef.ruleDef != Def)
                return;

            if (choiceDef.extraData == _val)
                return;

            _val = choiceDef.extraData;
            ValueChanged?.Invoke(this, Value);
        }

        #endregion internal
    }

    #region Hooks

    private static void HookAwake_PreGameController(On.RoR2.PreGameController.orig_Awake orig, PreGameController self)
    {
        orig(self);

        // Late hooking, make sure to not hook twice.
        On.RoR2.RuleBook.ApplyChoice -= HookApplyChoice_RuleBook;
        On.RoR2.RuleBook.ApplyChoice += HookApplyChoice_RuleBook;
    }

    private static void HookApplyChoice_RuleBook(On.RoR2.RuleBook.orig_ApplyChoice orig, RuleBook self, RuleChoiceDef choiceDef)
    {
        orig(self, choiceDef);

        UpdateValues?.Invoke(self, choiceDef);
    }

    private static void HookStart_RuleBookViewer(On.RoR2.UI.RuleBookViewer.orig_Start orig, RuleBookViewer self)
    {
        orig(self);

        _controllers = self.GetFieldValue<UIElementAllocator<RuleCategoryController>>("categoryElementAllocator")
            .GetFieldValue<List<RuleCategoryController>>("elementControllerComponentsList");
    }

    private static void HookTogglePopoutPanel_RuleCategoryController(On.RoR2.UI.RuleCategoryController.orig_TogglePopoutPanel orig, RuleCategoryController self)
    {
        orig(self);

        CollapseCategory?.Invoke(null, self);
    }

    #endregion Hooks

    private static bool _hooksEnabled = false;

    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        On.RoR2.PreGameController.Awake += HookAwake_PreGameController;
        On.RoR2.UI.RuleBookViewer.Start += HookStart_RuleBookViewer;
        On.RoR2.UI.RuleCategoryController.TogglePopoutPanel += HookTogglePopoutPanel_RuleCategoryController;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        On.RoR2.PreGameController.Awake -= HookAwake_PreGameController;
        On.RoR2.RuleBook.ApplyChoice -= HookApplyChoice_RuleBook;
        On.RoR2.UI.RuleBookViewer.Start -= HookStart_RuleBookViewer;

        _hooksEnabled = false;
    }
}
