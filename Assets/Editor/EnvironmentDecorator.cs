using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TombOfServilii
{
    public class EnvironmentDecorator : EditorWindow
    {
        private List<GameObject> prefabsToScatter = new List<GameObject>();
        private int scatterCount = 50;
        private float minRadius = 15f;
        private float maxRadius = 80f;
        private Transform centerPoint;
        private float minScaleMultiplier = 0.8f;
        private float maxScaleMultiplier = 1.3f;
        
        private Vector2 scrollPosition;

        [MenuItem("Tomb of Servilii/Environment Decorator")]
        public static void ShowWindow()
        {
            GetWindow<EnvironmentDecorator>("Environment Decorator");
        }

        private void OnGUI()
        {
            // Title Header
            GUILayout.Space(10);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 16;
            titleStyle.normal.textColor = new Color(0.96f, 0.65f, 0.14f); // Golden accent
            GUILayout.Label("🏛️ Environment Decorator", titleStyle);
            GUILayout.Label("Procedurally scatter trees, rocks, and foliage on terrains or ground models.", EditorStyles.miniLabel);
            GUILayout.Space(10);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Center Point
            centerPoint = (Transform)EditorGUILayout.ObjectField("Center Anchor Point", centerPoint, typeof(Transform), true);
            if (centerPoint == null)
            {
                EditorGUILayout.HelpBox("If no Center Anchor Point is assigned, scattering will center around origin (0, 0, 0).", MessageType.Info);
            }

            EditorGUILayout.Space();
            GUILayout.Label("Prefabs to Scatter", EditorStyles.boldLabel);
            
            // Prefab List Editing
            int listSize = EditorGUILayout.IntField("Prefab Slots", prefabsToScatter.Count);
            while (listSize > prefabsToScatter.Count) prefabsToScatter.Add(null);
            while (listSize < prefabsToScatter.Count) prefabsToScatter.RemoveAt(prefabsToScatter.Count - 1);

            for (int i = 0; i < prefabsToScatter.Count; i++)
            {
                prefabsToScatter[i] = (GameObject)EditorGUILayout.ObjectField($"Slot {i + 1}", prefabsToScatter[i], typeof(GameObject), false);
            }

            if (prefabsToScatter.Count == 0)
            {
                EditorGUILayout.HelpBox("Drag and drop tree/foliage prefabs into slots to begin.", MessageType.Warning);
            }

            // Scatter Settings
            EditorGUILayout.Space();
            GUILayout.Label("Scatter Parameters", EditorStyles.boldLabel);
            scatterCount = EditorGUILayout.IntField("Total Count", scatterCount);
            minRadius = EditorGUILayout.FloatField("Min Radius", minRadius);
            maxRadius = EditorGUILayout.FloatField("Max Radius", maxRadius);
            minScaleMultiplier = EditorGUILayout.FloatField("Min Scale", minScaleMultiplier);
            maxScaleMultiplier = EditorGUILayout.FloatField("Max Scale", maxScaleMultiplier);

            EditorGUILayout.Space();
            
            // Buttons
            GUI.backgroundColor = new Color(0.18f, 0.8f, 0.44f); // Green for Scatter
            if (GUILayout.Button("Scatter Objects On Ground", GUILayout.Height(35)))
            {
                Scatter();
            }

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f); // Red for Clear
            if (GUILayout.Button("Clear Scattered Group", GUILayout.Height(25)))
            {
                ClearScattered();
            }

            EditorGUILayout.EndScrollView();
        }

        private void Scatter()
        {
            // Validate that we have at least one prefab
            bool hasPrefab = false;
            foreach (var p in prefabsToScatter)
            {
                if (p != null) { hasPrefab = true; break; }
            }

            if (!hasPrefab)
            {
                EditorUtility.DisplayDialog("Environment Decorator", "Please assign at least one active Prefab in the slots before scattering!", "OK");
                return;
            }

            Vector3 center = centerPoint != null ? centerPoint.position : Vector3.zero;

            // Find or create parent container
            GameObject parentObj = GameObject.Find("ScatteredEnvironment");
            if (parentObj == null)
            {
                parentObj = new GameObject("ScatteredEnvironment");
                Undo.RegisterCreatedObjectUndo(parentObj, "Create ScatteredEnvironment Parent");
            }

            int placedCount = 0;
            for (int i = 0; i < scatterCount; i++)
            {
                // Generate a random position in a ring/circle
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = Random.Range(minRadius, maxRadius);
                float x = center.x + Mathf.Cos(angle) * radius;
                float z = center.z + Mathf.Sin(angle) * radius;

                // Pick a random assigned prefab
                List<GameObject> validPrefabs = new List<GameObject>();
                foreach (var p in prefabsToScatter)
                {
                    if (p != null) validPrefabs.Add(p);
                }

                GameObject selectedPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)];

                // Raycast downwards from high up to find the terrain/collider hit point
                Vector3 origin = new Vector3(x, 250f, z);
                RaycastHit hit;
                float y = center.y;
                bool hitGround = Physics.Raycast(origin, Vector3.down, out hit, 350f);
                if (hitGround)
                {
                    y = hit.point.y;
                }

                Vector3 spawnPos = new Vector3(x, y, z);

                // Instantiate prefab inside the Editor Scene
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
                if (instance == null) continue;

                // Set position, random Y rotation, and scale multiplier
                instance.transform.position = spawnPos;
                instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                
                float scaleScale = Random.Range(minScaleMultiplier, maxScaleMultiplier);
                instance.transform.localScale = selectedPrefab.transform.localScale * scaleScale;

                // Parent it to keep hierarchy clean
                instance.transform.SetParent(parentObj.transform);
                
                // Allow Undo operations
                Undo.RegisterCreatedObjectUndo(instance, "Procedural Object Scatter");
                placedCount++;
            }

            Debug.Log($"EnvironmentDecorator: Procedurally scattered {placedCount} objects around center {center}!");
        }

        private void ClearScattered()
        {
            GameObject parentObj = GameObject.Find("ScatteredEnvironment");
            if (parentObj != null)
            {
                Undo.DestroyObjectImmediate(parentObj);
                Debug.Log("EnvironmentDecorator: Cleared 'ScatteredEnvironment' from the hierarchy.");
            }
            else
            {
                EditorUtility.DisplayDialog("Environment Decorator", "No GameObject named 'ScatteredEnvironment' was found in the scene to clear.", "OK");
            }
        }
    }
}
