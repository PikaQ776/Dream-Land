using System;
using System.Threading;
public class StatusKeeper
{
    private Timer timer;
    private StatusType currentStatus;
    public enum StatusType
    {
        Idle,
        Run,
        Attack,
        Climb,
        Die,
        Hurt
    }

    public StatusType CurrentStatus => currentStatus;
    public StatusKeeper()
    {
        currentStatus = StatusType.Idle;
        timer = new Timer(ResetStatus, null, Timeout.Infinite, Timeout.Infinite);
    }
    //調整為傳入狀態一段時間後再回歸Idle
    public void Trigger( StatusType triggerStatus,int timeoutMilliseconds)
    {
        timer.Change(timeoutMilliseconds, Timeout.Infinite);
        currentStatus = triggerStatus;
    }
    public void SetStatus(StatusType newStatus)
    {
        currentStatus = newStatus;
    }

    public void ResetStatus(object state)
    {
        // 將狀態設為閒置
        currentStatus = StatusType.Idle;
        // 停止計時器
        timer.Change(Timeout.Infinite, Timeout.Infinite);
    }
}
