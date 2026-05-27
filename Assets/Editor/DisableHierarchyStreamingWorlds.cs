#if UNITY_EDITOR
using UnityEditor;
using Unity.Entities;
using System.Reflection;
using System.Linq;

namespace BovineLabs.Editor
{
    [InitializeOnLoad]
    public static class DisableHierarchyStreamingWorlds
    {
        static DisableHierarchyStreamingWorlds()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            DisableStreamingStagingInHierarchy(); // Also run on domain reload
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode)
            {
                DisableStreamingStagingInHierarchy();
            }
        }

        private static void DisableStreamingStagingInHierarchy()
        {
            var assembly = System.AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Unity.Entities.Editor");
            if (assembly == null) return;
            var type = assembly.GetType("Unity.Entities.Editor.HierarchyEntitiesSettings");
            
            if (type != null)
            {
                var setMethod = type.GetMethod("SetTypesOfWorldsShown", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var getMethod = type.GetMethod("GetTypesOfWorldsShown", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                
                if (setMethod != null && getMethod != null)
                {
                    var currentFlags = (WorldFlags)getMethod.Invoke(null, null);
                    
                    // Remove Streaming and Staging flags to avoid InvalidOperationException during AsyncLoadSceneOperation
                    var newFlags = currentFlags & ~WorldFlags.Streaming & ~WorldFlags.Staging;
                    
                    if (currentFlags != newFlags)
                    {
                        setMethod.Invoke(null, new object[] { newFlags });
                        UnityEngine.Debug.Log("Disabled Streaming and Staging worlds in Hierarchy to prevent AsyncLoadSceneJob Exceptions.");
                    }
                }
            }
        }
    }
}
#endif
