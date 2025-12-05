using RoR2;
using RoR2.UI;
using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("R2API.CharacterBody")]

namespace R2API;

internal static class CharacterBodyInterop
{
    public static byte[] GetModdedBodyFlags(CharacterBody characterBody) => characterBody.r2api_moddedBodyFlags;
    public static void SetModdedBodyFlags(CharacterBody characterBody, byte[] value) => characterBody.r2api_moddedBodyFlags = value;
    public static Sprite GetCustomSprintIcon(CharacterBody characterBody) => characterBody.r2api_customSprintIcon;
    public static void SetCustomSprintIcon(CharacterBody characterBody, Sprite value) => characterBody.r2api_customSprintIcon = value;
    public static GameObject GetCustomIconObject(SprintIcon sprintIcon) => sprintIcon.r2api_customIconObject;
    public static void SetCustomIconObject(SprintIcon sprintIcon, GameObject value) => sprintIcon.r2api_customIconObject = value;
    public static Sprite GetCurrentCustomSprintIcon(SprintIcon sprintIcon) => sprintIcon.r2api_currentCustomSprintIcon;
    public static void SetCurrentCustomSprintIcon(SprintIcon sprintIcon, Sprite value) => sprintIcon.r2api_currentCustomSprintIcon = value;
}
