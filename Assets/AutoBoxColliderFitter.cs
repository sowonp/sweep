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
            // Renderer �������� (�ڽı��� Ž��)
            Renderer rend = obj.GetComponent<Renderer>() ?? obj.GetComponentInChildren<Renderer>();
            if (rend == null) continue;

            // BoxCollider �������� (������ �߰�)
            BoxCollider box = obj.GetComponent<BoxCollider>();
            if (box == null)
                box = obj.AddComponent<BoxCollider>();

            // ������� ��ġ ����
            box.center = rend.bounds.center - obj.transform.position;
            box.size = rend.bounds.size;

            successCount++;
        }

        Debug.Log($" Auto Fit �Ϸ�! ����� ������Ʈ ��: {successCount}");
    }
}
