using UnityEngine;
using UnityEditor;

public class ScaleAdjuster : MonoBehaviour
{
    [MenuItem("Tools/Set Trash Scale to 0.9")]
    static void SetTrashScale()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            obj.transform.localScale = Vector3.one * 0.9f;
        }
        Debug.Log(" ������ ������ 0.9�� ���� �Ϸ�!");
    }
}
