using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
不要修改這個程式碼，請去PlayerAnimationCus ，這是範例code
*/
public class PlayerAnimation : MonoBehaviour
{
    private Rigidbody2D _rb;
    private Animator _anim;
    private SpriteRenderer _renderer;
    private PlayerRigidBodyMovement _movement;

    void Start()
    {
        _rb = GetComponentInParent<Rigidbody2D>();
        _movement = GetComponentInParent<PlayerRigidBodyMovement>();
        _anim = GetComponent<Animator>();
        _renderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
/*
不要修改這個程式碼，請去PlayerAnimationCus ，這是範例code
*/
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
}
/*
不要修改這個程式碼，請去PlayerAnimationCus ，這是範例code
*/