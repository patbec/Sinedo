using System;
using System.Collections.Generic;
using System.Text;
using Sharehoster.Interfaces;

namespace Sharehoster.Exceptions
{
    /// <summary>
    /// Der Zugriff auf das Konto ist abgelaufen.
    /// </summary>
    public class AccountExpiredException : Exception
    {
        private const string MESSAGE = "Access to the account has expired.";

        /// <summary>
        /// Hoster wo das Problem aufgetreten ist.
        /// </summary>
        public ISharehoster Sharehoster { get; init; }

        public AccountExpiredException(ISharehoster sharehoster) : base(MESSAGE) {
            Sharehoster = sharehoster;
        }
    }
}
