using UnityEngine;

namespace BovineLabs.Examples
{
    /// <summary>
    /// Demonstrates Next Greater Element using Monotonic Stack.
    /// O(n) to find next greater for each element.
    /// </summary>
    public class NextGreaterExample : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private int[] input = { 73, 74, 75, 71, 69, 72, 76, 73 };
        
        [Header("Visualization")]
        [SerializeField] private float barWidth = 0.5f;
        [SerializeField] private float heightScale = 0.1f;
        
        [Header("Results")]
        [SerializeField] private int[] result;
        [SerializeField] private int count;
        [SerializeField] private bool hasRun;
        
        private void Start()
        {
            RunNextGreater();
        }
        
        private void RunNextGreater()
        {
            int n = input.Length;
            result = new int[n];
            
            int[] stack = new int[n];
            int top = 0;
            
            for (int i = 0; i < n; i++)
            {
                while (top > 0 && input[stack[top - 1]] < input[i])
                {
                    result[stack[--top]] = i;
                    count++;
                }
                stack[top++] = i;
            }
            
            for (int i = 0; i < top; i++)
                result[stack[i]] = -1;
            
            hasRun = true;
        }
        
        private void OnDrawGizmos()
        {
            if (!hasRun || input == null) return;
            
            for (int i = 0; i < input.Length; i++)
            {
                float h = input[i] * heightScale;
                Vector3 pos = transform.position + new Vector3(i * barWidth, 0, 0);
                
                // Bar
                Gizmos.color = Color.Lerp(Color.blue, Color.red, (float)input[i] / 100);
                Gizmos.DrawCube(pos + Vector3.up * h / 2, new Vector3(barWidth * 0.8f, h, barWidth * 0.8f));
                
                // Next greater arrow
                if (result[i] >= 0)
                {
                    Vector3 nextPos = transform.position + new Vector3(result[i] * barWidth, input[result[i]] * heightScale + 0.5f, 0);
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(pos + Vector3.up * (h + 0.3f), 0.15f);
                    Gizmos.DrawLine(pos + Vector3.up * (h + 0.3f), nextPos);
                    Gizmos.DrawWireSphere(nextPos, 0.15f);
                }
                else
                {
                    // No greater - X marker
                    Gizmos.color = Color.red;
                    Vector3 markerPos = pos + Vector3.up * (h + 0.5f);
                    Gizmos.DrawWireSphere(markerPos, 0.15f);
                }
            }
            
            // Draw stack visualization at bottom
            Gizmos.color = new Color(1, 1, 0, 0.5f);
            for (int i = 0; i < input.Length; i++)
            {
                Vector3 pos = transform.position + new Vector3(i * barWidth, -1, 0);
                Gizmos.DrawWireCube(pos, Vector3.one * 0.2f);
            }
        }
    }
}