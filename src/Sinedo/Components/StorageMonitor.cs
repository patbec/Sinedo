using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Security;
using Sinedo.Exceptions;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Sinedo.Singleton;
using System.ComponentModel;

namespace Sinedo.Components
{
    /// <summary>
    /// Überwacht ob ein Pfad erreichbar ist.
    /// Wird benötigt um ein ausstecken eines USB-Sticks, der als Speicherort festgelegt ist, zu erkennen.
    /// Die Lösung ist nicht optimal, da mit einem Timer gearbeitet wird, aber es funktioniert Plattformübergreifend.
    /// </summary>
    public class StorageMonitor
    {
        private readonly Timer timer;
        private readonly string path;
        private bool lastDeviceState;
        private uint tickCount = 0;

        public delegate void StorageEvent();

        public event StorageEvent StorageOnline;
        public event StorageEvent StorageOffline;
        public event StorageEvent StorageUpdate;

        public StorageMonitor(string pathToMonitor) {
            path = pathToMonitor;
            timer = new Timer(Update, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start() {
            if(Directory.Exists(path)) {
                lastDeviceState = true;
                StorageOnline?.Invoke();
            }
            else {
                lastDeviceState = false;
                StorageOffline?.Invoke();
            }

            timer.Change(3000, 3000);
        }

        public void Stop() {
             timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void Update(object state) {
            if (Directory.Exists(path)) {
                if (lastDeviceState == false) {
                    lastDeviceState = true;
                    StorageOnline?.Invoke();
                }
            } else {
                if (lastDeviceState == true) {
                    lastDeviceState = false;
                    StorageOffline?.Invoke();
                }
            }

            // Nach 1 Minute eine aktualisieren senden.
            if(tickCount % 20 == 0 && lastDeviceState == true)
            {
                StorageUpdate?.Invoke();
            }

            tickCount++;
            if (tickCount == uint.MaxValue) {
                tickCount = 0;
            }
        }
    }
}