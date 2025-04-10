using UnityEngine;

public class BuoyFloat : MonoBehaviour
{
    public float floatStrength = 0.3f;
    public float frequency = 1f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * frequency) * floatStrength;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}
