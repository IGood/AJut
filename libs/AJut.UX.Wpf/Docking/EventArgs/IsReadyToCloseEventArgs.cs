﻿namespace AJut.UX.Docking
{
    using System;

    public class IsReadyToCloseEventArgs : EventArgs
    {
        public bool IsReadyToClose { get; set; } = true;
    }
}
