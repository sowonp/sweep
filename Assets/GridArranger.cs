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
            Debug.LogWarning("정렬할 오브젝트가 선택되지 않았어요!");
            return;
        }

        int count = selected.Length;
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(count)); // 격자 크기 자동 계산
        float spacing = 5f; // 간격 설정 (원하는 대로 조절 가능)

        Vector3 startPos = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            int row = i / gridSize;
            int col = i % gridSize;

            Vector3 pos = startPos + new Vector3(col * spacing, 0f, row * spacing);
            selected[i].transform.position = pos;
        }

        Debug.Log($" {count}개 오브젝트를 {gridSize}x{gridSize} 격자로 정렬 완료!");
    }
}
