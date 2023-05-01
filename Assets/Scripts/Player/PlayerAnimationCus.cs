using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
//This is the variant from playerAnimation use to modify new feature but not affect old one
public class PlayerAnimationCus : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Animator _anim;
    private SpriteRenderer _renderer;
    private PlayerRigidBodyMovement _movement;
    private int _attackType = 0;
    private int attackType
    {
        //You can only call at one place, or data would be wrong;
        get { _attackType %= 3; return ++_attackType; }
    }
    void Start()
    {
        _rb = GetComponentInParent<Rigidbody2D>();
        _movement = GetComponentInParent<PlayerRigidBodyMovement>();
        _anim = GetComponent<Animator>();
        _renderer = GetComponent<SpriteRenderer>();
        SubscribeToMovementStatus();
    }
    void SubscribeToMovementStatus()
    {
        _movement.statusPublisher.StatusEvent += PlayTriggerAnim;
    }
    void LateUpdate()
    {
        if (_rb.velocity.x < -.1f)
        {
            _renderer.flipX = true;
        }
        else if (_rb.velocity.x > .1f)
        {
            _renderer.flipX = false;
        }
        _anim.SetBool("Grounded", _movement.GetGrounded());
        _anim.SetFloat("Y_Speed", _rb.velocity.y);
        _anim.SetFloat("X_Speed_Abs", Mathf.Abs(_rb.velocity.x));
    }

    void PlayTriggerAnim(object sender, StatusPublisher.StatusType statusType)
    {
        switch (statusType)
        {
            case StatusPublisher.StatusType.Hurt:
                _anim.SetTrigger("Hurt");
                break;
            case StatusPublisher.StatusType.Attack:
                _anim.SetTrigger($"Attack{attackType}");
                break;
        }

    }
}
