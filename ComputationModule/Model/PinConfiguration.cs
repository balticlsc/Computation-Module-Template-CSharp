﻿using System;
using System.Collections.Generic;
using ComputationModule.Model.BalticDataModel;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson.IO;
using Serilog;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace ComputationModule.Model
{
    public class PinConfiguration
    {
        //Basic pin configuration, could be extended
        public string PinName;
        public string PinType;
        public string AccessType;
        public DataMultiplicity DataMultiplicity;
        public TokenMultiplicity TokenMultiplicity;
        public object AccessCredential;

        public PinConfiguration(IConfigurationSection section)
        {
            PinName = section.GetValue<string>("PinName");
            PinType = section.GetValue<string>("PinType");
            AccessType = section.GetValue<string>("AccessType");
            DataMultiplicity = (DataMultiplicity)Enum.Parse(typeof(DataMultiplicity),
                section.GetValue<string>("DataMultiplicity"), true);
            TokenMultiplicity = (TokenMultiplicity)Enum.Parse(typeof(TokenMultiplicity),
                section.GetValue<string>("TokenMultiplicity"), true);
            //Load access credentials here, access credentials are based on data store which pin will access
        }
    }
}