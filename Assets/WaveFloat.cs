using UnityEngine;

public class WaveFloat : MonoBehaviour
{
    public float amplitude = 0.3f;    // 출렁출렁 높이
    public float frequency = 1f;      // 출렁출렁 속도
    public float offset = 0f;         // 각 오브젝트별 랜덤 위치차
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        offset = Random.Range(0f, 10f); // 각기 다른 타이밍
    }

    void Update()
    {
        float wave = Mathf.Sin(Time.time * frequency + offset) * amplitude;
        transform.position = new Vector3(startPos.x, startPos.y + wave, startPos.z);
    }
}
