using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace R2API.Utils;

internal static class EnumPatcher
{
    static readonly Dictionary<Type, EnumData> _enumDataByType = [];

    static readonly List<IDetour> _hookInstances = [];

    static bool _hooksEnabled;

    internal static void SetHooks()
    {
        if (_hooksEnabled)
            return;

        MethodInfo getValuesAndNamesMethod = AccessTools.DeclaredMethod(typeof(Enum), "GetCachedValuesAndNames");
        if (getValuesAndNamesMethod != null)
        {
            _hookInstances.Add(new ILHook(getValuesAndNamesMethod, AppendEnumValuesAndNamesHook));
        }

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        if (!_hooksEnabled)
            return;

        foreach (IDetour detour in _hookInstances)
        {
            detour?.Dispose();
        }

        _hookInstances.Clear();

        _hooksEnabled = false;
    }

    private static void AppendEnumValuesAndNamesHook(ILContext il)
    {
        ILCursor c = new ILCursor(il);

        TypeReference returnTypeReference = il.Method.ReturnType;
        TypeDefinition returnType = returnTypeReference.SafeResolve();
        if (returnType == null)
        {
            R2API.Logger.LogError($"EnumPatcher.AppendEnumValuesAndNamesHook: Failed to resolve return type of {il.Method.FullName}");
            return;
        }

        FieldDefinition valuesField = returnType.FindField("Values");
        if (valuesField == null)
        {
            R2API.Logger.LogError($"EnumPatcher.AppendEnumValuesAndNamesHook ({il.Method.FullName}): Failed to find Values field");
            return;
        }

        FieldDefinition namesField = returnType.FindField("Names");
        if (namesField == null)
        {
            R2API.Logger.LogError($"EnumPatcher.AppendEnumValuesAndNamesHook ({il.Method.FullName}): Failed to find Names field");
            return;
        }

        VariableDefinition returnTypeTempVar = new VariableDefinition(returnTypeReference);
        il.Method.Body.Variables.Add(returnTypeTempVar);

        while (c.TryGotoNext(MoveType.AfterLabel,
                             x => x.MatchRet()))
        {
            ILLabel retLabel = c.DefineLabel();

            c.Emit(OpCodes.Stloc, returnTypeTempVar);
            c.Emit(OpCodes.Ldloc, returnTypeTempVar);
            c.Emit(OpCodes.Brfalse, retLabel);

            c.Emit(OpCodes.Ldarg_0);

            c.Emit(OpCodes.Ldloc, returnTypeTempVar);
            c.Emit(OpCodes.Ldflda, valuesField);

            c.Emit(OpCodes.Ldloc, returnTypeTempVar);
            c.Emit(OpCodes.Ldflda, namesField);

            static void modifyEnumValues(Type enumType, ref ulong[] values, ref string[] names)
            {
                if (_enumDataByType.TryGetValue(enumType, out EnumData enumData))
                {
                    enumData.ModifyEnumValues(ref names, ref values);
                }
            }

            c.EmitDelegate(modifyEnumValues);

            c.MarkLabel(retLabel);
            c.Emit(OpCodes.Ldloc, returnTypeTempVar);

            c.SearchTarget = SearchTarget.Next;
        }
    }

    [SuppressMessage("Design", "R2APISubmodulesAnalyzer:Public API Method is not enabling the hooks if needed.", Justification = "Hooks are applied in the called method")]
    public static EnumValueHandle SetEnumValue<TEnum>(string name, TEnum value) where TEnum : Enum
    {
        return SetEnumValue(typeof(TEnum), name, value);
    }

    public static EnumValueHandle SetEnumValue(Type enumType, string name, object enumValue)
    {
        if (enumType is null)
            throw new ArgumentNullException(nameof(enumType));

        if (string.IsNullOrEmpty(name))
            throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));

        if (enumValue is null)
            throw new ArgumentNullException(nameof(enumValue));

        if (!enumType.IsEnum)
            throw new ArgumentException($"'{nameof(enumType)}' must be an enum type", nameof(enumType));

        if (!enumType.IsAssignableFrom(enumValue.GetType()))
            throw new ArgumentException($"'{nameof(enumValue)}' must be of type '{nameof(enumType)}'");

        SetHooks();

        if (!_enumDataByType.TryGetValue(enumType, out EnumData enumData))
        {
            enumData = new EnumData(enumType);
            _enumDataByType.Add(enumType, enumData);
        }

        return enumData.AddEntry(name, enumValue);
    }

    [SuppressMessage("Design", "R2APISubmodulesAnalyzer:Public API Method is not enabling the hooks if needed.", Justification = "Hooks are not necessary here")]
    public static void RemoveEnumValueEntry(in EnumValueHandle handle)
    {
        if (!IsValid(handle))
            return;

        if (_enumDataByType.TryGetValue(handle.EnumType, out EnumData enumData))
        {
            enumData.RemoveEntry(handle);

            if (enumData.EntryCount <= 0)
            {
                _enumDataByType.Remove(handle.EnumType);
            }
        }
    }

    [SuppressMessage("Design", "R2APISubmodulesAnalyzer:Public API Method is not enabling the hooks if needed.", Justification = "Hooks are not necessary here")]
    public static bool IsValid(in EnumValueHandle handle)
    {
        if (handle.EnumType == null || handle.Value == 0)
            return false;

        return _enumDataByType.TryGetValue(handle.EnumType, out EnumData enumData) && enumData.IsHandleValid(handle);
    }

    class EnumData
    {
        const int EntriesCapacityIncrement = 4;

        public Type Type { get; }

        readonly Type _underlyingType;
        readonly bool _isUnderlyingTypeUnsigned;

        ulong _nextEntryHandle = 1;

        string[] _baseNames;
        ulong[] _baseValues;

        int _entriesCount;

        EnumValueEntry[] _entries = new EnumValueEntry[EntriesCapacityIncrement];

        bool _combinedValuesDirty;

        string[] _cachedCombinedNames;
        ulong[] _cachedCombinedValues;

        public int EntryCount => _entriesCount;

        internal EnumData(Type type)
        {
            Type = type;
            _underlyingType = Type.GetEnumUnderlyingType();
            _isUnderlyingTypeUnsigned = Type.GetTypeCode(_underlyingType) switch
            {
                TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => true,
                _ => false
            };
        }

        private void EnsureDesiredEntriesCapacity()
        {
            int desiredSize = ((_entriesCount / EntriesCapacityIncrement) + 1) * EntriesCapacityIncrement;
            if (_entries.Length != desiredSize)
            {
                Array.Resize(ref _entries, desiredSize);
            }
        }

        internal ulong ToUInt64(object value)
        {
            if (_isUnderlyingTypeUnsigned)
            {
                return Convert.ToUInt64(value);
            }
            else
            {
                // Convert to signed int first to handle negative numbers
                return unchecked((ulong)Convert.ToInt64(value));
            }
        }

        internal object ToEnumValue(ulong value)
        {
            if (_isUnderlyingTypeUnsigned)
            {
                return Convert.ChangeType(value, _underlyingType);
            }
            else
            {
                return Convert.ChangeType(unchecked((long)value), _underlyingType);
            }
        }

        internal EnumValueHandle AddEntry(string name, object value)
        {
            EnsureDesiredEntriesCapacity();

            EnumValueHandle handle = new EnumValueHandle(Type, _nextEntryHandle++);

            _entries[_entriesCount++] = new EnumValueEntry(name, ToUInt64(value), handle);

            _combinedValuesDirty = true;

            return handle;
        }

        private void RemoveEntryAt(int entryIndex)
        {
            Array.Copy(_entries, entryIndex + 1, _entries, entryIndex, _entriesCount - entryIndex - 1);
            _entriesCount--;
        }

        internal void RemoveEntry(in EnumValueHandle handle)
        {
            for (int i = 0; i < _entriesCount; i++)
            {
                if (_entries[i].Handle.Value == handle.Value)
                {
                    RemoveEntryAt(i);
                    break;
                }
            }

            EnsureDesiredEntriesCapacity();
        }

        internal bool IsHandleValid(in EnumValueHandle handle)
        {
            for (int i = 0; i < _entriesCount; i++)
            {
                if (_entries[i].Handle.Value == handle.Value)
                {
                    return true;
                }
            }

            return false;
        }

        internal void ModifyEnumValues(ref string[] names, ref ulong[] values)
        {
            if (_entriesCount <= 0)
                return;

            if (_baseNames == null || _baseValues == null)
            {
                _baseNames = names;
                _baseValues = values;

                _combinedValuesDirty = true;
            }

            if (_combinedValuesDirty)
            {
                _combinedValuesDirty = false;
                RecalculateCombinedValues();
            }

            names = _cachedCombinedNames;
            values = _cachedCombinedValues;
        }

        private void RecalculateCombinedValues()
        {
            int combinedEntriesCount = _baseValues.Length;
            for (int i = 0; i < _entriesCount; i++)
            {
                if (Array.BinarySearch(_baseValues, _entries[i].Value) < 0)
                {
                    combinedEntriesCount++;
                }
            }

            if (_cachedCombinedNames == null || _cachedCombinedNames.Length != combinedEntriesCount)
            {
                _cachedCombinedNames = new string[combinedEntriesCount];
                _cachedCombinedValues = new ulong[combinedEntriesCount];
            }

            Array.Copy(_baseValues, _cachedCombinedValues, _baseValues.Length);
            Array.Copy(_baseNames, _cachedCombinedNames, _baseNames.Length);

            int combinedArraySize = _baseValues.Length;

            for (int i = 0; i < _entriesCount; i++)
            {
                int existingValueIndex = Array.BinarySearch(_baseValues, _entries[i].Value);
                if (existingValueIndex >= 0)
                {
#if DEBUG
                    R2API.LogDebug($"Replacing enum value {_cachedCombinedNames[existingValueIndex]}={_cachedCombinedValues[existingValueIndex]} with {_entries[i].Name} for {Type.FullName}");
#endif

                    _cachedCombinedValues[existingValueIndex] = _entries[i].Value;
                    _cachedCombinedNames[existingValueIndex] = _entries[i].Name;
                }
                else
                {
#if DEBUG
                    R2API.LogDebug($"Appending enum value {_entries[i].Name}={ToEnumValue(_entries[i].Value)} for {Type.FullName}");
#endif

                    _cachedCombinedValues[combinedArraySize] = _entries[i].Value;
                    _cachedCombinedNames[combinedArraySize] = _entries[i].Name;

                    combinedArraySize++;
                }
            }

            Array.Sort(_cachedCombinedValues, _cachedCombinedNames);
        }

        readonly struct EnumValueEntry
        {
            public readonly string Name;
            public readonly ulong Value;

            public readonly EnumValueHandle Handle;

            internal EnumValueEntry(string name, ulong value, EnumValueHandle handle)
            {
                Name = name;
                Value = value;
                Handle = handle;
            }
        }
    }

    public readonly struct EnumValueHandle
    {
        internal readonly Type EnumType;

        internal readonly ulong Value;

        internal EnumValueHandle(Type enumType, ulong value)
        {
            EnumType = enumType;
            Value = value;
        }
    }
}
