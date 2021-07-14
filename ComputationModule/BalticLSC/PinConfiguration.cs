using System;
using System.Collections.Generic;
using ComputationModule.Messages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ComputationModule.BalticLSC
{
    public class PinConfiguration
    {
        public readonly string PinName;
        public readonly string PinType;
        public readonly string IsRequired;
        public readonly string AccessType;
        public readonly DataMultiplicity DataMultiplicity;
        public readonly TokenMultiplicity TokenMultiplicity;
        public readonly Dictionary<string,string> AccessCredential;

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
            AccessCredential = new Dictionary<string, string>();
            foreach (IConfigurationSection aSection in section.GetSection("AccessCredential").GetChildren())
                AccessCredential.Add(aSection.Key,aSection.Value);
        }
    }
}