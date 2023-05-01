using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using FusionUtilsEvents;

public class PlayerData: NetworkBehaviour
{
    [Networked(OnChanged = nameof(OnNickUpdate))]
    public NetworkString<_16> Nick { get; set; }
    [Networked]
    public NetworkObject Instance { get; set; }

    public FusionEvent OnPlayerDataSpawnedEvent;

    [Rpc(sources: RpcSources.InputAuthority, targets: RpcTargets.StateAuthority)]
    public void RPC_SetNick(string nick)
    {
        Nick = nick;
    }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
            RPC_SetNick(PlayerPrefs.GetString("Nick"));

        DontDestroyOnLoad(this);
        Runner.SetPlayerObject(Object.InputAuthority, Object);
        OnPlayerDataSpawnedEvent?.Raise(Object.InputAuthority, Runner);
    }

    public static void OnNickUpdate(Changed<PlayerData> changed)
    {
        changed.Behaviour.OnPlayerDataSpawnedEvent?.Raise(changed.Behaviour.Object.InputAuthority, changed.Behaviour.Runner);
    }
}
