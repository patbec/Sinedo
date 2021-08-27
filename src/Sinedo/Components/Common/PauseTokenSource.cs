using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sinedo.Components.Common
{
    /// <summary>
    ///  Steuert den Zustand eines <see cref="PauseToken"/>.
    /// </summary>
    public class PauseTokenSource
    {
        private readonly ManualResetEventSlim _manualResetEvent;
        private readonly List<Action<PauseToken>> _callbacksList;
        private readonly PauseToken _pauseToken;

        #region Properties

        /// <summary>
        /// Das zugrundeliegende <see cref="WaitHandle"/>.
        /// </summary>
        public WaitHandle WaitHandle => _manualResetEvent.WaitHandle;

        /// <summary>
        /// Das zugeordnete <see cref="PauseToken"/>.
        /// </summary>
        public PauseToken Token => _pauseToken;

        /// <summary>
        /// Ruft einen Wert ab, der angibt, ob für dieses Token eine Unterbrechung angefordert wurde.
        /// </summary>
        /// <returns></returns>
        public bool IsPauseRequested
        {
            get => !_manualResetEvent.IsSet;
        }

        #endregion

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="PauseToken"/>-Klasse.
        /// </summary>
        public PauseTokenSource()
        {
            _manualResetEvent = new ManualResetEventSlim(true);
            _callbacksList = new List<Action<PauseToken>>();

            _pauseToken = new PauseToken(
                ref _manualResetEvent,
                ref _callbacksList);
        }

        /// <summary>
        /// Übermittelt eine Pausenanforderung.
        /// </summary>
        public void Pause()
        {
            _manualResetEvent.Reset();

            _callbacksList.ForEach(
                callback => callback.Invoke(Token));
        }

        /// <summary>
        /// Übermittelt eine Fortführungsanforderung.
        /// </summary>
        public void Resume()
        {
            _manualResetEvent.Set();

            _callbacksList.ForEach(
                callback => callback.Invoke(Token));
        }
    }
}
