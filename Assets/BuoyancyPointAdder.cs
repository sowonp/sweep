using UnityEngine;

public class BuoyancyPointAdder : MonoBehaviour
{
    [ContextMenu("Add Buoyancy Point")]
    void AddBuoyancyPoint()
    {
        GameObject buoyancyPoint = new GameObject("BuoyancyPoint");
        buoyancyPoint.transform.parent = this.transform;

        // Collider bounds로 배 중심 하단 추정
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Vector3 bottom = col.bounds.center - new Vector3(0, col.bounds.extents.y, 0);
            buoyancyPoint.transform.position = bottom;
        }
        else
        {
            // 콜라이더 없으면 그냥 밑으로 1유닛
            buoyancyPoint.transform.localPosition = new Vector3(0, -1f, 0);
        }

        Debug.Log("BuoyancyPoint 생성 완료: " + buoyancyPoint.transform.position);
    }
}
