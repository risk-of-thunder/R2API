using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace R2API.Models;

public class State
{
    public string Name { get; set; }
    public string ClipBundlePath { get; set; }
    public AnimationClip Clip { get; set; }
    public string Tag { get; set; }
    public string SpeedParam { get; set; }
    public string MirrorParam { get; set; }
    public string CycleOffsetParam { get; set; }
    public string TimeParam { get; set; }
    public float Speed { get; set; }
    public float CycleOffset { get; set; }
    public bool IKOnFeet { get; set; }
    public bool WriteDefaultValues { get; set; }
    public bool Loop { get; set; }
    public bool Mirror { get; set; }
    public List<Transition> Transitions { get; } = [];

    /// <summary>
    /// Writing into a binary writer for caching purposes.
    /// </summary>
    /// <param name="writer"></param>
    public void WriteBinary(BinaryWriter writer)
    {
        writer.Write(Name ?? "");
        writer.Write(ClipBundlePath ?? "");
        writer.Write(NativeHelpers.GetAssetPathID(Clip));
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
