using System;
using UnityEngine;
using Mirror;

public class EntitySyncMarker : MonoBehaviour
{
    public Guid guid;
    public bool isSync = false;

    public Vector3 targetPosition;
    public Quaternion targetRotation;
    public bool hasTarget = false;

    void Start()
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    void Update()
    {
        // Si no somos servidor (somos cliente proxy), interpolamos suavemente en cada frame hacia el objetivo del servidor
        if (!NetworkServer.active && hasTarget)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
        }
    }

    public void SetTarget(Vector3 pos, Quaternion rot)
    {
        targetPosition = pos;
        targetRotation = rot;
        hasTarget = true;
    }
}
