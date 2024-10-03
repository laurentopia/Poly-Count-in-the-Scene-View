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
		var minCount  = 0;
		var maxCount  = 0;
		var renderers = obj.GetComponentsInChildren<Renderer>().ToList();
		//lodgroups
		var lodGroups = obj.GetComponentsInChildren<LODGroup>();
		foreach (var lodGroup in lodGroups) {
			var lods        = lodGroup.GetLODs();
			var firstLOD    = lods.First();
			var lodMaxCount = firstLOD.renderers.Sum(r => GetRendererPolyCount(r));
			maxCount += lodMaxCount;
			if (lods.Length > 0) {
				var lastLOD     = lods.Last();
				var lodMinCount = lastLOD.renderers.Sum(r => GetRendererPolyCount(r));
				minCount += lodMinCount;
			}
			//sanitize the list of renderers
			foreach (var lod in lods) {
				foreach (var r in lod.renderers) { renderers.Remove(r); }
			}
		}
		//add children polycount (we've already sanitized the list)
		foreach (var r in renderers) {
			var polyCount = GetRendererPolyCount(r);
			minCount += polyCount;
			maxCount += polyCount;
		}
		return (minCount, maxCount);
	}

	static int GetRendererPolyCount(Renderer renderer)
	{
		if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) return 0;
		if (renderer is MeshRenderer) {
			var meshFilter = renderer.GetComponent<MeshFilter>();
			if (meshFilter != null && meshFilter.sharedMesh != null) { return meshFilter.sharedMesh.triangles.Length / 3; }
		} else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
			if (skinnedMeshRenderer.sharedMesh != null) { return skinnedMeshRenderer.sharedMesh.triangles.Length / 3; }
		}
		return 0;
	}
}
