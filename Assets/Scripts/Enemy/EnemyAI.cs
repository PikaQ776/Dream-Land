using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
public abstract class EnemyAI : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public abstract void TakeDamage(int damage,Vector2 force);
}
