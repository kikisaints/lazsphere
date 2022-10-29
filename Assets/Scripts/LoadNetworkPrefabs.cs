using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadNetworkPrefabs : MonoBehaviour
{
    public string networkedPrefabPath;

    private Unity.Netcode.NetworkManager _networkManager; 

    // Start is called before the first frame update
    void Awake()
    {
        _networkManager = this.GetComponent<Unity.Netcode.NetworkManager>();
        GameObject[] networkedPrefabs = Resources.LoadAll<GameObject>(networkedPrefabPath);

        System.Array.ForEach(networkedPrefabs, netPrefab => _networkManager.AddNetworkPrefab(netPrefab));
    }
}
