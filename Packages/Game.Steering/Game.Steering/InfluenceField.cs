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
        public int2 Origin;

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