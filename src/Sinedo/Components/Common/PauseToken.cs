using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sinedo.Components.Common
{
    /// <summary>
    /// Gibt eine Benachrichtigung darüber weiter, dass Vorgänge angehalten oder fortgesetzt werden sollen.
    /// </summary>
    public readonly struct PauseToken
    {
        private readonly ManualResetEventSlim _manualResetEvent;
        private readonly List<Action<PauseToken>> _callbacksList;

        /// <summary>
        /// Gibt ein leeres <see cref="PauseToken"/> zurück.
        /// </summary>
        public static PauseToken None { get; }

        /// <summary>
        /// Ruft einen Wert ab, der angibt, ob für dieses Token eine Unterbrechung angefordert wurde.
        /// </summary>
        /// <returns></returns>
        public bool IsPauseRequested
        {
            get => !_manualResetEvent.IsSet;
        }

        /// <summary>
        /// Das zugrundeliegende <see cref="WaitHandle"/>.
        /// </summary>
        public WaitHandle WaitHandle => _manualResetEvent.WaitHandle;

        /// <summary>
        /// Initialisiert das <see cref="PauseToken"/>.
        /// </summary>
        /// <param name="waitHandle">Das <see cref="ManualResetEventSlim"/> das den Zustand steuert. Verwenden Sie ein <see cref="PauseTokenSource"/> um diese Klasse zu steuern.</param>
        public PauseToken(ref ManualResetEventSlim waitHandle, ref List<Action<PauseToken>> actionsHandle)
        {
            _manualResetEvent = waitHandle;
            _callbacksList = actionsHandle;
        }

        /// <summary>
        /// Registriert einen Delegierten, der aufgerufen wird,
        /// wenn dieses <see cref="PauseToken"/> einen neuen Zustand erhalten hat.
        /// </summary>
        /// <param name="action">Der Delegierte, der ausgeführt werden soll,
        /// wenn das <see cref="PauseToken"/> einen neuen Zustand erhalten hat.</param>
        /// 
        /// <exception cref="ArgumentNullException"/>
        /// 
        public void Register(Action<PauseToken> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            _callbacksList.Add(callback);
        }

        /// <summary>
        /// Löst eine <see cref="OperationPausedException"/> Ausnahme aus, wenn eine Pausierungsanforderung empfangen wurde.
        /// </summary>
        /// 
        /// <exception cref="OperationPausedException"/>
        /// 
        public void ThrowIfPauseRequested()
        {
            if (IsPauseRequested)
                throw new OperationPausedException();
        }

        /// <summary>
        /// Blockiert den aktuellen Thread, bis das <see cref="PauseTokenSource"/>
        /// eine Fortführungsanforderung empfängt.
        /// </summary>
        /// <param name="millisecondsTimeout">Die Anzahl von Millisekunden, die gewartet wird, oder <see cref="Timeout.Infinite"/> (-1) für Warten ohne Timeout.</param>
        /// <param name="cancellationToken">Token um den Wartevorgang abzubrechen.</param>
        /// 
        /// <exception cref="OperationCanceledException"/>
        /// 
        public void WaitIfPauseRequested(int millisecondsTimeout = -1, CancellationToken cancellationToken = default)
        {
            _manualResetEvent.Wait(millisecondsTimeout, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
