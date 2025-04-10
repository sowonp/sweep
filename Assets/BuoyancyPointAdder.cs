using UnityEngine;

public class BuoyancyPointAdder : MonoBehaviour
{
    [ContextMenu("Add Buoyancy Point")]
    void AddBuoyancyPoint()
    {
        GameObject buoyancyPoint = new GameObject("BuoyancyPoint");
        buoyancyPoint.transform.parent = this.transform;

        // Collider bounds�� �� �߽� �ϴ� ����
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Vector3 bottom = col.bounds.center - new Vector3(0, col.bounds.extents.y, 0);
            buoyancyPoint.transform.position = bottom;
        }
        else
        {
            // �ݶ��̴� ������ �׳� ������ 1����
            buoyancyPoint.transform.localPosition = new Vector3(0, -1f, 0);
        }

        Debug.Log("BuoyancyPoint ���� �Ϸ�: " + buoyancyPoint.transform.position);
    }
}
