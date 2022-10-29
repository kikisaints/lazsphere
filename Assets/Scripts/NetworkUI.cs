using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
{
    [SerializeField]
    private Button serverBtn;

    [SerializeField]
    private Button hostBtn;

    [SerializeField]
    private Button clientBtn;

    [SerializeField]
    private Button stopBtn;

    private void Awake()
    {
        serverBtn.onClick.AddListener(() => {
            Unity.Netcode.NetworkManager.Singleton.StartServer();
        });

        hostBtn.onClick.AddListener(() => {
            Unity.Netcode.NetworkManager.Singleton.StartHost();
        });

        clientBtn.onClick.AddListener(() => {
            Unity.Netcode.NetworkManager.Singleton.StartClient();
        });

        stopBtn.onClick.AddListener(() => {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
        });
    }
}
