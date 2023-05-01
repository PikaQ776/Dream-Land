using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using System.Linq;
public class ScorptionSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkPrefabRef ScorptionPrefab;
    public static Vector3[] ScorptionSpawnPosArr;

    public void RespawnScorptions(NetworkRunner runner)
    {
        if (!runner.IsClient)
        {
            ScorptionSpawnPosArr = GameObject.FindGameObjectsWithTag("EnemyRespawn").Select(gameObj => gameObj.transform.position).ToArray();
            foreach (var scorptionSpawnPos in ScorptionSpawnPosArr)
            {
                SpawnScorption(runner, scorptionSpawnPos, runner.LocalPlayer);
            }
        }
    }
    private void SpawnScorption(NetworkRunner runner, Vector3 scorptionSpawnPos, PlayerRef player)
    {
        if (runner.IsServer)
        {
            NetworkObject playerObj = runner.Spawn(ScorptionPrefab, (Vector2)scorptionSpawnPos, Quaternion.identity, player, InitializeObjBeforeSpawn);
        }
    }
    private void InitializeObjBeforeSpawn(NetworkRunner runner, NetworkObject obj)
    {

    }
    public void OnConnectedToServer(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        throw new NotImplementedException();
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        throw new NotImplementedException();
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        throw new NotImplementedException();
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        throw new NotImplementedException();
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        throw new NotImplementedException();
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        throw new NotImplementedException();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        throw new NotImplementedException();
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        throw new NotImplementedException();
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        throw new NotImplementedException();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        throw new NotImplementedException();
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        throw new NotImplementedException();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
