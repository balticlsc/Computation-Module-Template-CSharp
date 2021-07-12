using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using ComputationModule.Messages;
using Newtonsoft.Json;

namespace ComputationModule.BalticLSC
{
    public class TokensProxy
    {
        private readonly HttpClient _httpClient;
        private readonly string _batchManagerAckUrl;
        private readonly string _batchManagerTokenUrl;
        private readonly string _senderUid;
        
        public TokensProxy()
        {
            _httpClient = new HttpClient();
            _senderUid = Environment.GetEnvironmentVariable("SYS_MODULE_INSTANCE_UID"); 
            _batchManagerAckUrl = Environment.GetEnvironmentVariable("SYS_BATCH_MANAGER_ACK_ENDPOINT");
            _batchManagerTokenUrl = Environment.GetEnvironmentVariable("SYS_BATCH_MANAGER_TOKEN_ENDPOINT");
        }

        public HttpStatusCode SendOutputToken(string pinName, string values, string baseMsgUid, bool isFinal)
        {
            var xOutputToken = new OutputTokenMessage
            {
                PinName = pinName,
                SenderUid = _senderUid,
                Values = values,
                BaseMsgUid = baseMsgUid,
                IsFinal = isFinal
            };

            var serializedXOutputToken = JsonConvert.SerializeObject(xOutputToken);
            var data = new StringContent(serializedXOutputToken, Encoding.UTF8, "application/json");
            var result = _httpClient.PostAsync(_batchManagerTokenUrl, data).Result.StatusCode;

            return result;
        }

        public HttpStatusCode SendAckToken(List<string> msgUids, bool isFinal, bool isFailed = false, string note = null)
        {
            var ackToken = new TokensAck
            {
                SenderUid = _senderUid,
                MsgUids = msgUids,
                IsFinal = isFinal,
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