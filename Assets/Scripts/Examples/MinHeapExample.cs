using UnityEngine;

namespace BovineLabs.Examples
{
    /// <summary>
    /// Demonstrates Priority Queue (Min Heap).
    /// Used for Dijkstra, Prim's, task scheduling.
    /// </summary>
    public class MinHeapExample : MonoBehaviour
    {
        [Header("Heap Settings")]
        [SerializeField] private int capacity = 10;
        [SerializeField] private int[] initialValues = { 15, 10, 20, 5, 8 };
        
        [Header("Visualization")]
        [SerializeField] private float nodeRadius = 0.3f;
        [SerializeField] private float levelHeight = 1f;
        
        [Header("Results")]
        [SerializeField] private int[] heapArray;
        [SerializeField] private int count;
        [SerializeField] private bool hasRun;
        
        private void Start()
        {
            RunHeapDemo();
        }
        
        private void RunHeapDemo()
        {
            heapArray = new int[capacity];
            count = 0;
            
            foreach (int val in initialValues)
            {
                if (count < capacity) Push(val);
            }
            
            hasRun = true;
        }
        
        private void Push(int val)
        {
            int i = count;
            heapArray[i] = val;
            
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (heapArray[parent] <= heapArray[i]) break;
                (heapArray[parent], heapArray[i]) = (heapArray[i], heapArray[parent]);
                i = parent;
            }
            count++;
        }
        
        private void OnDrawGizmos()
        {
            if (!hasRun || heapArray == null) return;
            
            // Draw tree structure
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetNodePosition(i);
                
                // Draw edges to children
                int left = (i << 1) + 1;
                int right = left + 1;
                
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f);
                if (left < count)
                {
                    Gizmos.DrawLine(pos, GetNodePosition(left));
                }
                if (right < count)
                {
                    Gizmos.DrawLine(pos, GetNodePosition(right));
                }
                
                // Draw node
                Color nodeColor = i == 0 ? Color.yellow : Color.cyan;
                Gizmos.color = nodeColor;
                Gizmos.DrawWireSphere(pos, nodeRadius);
                
                // Draw value
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(pos + Vector3.up * 0.3f, Vector3.one * 0.2f);
            }
            
            // Draw root indicator
            if (count > 0)
            {
                Gizmos.color = Color.green;
                Vector3 minPos = transform.position + new Vector3(0, -1, count + 1);
                Gizmos.DrawWireSphere(minPos, nodeRadius);
                Gizmos.DrawLine(minPos, transform.position + Vector3.right * 2);
            }
        }
        
        private Vector3 GetNodePosition(int index)
        {
            // Calculate depth and position in level
            int depth = 0;
            int nodesInPrevLevels = 0;
            int temp = index;
            
            while (temp > 0)
            {
                temp = (temp - 1) >> 1;
                depth++;
                nodesInPrevLevels += 1 << depth - 1;
            }
            
            int posInLevel = index - nodesInPrevLevels;
            int nodesInLevel = 1 << depth;
            
            float levelWidth = nodesInLevel * nodeRadius * 2.5f;
            float xSpacing = levelWidth / (nodesInLevel + 1);
            
            float x = (posInLevel + 1) * xSpacing - levelWidth / 2;
            float y = -depth * levelHeight;
            
            return transform.position + new Vector3(x, 0, 0) + Vector3.up * y;
        }
    }
}