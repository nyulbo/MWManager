using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text.RegularExpressions;
using System.Data.Common;
using System.Windows.Markup;
using System.Windows.Data;
using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MessageBus;
using MWManagerApp.Models;
using System.Windows;

namespace MWManagerApp.Helpers
{
    public static class ModelConverter
    {
        public static MWLog ToMWLog(DbDataReader reader)
        {
            ReceivedInfo info = new ReceivedInfo
            {
                Seq = Convert.ToInt64(reader["seq"]),
                Exchange = reader["exchange"].ToString(),
                RoutingKey = reader["routing_key"].ToString(),
                Queue = reader["queue"].ToString(),
                DeliverTag = Convert.ToUInt64(reader["deliver_tag"]),
                ConsumerTag = reader["consumer_tag"].ToString(),
                Redelivered = Convert.ToBoolean(reader["redelivered"]),
                PropertySeq = Convert.ToInt64(reader["property_seq"]),
                Payload = reader["payload"].ToString(),
                InsDate = Convert.ToDateTime(reader["ins_date"]),
                UpdDate = Convert.ToDateTime(reader["upd_date"])
            };
            ReceivedProps prop = new ReceivedProps
            {
                Seq = Convert.ToInt64(reader["seq"]),
                Headers = JsonConvert.DeserializeObject<Dictionary<string, object>>(reader["headers"].ToString()),
                HeadersJSON = JsonConvert.SerializeObject(ModelConverter.ConvertToHeadersBase64(reader["headers"].ToString()), Formatting.Indented),
                InsDate = Convert.ToDateTime(reader["ins_date"]),
                UpdDate = Convert.ToDateTime(reader["upd_date"])
            };
            MWLog mwLog = new MWLog
            {
                Info = info,
                Prop = prop
            };
            return mwLog;
        }
        public static IDictionary<string, object> ConvertToHeadersBase64(string jsonHeader)
        {
            Dictionary<string, object> header = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonHeader);
            Dictionary<string, object> ret = new Dictionary<string, object>();
            object decodedString = null;
            foreach (KeyValuePair<string, object> item in header)
            {
                try
                {
                    switch (item.Key)
                    {
                        case "routing_keys":
                        case "routed_queues":
                        case "arguments":
                            if (!string.IsNullOrEmpty(item.Value.ToString()))
                            {
                                JArray jsonArr = JArray.Parse(item.Value.ToString());
                                List<string> jsonList = new List<string>();
                                foreach (var json in jsonArr)
                                {
                                    if (!string.IsNullOrEmpty(json.ToString()))
                                        jsonList.Add(json.ToString().ConvertToBase64String());
                                    else
                                        jsonList.Add(json.ToString());
                                }
                                decodedString = jsonList;
                            }
                            else
                                decodedString = item.Value;
                            break;
                        case "properties":
                        case "headers":
                            if (!string.IsNullOrEmpty(item.Value.ToString()))
                            {
                                decodedString = ConvertToHeadersBase64(JsonConvert.SerializeObject(item.Value));
                            }
                            else
                                decodedString = item.Value;
                            break;
                        case "timestamp":
                        case "timestamp_in_ms":
                            if (!string.IsNullOrEmpty(item.Value.ToString()))
                                decodedString = UnixTimeStampToDateTime(double.Parse(item.Value.ToString())).ToString("yyyy-MM-dd HH:mm:ss:fff");
                            else
                                decodedString = item.Value;
                            break;
                        default:
                            if (!string.IsNullOrEmpty(item.Value.ToString()))
                                decodedString = item.Value.ToString().ConvertToBase64String();
                            else
                                decodedString = item.Value;
                            break;
                    }
                }
                catch
                {
                    decodedString = item.Value;
                }
                ret.Add(item.Key, decodedString);
            }
            return ret;
        }

        public static IDictionary<string, object> ConvertToHeadersBase64(IDictionary<string, object> header)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            object decodedString = null;
            foreach (KeyValuePair<string, object> item in header)
            {   try
                {
                    switch (item.Key)
                    {
                        case "routing_keys":
                        case "routed_queues":
                        case "arguments":
                            if (!string.IsNullOrEmpty(item.Value.ToString()))
                            {
                                JArray jsonArr = JArray.Parse(item.Value.ToString());
                                List<string> jsonList = new List<string>();
                                foreach (var json in jsonArr)
                                {
                                    if (!string.IsNullOrEmpty(json.ToString()))
                                        jsonList.Add(json.ToString().ConvertToBase64String());
                                    else
                                        jsonList.Add(json.ToString());
                                }
                                decodedString = jsonList;
                            }
                            else
                                decodedString = item.Value;
                            break;
                        case "properties":
                        case "headers":
                            if (!string.IsNullOrEmpty(item.Value.ToString()))
                            {
                                IDictionary<string, object> dic = JsonConvert.DeserializeObject<IDictionary<string, object>>(JsonConvert.SerializeObject(item.Value));
                                decodedString = ConvertToHeadersBase64(dic);
                            }
                            else
                                decodedString = item.Value;
                            break;
                        case "timestamp":
                        case "timestamp_in_ms":
                            if (!string.IsNullOrEmpty(item.Value.ToString()))
                                decodedString = UnixTimeStampToDateTime(double.Parse(item.Value.ToString())).ToString("yyyy-MM-dd HH:mm:ss:fff");
                            else
                                decodedString = item.Value;
                            break;
                        default:
                            if (!string.IsNullOrEmpty(item.Value.ToString()))
                                decodedString = item.Value.ToString().ConvertToBase64String();
                            else
                                decodedString = item.Value;
                            break;
                    }
                }
                catch
                {
                    decodedString = item.Value;
                }
                ret.Add(item.Key, decodedString);
            }
            return ret;
        }

        //https://stackoverflow.com/questions/6309379/how-to-check-for-a-valid-base64-encoded-string
        public static string ConvertToBase64String(this string s)
        {
            try
            {
                s = s.Trim();
                if ((s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None) && !s.Equals("True"))
                {
                    return Encoding.UTF8.GetString(Convert.FromBase64String(s));
                }
                else
                    return s;
            }
            catch
            {
                return s;
            }
        }
        static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        static readonly double MaxUnixSeconds = (DateTime.MaxValue - UnixEpoch).TotalSeconds;
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            return unixTimeStamp > MaxUnixSeconds
               ? UnixEpoch.AddMilliseconds(unixTimeStamp).ToLocalTime()
               : UnixEpoch.AddSeconds(unixTimeStamp).ToLocalTime();
        }
    }


    public abstract class BaseOnewayConverter :
                          MarkupExtension, IValueConverter
    {
        public BaseOnewayConverter() { }
        public abstract object Convert(object value, Type targetType,
                               object parameter, CultureInfo culture);
        public object ConvertBack(object value, Type targetType,
                      object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }
        public override object ProvideValue(System.IServiceProvider serviceProvider)
        {
            return this;
        }
    }
    public class BoolToVisibilityConverter : BaseOnewayConverter
    {
        public override object Convert(object value, Type targetType,
                               object parameter, CultureInfo culture)
        {
            return ((bool)value) ? Visibility.Visible :
                                   Visibility.Collapsed;
        }
    }
    public class BGConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            MWLog log = (MWLog)value;
            string bgColor = "Blue";
            switch (log.Info.RoutingKey.Substring(0, 7))
            {
                case "publish":
                    bgColor = "Blue";
                    break;
                case "deliver":
                    bgColor = "Red";
                    break;
                default:
                    bgColor = "Blue";
                    break;
            }
            return bgColor;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class FGConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            MWLog log = (MWLog)value;
            string bgColor = "White";
            switch (log.Info.RoutingKey.Substring(0, 7))
            {
                case "publish":
                    bgColor = "White";
                    break;
                case "deliver":
                    bgColor = "Yellow";
                    break;
                default:
                    bgColor = "White";
                    break;
            }
            return bgColor;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class MultiElementConvert : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {   
            if (values[0].ToString() != "Log")
            {
                return values[1];
            }
            else
            {
                return values[2];
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    public class NewLineConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string original = value.ToString();
            return original.Replace("\n", "");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class HeaderBase64Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            
            string original = value.ToString();
            return original.Replace("\n", "");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class EnableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool)value) ? false : true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}