﻿using System;

namespace MessageBus.Consumer
{
    public class ConsumerCancellation : IDisposable
    {
        private readonly Action onCancellation;

        public ConsumerCancellation(Action onCancellation)
        {
            Preconditions.CheckNotNull(onCancellation, "onCancellation");

            this.onCancellation = onCancellation;
        }

        public void Dispose()
        {
            onCancellation();
        }
    }
}