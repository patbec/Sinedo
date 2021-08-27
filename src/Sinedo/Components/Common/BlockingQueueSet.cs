using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sinedo.Models;

namespace Sinedo.Components.Common
{
    /// <summary>
    /// Stellt eine erweiterte threadsichere FIFO-Auflistung (First-In-First-Out) dar.
    /// </summary>
    public class BlockingQueueSet<T>
    {
        private readonly List<T> _jobs = new();
        private readonly ManualResetEvent _jobEvent = new(false);


        public object SyncRoot
        {
            get;
        } = new object();

        public int Count {
            get 
            {
                lock (SyncRoot)
                {
                    return _jobs.Count;
                }
            }
        }

        /// <summary>
        /// Fügt am Ende der <see cref="BlockingQueueSet{T}"/> ein Objekt hinzu.
        /// </summary>
        /// <param name="item">Das Objekt, das am Ende der <see cref="BlockingQueueSet{T}"/> hinzugefügt werden soll.</param>
        public void Add(T item)
        {
            lock (SyncRoot)
            {
                // Element hinzufügen.
                _jobs.Add(item);

                // Wartenden Threads signalisieren,
                // dass eine Änderung stattgefunden hat.
                _jobEvent.Set();
            }
        }

        /// <summary>
        /// Versucht, das angegebene Objekt aus der <see cref="BlockingQueueSet{T}"/> zu entfernen.
        /// </summary>
        /// <param name="item">Das Objekt, das aus der <see cref="BlockingQueueSet{T}"/> entfernt werden soll.
        public bool Remove(T item)
        {
            lock (SyncRoot)
            {
                return _jobs.Remove(item);
            }
        }

        /// <summary>
        /// Gibt die Anzahl an entfernten Objekten in der <see cref="BlockingQueueSet{T}"/> zurück.
        /// </summary>
        public int Remove(Func<T, bool> func)
        {
            lock (SyncRoot)
            {
                int itemsRemoved = 0;

                for (int i = _jobs.Count - 1; i != -1; i--)
                {
                    if (func(_jobs[i]))
                    {
                        _jobs.RemoveAt(i);
                        itemsRemoved++;
                    }
                }

                return itemsRemoved;
            }
        }

        /// <summary>
        /// Gibt einen Wert zurück ob das angegebene Objekt in der <see cref="BlockingQueueSet{T}"/> exestiert.
        /// </summary>
        public bool Contains(T item)
        {
            lock (SyncRoot)
            {
                return _jobs.Contains(item);
            }
        }

        /// <summary>
        /// Gibt einen Wert zurück ob das angegebene Objekt in der <see cref="BlockingQueueSet{T}"/> exestiert.
        /// </summary>
        public bool Contains(Func<T, bool> func)
        {
            lock (SyncRoot)
            {
                foreach (T item in _jobs)
                {
                    if (func(item))
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gibt das Erste vorkommen in der <see cref="BlockingQueueSet{T}"/> zurück.
        /// </summary>
        public T First(Func<T, bool> func)
        {
            lock (SyncRoot)
            {
                foreach (T item in _jobs)
                {
                    if (func(item))
                        return item;
                }

                return default;
            }
        }

        public void WaitOnChange() {
            _jobEvent.WaitOne(Timeout.Infinite);
        }

        /// <summary>
        /// Gibt ein Objekt am Anfang der gleichzeitigen Warteschlange zurück
        /// und entfernt es aus der <see cref="BlockingQueueSet{T}"/>.
        /// </summary>
        public bool TryTake(out T item)
        {
            item = default;

            lock (SyncRoot)
            {
                if (_jobs.Count != 0)
                {
                    // Erstes Objekt abrufen und entfernen.
                    item = _jobs[0];
                    _jobs.RemoveAt(0);

                    return true;
                }

                // Signalisieren das keine Objekte vorhanden sind.
                _jobEvent.Reset();
            }

            return false;
        }

        /// <summary>
        /// Gibt ein Objekt am Anfang der gleichzeitigen Warteschlange zurück
        /// und entfernt es aus der <see cref="BlockingQueueSet{T}"/>.
        /// Ist kein Objekt vorhanden wird der Vorgang solange blockiert bis ein Objekt hinzugefügt wurde.
        /// </summary>
        public T Take(int millisecondsTimeout = Timeout.Infinite)
        {
            // Wiederholen bis ein Objekt gefunden wurde.
            while (true)
            {
                // Warten bis ein Objekt hinzugefügt wurde.
                if ( ! _jobEvent.WaitOne(millisecondsTimeout))
                    return default;

                lock (SyncRoot)
                {
                    if (_jobs.Count != 0)
                    {
                        // Erstes Objekt abrufen und entfernen.
                        T itemToTake = _jobs[0];
                        _jobs.RemoveAt(0);

                        return itemToTake;
                    }

                    // Signalisieren das keine Objekte vorhanden sind.
                    _jobEvent.Reset();
                }
            }
        }
    }
}