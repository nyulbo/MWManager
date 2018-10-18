using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using MessageBus;
using MWManagerApp.Data;
using MWManagerApp.Models;


namespace MWManagerApp.Services
{
    public class MWLogService
    {
        public static ConcurrentDictionary<int, IBus> BusCollection = new ConcurrentDictionary<int, IBus>();
        public async Task<long> AddLogAsync(MessageReceivedInfo info, MessageProperties prop, string message, DateTime nowDt)
        {
            return await new MWLogData().InsertLogAsync(info, prop, message, nowDt);
        }
        public void AddLog(MessageReceivedInfo info, MessageProperties prop, string message)
        {
            new MWLogData().InsertLog(info, prop, message);
        }
        public IEnumerable<MWLog> GetLogs(LookupCondition lookupCondition)
        {
            if (lookupCondition.Seq > 0)
                return new MWLogData().SelectLog(lookupCondition.Seq);
            return new MWLogData().SelectLog(lookupCondition);
        }
        public async Task<IEnumerable<MWLog>> GetLogsAsync(LookupCondition lookupCondition)
        {
            if (lookupCondition.Seq > 0)
                return await new MWLogData().SelectLogAsync(lookupCondition.Seq);
            return await new MWLogData().SelectLogAsync(lookupCondition);
        }
        public IDisposable Subscribe(Func<byte[], MessageProperties, MessageReceivedInfo, Task> onMessage, int idx, MessageConfiguration messageConfig)
        {
            IBus bus;
            BusCollection.TryGetValue(idx, out bus);
            if (bus == null)
            {
                bus = ConnectionManager.CreateBus(MessageBusConfig.Connection, MessageBusConfig.Host);
                BusCollection.TryAdd(idx, bus);
            }

            return bus.Subscribe(onMessage, messageConfig);
            //bus.Subscribe((body, prop, info) => Task.Factory.StartNew(() =>
            //{
            //    new LogService().AddLogs(info, prop, Encoding.UTF8.GetString(body));
            //}), messageConfig);
        }
    }
}
