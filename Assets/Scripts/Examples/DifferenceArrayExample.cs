using UnityEngine;

namespace BovineLabs.Examples
{
    /// <summary>
    /// Demonstrates Difference Array for O(1) range updates.
    /// </summary>
    public class DifferenceArrayExample : MonoBehaviour
    {
        [Header("Array Settings")]
        [SerializeField] private int size = 10;
        [SerializeField] private int[] rangesStart = { 2, 0, 7 };
        [SerializeField] private int[] rangesEnd = { 5, 3, 9 };
        [SerializeField] private long[] rangeValues = { 10, 5, 3 };
        
        [Header("Visualization")]
        [SerializeField] private float barWidth = 0.5f;
        [SerializeField] private float heightScale = 0.2f;
        
        [Header("Results")]
        [SerializeField] private long[] diff;
        [SerializeField] private long[] final;
        [SerializeField] private bool hasRun;
        
        private void Start()
        {
            RunDifferenceDemo();
        }
        
        private void RunDifferenceDemo()
        {
            diff = new long[size];
            final = new long[size];
            
            // Apply range updates
            for (int i = 0; i < rangesStart.Length && i < rangeValues.Length; i++)
            {
                int l = rangesStart[i];
                int r = rangesEnd[i];
                if (r >= size) r = size - 1;
                if (l > r) continue;
                
                diff[l] += rangeValues[i];
                if (r + 1 < size) diff[r + 1] -= rangeValues[i];
            }
            
            // Build final array
            long running = 0;
            for (int i = 0; i < size; i++)
            {
                running += diff[i];
                final[i] = running;
            }
            
            hasRun = true;
        }
        
        private void OnDrawGizmos()
        {
            if (!hasRun || final == null) return;
            
            // Draw diff array (steps)
            for (int i = 0; i < size; i++)
            {
                Vector3 pos = transform.position + new Vector3(i * barWidth, 0, 0);
                
                // Diff marker
                float diffH = diff[i] * heightScale;
                Gizmos.color = diff[i] >= 0 ? Color.green : Color.red;
                Vector3 diffTop = pos + Vector3.up * Mathf.Abs(diffH);
                if (diff[i] < 0) diffTop.y = pos.y;
                Gizmos.DrawWireCube(diffTop + Vector3.up * diffH / 2, new Vector3(barWidth * 0.3f, Mathf.Abs(diffH), barWidth * 0.3f));
                
                // Final value bar
                float finalH = final[i] * heightScale;
                Gizmos.color = Color.cyan;
                Gizmos.DrawCube(pos + Vector3.up * finalH / 2, new Vector3(barWidth * 0.8f, finalH, barWidth * 0.8f));
                
                // Range indicator line
                if (i < rangesStart.Length)
                {
                    Gizmos.color = new Color(1, 1, 0, 0.3f);
                    int end = Mathf.Min(rangesEnd[i], size - 1);
                    Vector3 start = transform.position + new Vector3(rangesStart[i] * barWidth, -1, 0);
                    Vector3 endPos = transform.position + new Vector3(end * barWidth, -1, 0);
                    Gizmos.DrawLine(start, endPos);
                }
            }
        }
    }
}