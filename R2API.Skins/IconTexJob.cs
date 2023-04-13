using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace R2API;

internal struct IconTexJob : IJobParallelFor
{

    [ReadOnly]
    public Color32 Top;

    [ReadOnly]
    public Color32 Right;

    [ReadOnly]
    public Color32 Bottom;

    [ReadOnly]
    public Color32 Left;

    [ReadOnly]
    public Color32 Line;

    public NativeArray<Color32> TexOutput;

    public void Execute(int index)
    {
        int x = index % 128 - 64;
        int y = index / 128 - 64;

        if (Math.Abs(Math.Abs(y) - Math.Abs(x)) <= 2)
        {
            TexOutput[index] = Line;
            return;
        }
        if (y > x && y > -x)
        {
            TexOutput[index] = Top;
            return;
        }
        if (y < x && y < -x)
        {
            TexOutput[index] = Bottom;
            return;
        }
        if (y > x && y < -x)
        {
            TexOutput[index] = Left;
            return;
        }
        if (y < x && y > -x)
        {
            TexOutput[index] = Right;
        }
    }
}
