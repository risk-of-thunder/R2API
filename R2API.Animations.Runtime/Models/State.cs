using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace R2API.Models;

/// <summary>
/// States are the basic building blocks of a state machine. Each state contains
/// a Motion (AnimationClip or BlendTree) which will play while the character is
/// in that state. When an event in the game triggers a state transition, the character
/// will be left in a new state whose animation sequence will then take over.
/// </summary>
[Serializable]
public class State
{
    [SerializeField]
    private string name;
    /// <summary>
    /// Name of the state.
    /// </summary>
    public string Name { get => name; set => name = value; }

    [SerializeField]
    private string clipBundlePath;
    /// <summary>
    /// Full path to an AssetBundle that contains AnimationClip for this state.
    /// </summary>
    public string ClipBundlePath { get => clipBundlePath; set => clipBundlePath = value; }

    [SerializeField]
    private AnimationClip clip;
    /// <summary>
    /// AnimationClip that will be played by this state. Leave null if BlendTree is set.
    /// </summary>
    public AnimationClip Clip { get => clip; set => clip = value; }

    [SerializeField]
    private BlendTree blendTree;
    /// <summary>
    /// BlendTree that will be played by this state. Ignored if Clip is not null.
    /// </summary>
    public BlendTree BlendTree { get => blendTree; set => blendTree = value; }

    [SerializeField]
    private string tag;
    /// <summary>
    /// A tag can be used to identify a state.
    /// </summary>
    public string Tag { get => tag; set => tag = value; }

    [SerializeField]
    private string speedParam;
    /// <summary>
    /// The animator controller parameter that drives the speed value. Leave null if you want to use constant value.
    /// </summary>
    public string SpeedParam { get => speedParam; set => speedParam = value; }

    [SerializeField]
    private string mirrorParam;
    /// <summary>
    /// The animator controller parameter that drives the mirror value. Leave null if you want to use constant value.
    /// </summary>
    public string MirrorParam { get => mirrorParam; set => mirrorParam = value; }

    [SerializeField]
    private string cycleOffsetParam;
    /// <summary>
    /// The animator controller parameter that drives the cycle offset value. Leave null if you want to use constant value.
    /// </summary>
    public string CycleOffsetParam { get => cycleOffsetParam; set => cycleOffsetParam = value; }

    [SerializeField]
    private string timeParam;
    /// <summary>
    /// The animator controller parameter that drives the time value. Leave null if you want to use constant value.
    /// </summary>
    public string TimeParam { get => timeParam; set => timeParam = value; }

    [SerializeField]
    private float speed;
    /// <summary>
    /// The default speed of the motion.
    /// </summary>
    public float Speed { get => speed; set => speed = value; }

    [SerializeField]
    private float cycleOffset;
    /// <summary>
    /// Offset at which the animation loop starts. Useful for synchronizing looped animations.
    /// Units is normalized time.
    /// </summary>
    public float CycleOffset { get => cycleOffset; set => cycleOffset = value; }

    [SerializeField]
    private bool ikOnFeet;
    /// <summary>
    /// Should Foot IK be respected for this state.
    /// </summary>
    public bool IKOnFeet { get => ikOnFeet; set => ikOnFeet = value; }

    [SerializeField]
    private bool writeDefaultValues;
    /// <summary>
    /// Whether or not the AnimatorStates writes back the default values for properties
    /// that are not animated by its Motion.
    /// </summary>
    public bool WriteDefaultValues { get => writeDefaultValues; set => writeDefaultValues = value; }

    [SerializeField]
    private bool loop;
    /// <summary>
    /// AnimationClip is looped.
    /// </summary>
    public bool Loop { get => loop; set => loop = value; }

    [SerializeField]
    private bool mirror;
    /// <summary>
    /// Should the state be mirrored.
    /// </summary>
    public bool Mirror { get => mirror; set => mirror = value; }

    [SerializeField]
    private List<Transition> transitions = [];
    /// <summary>
    /// The transitions that are going out of the state.
    /// </summary>
    public List<Transition> Transitions { get => transitions; }

    /// <summary>
    /// Writing into a binary writer for caching purposes.
    /// </summary>
    /// <param name="writer"></param>
    public void WriteBinary(BinaryWriter writer)
    {
        writer.Write(Name ?? "");
        writer.Write(ClipBundlePath ?? "");
        if (Clip)
        {
            writer.Write(Clip.name);
        }
        if (BlendTree)
        {
            BlendTree.WriteBinary(writer);
        }
        writer.Write(Tag ?? "");
        writer.Write(SpeedParam ?? "");
        writer.Write(MirrorParam ?? "");
        writer.Write(CycleOffsetParam ?? "");
        writer.Write(TimeParam ?? "");
        writer.Write(Speed);
        writer.Write(CycleOffset);
        writer.Write(IKOnFeet);
        writer.Write(WriteDefaultValues);
        writer.Write(Loop);
        writer.Write(Mirror);

        foreach (var transition in Transitions)
        {
            transition.WriteBinary(writer);
        }
    }
}
