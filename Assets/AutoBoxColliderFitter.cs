using UnityEngine;
using UnityEditor;

public class ColliderFitterUtility : MonoBehaviour
{
    [MenuItem("Tools/Auto Fit All Box Colliders")]
    public static void FitAllBoxColliders()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected. Please select objects in the Hierarchy.");
            return;
        }

        int successCount = 0;

        foreach (GameObject obj in selectedObjects)
        {
            // Renderer 가져오기 (자식까지 탐색)
            Renderer rend = obj.GetComponent<Renderer>() ?? obj.GetComponentInChildren<Renderer>();
            if (rend == null) continue;

            // BoxCollider 가져오기 (없으면 추가)
            BoxCollider box = obj.GetComponent<BoxCollider>();
            if (box == null)
                box = obj.AddComponent<BoxCollider>();

            // 사이즈와 위치 설정
            box.center = rend.bounds.center - obj.transform.position;
            box.size = rend.bounds.size;

            successCount++;
        }

        Debug.Log($" Auto Fit 완료! 적용된 오브젝트 수: {successCount}");
    }
}
