using UnityEngine;

public class TrashSpawner : MonoBehaviour
{
    public GameObject trashPrefab;
    public int trashCount = 100;         // ���ϴ� ������ ����
    public int gridSize = 50;            // �ٴ� ���� (50x50)

    void Start()
    {
        var eval = FindObjectOfType<EvaluationManager>(); // �� �ý��� ã��

        for (int i = 0; i < trashCount; i++)
        {
            float x = Random.Range(-gridSize / 2f, gridSize / 2f);
            float z = Random.Range(-gridSize / 2f, gridSize / 2f);
            float y = 0f;

            Vector3 spawnPos = new Vector3(x, y, z);
            GameObject spawned = Instantiate(trashPrefab, spawnPos, Quaternion.identity);

            if (eval != null)
            {
                eval.allTrash.Add(spawned); // �� ����Ʈ�� �߰�
            }
        }
    }
}
