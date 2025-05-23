using UnityEngine;

public class TrashSpawner : MonoBehaviour
{
    public GameObject trashPrefab;
    public int trashCount = 100;         // 원하는 쓰레기 개수
    public int gridSize = 50;            // 바다 범위 (50x50)

    void Start()
    {
        var eval = FindObjectOfType<EvaluationManager>(); // 평가 시스템 찾기

        for (int i = 0; i < trashCount; i++)
        {
            float x = Random.Range(-gridSize / 2f, gridSize / 2f);
            float z = Random.Range(-gridSize / 2f, gridSize / 2f);
            float y = 0f;

            Vector3 spawnPos = new Vector3(x, y, z);
            GameObject spawned = Instantiate(trashPrefab, spawnPos, Quaternion.identity);

            if (eval != null)
            {
                eval.allTrash.Add(spawned); // 평가 리스트에 추가
            }
        }
    }
}
