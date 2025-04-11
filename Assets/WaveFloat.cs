using UnityEngine;

public class WaveFloat : MonoBehaviour
{
    public float amplitude = 0.3f;    // �ⷷ�ⷷ ����
    public float frequency = 1f;      // �ⷷ�ⷷ �ӵ�
    public float offset = 0f;         // �� ������Ʈ�� ���� ��ġ��
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        offset = Random.Range(0f, 10f); // ���� �ٸ� Ÿ�̹�
    }

    void Update()
    {
        float wave = Mathf.Sin(Time.time * frequency + offset) * amplitude;
        transform.position = new Vector3(startPos.x, startPos.y + wave, startPos.z);
    }
}
