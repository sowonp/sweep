using UnityEngine;

public class BoatFloat : MonoBehaviour
{
    public float floatStrength = 0.5f;
    public float frequency = 1.0f;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float wave = Mathf.Sin(Time.time * frequency) * floatStrength;
        transform.position = new Vector3(startPos.x, startPos.y + wave, startPos.z);
    }
}
