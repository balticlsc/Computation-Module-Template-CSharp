using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using ComputationModule.Model.BalticDataModel;
using Newtonsoft.Json;

namespace ComputationModule.Model
{
    public class TokensProxy
    {
        private readonly HttpClient _httpClient;
        private readonly string _batchManagerAckUrl;
        private readonly string _batchManagerTokenUrl;
        private readonly string _senderUid;
        private readonly string _baseMsgUid;
        private readonly string _pinName;

        public TokensProxy(string baseMsgUid, string pinName)
        {
            _baseMsgUid = baseMsgUid;
            _pinName = pinName;
            _httpClient = new HttpClient();
            _senderUid = Environment.GetEnvironmentVariable("SYS_MODULE_INSTANCE_UID"); 
            _batchManagerAckUrl = Environment.GetEnvironmentVariable("SYS_BATCH_MANAGER_ACK_ENDPOINT");
            _batchManagerTokenUrl = Environment.GetEnvironmentVariable("SYS_BATCH_MANAGER_TOKEN_ENDPOINT");
        }

        public HttpStatusCode SendOutputToken(Dictionary<string, string> handle, bool isFinal)
        {
            var xOutputToken = new XOutputTokenMessage
            {
                PinName = _pinName,
                SenderUid = _senderUid,
                Values = JsonConvert.SerializeObject(handle),
                BaseMsgUid = _baseMsgUid,
                IsFinal = isFinal
            };

            var serializedXOutputToken = JsonConvert.SerializeObject(xOutputToken);
            var data = new StringContent(serializedXOutputToken, Encoding.UTF8, "application/json");
            var result = _httpClient.PostAsync(_batchManagerTokenUrl, data).Result.StatusCode;

            return result;
        }

        public HttpStatusCode SendAckToken(bool isFailed = false, string note = null)
        {
            var ackToken = new XTokensAck
            {
                SenderUid = _senderUid,
                MsgUids = new List<string> {_baseMsgUid},
                IsFailed = isFailed,
                Note = note
            };

            var serializedAckToken = JsonConvert.SerializeObject(ackToken);
            var data = new StringContent(serializedAckToken, Encoding.UTF8, "application/json");

            var result = _httpClient.PostAsync(_batchManagerAckUrl, data).Result.StatusCode;

            return result;
        }
    }
}