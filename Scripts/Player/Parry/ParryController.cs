using System.Collections.Generic;

using UnityEngine;

public sealed class ParryController
{
    private Queue<ParryInfo> queue = new Queue<ParryInfo>(30);

    public void StackParry()
    {
        queue.Enqueue(new ParryInfo(Time.time));
    }

    public bool CanParry()
    {
        return 0 < queue.Count;
    }

    public void Parry()
    {
        queue.Dequeue();
    }

    public void RemoveTooEarlyParries(float parryDuration = 0.5f)
    {
        while (0 < queue.Count)
        {
            ParryInfo info = queue.Peek();
            if (info.time + parryDuration < Time.time)
            {
                queue.Dequeue();
            }
            else
            {
                break;
            }
        }
    }
}