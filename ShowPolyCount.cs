using UnityEngine;
using UnityEditor;
using System.Linq;

[InitializeOnLoad]
public class PolyCountSceneViewDisplay
{
	static PolyCountSceneViewDisplay()
	{
		SceneView.duringSceneGui += OnSceneGUI;
	}

	static void OnSceneGUI(SceneView sceneView)
	{
		var selectedObjects = Selection.gameObjects;
		if (selectedObjects.Length == 0) return;
		Handles.BeginGUI();
		var totalPolyCountMin = 0;
		var totalPolyCountMax = 0;
		var yOffset           = 10f;
		var yellowText        = new GUIStyle(GUI.skin.label) {normal = {textColor = Color.yellow}};
		foreach (var obj in selectedObjects) {
			var (minCount, maxCount) =  GetPolyCountRangeRecursive(obj);
			totalPolyCountMin        += minCount;
			totalPolyCountMax        += maxCount;
			var worldPosition = obj.transform.position;
			var guiPosition   = HandleUtility.WorldToGUIPoint(worldPosition);
			var labelRect     = new Rect(guiPosition.x + 10, guiPosition.y + yOffset, 300, 20);
			GUI.Label(labelRect, $"{obj.name}: {minCount} - {maxCount}", yellowText);
			yOffset += 20;
		}
		var totalRect = new Rect(10, 10, 300, 20);
		GUI.Label(totalRect, $"Total Poly Count: {totalPolyCountMin} - {totalPolyCountMax}", yellowText);
		Handles.EndGUI();
	}

	static (int min, int max) GetPolyCountRangeRecursive(GameObject obj)
	{
		var minCount = 0;
		var maxCount = 0;
		var lodGroup = obj.GetComponent<LODGroup>();
		if (lodGroup != null) {
			var lods = lodGroup.GetLODs();
			if (lods.Length > 0) {
				var lastLOD     = lods.Last();
				var lodMinCount = lastLOD.renderers.Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy).Sum(r => GetRendererPolyCount(r));
				var firstLOD    = lods.First();
				var lodMaxCount = firstLOD.renderers.Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy).Sum(r => GetRendererPolyCount(r));
				minCount += lodMinCount;
				maxCount += lodMaxCount;
			}
		} else {
			var objCount = obj.GetComponents<Renderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy).Sum(r => GetRendererPolyCount(r));
			minCount += objCount;
			maxCount += objCount;
			// Only recursively count child objects that are not part of an LODGroup
			foreach (Transform child in obj.transform) {
				var (childMin, childMax) =  GetPolyCountRangeRecursive(child.gameObject);
				minCount                 += childMin;
				maxCount                 += childMax;
			}
		}
		return (minCount, maxCount);
	}

	static int GetRendererPolyCount(Renderer renderer)
	{
		if (renderer is MeshRenderer) {
			var meshFilter = renderer.GetComponent<MeshFilter>();
			if (meshFilter != null && meshFilter.sharedMesh != null && renderer.gameObject.activeInHierarchy && renderer.enabled) { return meshFilter.sharedMesh.triangles.Length / 3; }
		} else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
			if (skinnedMeshRenderer.sharedMesh != null && skinnedMeshRenderer.gameObject.activeInHierarchy && skinnedMeshRenderer.enabled) {
				return skinnedMeshRenderer.sharedMesh.triangles.Length / 3;
			}
		}
		return 0;
	}
}
