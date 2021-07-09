using System;
using System.Collections.Generic;
using ComputationModule.Messages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ComputationModule.BalticLSC
{
    public class PinConfiguration
    {
        //Basic pin configuration, could be extended
        public string PinName;
        public string PinType;
        public string IsRequired;
        public string AccessType;
        public DataMultiplicity DataMultiplicity;
        public TokenMultiplicity TokenMultiplicity;
        public Dictionary<string,string> AccessCredential;

        public PinConfiguration(IConfigurationSection section)
        {
            PinName = section.GetValue<string>("PinName");
            PinType = section.GetValue<string>("PinType");
            IsRequired = section.GetValue<string>("IsRequired");
            AccessType = section.GetValue<string>("AccessType");
            DataMultiplicity = (DataMultiplicity)Enum.Parse(typeof(DataMultiplicity),
                section.GetValue<string>("DataMultiplicity"), true);
            TokenMultiplicity = (TokenMultiplicity)Enum.Parse(typeof(TokenMultiplicity),
                section.GetValue<string>("TokenMultiplicity"), true);
            AccessCredential = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                section.GetValue<string>("AccessCredential"));
        }
    }
}