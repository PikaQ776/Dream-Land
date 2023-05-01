using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class ScorptionBehavior : NetworkBehaviour
{
    private NetworkRigidbody2D _rb;

    [Networked]
    public NetworkBool InputsAllowed { get; set; }
    private void Awake()
    {
        _rb = GetBehaviour<NetworkRigidbody2D>();
    }
    public void RespawnsScorption()
    {
        StartCoroutine(Respawn());
    }
    private IEnumerator Respawn()
    {

        _rb.TeleportToPosition(ScorptionSpawner.ScorptionSpawnPosArr[Random.Range(0, 2)]);
        yield return new WaitForSeconds(.1f);
        SetInputsAllowed(true);
    }
    public void SetInputsAllowed(bool value)
    {
        InputsAllowed = value;
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
