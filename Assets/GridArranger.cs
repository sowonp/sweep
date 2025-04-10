using UnityEditor;
using UnityEngine;

public class GridArranger : MonoBehaviour
{
    [MenuItem("Tools/Arrange Selected in Grid")]
    static void ArrangeInGrid()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            Debug.LogWarning("������ ������Ʈ�� ���õ��� �ʾҾ��!");
            return;
        }

        int count = selected.Length;
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(count)); // ���� ũ�� �ڵ� ���
        float spacing = 5f; // ���� ���� (���ϴ� ��� ���� ����)

        Vector3 startPos = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            int row = i / gridSize;
            int col = i % gridSize;

            Vector3 pos = startPos + new Vector3(col * spacing, 0f, row * spacing);
            selected[i].transform.position = pos;
        }

        Debug.Log($" {count}�� ������Ʈ�� {gridSize}x{gridSize} ���ڷ� ���� �Ϸ�!");
    }
}
