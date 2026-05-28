using UnityEngine;
using System.Collections.Generic;

namespace BovineLabs.Examples
{
    /// <summary>
    /// Master controller - creates example GameObjects with Gizmos.
    /// Each example visualizes an algorithm from IAFahim.CS packages.
    /// </summary>
    public class AlgorithmExamplesController : MonoBehaviour
    {
        [Header("Examples to Spawn")]
        [SerializeField] private List<ExampleType> activeExamples = new()
        {
            ExampleType.GridBFS,
            ExampleType.Fenwick,
            ExampleType.SlidingWindow,
            ExampleType.Imos2D,
            ExampleType.FastPow,
            ExampleType.MinHeap,
            ExampleType.NextGreater,
            ExampleType.MathBasic,
            ExampleType.GridRotate,
            ExampleType.DifferenceArray
        };
        
        [Header("Spawn Settings")]
        [SerializeField] private float spacingX = 20f;
        [SerializeField] private float spacingZ = 20f;
        
        private readonly Dictionary<ExampleType, GameObject> _instances = new();
        
        public enum ExampleType
        {
            GridBFS,
            Fenwick,
            SlidingWindow,
            Imos2D,
            FastPow,
            MinHeap,
            NextGreater,
            MathBasic,
            GridRotate,
            DifferenceArray
        }
        
        private void Start()
        {
            SpawnExamples();
        }
        
        private void SpawnExamples()
        {
            int index = 0;
            foreach (var example in activeExamples)
            {
                int row = index / 4;
                int col = index % 4;
                Vector3 pos = transform.position + new Vector3(col * spacingX, 0, row * spacingZ);
                
                var go = CreateExample(example, pos);
                _instances[example] = go;
                index++;
            }
        }
        
        private GameObject CreateExample(ExampleType type, Vector3 position)
        {
            string name = type.ToString();
            var go = new GameObject($"Algo_{name}");
            go.transform.position = position;
            go.transform.parent = transform;
            
            switch (type)
            {
                case ExampleType.GridBFS:
                    go.AddComponent<GridBfsExample>();
                    break;
                case ExampleType.Fenwick:
                    go.AddComponent<FenwickExample>();
                    break;
                case ExampleType.SlidingWindow:
                    go.AddComponent<SlidingWindowExample>();
                    break;
                case ExampleType.Imos2D:
                    go.AddComponent<Imos2DExample>();
                    break;
                case ExampleType.FastPow:
                    go.AddComponent<FastPowExample>();
                    break;
                case ExampleType.MinHeap:
                    go.AddComponent<MinHeapExample>();
                    break;
                case ExampleType.NextGreater:
                    go.AddComponent<NextGreaterExample>();
                    break;
                case ExampleType.MathBasic:
                    go.AddComponent<MathBasicExample>();
                    break;
                case ExampleType.GridRotate:
                    go.AddComponent<GridRotateExample>();
                    break;
                case ExampleType.DifferenceArray:
                    go.AddComponent<DifferenceArrayExample>();
                    break;
            }
            
            return go;
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw spawn grid preview
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            
            int index = 0;
            foreach (var example in activeExamples)
            {
                int row = index / 4;
                int col = index % 4;
                Vector3 pos = transform.position + new Vector3(col * spacingX, 0, row * spacingZ);
                
                Gizmos.DrawWireCube(pos, Vector3.one * 3f);
                index++;
            }
        }
    }
}