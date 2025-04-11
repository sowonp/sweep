using UnityEngine;

public class FloatingMotion : MonoBehaviour
{
    public float floatStrength = 0.2f;
    public float frequency = 1.5f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * frequency) * floatStrength;
    }
}
