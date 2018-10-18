﻿using System;

namespace MessageBus
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public class DeliveryModeAttribute : Attribute
    {
        public DeliveryModeAttribute(bool isPersistent)
        {
            IsPersistent = isPersistent;
        }

        public bool IsPersistent { get; private set; }
    }
}