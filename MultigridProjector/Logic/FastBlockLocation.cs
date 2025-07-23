using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using VRageMath;

namespace MultigridProjector.Logic
{
    // IEquatable is implemented to avoid boxing on comparison
    public readonly struct FastBlockLocation : IEquatable<FastBlockLocation>
    {
        public readonly int GridIndex;
        public readonly Vector3I Position;
        
        public static readonly FastBlockLocation INVALID = new FastBlockLocation(-1, Vector3I.Zero);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FastBlockLocation(int gridIndex, Vector3I position)
        {
            // Subgrid index
            GridIndex = gridIndex;

            // Block position inside the subgrid in blueprint block coordinates
            Position = position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return $"S{GridIndex}[{Position.X},{Position.Y},{Position.Z}]";
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            unchecked
            {
                return ((GridIndex * 397 ^ Position.X) * 397 ^ Position.Y) * 397 ^ Position.Z;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(FastBlockLocation other)
        {
            return GridIndex == other.GridIndex && Position.Equals(other.Position);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is FastBlockLocation other && Equals(other);
        }
    }
}