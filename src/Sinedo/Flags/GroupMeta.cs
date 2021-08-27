using System;

namespace Sinedo.Flags
{
    /// <summary>
    /// Auflistung von erlaubten unterzuständen eines Status.
    /// </summary>
    public enum GroupMeta : ushort
    {
        CheckStatus = 0,
        Download = 1,
        Retry = 2,
        Extract = 3,
    }
}