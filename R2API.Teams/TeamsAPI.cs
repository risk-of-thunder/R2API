using HarmonyLib;
using HG;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using R2API.AutoVersionGen;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API;

#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class TeamsAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".teams";
    public const string PluginName = R2API.PluginName + ".Teams";

    /// <summary>
    /// The absolute maximum amount of teams there can be in the catalog
    /// </summary>
    public static int TeamCountLimit => sbyte.MaxValue - 1;

    /// <summary>
    /// How many teams there are in total, this should be used instead of TeamIndex.Count wherever possible as this accounts for modded teams
    /// </summary>
    public static int TeamCount => TeamCatalog.teamDefs.Length;

    /// <summary>
    /// The absolute maximum amount of modded teams there can be in the catalog
    /// </summary>
    public static int ModdedTeamCountLimit => TeamCountLimit - _vanillaTeamsCount;

    /// <summary>
    /// How many modded teams that have been registered
    /// </summary>
    public static int ModdedTeamCount => Math.Max(0, TeamCount - _vanillaTeamsCount);

    static readonly int _vanillaTeamsCount = TeamCatalog.teamDefs.Length;

    static readonly int _allVanillaTeamsDirtyBits = (1 << _vanillaTeamsCount) - 1;

    static byte[] _allModdedTeamsDirtyBits = [];

    static TeamBehavior[] _moddedTeamBehaviors = new TeamBehavior[4];

    static EnumPatcher.EnumValueHandle _teamCountHandle;

    internal static void Init()
    {
        // Force the TeamCatalog static constructor to run before anything else is initialized,
        // this prevents a crash during loading, something related to asset loading from a component constructor.
        _ = TeamCatalog.teamDefs;
    }

    #region Hooks
    static bool _hooksEnabled;

    static readonly List<IDetour> _hookInstances = [];

    internal static void SetHooks()
    {
        if (_hooksEnabled)
            return;
        
        IL.EntityStates.GrandParentBoss.Offspring.FindTargetFarthest += ReplaceTeamIndexCount;
        IL.RoR2.AffixBeadAttachment.ClearEnemyLunarRuinDamage += ReplaceTeamIndexCount;
        IL.RoR2.BuffWard.FixedUpdate += ReplaceTeamIndexCount;
        IL.RoR2.FogDamageController.MyFixedUpdate += ReplaceTeamIndexCount;
        IL.RoR2.GhostGunController.FindTarget += ReplaceTeamIndexCount;
        IL.RoR2.GoldTitanManager.CalcTitanPowerAndBestTeam += ReplaceTeamIndexCount;
        IL.RoR2.HoldoutZoneController.UpdateHealingNovas += ReplaceTeamIndexCount;
        IL.RoR2.TeamComponent.TeamIsValid += ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.Start += ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.GetTeamExperience += ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.GetTeamCurrentLevelExperience += ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.GetTeamNextLevelExperience += ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.GetTeamLevel += ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.SetTeamLevel += ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.GiveTeamMoney_TeamIndex_int += ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.GiveTeamItem += ReplaceTeamIndexCount;
        IL.RoR2.Util.GetEnemyEasyTarget += ReplaceTeamIndexCount;

        On.RoR2.TeamManager.Start += TeamManager_Start;
        IL.RoR2.TeamManager.SetTeamExperience += TeamManager_SetTeamExperience;
        On.RoR2.TeamManager.OnSerialize += TeamManager_OnSerialize;
        On.RoR2.TeamManager.OnDeserialize += TeamManager_OnDeserialize;

        On.RoR2.LayerIndex.GetAppropriateLayerForTeam += LayerIndex_GetAppropriateLayerForTeam;
        On.RoR2.LayerIndex.GetAppropriateFakeLayerForTeam += LayerIndex_GetAppropriateFakeLayerForTeam;

        On.RoR2.TeamMask.HasTeam += TeamMask_HasTeam;
        On.RoR2.TeamMask.AddTeam += TeamMask_AddTeam;
        On.RoR2.TeamMask.RemoveTeam += TeamMask_RemoveTeam;

        IL.RoR2.GenericPickupController.AttemptGrant += GenericPickupController_AttemptGrant;

        MethodInfo fogDamageGetAffectedBodiesMoveNextMethod = null;

        MethodInfo fogDamageGetAffectedBodiesMethod = SymbolExtensions.GetMethodInfo<FogDamageController>(_ => _.GetAffectedBodies());
        if (fogDamageGetAffectedBodiesMethod != null)
        {
            fogDamageGetAffectedBodiesMoveNextMethod = AccessTools.EnumeratorMoveNext(fogDamageGetAffectedBodiesMethod);
        }

        if (fogDamageGetAffectedBodiesMoveNextMethod != null)
        {
            _hookInstances.Add(new ILHook(fogDamageGetAffectedBodiesMoveNextMethod, ReplaceTeamIndexCount));
        }
        else
        {
            Log.Error("Failed to find FogDamageController.GetAffectedBodies enumerator MoveNext method for patches");
        }

        ConstructorInfo holdoutZoneControllerCtor = AccessTools.DeclaredConstructor(typeof(HoldoutZoneController));
        if (holdoutZoneControllerCtor != null)
        {
            _hookInstances.Add(new ILHook(holdoutZoneControllerCtor, ReplaceTeamArraySize));
        }
        else
        {
            Log.Error("Failed to find HoldoutZoneController constructor for patches");
        }

        ConstructorInfo teamManagerCtor = AccessTools.DeclaredConstructor(typeof(TeamManager));
        if (teamManagerCtor != null)
        {
            _hookInstances.Add(new ILHook(teamManagerCtor, ReplaceTeamArraySize));
        }
        else
        {
            Log.Error("Failed to find TeamManager constructor for patches");
        }

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        if (!_hooksEnabled)
            return;

        IL.EntityStates.GrandParentBoss.Offspring.FindTargetFarthest -= ReplaceTeamIndexCount;
        IL.RoR2.AffixBeadAttachment.ClearEnemyLunarRuinDamage -= ReplaceTeamIndexCount;
        IL.RoR2.BuffWard.FixedUpdate -= ReplaceTeamIndexCount;
        IL.RoR2.FogDamageController.MyFixedUpdate -= ReplaceTeamIndexCount;
        IL.RoR2.GhostGunController.FindTarget -= ReplaceTeamIndexCount;
        IL.RoR2.GoldTitanManager.CalcTitanPowerAndBestTeam -= ReplaceTeamIndexCount;
        IL.RoR2.HoldoutZoneController.UpdateHealingNovas -= ReplaceTeamIndexCount;
        IL.RoR2.TeamComponent.TeamIsValid -= ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.Start -= ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.OnSerialize -= ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.OnDeserialize -= ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.GetTeamExperience -= ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.GetTeamCurrentLevelExperience -= ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.GetTeamNextLevelExperience -= ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.GetTeamLevel -= ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.SetTeamLevel -= ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.GiveTeamMoney_TeamIndex_int -= ReplaceTeamIndexCount;
        IL.RoR2.TeamManager.GiveTeamItem -= ReplaceTeamIndexCount;
        IL.RoR2.Util.GetEnemyEasyTarget -= ReplaceTeamIndexCount;

        On.RoR2.TeamManager.Start -= TeamManager_Start;
        IL.RoR2.TeamManager.SetTeamExperience -= TeamManager_SetTeamExperience;
        On.RoR2.TeamManager.OnSerialize -= TeamManager_OnSerialize;
        On.RoR2.TeamManager.OnDeserialize -= TeamManager_OnDeserialize;

        On.RoR2.LayerIndex.GetAppropriateLayerForTeam -= LayerIndex_GetAppropriateLayerForTeam;
        On.RoR2.LayerIndex.GetAppropriateFakeLayerForTeam -= LayerIndex_GetAppropriateFakeLayerForTeam;

        On.RoR2.TeamMask.HasTeam -= TeamMask_HasTeam;
        On.RoR2.TeamMask.AddTeam -= TeamMask_AddTeam;
        On.RoR2.TeamMask.RemoveTeam -= TeamMask_RemoveTeam;

        IL.RoR2.GenericPickupController.AttemptGrant -= GenericPickupController_AttemptGrant;

        foreach (IDetour hookInstance in _hookInstances)
        {
            hookInstance?.Dispose();
        }

        _hookInstances.Clear();

        _hooksEnabled = false;
    }

    // Purpose: Replace 'new T[(int)TeamIndex.Count]' with 'new T[TeamsAPI.TeamCount]'
    static void ReplaceTeamArraySize(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        MethodInfo teamCountGetterMethod = AccessTools.PropertyGetter(typeof(TeamsAPI), nameof(TeamCount));

        int patchCount = 0;

        while (c.TryGotoNext(MoveType.Before,
                             x => x.MatchLdcI4(_vanillaTeamsCount),
                             x => x.MatchNewarr(out _)))
        {
            c.Index++;
            c.MoveAfterLabels();

            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Call, teamCountGetterMethod);

            patchCount++;
        }

        if (patchCount == 0)
        {
            Log.Error($"Failed to find any TeamIndex.Count array size patch locations for {il.Method.FullName}");
        }
        else
        {
            Log.Debug($"TeamIndex.Count array size patch for {il.Method.FullName} found {patchCount} location(s)");
        }
    }

    // Purpose: Replace 'X < TeamIndex.Count' with modded TeamIndex "in-bounds" check, also accounts for other conditions/comparisons
    static void ReplaceTeamIndexCount(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        static bool matchCompareOrBranch(Instruction x)
        {
            return x.OpCode.FlowControl == FlowControl.Cond_Branch || // Match any conditional branch (blt, beq, bgt, etc.)
                   x.OpCode == OpCodes.Ceq ||
                   x.OpCode == OpCodes.Cgt ||
                   x.OpCode == OpCodes.Cgt_Un ||
                   x.OpCode == OpCodes.Clt ||
                   x.OpCode == OpCodes.Clt_Un; // Match any comparison
        }

        int patchCount = 0;

        while (c.TryGotoNext(MoveType.Before,
                             x => x.MatchLdcI4(_vanillaTeamsCount),
                             x => matchCompareOrBranch(x)))
        {
            // A bit gross, but we need the instruction after TeamIndex.Count without moving the cursor
            // It's all within the match though, so no risks taken really.
            Instruction teamIndexCompareOrBranchInstruction = c.Next.Next;

            // Specify delegate signature to ensure hook won't silently fail if IsValidTeam signature changes
            c.EmitDelegate<Func<TeamIndex, bool>>(IsValidTeam);

            if (teamIndexCompareOrBranchInstruction.OpCode.FlowControl == FlowControl.Cond_Branch)
            {
                bool isInBoundsCheck = teamIndexCompareOrBranchInstruction.MatchBlt(out _) ||
                                       teamIndexCompareOrBranchInstruction.MatchBltUn(out _) ||
                                       teamIndexCompareOrBranchInstruction.MatchBneUn(out _);

                c.Emit(isInBoundsCheck ? OpCodes.Brtrue : OpCodes.Brfalse, teamIndexCompareOrBranchInstruction.Operand);
            }
            else // non-branching comparison
            {
                bool isInBoundsCheck = teamIndexCompareOrBranchInstruction.MatchClt() ||
                                       teamIndexCompareOrBranchInstruction.MatchCltUn();

                if (!isInBoundsCheck)
                {
                    c.Emit(OpCodes.Not);
                }
            }

            ILLabel skipTeamIndexOperationLabel = c.DefineLabel();
            c.Emit(OpCodes.Br, skipTeamIndexOperationLabel);

            // Even though the original instruction will always be skipped,
            // we still need to make sure the stack isn't unbalanced
            c.Emit(OpCodes.Ldc_I4_M1);
            c.Index += 2; // Move past TeamIndex.Count & compare/branch operation
            c.MarkLabel(skipTeamIndexOperationLabel);

            patchCount++;
        }

        if (patchCount == 0)
        {
            Log.Error($"Failed to find any TeamIndex.Count patch locations for {il.Method.FullName}");
        }
        else
        {
            Log.Debug($"TeamIndex.Count patch for {il.Method.FullName} found {patchCount} location(s)");
        }
    }

    static int LayerIndex_GetAppropriateLayerForTeam(On.RoR2.LayerIndex.orig_GetAppropriateLayerForTeam orig, TeamIndex teamIndex)
    {
        int layer = orig(teamIndex);

        int moddedTeamIndex = GetModdedTeamIndex(teamIndex);
        if (moddedTeamIndex >= 0 && moddedTeamIndex < ModdedTeamCount)
        {
            TeamBehavior teamBehavior = ArrayUtils.GetSafe(_moddedTeamBehaviors, moddedTeamIndex);
            if (teamBehavior != null)
            {
                layer = teamBehavior.TeamLayer.intVal;
            }
        }

        return layer;
    }

    static void GenericPickupController_AttemptGrant(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        int locNum = 0;
        if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(out locNum),
                x => x.MatchCallvirt(typeof(TeamComponent).GetPropertyGetter("teamIndex"))
            ))
        {
            c.EmitDelegate(HandleAttemptGrant);
        }
        else
        {
            Debug.LogError(il.Method.Name + " IL Hook failed!");
        }
    }
    static TeamIndex HandleAttemptGrant(TeamIndex teamIndex)
    {
        TeamBehavior teamBehavior = GetTeamBehavior(teamIndex);
        if (teamBehavior == null) return teamIndex;
        if (teamBehavior.CanPickup) return TeamIndex.Player;
        return teamIndex;
    }
    static LayerIndex LayerIndex_GetAppropriateFakeLayerForTeam(On.RoR2.LayerIndex.orig_GetAppropriateFakeLayerForTeam orig, TeamIndex teamIndex)
    {
        LayerIndex fakeLayer = orig(teamIndex);

        int moddedTeamIndex = GetModdedTeamIndex(teamIndex);
        if (moddedTeamIndex >= 0 && moddedTeamIndex < ModdedTeamCount)
        {
            TeamBehavior teamBehavior = ArrayUtils.GetSafe(_moddedTeamBehaviors, moddedTeamIndex);
            if (teamBehavior != null)
            {
                fakeLayer = teamBehavior.TeamFakeLayer;
            }
        }

        return fakeLayer;
    }

    static bool TeamMask_HasTeam(On.RoR2.TeamMask.orig_HasTeam orig, ref TeamMask self, TeamIndex teamIndex)
    {
        bool hasTeam = orig(ref self, teamIndex);

        int moddedTeamIndex = GetModdedTeamIndex(teamIndex);
        if (moddedTeamIndex >= 0 && moddedTeamIndex < ModdedTeamCount)
        {
            hasTeam = GetModdedTeamMaskBit(self, moddedTeamIndex);
        }

        return hasTeam;
    }

    static void TeamMask_AddTeam(On.RoR2.TeamMask.orig_AddTeam orig, ref TeamMask self, TeamIndex teamIndex)
    {
        orig(ref self, teamIndex);

        int moddedTeamIndex = GetModdedTeamIndex(teamIndex);
        if (moddedTeamIndex >= 0 && moddedTeamIndex < ModdedTeamCount)
        {
            SetModdedTeamMaskBit(ref self, moddedTeamIndex, true);
        }
    }

    static void TeamMask_RemoveTeam(On.RoR2.TeamMask.orig_RemoveTeam orig, ref TeamMask self, TeamIndex teamIndex)
    {
        orig(ref self, teamIndex);

        int moddedTeamIndex = GetModdedTeamIndex(teamIndex);
        if (moddedTeamIndex >= 0 && moddedTeamIndex < ModdedTeamCount)
        {
            SetModdedTeamMaskBit(ref self, moddedTeamIndex, false);
        }
    }

    static void TeamManager_Start(On.RoR2.TeamManager.orig_Start orig, TeamManager self)
    {
        self.gameObject.EnsureComponent<TeamManagerModdedTeamsHelper>();
        orig(self);
    }

    // Purpose: Replace 'base.SetDirtyBit(1U << (int)teamIndex)' with
    // if (IsModdedTeam(teamIndex))
    //     setModdedTeamDirtyBit(this, teamIndex);
    // else
    //     base.SetDirtyBit(1U << (int)teamIndex)
    static void TeamManager_SetTeamExperience(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        ParameterDefinition teamIndexParameter = il.Method.Parameters.FirstOrDefault(p => p.ParameterType.Is(typeof(TeamIndex)));
        if (teamIndexParameter == null)
        {
            Log.Error("TeamManager.SetTeamExperience hook failed to find TeamIndex parameter");
            return;
        }

        if (!c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<NetworkBehaviour>(nameof(NetworkBehaviour.SetDirtyBit))))
        {
            Log.Error("Failed to find TeamManager.SetTeamExperience SetDirtyBit call");
            return;
        }

        ILLabel vanillaSetDirtyBitEndLabel = c.MarkLabel();

        if (!c.TryGotoPrev(MoveType.AfterLabel, x => x.MatchLdarg(0)))
        {
            Log.Error("Failed to find TeamManager.SetTeamExperience SetDirtyBit patch location");
            return;
        }

        ILLabel vanillaSetDirtyBitStartLabel = c.MarkLabel();
        c.MoveBeforeLabels();

        c.Emit(OpCodes.Ldarg, teamIndexParameter);
        c.EmitDelegate<Func<TeamIndex, bool>>(IsModdedTeam);
        c.Emit(OpCodes.Brfalse, vanillaSetDirtyBitStartLabel);

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg, teamIndexParameter);
        c.EmitDelegate(setModdedTeamDirtyBit);
        static void setModdedTeamDirtyBit(TeamManager self, TeamIndex teamIndex)
        {
            if (self.TryGetComponent(out TeamManagerModdedTeamsHelper moddedTeamsHelper))
            {
                moddedTeamsHelper.SetDirtyBit(GetModdedTeamIndex(teamIndex));
            }
            else
            {
                Log.Error($"TeamManager wants to set dirty bit of modded team '{teamIndex}', but modded teams helper is not attached!");
            }
        }

        c.Emit(OpCodes.Br, vanillaSetDirtyBitEndLabel);
    }

    static bool TeamManager_OnSerialize(On.RoR2.TeamManager.orig_OnSerialize orig, TeamManager self, NetworkWriter writer, bool initialState)
    {
        bool anythingWritten = orig(self, writer, initialState);

        if (self.TryGetComponent(out TeamManagerModdedTeamsHelper moddedTeamsHelper))
        {
            byte[] dirtyModdedTeamsBits;
            if (initialState)
            {
                // No need to access dirty bits in initialState, since we know all of them are dirty.
                // Also since receiver will know to read all of them during initial deserialization, there is no need to write them either.
                dirtyModdedTeamsBits = null;
            }
            else
            {
                dirtyModdedTeamsBits = moddedTeamsHelper.ModdedTeamsDirtyBits ?? [];
                CompressedFlagArrayUtilities.WriteToNetworkWriter(dirtyModdedTeamsBits, writer, ModdedTeamCount);
            }

            for (int i = 0; i < ModdedTeamCount; i++)
            {
                if (initialState || CompressedFlagArrayUtilities.Has(dirtyModdedTeamsBits, i))
                {
                    writer.WritePackedUInt64(self.teamExperience[(int)GetTeamIndex(i)]);
                }
            }

            anythingWritten = true;

            moddedTeamsHelper.ClearDirtyBits();
        }

        return anythingWritten;
    }

    static void TeamManager_OnDeserialize(On.RoR2.TeamManager.orig_OnDeserialize orig, TeamManager self, NetworkReader reader, bool initialState)
    {
        orig(self, reader, initialState);

        if (self.TryGetComponent(out TeamManagerModdedTeamsHelper moddedTeamsHelper))
        {
            byte[] dirtyModdedTeamsBits = initialState ? null : CompressedFlagArrayUtilities.ReadFromNetworkReader(reader, ModdedTeamCount);

            for (int i = 0; i < ModdedTeamCount; i++)
            {
                if (initialState || CompressedFlagArrayUtilities.Has(dirtyModdedTeamsBits, i))
                {
                    ulong experience = reader.ReadPackedUInt64();
                    self.SetTeamExperience(GetTeamIndex(i), experience);
                }
            }
        }
    }

    class TeamManagerModdedTeamsHelper : MonoBehaviour
    {
        public byte[] ModdedTeamsDirtyBits;

        internal void SetDirtyBit(int moddedTeamIndex)
        {
            CompressedFlagArrayUtilities.AddImmutable(ref ModdedTeamsDirtyBits, moddedTeamIndex);
        }

        internal void ClearDirtyBits()
        {
            ModdedTeamsDirtyBits = null;
        }
    }
    #endregion

    static bool GetModdedTeamMaskBit(in TeamMask teamMask, int moddedTeamIndex)
    {
        byte[] moddedTeamsMask = TeamsInterop.GetModdedMask(teamMask);
        return CompressedFlagArrayUtilities.Has(moddedTeamsMask, moddedTeamIndex);
    }

    static void SetModdedTeamMaskBit(ref TeamMask teamMask, int moddedTeamIndex, bool bitState)
    {
        ref byte[] moddedTeamsMask = ref TeamsInterop.GetModdedMaskRef(ref teamMask);

        if (bitState)
        {
            CompressedFlagArrayUtilities.AddImmutable(ref moddedTeamsMask, moddedTeamIndex);
        }
        else
        {
            CompressedFlagArrayUtilities.RemoveImmutable(ref moddedTeamsMask, moddedTeamIndex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int GetModdedTeamIndex(TeamIndex teamIndex)
    {
        return (int)teamIndex - _vanillaTeamsCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static TeamIndex GetTeamIndex(int moddedTeamIndex)
    {
        return (TeamIndex)(_vanillaTeamsCount + moddedTeamIndex);
    }

    #region Public
    /// <summary>
    /// Determines if a <see cref="TeamIndex"/> is a valid modded team
    /// </summary>
    /// <param name="teamIndex"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage("Design", "R2APISubmodulesAnalyzer:Public API Method is not enabling the hooks if needed.", Justification = "Hooks are not necessary here")]
    public static bool IsModdedTeam(TeamIndex teamIndex)
    {
        return teamIndex != TeamIndex.None && (byte)teamIndex >= (byte)_vanillaTeamsCount && (byte)teamIndex < TeamCount;
    }

    /// <summary>
    /// Determines if a <see cref="TeamIndex"/> is valid, prefer to use this instead of comparing with <see cref="TeamIndex.Count"/>
    /// </summary>
    /// <param name="teamIndex"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage("Design", "R2APISubmodulesAnalyzer:Public API Method is not enabling the hooks if needed.", Justification = "Hooks are not necessary here")]
    public static bool IsValidTeam(TeamIndex teamIndex)
    {
        return teamIndex != TeamIndex.None && (byte)teamIndex < TeamCount;
    }

    /// <summary>
    /// Enumerates all registered teams, prefer to use this over iterating through [0 .. <see cref="TeamCount"/>) yourself
    /// </summary>
    /// <returns>An enumerable for all registered team indices</returns>
    /// <remarks>
    /// Teams are returned in order, vanilla teams first [<see cref="TeamIndex.Neutral"/> .. <see cref="TeamIndex.Count"/>) then modded teams, in the order they were registered.
    /// </remarks>
    [SuppressMessage("Design", "R2APISubmodulesAnalyzer:Public API Method is not enabling the hooks if needed.", Justification = "Hooks are not necessary here")]
    public static IEnumerable<TeamIndex> TeamsEnumerator()
    {
        for (int i = 0; i < TeamCount; i++)
        {
            yield return (TeamIndex)i;
        }
    }

    /// <summary>
    /// Registers a new team to the catalog and returns it's <see cref="TeamIndex"/>
    /// </summary>
    /// <remarks>
    /// If a team has already been added with the same name configured in <paramref name="teamBehavior"/>, then the previously registered team's <see cref="TeamIndex"/> will be returned.
    /// </remarks>
    /// <param name="teamDef">The <see cref="TeamDef"/> of the new team.</param>
    /// <param name="teamBehavior">Additional configuration not covered by the <see cref="TeamDef"/></param>
    /// <returns>The <see cref="TeamIndex"/> of the newly added team, or <see cref="TeamIndex.None"/> if the team limit has been reached.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="teamDef"/> or <paramref name="teamBehavior"/> are <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException">This method is called after game initialization has finished.</exception>
    public static TeamIndex RegisterTeam(TeamDef teamDef, TeamBehavior teamBehavior)
    {
        if (teamDef is null)
            throw new ArgumentNullException(nameof(teamDef));

        if (teamBehavior is null)
            throw new ArgumentNullException(nameof(teamBehavior));

        if (RoR2Application.loadFinished)
            throw new InvalidOperationException("Cannot register a new team post initialization");

        int newTeamIndex = TeamCatalog.teamDefs.Length;
        int newTeamCount = newTeamIndex + 1;

        if (newTeamIndex >= TeamCountLimit)
        {
            Log.Fatal($"Failed to add team '{teamBehavior.Name}': Modded team limit of {ModdedTeamCountLimit} has been reached.");
            return TeamIndex.None;
        }

        // Will catch any name clashes with vanilla teams or registered modded ones
        if (Enum.TryParse(teamBehavior.Name, out TeamIndex existingTeamIndex))
        {
            Log.Error($"Attempting to register team with name '{teamBehavior.Name}' multiple times, previously assigned TeamIndex will be returned, TeamDef and TeamBehavior will be discarded");
            return existingTeamIndex;
        }

        if (Enum.TryParse(teamBehavior.Name, true, out TeamIndex existingTeamIndexCaseInsensitive))
        {
            Log.Warning($"Registering team '{teamBehavior.Name}' with the same case-insensitive name as an existing team '{existingTeamIndexCaseInsensitive}', consider re-naming your team to avoid name clashes.");
        }

        SetHooks();

        ArrayUtils.EnsureCapacity(ref TeamCatalog.teamDefs, newTeamCount);
        TeamCatalog.Register((TeamIndex)newTeamIndex, teamDef);

        ArrayUtils.EnsureCapacity(ref TeamComponent.teamsList, newTeamCount);
        TeamComponent.teamsList[newTeamIndex].Init((TeamIndex)newTeamIndex);

        if (EnumPatcher.IsValid(_teamCountHandle))
        {
            EnumPatcher.RemoveEnumValueEntry(_teamCountHandle);
        }

        EnumPatcher.SetEnumValue(teamBehavior.Name, (TeamIndex)newTeamIndex);

        // Keep TeamIndex.Count updated, just in case someone accesses it via Enum
        _teamCountHandle = EnumPatcher.SetEnumValue(nameof(TeamIndex.Count), (TeamIndex)newTeamCount);

        TeamMask.all.AddTeam((TeamIndex)newTeamIndex);

        if ((teamBehavior.Classification & TeamClassification.Neutral) == 0)
        {
            TeamMask.allButNeutral.AddTeam((TeamIndex)newTeamIndex);
        }

        int moddedTeamIndex = GetModdedTeamIndex((TeamIndex)newTeamIndex);
        if (moddedTeamIndex >= _moddedTeamBehaviors.Length)
        {
            Array.Resize(ref _moddedTeamBehaviors, Math.Clamp(_moddedTeamBehaviors.Length * 2, moddedTeamIndex + 1, ModdedTeamCountLimit));
        }

        _moddedTeamBehaviors[moddedTeamIndex] = teamBehavior;

        CompressedFlagArrayUtilities.AddImmutable(ref _allModdedTeamsDirtyBits, moddedTeamIndex);

        return (TeamIndex)newTeamIndex;
    }

    /// <summary>
    /// Gets the <see cref="TeamBehavior"/> instance of a modded team
    /// </summary>
    /// <param name="teamIndex"></param>
    /// <returns>The <see cref="TeamBehavior"/> instance of the modded team associated with <paramref name="teamIndex"/>, or <see langword="null"/> if <paramref name="teamIndex"/> is a vanilla team or outside the bounds of modded teams</returns>
    [SuppressMessage("Design", "R2APISubmodulesAnalyzer:Public API Method is not enabling the hooks if needed.", Justification = "Hooks are not necessary here")]
    public static TeamBehavior GetTeamBehavior(TeamIndex teamIndex)
    {
        int moddedTeamIndex = GetModdedTeamIndex(teamIndex);
        if (moddedTeamIndex < 0)
            return null;

        return ArrayUtils.GetSafe(_moddedTeamBehaviors, moddedTeamIndex);
    }

    /// <summary>
    /// Determines how a team should behave and interact with the game.
    /// </summary>
    /// <remarks>
    /// Properties that support changing mid-game are marked <see langword="virtual"/>, inherit this class to implement custom behavior for them.
    /// </remarks>
    public class TeamBehavior
    {
        /// <summary>
        /// The internal name of this team, used in place of an enum string representation
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// What vanilla team's behavior this team should be based on
        /// </summary>
        public TeamClassification Classification { get; }

        /// <summary>
        /// The layer used by members of this team, returned by <see cref="LayerIndex.GetAppropriateLayerForTeam(TeamIndex)"/> for the TeamIndex assigned to this team
        /// </summary>
        public virtual LayerIndex TeamLayer => (Classification & TeamClassification.Player) != 0 ? LayerIndex.playerBody : LayerIndex.enemyBody;

        /// <summary>
        /// The fake layer used by members of this team, returned by <see cref="LayerIndex.GetAppropriateFakeLayerForTeam(TeamIndex)"/> for the TeamIndex assigned to this team
        /// </summary>
        public virtual LayerIndex TeamFakeLayer => (Classification & TeamClassification.Player) != 0 ? LayerIndex.playerFakeActor : LayerIndex.fakeActor;

        /// <summary>
        /// Constructs a <see cref="TeamBehavior"/> with all required values set
        /// </summary>
        /// <param name="name">The internal name of this team, used in place of an enum string representation</param>
        /// <param name="teamClassification">What vanilla team's behavior this team should be based on</param>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/>, empty, or contains only whitespace characters.</exception>
        [SuppressMessage("Design", "R2APISubmodulesAnalyzer:Public API Method is not enabling the hooks if needed.", Justification = "Hooks are not necessary here")]
        public TeamBehavior(string name, TeamClassification teamClassification)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));

            Name = name;
            Classification = teamClassification;
        }
        /// <summary>
        /// Make custom team be able to pickup pickups
        /// </summary>
        public virtual bool CanPickup => Classification == TeamClassification.Player;
    }

    /// <summary>
    /// What vanilla team's behavior a modded team should be based on
    /// </summary>
    [Flags]
    public enum TeamClassification
    {
        /// <summary>
        /// No behavior is shared
        /// </summary>
        None = 0,

        /// <summary>
        /// Certain behavior is shared with <see cref="TeamIndex.Neutral"/>
        /// </summary>
        /// <remarks>
        /// If set, a team will not be added to <see cref="TeamMask.allButNeutral"/>
        /// </remarks>
        Neutral = 1 << 0,

        /// <summary>
        /// Certain behavior is shared with any non-player, non-neutral teams
        /// </summary>
        /// <remarks>
        /// Not currently implemented
        /// </remarks>
        Enemy = 1 << 1,

        /// <summary>
        /// Certain behavior is shared with <see cref="TeamIndex.Player"/>
        /// </summary>
        /// <remarks>
        /// Default implementation of <see cref="TeamBehavior.TeamLayer"/> and <see cref="TeamBehavior.TeamFakeLayer"/> checks against this for determining desired layer
        /// </remarks>
        Player = 1 << 2,
    }
    #endregion
}
