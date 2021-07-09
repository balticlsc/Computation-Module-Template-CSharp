using System;
using System.Collections.Generic;
using ComputationModule.Messages;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace ComputationModule.BalticLSC
{
    public class MongoDbHandle : DataHandle
    {
        private string _connectionString;
        private string _id;
        private string _databaseName;
        private string _collectionName;
        private IMongoClient _mongoClient;
        private IMongoDatabase _mongoDatabase;
        private string _host;
        private string _port;
        
        public MongoDbHandle(string pinName, IConfiguration configuration) : base(pinName, configuration)
        {
            _host = PinConfiguration.AccessCredential["Host"];
            _port = PinConfiguration.AccessCredential["Port"];
            
            var user = PinConfiguration.AccessCredential["User"];
            var password = PinConfiguration.AccessCredential["Password"];
            _connectionString = $"mongodb://{user}:{password}@{_host}:{_port}";
        }

        public override short CheckConnection()
        {
            throw new System.NotImplementedException();
        }

        public override string Download(Dictionary<string, string> handle)
        {
            if ("input" != PinConfiguration.PinType)
                throw new Exception("Download cannot be called for output pins");
            if (!handle.TryGetValue("Database", out _databaseName))
                throw new ArgumentException("Incorrect DataHandle.");
            if (!handle.TryGetValue("Collection", out _collectionName))
                throw new ArgumentException("Incorrect DataHandle.");

            if (PinConfiguration.DataMultiplicity == DataMultiplicity.Single 
                && !handle.TryGetValue("ObjectId", out _id))
                throw new ArgumentException("Incorrect DataHandle.");

            throw new System.NotImplementedException();
        }

        public override Dictionary<string, string> Upload(string localPath)
        {
            if ("input" == PinConfiguration.PinType)
                throw new Exception("Upload cannot be called for input pins");
            throw new System.NotImplementedException();
        }
        
        protected void PrepareDatabase()
        {
            _mongoClient = new MongoClient(_connectionString);
            _mongoDatabase = _mongoClient.GetDatabase(_databaseName);
        }
    }
}