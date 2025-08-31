using System;
using System.Collections.Generic;
using System.Reflection;

public static class Observer
{

    public interface IEvent
    {
        IEvent Add(Action callback, int order = 0);

        IEvent Remove(Action callback, int order = 0);

        void SetEventName(string name);

        string GetEventName();
    }

    public class Event : IEvent
    {
        private readonly SortedList<int, List<Action>> _callbackDic = new();

        private string _eventName;

        public void SetEventName(string name)
        {
            if (_eventName != null)
                return;

            _eventName = name;
        }

        public string GetEventName() => _eventName;

        public IEvent Add(Action callback, int order = 0)
        {
            if (_callbackDic.ContainsKey(order))
                _callbackDic[order].Add(callback);
            else
                _callbackDic.Add(order, new List<Action>() { callback });

            return this;
        }

        public IEvent Remove(Action callback, int order = 0)
        {
            if (_callbackDic.ContainsKey(order))
                _callbackDic[order].Remove(callback);

            return this;
        }

        public Event Notify()
        {
            foreach (var keyPair in _callbackDic)
                foreach (var callback in keyPair.Value)
                    callback?.Invoke();

            return this;
        }

        public static Event operator +(Event ev, Action callback) => (Event) ev.Add(callback);

        public static Event operator -(Event ev, Action callback) => (Event) ev.Remove(callback);
    }

    public class Event<T> : IEvent
    {
        private readonly SortedList<int, List<Action<T>>> _callbackDic = new();

        private string _eventName;

        public void SetEventName(string name)
        {
            if (_eventName != null)
                return;

            _eventName = name;
        }

        public string GetEventName() => _eventName;

        public IEvent Add(Action callback, int order = 0) => Add(obj => callback?.Invoke(), order);

        public IEvent Remove(Action callback, int order = 0) => Remove(obj => {}, order);

        public Event<T> Add(Action<T> callback, int order = 0)
        {
            if (_callbackDic.ContainsKey(order))
                _callbackDic[order].Add(callback);
            else
                _callbackDic.Add(order, new List<Action<T>>() { callback });

            return this;
        }

        public Event<T> Remove(Action<T> callback, int order = 0)
        {
            if (_callbackDic.ContainsKey(order))
                _callbackDic[order].Remove(callback);

            return this;
        }

        public Event<T> Notify(T obj)
        {
            foreach (var keyPair in _callbackDic)
                foreach (var callback in keyPair.Value)
                    callback?.Invoke(obj);

            return this;
        }

        public static IEvent operator +(Event<T> ev, Action callback) => ev.Add(callback);

        public static IEvent operator -(Event<T> ev, Action callback) => ev.Remove(callback);

        public static Event<T> operator +(Event<T> ev, Action<T> callback) => ev.Add(callback);

        public static Event<T> operator -(Event<T> ev, Action<T> callback) => ev.Remove(callback);
    }

    public class Event<T1, T2> : IEvent
    {
        private readonly SortedList<int, List<Action<T1, T2>>> _callbackDic = new();

        private string _eventName;

        public void SetEventName(string name)
        {
            if (_eventName != null)
                return;

            _eventName = name;
        }

        public string GetEventName() => _eventName;

        public IEvent Add(Action callback, int order = 0) => Add((obj1, obj2) => callback?.Invoke(), order);

        public IEvent Remove(Action callback, int order = 0) => Remove((obj1, obj2) => {}, order);

        public Event<T1, T2> Add(Action<T1, T2> callback, int order = 0)
        {
            if (_callbackDic.ContainsKey(order))
                _callbackDic[order].Add(callback);
            else
                _callbackDic.Add(order, new List<Action<T1, T2>>() { callback });

            return this;
        }

        public Event<T1, T2> Remove(Action<T1, T2> callback, int order = 0)
        {
            if (_callbackDic.ContainsKey(order))
                _callbackDic[order].Remove(callback);

            return this;
        }

        public Event<T1, T2> Notify(T1 obj1, T2 obj2)
        {
            foreach (var keyPair in _callbackDic)
                foreach (var callback in keyPair.Value)
                    callback?.Invoke(obj1, obj2);

            return this;
        }

        public static IEvent operator +(Event<T1, T2> ev, Action callback) => ev.Add(callback);

        public static IEvent operator -(Event<T1, T2> ev, Action callback) => ev.Remove(callback);

        public static Event<T1, T2> operator +(Event<T1, T2> ev, Action<T1, T2> callback) => ev.Add(callback);

        public static Event<T1, T2> operator -(Event<T1, T2> ev, Action<T1, T2> callback) => ev.Remove(callback);
    }

    public class Event<T1, T2, T3> : IEvent
    {
        private readonly SortedList<int, List<Action<T1, T2, T3>>> _callbackDic = new();

        private string _eventName;

        public void SetEventName(string name)
        {
            if (_eventName != null)
                return;

            _eventName = name;
        }

        public string GetEventName() => _eventName;

        public IEvent Add(Action callback, int order = 0) => Add((obj1, obj2, obj3) => callback?.Invoke(), order);

        public IEvent Remove(Action callback, int order = 0) => Remove((obj1, obj2, obj3) => {}, order);

        public Event<T1, T2, T3> Add(Action<T1, T2, T3> callback, int order = 0)
        {
            if (_callbackDic.ContainsKey(order))
                _callbackDic[order].Add(callback);
            else
                _callbackDic.Add(order, new List<Action<T1, T2, T3>>() { callback });

            return this;
        }

        public Event<T1, T2, T3> Remove(Action<T1, T2, T3> callback, int order = 0)
        {
            if (_callbackDic.ContainsKey(order))
                _callbackDic[order].Remove(callback);

            return this;
        }

        public Event<T1, T2, T3> Notify(T1 obj1, T2 obj2, T3 obj3)
        {
            foreach (var keyPair in _callbackDic)
                foreach (var callback in keyPair.Value)
                    callback?.Invoke(obj1, obj2, obj3);

            return this;
        }

        public static IEvent operator +(Event<T1, T2, T3> ev, Action callback) => ev.Add(callback);

        public static IEvent operator -(Event<T1, T2, T3> ev, Action callback) => ev.Remove(callback);

        public static Event<T1, T2, T3> operator +(Event<T1, T2, T3> ev, Action<T1, T2, T3> callback) => ev.Add(callback);

        public static Event<T1, T2, T3> operator -(Event<T1, T2, T3> ev, Action<T1, T2, T3> callback) => ev.Remove(callback);
    }

}
