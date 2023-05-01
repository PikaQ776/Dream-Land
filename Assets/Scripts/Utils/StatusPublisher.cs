
using System;
using UnityEngine;
public class StatusPublisher
{
    // 定义事件
    public event EventHandler<StatusType> StatusEvent;
    public enum StatusType
    {
        Idle,
        Run,
        Attack,
        Climb,
        Die,
        Hurt
    }
    public void Broadcast(StatusType statusType)
    {
        var statusEvent = StatusEvent;
        if (statusEvent != null)
        {
            statusEvent(this, statusType);
        }
    }
}