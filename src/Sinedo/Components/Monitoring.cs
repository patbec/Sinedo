    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    namespace Sinedo.Components
    {
        public class Monitoring {
            private const int STORED_HISTORY_IN_SECONDS = 30;

            private readonly long _sizeTotal;
            private long _sizeCurrent;
            private long _bytesRead;
            private readonly List<long> _bytesReadHistory = new();


            #region Properties

            public long? BytesPerSecond
            {
                 get; private set;
            }
            public int? Percent
            {
                 get; private set;
            }
            public long? SecondsToComplete
            {
                get; private set;
            }

            #endregion

            public Monitoring(long sizeTotal, long currentSize) {
                _sizeTotal = sizeTotal;
                _sizeCurrent = currentSize;

                // Fortschritt in Prozent.
                if (_sizeCurrent < _sizeTotal && _sizeTotal != 0) {
                    Percent = (int)(_sizeCurrent / (_sizeTotal / 100));
                }
            }

            /// <summary>
            /// Anzahl an gelesenen Bytes hinzufügen.
            /// </summary>
            public void Report(long bytesRead) {
                Interlocked.Add(ref _bytesRead, bytesRead);
            }

            public long Update()
            {
                var bytesRead = Interlocked.Exchange(ref _bytesRead, 0);

                _sizeCurrent += bytesRead;

                if (_sizeCurrent < _sizeTotal && _sizeTotal != 0)
                {
                    if (_bytesReadHistory.Count >= STORED_HISTORY_IN_SECONDS) {
                        _bytesReadHistory.Remove(0);
                    }
                    _bytesReadHistory.Add(bytesRead);

                    // Durchschnittliche Anzahl von Bytes pro Sekunde.
                    BytesPerSecond = _bytesReadHistory.Sum() / _bytesReadHistory.Count;

                    // Fortschritt in Prozent.
                    Percent = (int)(_sizeCurrent / (_sizeTotal / 100));

                    if(bytesRead != 0) {
                        // Verbleibende Sekunden bis zum Fertigstellen des Downloads.
                        SecondsToComplete = (int)((_sizeTotal - _sizeCurrent) / BytesPerSecond);
                    }
                    else {
                        SecondsToComplete = null;
                    }
                }

                return bytesRead;
            }
    }
}