using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField]
    private float sensitivity = 10f;

    [SerializeField]
    private float maxYAngle = 80f;

    [SerializeField]
    private float rotateSpeed = 7f;

    [SerializeField]
    private float maxSpeed = 25f;

    [SerializeField]
    private Transform cameraLocation;

    [SerializeField]
    private Transform cameraLookTarget;

    private Vector2 m_OldlookRotation;

    private Rigidbody physicsBody;

    private NetworkVariable<Quaternion> networkedRotation = new NetworkVariable<Quaternion>();
    private NetworkVariable<Vector3> networkedVelocity = new NetworkVariable<Vector3>();

    private void Start()
    {
        physicsBody = this.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (IsClient) UpdateClient();
        if (IsServer) UpdateServer();
    }

    private void LateUpdate()
    {
        if (IsLocalPlayer)
        {
            Camera.main.transform.position = 
                Vector3.Lerp(Camera.main.transform.position, 
                cameraLocation.position, 
                Time.deltaTime * rotateSpeed);

            Camera.main.transform.LookAt(cameraLookTarget, Vector3.up);
        }
    }

    private void UpdateClient()
    {
        if (!IsLocalPlayer) return;

        Vector2 tempVec = m_OldlookRotation;

        tempVec.x += Input.GetAxis("Mouse X") * sensitivity;
        tempVec.y -= Input.GetAxis("Mouse Y") * sensitivity;
        tempVec.x = Mathf.Repeat(tempVec.x, 359);
        tempVec.y = Mathf.Clamp(tempVec.y, -maxYAngle, maxYAngle);

        UpdateMovementServerRpc(Quaternion.Euler(tempVec.y, tempVec.x, 0), 
            this.transform.forward * Input.GetAxis("Vertical") * maxSpeed);

        m_OldlookRotation = tempVec;

        if (Input.GetMouseButtonDown(0))
            Cursor.lockState = CursorLockMode.Locked;
    }

    private void UpdateServer()
    {
        this.transform.rotation = Quaternion.Lerp(this.transform.rotation, networkedRotation.Value, Time.deltaTime * rotateSpeed);
        physicsBody.velocity = networkedVelocity.Value;
    }

    [ServerRpc]
    private void UpdateMovementServerRpc(Quaternion rot, Vector3 vel)
    {
        networkedRotation.Value = rot;
        networkedVelocity.Value = vel;
    }
}
