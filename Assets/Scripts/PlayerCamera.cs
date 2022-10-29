using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    [SerializeField]
    private float sensitivity = 10f;

    [SerializeField]
    private float maxYAngle = 80f;

    [SerializeField]
    private float rotateSpeed = 15f;

    [SerializeField]
    private Transform playerCamParent;

    [SerializeField]
    private Transform playerTransform;

    private Vector2 currentRotation;

    void Update()
    {
        if (!IsOwner) { return; }

        currentRotation.x += Input.GetAxis("Mouse X") * sensitivity;
        currentRotation.y -= Input.GetAxis("Mouse Y") * sensitivity;
        currentRotation.x = Mathf.Repeat(currentRotation.x, 360);
        currentRotation.y = Mathf.Clamp(currentRotation.y, -maxYAngle, maxYAngle);
        Quaternion toRotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0);

        playerCamParent.rotation = Quaternion.Lerp(playerCamParent.rotation, toRotation, Time.deltaTime * rotateSpeed);
        playerTransform.rotation = Quaternion.Lerp(playerTransform.rotation, toRotation, Time.deltaTime * (rotateSpeed / 2));

        if (Input.GetMouseButtonDown(0))
            Cursor.lockState = CursorLockMode.Locked;
    }
}
