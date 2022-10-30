using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerMovementServer : NetworkBehaviour
{
    [SerializeField]
    public static PlayerMovementServer Instance;

    private float timer;
    private int currentTick;
    private float minTimeBetweenTicks;
    private const float SERVER_TICK_RATE = 30f;
    private const int BUFFER_SIZE = 1024;

    private StatePayload[] stateBuffer;
    private Queue<InputPayload> inputQueue;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        minTimeBetweenTicks = 1f / SERVER_TICK_RATE;

        stateBuffer = new StatePayload[BUFFER_SIZE];
        inputQueue = new Queue<InputPayload>();
    }

    void Update()
    {
        if (IsServer && IsOwner)
        {
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
    public void OnInputServerRPC(InputPayload inputPayload)
    {
        inputQueue.Enqueue(inputPayload);
    }

    IEnumerator SendToClient(StatePayload statePayload)
    {
        yield return new WaitForSeconds(0.02f);

        PlayerMovementClient.Instance.OnServerMovementStateServerRPC(statePayload);
    }

    void HandleTick()
    {
        // Process the input queue
        int bufferIndex = -1;
        while (inputQueue.Count > 0)
        {
            InputPayload inputPayload = inputQueue.Dequeue();

            bufferIndex = inputPayload.tick % BUFFER_SIZE;
            ProcessMovement(inputPayload, bufferIndex);
        }

        if (bufferIndex != -1)
        {
            StartCoroutine(SendToClient(stateBuffer[bufferIndex]));
        }
    }

    void ProcessMovement(InputPayload input, int buffIndex)
    {
        // Should always be in sync with same function on Client
        transform.position += input.inputVector * 5f * minTimeBetweenTicks;
        stateBuffer[buffIndex].position = transform.position;
        stateBuffer[buffIndex].tick = input.tick;
    }
}
