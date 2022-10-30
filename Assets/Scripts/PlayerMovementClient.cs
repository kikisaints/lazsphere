using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public struct InputPayload : INetworkSerializeByMemcpy
{
    public int tick;
    public Vector3 inputVector;
}

public struct StatePayload : INetworkSerializeByMemcpy
{
    public int tick;
    public Vector3 position;
}

public class PlayerMovementClient : NetworkBehaviour
{
    public static PlayerMovementClient Instance;

    // Shared
    private float timer;
    private int currentTick;
    private float minTimeBetweenTicks;
    private const float SERVER_TICK_RATE = 30f;
    private const int BUFFER_SIZE = 1024;

    // Client specific
    private NetworkVariable<StatePayload>[] stateBuffer;
    private NetworkVariable<InputPayload>[] inputBuffer;

    private StatePayload latestServerState;
    private StatePayload lastProcessedState;
    private float horizontalInput;
    private float verticalInput;

    void Awake()
    {
        Instance = this;

        inputBuffer = new NetworkVariable<InputPayload>[BUFFER_SIZE];
        stateBuffer = new NetworkVariable<StatePayload>[BUFFER_SIZE];

        for (int i = 0; i < inputBuffer.Length; i++)
            inputBuffer[i] = new NetworkVariable<InputPayload>(new InputPayload() { tick = 0, inputVector = new Vector3() });

        for (int i = 0; i < stateBuffer.Length; i++)
            stateBuffer[i] = new NetworkVariable<StatePayload>(new StatePayload() { tick = 0, position = new Vector3() });
    }

    void Start()
    {
        minTimeBetweenTicks = 1f / SERVER_TICK_RATE;
    }

    void Update()
    {
        if (IsLocalPlayer && IsOwner)
        {
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput = Input.GetAxis("Vertical");

            timer += Time.deltaTime;

            while (timer >= minTimeBetweenTicks)
            {
                timer -= minTimeBetweenTicks;
                HandleTick();
                currentTick++;
            }
        }
    }

    [ServerRpc]
    public void OnServerMovementStateServerRPC(StatePayload serverState)
    {
        latestServerState = serverState;
    }

    IEnumerator SendToServer(InputPayload inputPayload)
    {
        yield return new WaitForSeconds(0.02f);

        PlayerMovementServer.Instance.OnInputServerRPC(inputPayload);
    }

    void HandleTick()
    {
        if (!latestServerState.Equals(default(StatePayload)) &&
            (lastProcessedState.Equals(default(StatePayload)) ||
            !latestServerState.Equals(lastProcessedState)))
        {
            HandleServerReconciliation();
        }

        int bufferIndex = currentTick % BUFFER_SIZE;

        if (inputBuffer[bufferIndex] == null) return;

        // Add payload to inputBuffer
        InputPayload inputPayload = new InputPayload();
        inputPayload.tick = currentTick;
        inputPayload.inputVector = new Vector3(horizontalInput, 0, verticalInput);
        inputBuffer[bufferIndex].Value = inputPayload;

        // Add payload to stateBuffer
        stateBuffer[bufferIndex].Value = ProcessMovement(inputPayload);

        // Send input to server
        StartCoroutine(SendToServer(inputPayload));
    }

    StatePayload ProcessMovement(InputPayload input)
    {
        // Should always be in sync with same function on Server
        transform.position += input.inputVector * 5f * minTimeBetweenTicks;

        return new StatePayload()
        {
            tick = input.tick,
            position = transform.position,
        };
    }

    void HandleServerReconciliation()
    {
        lastProcessedState = latestServerState;

        int serverStateBufferIndex = latestServerState.tick % BUFFER_SIZE;
        float positionError = Vector3.Distance(latestServerState.position, stateBuffer[serverStateBufferIndex].Value.position);

        if (positionError > 0.001f)
        {
            Debug.Log("We have to reconcile bro");
            // Rewind & Replay
            transform.position = latestServerState.position;

            // Update buffer at index of latest server state
            stateBuffer[serverStateBufferIndex].Value = latestServerState;

            // Now re-simulate the rest of the ticks up to the current tick on the client
            int tickToProcess = latestServerState.tick + 1;

            while (tickToProcess < currentTick)
            {
                int bufferIndex = tickToProcess % BUFFER_SIZE;

                // Process new movement with reconciled state
                StatePayload statePayload = ProcessMovement(inputBuffer[bufferIndex].Value);

                // Update buffer with recalculated state
                stateBuffer[bufferIndex].Value = statePayload;

                tickToProcess++;
            }
        }
    }
}
