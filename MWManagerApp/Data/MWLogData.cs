using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

using MySql.Data.MySqlClient;
using MessageBus;
using Newtonsoft.Json;
using MWManagerApp.Helpers;
using MWManagerApp.Models;

namespace MWManagerApp.Data
{
    public class MWLogData
    {
        private string connString = ConfigurationManager.AppSettings["DBConn"];

        public async Task<long> InsertLogAsync(MessageReceivedInfo info, MessageProperties prop, string body, DateTime nowDt)
        {
            
            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync();

                using (var cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = EmbeddedResource.GetString("InsertLog.sql");
                    // MessageReceivedInfo
                    cmd.Parameters.AddWithValue("exchange", info.Exchange);
                    cmd.Parameters.AddWithValue("routing_key", info.RoutingKey);
                    cmd.Parameters.AddWithValue("queue", info.Queue);
                    cmd.Parameters.AddWithValue("deliver_tag", info.DeliverTag);
                    cmd.Parameters.AddWithValue("consumer_tag", info.ConsumerTag);
                    cmd.Parameters.AddWithValue("redelivered", info.Redelivered);
                    cmd.Parameters.AddWithValue("payload", body);
                    cmd.Parameters.AddWithValue("ins_date", DateTime.Now);
                    //MessageProperties
                    cmd.Parameters.AddWithValue("content_type", prop.ContentType);
                    cmd.Parameters.AddWithValue("content_encoding", prop.ContentEncoding);
                    cmd.Parameters.AddWithValue("delivery_mode", prop.DeliveryMode);
                    cmd.Parameters.AddWithValue("priority", prop.Priority);
                    cmd.Parameters.AddWithValue("correlation_id", prop.CorrelationId);
                    cmd.Parameters.AddWithValue("reply_to", prop.ReplyTo);
                    cmd.Parameters.AddWithValue("expiration", prop.Expiration);
                    cmd.Parameters.AddWithValue("app_id", prop.AppId);
                    cmd.Parameters.AddWithValue("message_id", prop.MessageId);
                    cmd.Parameters.AddWithValue("timestamp", prop.Timestamp);
                    cmd.Parameters.AddWithValue("type", prop.Type);
                    cmd.Parameters.AddWithValue("user_id", prop.UserId);
                    cmd.Parameters.AddWithValue("cluster_id", prop.ClusterId);
                    cmd.Parameters.AddWithValue("headers", JsonConvert.SerializeObject(prop.Headers));
                    cmd.Parameters.AddWithValue("ins_date", nowDt);
                    cmd.Parameters.AddWithValue("upd_date", nowDt);
                    cmd.Prepare();
                    var reader = await cmd.ExecuteScalarAsync();
                    return long.Parse(reader.ToString());
                }
            }
        }
        public void InsertLog(MessageReceivedInfo info, MessageProperties prop, string body)
        {
            DateTime sysNow = DateTime.Now;

            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();

                using (var cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = EmbeddedResource.GetString("InsertLog.sql");
                    // MessageReceivedInfo
                    cmd.Parameters.AddWithValue("exchange", info.Exchange);
                    cmd.Parameters.AddWithValue("routing_key", info.RoutingKey);
                    cmd.Parameters.AddWithValue("queue", info.Queue);
                    cmd.Parameters.AddWithValue("deliver_tag", info.DeliverTag);
                    cmd.Parameters.AddWithValue("consumer_tag", info.ConsumerTag);
                    cmd.Parameters.AddWithValue("redelivered", info.Redelivered);
                    cmd.Parameters.AddWithValue("payload", body);
                    cmd.Parameters.AddWithValue("ins_date", DateTime.Now);
                    //MessageProperties
                    cmd.Parameters.AddWithValue("content_type", prop.ContentType);
                    cmd.Parameters.AddWithValue("content_encoding", prop.ContentEncoding);
                    cmd.Parameters.AddWithValue("delivery_mode", prop.DeliveryMode);
                    cmd.Parameters.AddWithValue("priority", prop.Priority);
                    cmd.Parameters.AddWithValue("correlation_id", prop.CorrelationId);
                    cmd.Parameters.AddWithValue("reply_to", prop.ReplyTo);
                    cmd.Parameters.AddWithValue("expiration", prop.Expiration);
                    cmd.Parameters.AddWithValue("app_id", prop.AppId);
                    cmd.Parameters.AddWithValue("message_id", prop.MessageId);
                    cmd.Parameters.AddWithValue("timestamp", prop.Timestamp);
                    cmd.Parameters.AddWithValue("type", prop.Type);
                    cmd.Parameters.AddWithValue("user_id", prop.UserId);
                    cmd.Parameters.AddWithValue("cluster_id", prop.ClusterId);
                    cmd.Parameters.AddWithValue("headers", JsonConvert.SerializeObject(prop.Headers));
                    cmd.Parameters.AddWithValue("ins_date", sysNow);
                    cmd.Parameters.AddWithValue("upd_date", sysNow);
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public async Task<IEnumerable<MWLog>> SelectLogAsync(LookupCondition lookupCondition)
        {
            List<MWLog> retList = new List<MWLog>();

            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync();
                using (var cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    //string sql = "select info.*, prop.* from log_info info join log_property prop on info.property_seq = prop.seq order by info.seq desc limit 10;";
                    //cmd.CommandText = sql;
                    cmd.CommandText = EmbeddedResource.GetString("SelectLogInfoWithLogProperty.sql");
                    cmd.Parameters.AddWithValue("begin_date", lookupCondition.BeginDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("end_date", lookupCondition.EndDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("routing_key", "%" + lookupCondition.RoutingKey + "%");
                    cmd.Parameters.AddWithValue("limit", lookupCondition.Limit);
                    cmd.Prepare();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            retList.Add(ModelConverter.ToMWLog(reader));
                        }
                    }
                }
            }
            return retList;
        }
        public async Task<IEnumerable<MWLog>> SelectLogAsync(int seq)
        {
            List<MWLog> retList = new List<MWLog>();

            using (var conn = new MySqlConnection(connString))
            {
                await conn.OpenAsync();
                using (var cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = EmbeddedResource.GetString("SelectLogInfoWithLogPropertyBySeq.sql");
                    cmd.Parameters.AddWithValue("seq", seq);
                    cmd.Prepare();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            retList.Add(ModelConverter.ToMWLog(reader));
                        }
                    }
                }
            }
            return retList;
        }
        public IEnumerable<MWLog> SelectLog(LookupCondition lookupCondition)
        {
            List<MWLog> retList = new List<MWLog>();

            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand())
                {
                    string sql = "select * from log_info limt 10;";
                    cmd.Connection = conn;
                    //cmd.CommandText = EmbeddedResource.GetString("SelectLogInfoWithLogProperty.sql");
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("begin_date", lookupCondition.BeginDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("end_date", lookupCondition.EndDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("routing_key", "%" + lookupCondition.RoutingKey + "%");
                    cmd.Parameters.AddWithValue("limit", lookupCondition.Limit);
                    cmd.Prepare();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            retList.Add(ModelConverter.ToMWLog(reader));
                        }
                    }
                }
            }
            return retList;
        }
        public IEnumerable<MWLog> SelectLog(int seq)
        {
            List<MWLog> retList = new List<MWLog>();

            using (var conn = new MySqlConnection(connString))
            {
                conn.OpenAsync();
                using (var cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = EmbeddedResource.GetString("SelectLogInfoWithLogPropertyBySeq.sql");
                    cmd.Parameters.AddWithValue("seq", seq);
                    cmd.Prepare();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            retList.Add(ModelConverter.ToMWLog(reader));
                        }
                    }
                }
            }
            return retList;
        }
    }
}
