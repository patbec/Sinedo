using System;

namespace Sinedo.Flags
{
    [Flags]
    public enum GroupAction
    {
        None    = 1 << 0,
        Start   = 1 << 1,
        Stop    = 1 << 2,
        Update  = 1 << 3,
        Delete  = 1 << 4,
        Recycle = 1 << 5,
        Restore = 1 << 6,
    }
}
