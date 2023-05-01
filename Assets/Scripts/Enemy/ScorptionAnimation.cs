using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class ScorptionAnimation : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Animator _anim;
    private SpriteRenderer _renderer;
    private ScorptionMovement _movement;
    // Start is called before the first frame update
    void Start()
    {
        _rb = GetComponentInParent<Rigidbody2D>();
        _movement = GetComponentInParent<ScorptionMovement>();
        _anim = GetComponent<Animator>();
        _renderer = GetComponent<SpriteRenderer>();
        SubscribeToMovementStatus();
    }
    void SubscribeToMovementStatus()
    {
        _movement.statusPublisher.StatusEvent += TriggerAnim;
    }
    void LateUpdate()
    {

        if (_movement.get2DMoveDirection().x < -.1f)
        {
            _renderer.flipX = false;
        }
        else if (_movement.get2DMoveDirection().x > .1f)
        {
            _renderer.flipX = true;
        }
        _anim.SetFloat("Y_Speed", _rb.velocity.y);
        _anim.SetFloat("X_Speed_Abs", Mathf.Abs(_rb.velocity.x));
    }
    void TriggerAnim(object sender, StatusPublisher.StatusType statusType)
    {
        switch (statusType)
        {
            case StatusPublisher.StatusType.Hurt:
                _anim.SetTrigger("Hurt");
                break;
            case StatusPublisher.StatusType.Attack:
                _anim.SetTrigger("Attack");
                break;
            case StatusPublisher.StatusType.Die:
                _anim.SetTrigger("Die");
                break;
        }

    }

}
