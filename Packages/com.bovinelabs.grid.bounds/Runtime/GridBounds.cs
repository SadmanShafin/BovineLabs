using System;
using BovineLabs.Grid.Coordinates;
using Unity.Mathematics;

namespace BovineLabs.Grid.Bounds
{
    public readonly struct GridBounds2D : IEquatable<GridBounds2D>
    {
        public readonly int Width;
        public readonly int Height;

        private GridBounds2D(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }

        public int Length => this.Width * this.Height;

        public bool InBounds(int x, int y) =>
            (uint)x < (uint)this.Width && (uint)y < (uint)this.Height;

        public bool InBounds(int index) => (uint)index < (uint)this.Length;

        public static bool TryCreate(int width, int height, out GridBounds2D bounds)
        {
            bounds = default;
            if (width <= 0 || height <= 0) return false;
            if ((long)width * height > int.MaxValue) return false;
            bounds = new GridBounds2D(width, height);
            return true;
        }

        public bool Equals(GridBounds2D other) => this.Width == other.Width && this.Height == other.Height;
        public override bool Equals(object obj) => obj is GridBounds2D other && this.Equals(other);
        public override int GetHashCode() => HashCode.Combine(this.Width, this.Height);
        public static bool operator ==(GridBounds2D left, GridBounds2D right) => left.Equals(right);
        public static bool operator !=(GridBounds2D left, GridBounds2D right) => !left.Equals(right);
    }

    public readonly struct GridBounds3D : IEquatable<GridBounds3D>
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int Depth;

        private GridBounds3D(int width, int height, int depth)
        {
            this.Width = width;
            this.Height = height;
            this.Depth = depth;
        }

        public int Length => this.Width * this.Height * this.Depth;

        public bool InBounds(int x, int y, int z) =>
            (uint)x < (uint)this.Width && (uint)y < (uint)this.Height && (uint)z < (uint)this.Depth;

        public bool InBounds(int index) => (uint)index < (uint)this.Length;

        public static bool TryCreate(int width, int height, int depth, out GridBounds3D bounds)
        {
            bounds = default;
            if (width <= 0 || height <= 0 || depth <= 0) return false;
            long volume = (long)width * height * depth;
            if (volume > int.MaxValue) return false;
            bounds = new GridBounds3D(width, height, depth);
            return true;
        }

        public bool Equals(GridBounds3D other) =>
            this.Width == other.Width && this.Height == other.Height && this.Depth == other.Depth;

        public override bool Equals(object obj) => obj is GridBounds3D other && this.Equals(other);
        public override int GetHashCode() => HashCode.Combine(this.Width, this.Height, this.Depth);
        public static bool operator ==(GridBounds3D left, GridBounds3D right) => left.Equals(right);
        public static bool operator !=(GridBounds3D left, GridBounds3D right) => !left.Equals(right);
    }
}
