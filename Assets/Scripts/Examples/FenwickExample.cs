using UnityEngine;

namespace BovineLabs.Examples
{
    /// <summary>
    /// Demonstrates Fenwick Tree (Binary Indexed Tree) for prefix sums.
    /// O(log n) updates and queries for dynamic frequency data.
    /// </summary>
    public class FenwickExample : MonoBehaviour
    {
        [Header("Tree Settings")]
        [SerializeField] private int size = 16;
        
        [Header("Operations (Add in Start)")]
        [SerializeField] private int[] addIndices = { 0, 3, 7, 15 };
        [SerializeField] private long[] addValues = { 5, 10, 3, 7 };
        
        [Header("Visualization")]
        [SerializeField] private float barWidth = 0.1f;
        [SerializeField] private float maxHeight = 5f;
        
        [Header("Results")]
        [SerializeField] private long[] tree;
        [SerializeField] private bool hasRun;
        
        private void Start()
        {
            RunFenwick();
        }
        
        private void RunFenwick()
        {
            tree = new long[size + 1];
            
            for (int i = 0; i < addIndices.Length && i < addValues.Length; i++)
            {
                Add(addIndices[i], addValues[i]);
            }
            
            hasRun = true;
        }
        
        private void Add(int idx, long val)
        {
            idx++;
            while (idx <= size)
            {
                tree[idx] += val;
                idx += idx & -idx;
            }
        }
        
        private long PrefixSum(int idx)
        {
            idx++;
            long res = 0;
            while (idx > 0)
            {
                res += tree[idx];
                idx -= idx & -idx;
            }
            return res;
        }
        
        private void OnDrawGizmos()
        {
            if (!hasRun || tree == null) return;
            
            for (int i = 0; i < size; i++)
            {
                long prefix = PrefixSum(i);
                float h = Mathf.Min((float)prefix / maxHeight, 1f) * maxHeight;
                
                Vector3 basePos = transform.position + new Vector3(i * barWidth, 0, 0);
                Vector3 topPos = basePos + Vector3.up * h;
                
                Gizmos.color = Color.Lerp(Color.cyan, Color.magenta, (float)i / size);
                Gizmos.DrawLine(basePos, topPos);
                Gizmos.DrawWireSphere(topPos, barWidth * 0.3f);
                
                // Draw BIT value at this index
                if (tree[i + 1] > 0)
                {
                    Vector3 bitPos = basePos + Vector3.up * tree[i + 1] * 0.5f;
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(bitPos, barWidth * 0.2f);
                }
            }
        }
    }
}