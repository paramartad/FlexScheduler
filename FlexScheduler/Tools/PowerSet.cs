using System.Collections.Generic;
using System.Linq;

namespace FlexScheduler.Tools
{
    public static class PowerSet
    {

        public static IEnumerable<IEnumerable<T>> GetPowerSet<T>(IList<T> list)
        {
            return GetPowerSet(list, -1);
        }

        public static IEnumerable<IEnumerable<T>> GetPowerSet<T>(IList<T> list, int k)
        {
            return GetPowerSet(list, k, k);
        }

        public static IEnumerable<IEnumerable<T>> GetPowerSet<T>(IList<T> list, int minK, int maxK)
        {
            return
                Enumerable.Range(0, 1 << list.Count).Where(x =>
                {
                    if (minK == -1) return true;
                    var bitCount = SparseBitcount(x);
                    return bitCount >= minK && bitCount <= maxK;
                })
                    .Select(x => Enumerable.Range(0, list.Count).Where(y => (x & (1 << y)) != 0).Select(y => list[y]));
        }

        public static int SparseBitcount(int n)
        {
            var count = 0;
            while (n != 0)
            {
                count++;
                n &= (n - 1);
            }
            return count;
        }
    }
}
