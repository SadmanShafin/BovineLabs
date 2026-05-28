using System;
using Unity.Mathematics;

namespace BovineLabs.Grid.Coordinates
{
    [Serializable]
    public readonly struct GridCoord2 : IEquatable<GridCoord2>
    {
        public readonly int X;
        public readonly int Y;

        public GridCoord2(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public void ToInt2(out int2 result) => result = new int2(this.X, this.Y);

        public static void FromInt2(in int2 v, out GridCoord2 result) => result = new GridCoord2(v.x, v.y);

        public void ManhattanDelta(in GridCoord2 other, out int result) =>
            result = math.abs(this.X - other.X) + math.abs(this.Y - other.Y);

        public void ChebyshevDelta(in GridCoord2 other, out int result) =>
            result = math.max(math.abs(this.X - other.X), math.abs(this.Y - other.Y));

        public void OctileDelta(in GridCoord2 other, out float result)
        {
            int dx = math.abs(this.X - other.X);
            int dy = math.abs(this.Y - other.Y);
            result = math.max(dx, dy) + (1.41421354f - 1f) * math.min(dx, dy);
        }

        public void SquaredEuclideanDelta(in GridCoord2 other, out float result)
        {
            int dx = this.X - other.X;
            int dy = this.Y - other.Y;
            result = dx * dx + dy * dy;
        }

        public void EuclideanDelta(in GridCoord2 other, out float result)
        {
            int dx = this.X - other.X;
            int dy = this.Y - other.Y;
            result = math.sqrt(dx * dx + dy * dy);
        }

        public bool Equals(GridCoord2 other) => this.X == other.X && this.Y == other.Y;

        public override bool Equals(object obj) => obj is GridCoord2 other && this.Equals(other);

        public override int GetHashCode() => HashCode.Combine(this.X, this.Y);

        public static bool operator ==(GridCoord2 left, GridCoord2 right) => left.Equals(right);

        public static bool operator !=(GridCoord2 left, GridCoord2 right) => !left.Equals(right);

        public override string ToString() => $"({this.X}, {this.Y})";
    }

    [Serializable]
    public readonly struct GridCoord3 : IEquatable<GridCoord3>
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        public GridCoord3(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public void ToInt3(out int3 result) => result = new int3(this.X, this.Y, this.Z);

        public static void FromInt3(in int3 v, out GridCoord3 result) => result = new GridCoord3(v.x, v.y, v.z);

        public void ManhattanDelta(in GridCoord3 other, out int result) =>
            result = math.abs(this.X - other.X) + math.abs(this.Y - other.Y) + math.abs(this.Z - other.Z);

        public void ChebyshevDelta(in GridCoord3 other, out int result) =>
            result = math.max(math.max(math.abs(this.X - other.X), math.abs(this.Y - other.Y)), math.abs(this.Z - other.Z));

        public void SquaredEuclideanDelta(in GridCoord3 other, out float result)
        {
            int dx = this.X - other.X;
            int dy = this.Y - other.Y;
            int dz = this.Z - other.Z;
            result = dx * dx + dy * dy + dz * dz;
        }

        public void EuclideanDelta(in GridCoord3 other, out float result)
        {
            int dx = this.X - other.X;
            int dy = this.Y - other.Y;
            int dz = this.Z - other.Z;
            result = math.sqrt(dx * dx + dy * dy + dz * dz);
        }

        public bool Equals(GridCoord3 other) => this.X == other.X && this.Y == other.Y && this.Z == other.Z;

        public override bool Equals(object obj) => obj is GridCoord3 other && this.Equals(other);

        public override int GetHashCode() => HashCode.Combine(this.X, this.Y, this.Z);

        public static bool operator ==(GridCoord3 left, GridCoord3 right) => left.Equals(right);

        public static bool operator !=(GridCoord3 left, GridCoord3 right) => !left.Equals(right);

        public override string ToString() => $"({this.X}, {this.Y}, {this.Z})";
    }
}
