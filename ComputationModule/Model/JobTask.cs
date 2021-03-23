using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ComputationModule.Model.BalticDataModel;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;


namespace ComputationModule.Model
{
    public class JobTask
    {
        public ComputationStatus Status { get; private set; } = ComputationStatus.Idle;
        public long Progress { get; private set; } = -1;
        public DataStoreProxy DataStoreProxy;
        private PinConfiguration _pinConfiguration;
        private TokensProxy _tokensProxy;
        private Dictionary<string, string> _data;

        public JobTask(PinConfiguration pinConfiguration, TokensProxy tokensProxy, string values)
        {
            _pinConfiguration = pinConfiguration;
            DataStoreProxy = new DataStoreProxy(_pinConfiguration);
            _tokensProxy = tokensProxy;
            _data = JsonConvert.DeserializeObject<Dictionary<string, string>>(values);
        }

        public void StartDataProcessing()
        {
            //Set computation status and progress for task, start computations, send output token if necessary
            Status = ComputationStatus.Working;
            Progress = 0;

            try
            {
                //Example of executing computations
                var dataProcessing = new DataProcessing(_pinConfiguration);
                var outputData = dataProcessing.Start(_data);
                //After finishing task send output data token
                _tokensProxy.SendOutputToken(outputData, true);
                //If all tasks for whole computation module are finished, send ack token, if module has multiple input pins and/or token multiplicity is 
                //multiple some extended logic behind sending ack token should be implemented
                Status = ComputationStatus.Completed;
                Progress = 100;
                _tokensProxy.SendAckToken();
            }
            catch (Exception e)
            {
                //If exception occurs you may log it to make debugging easier
                Log.Error($"{e.Message}\n {e.StackTrace}");
                //You must send ack token with "isFailed = true" and optionally message indicating error
                _tokensProxy.SendAckToken(isFailed: true, e.Message);
            }
        }
    }
}
