using System;
using System.Collections.Generic;
using MessageBus;

namespace MWManagerApp.Models
{
    public class LookupCondition
    {
        public int Seq
        {
            get; set;
        }
        public DateTime BeginDate
        {
            get; set;
        }
        public DateTime EndDate
        {
            get; set;
        }
        public string RoutingKey
        {
            get; set;
        }
        public int Limit
        {
            get; set;
        }
    }
    public class MWLog
    {
        public ReceivedInfo Info
        {
            get; set;
        }
        public ReceivedProps Prop
        {
            get; set;
        }
    }
    public class ReceivedInfo : MessageReceivedInfo
    {
        public long Seq
        {
            get; set;
        }
        public long PropertySeq
        {
            get; set;
        }
        public string Payload
        {
            get; set;
        }
        public string DeliverTagString
        {
            get; set;
        }
        public DateTime InsDate
        {
            get; set;
        }
        public DateTime UpdDate
        {
            get; set;
        }
    }
    public class ReceivedProps : MessageProperties
    {
        public long Seq
        {
            get; set;
        }
        public string HeadersJSON
        {
            get; set;
        }
        public IDictionary<string, string> HeadersString
        {
            get;
            set;
        }
        public DateTime InsDate
        {
            get; set;
        }
        public DateTime UpdDate
        {
            get; set;
        }
    }
    public class MWQueue
    {
        public string Name
        {
            get; set;
        }
        public bool IsSelected
        {
            get; set;
        }
    }
    public class MWSubscribeStatus
    {
        public string Text
        {
            get; set;
        }
        public bool IsRunning
        {
            get; set;
        }
    }
    public class MWConfig
    {
        public string HostName
        {
            get; set;
        }
        public string ID
        {
            get; set;
        }
        public string PW
        {
            get; set;
        }
    }
}
