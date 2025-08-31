using UnityEngine;
using static Observer;

[System.Serializable]
public class InspectorEvent
{

    [SerializeField] private string _event = "";
    [SerializeField] private int _order = 0;

    public string Event => _event;
    public int Order => _order;

    public void Notify<T1, T2, T3>(T1 obj1, T2 obj2, T3 obj3)
    {
        if (Manager_Events.TryGetEvent(this, out IEvent ev) && ev is Event<T1, T2, T3> @event)
            @event.Notify(obj1, obj2, obj3);
        else
            Notify(obj1, obj2);
    }

    public void Notify<T1, T2>(T1 obj1, T2 obj2)
    {
        if (Manager_Events.TryGetEvent(this, out IEvent ev) && ev is Event<T1, T2> @event)
            @event.Notify(obj1, obj2);
        else
            Notify(obj1);
    }

    public void Notify<T1>(T1 obj)
    {
        if (Manager_Events.TryGetEvent(this, out IEvent ev) && ev is Event<T1> @event)
            @event.Notify(obj);
        else
            Notify();
    }

    public void Notify()
    {
        if (Manager_Events.TryGetEvent(this, out IEvent ev) && ev is Observer.Event @event)
            @event.Notify();
    }

    public static bool operator == (InspectorEvent _eventInspector, IEvent _event)
    {
        if (_event == null)
            return false;
        
        return _eventInspector.Event == _event.GetEventName();
    }

    public static bool operator != (InspectorEvent _eventInspector, IEvent _event) => !(_eventInspector == _event);

    public static bool operator == (IEvent _event, InspectorEvent _eventInspector) => _eventInspector == _event;

    public static bool operator != (IEvent _event, InspectorEvent _eventInspector) => !(_eventInspector == _event);

    public override bool Equals(object obj)
    {
        if (obj == null)
            return false;

        if (obj is not InspectorEvent || obj is not IEvent)
            return false;

        if (obj is InspectorEvent @objInspector)
            return this == @objInspector;

        if (obj is IEvent @objEvent)
            return this == @objEvent;

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString() => $"<color=cyan>Event: <b>{Event}</b></color>";
    
}
