using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Steering
{
    public enum Influence : byte { Objective, Threat, AllyPressure, Hazard, Lure }

    public static class Influences { public const int Count = 5; }

    public struct InfluenceField : IComponentData
    {
        public int2 Size;
        public float Step;
        public float InvStep;
        public int Channels;


        // Grid-cell origin in integer cell coordinates.
        // cell (Origin.x, Origin.y) maps to world position (0, 0).
        public int2 Origin;

        // World-space position of cell (0, 0) center.
        // Use this for all world-space debug drawing — never reconstruct
        // from Origin alone since Origin only tracks cell offsets.
        public float2 WorldOrigin;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(int channel, int2 cell)
        {
            return (((cell.y * Size.x) + cell.x) * Channels) + channel;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 CellCenter(int2 cell)
        {
            return ((float2)(Origin + cell) + 0.5f) * Step;
        }

        // World-space center of a cell (avoids using Origin directly).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 CellCenterWorld(int2 cell)
        {
            return ((float2)(cell) + 0.5f) * Step + WorldOrigin;
        }
    }

    [InternalBufferCapacity(0)]
    public struct InfluenceValue : IBufferElementData
    {
        public float2 Value;
    }

    [InternalBufferCapacity(0)]
    public struct OccupancyValue : IBufferElementData
    {
        public byte Blocked;
    }
}