using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MultigridProjector.Utilities
{
    public static class Arithmetic
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CombineHashCodes(IEnumerable<int> hashCodes)
        {
            // https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations

            unchecked
            {
                var hash1 = (5381 << 16) + 5381;
                var hash2 = hash1;

                var i = 0;
                foreach (var hashCode in hashCodes)
                {
                    if ((i & 1) == 0)
                        hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ hashCode;
                    else
                        hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ hashCode;

                    ++i;
                }

                return hash1 + hash2 * 1566083941;
            }
        }
    }
}