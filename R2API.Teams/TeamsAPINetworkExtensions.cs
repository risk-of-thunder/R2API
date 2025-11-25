using RoR2;
using UnityEngine.Networking;

namespace R2API.Utils;

/// <summary>
/// Network extensions for TeamsAPI
/// </summary>
public static class TeamsAPINetworkExtensions
{
    /// <summary>
    /// Writes a <see cref="TeamMask"/> to a network buffer
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="teamMask"></param>
    public static void Write(this NetworkWriter writer, in TeamMask teamMask)
    {
        writer.Write(teamMask.a);

        if (TeamsAPI.ModdedTeamCount > 0)
        {
            CompressedFlagArrayUtilities.WriteToNetworkWriter(TeamsInterop.GetModdedMask(teamMask) ?? [], writer, TeamsAPI.ModdedTeamCount);
        }
    }

    /// <summary>
    /// Reads a <see cref="TeamMask"/> from a network buffer
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    public static TeamMask ReadTeamMask(this NetworkReader reader)
    {
        TeamMask teamMask = new TeamMask();
        teamMask.a = reader.ReadByte();

        if (TeamsAPI.ModdedTeamCount > 0)
        {
            byte[] moddedTeamsMask = CompressedFlagArrayUtilities.ReadFromNetworkReader(reader, TeamsAPI.ModdedTeamCount);
            if (moddedTeamsMask != null && moddedTeamsMask.Length > 0)
            {
                TeamsInterop.SetModdedMask(ref teamMask, moddedTeamsMask);
            }
        }

        return teamMask;
    }
}
