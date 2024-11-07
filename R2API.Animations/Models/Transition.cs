using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace R2API.Models;

public class Transition
{
    public string DestinationStateName { get; set; }
    public float TransitionDuration { get; set; }
    public float Offset { get; set; }
    public float ExitTime { get; set; }
    public bool HasExitTime { get; set; }
    public bool HasFixedDuration { get; set; }
    public List<Condition> Conditions { get; } = [];

    /// <summary>
    /// Writing modifications into a binary writer for caching purposes.
    /// </summary>
    /// <param name="writer"></param>
    public void WriteBinary(BinaryWriter writer)
    {
        writer.Write(DestinationStateName ?? "");
        writer.Write(TransitionDuration);
        writer.Write(Offset);
        writer.Write(ExitTime);
        writer.Write(HasExitTime);
        writer.Write(HasFixedDuration);
        foreach (var condition in Conditions)
        {
            condition.WriteBinary(writer);
        }
    }
}
