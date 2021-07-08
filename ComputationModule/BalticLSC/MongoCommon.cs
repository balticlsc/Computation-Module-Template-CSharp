using System;
using System.Collections.Generic;
using ComputationModule.Messages;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace ComputationModule.BalticLSC
{
    public abstract class MongoCommon : DataHandle
    {
        protected string ConnectionString;
        protected string Id;
        protected string DatabaseName;
        protected IMongoClient MongoClient;
        protected IMongoDatabase MongoDatabase;
        protected bool IsTarget;
        protected string Host;
        protected string Port;

        protected MongoCommon(Dictionary<string, string> handle, bool isTarget, IConfiguration configuration) : base(configuration)
        {
            var index = isTarget ? TargetIndex : SourceIndex;
            if (isTarget)
            {
                if (IsOutput)
                {
                    DatabaseName = configuration[$"Pins:{index}:AccessPath:Database"];
                    if (DatabaseName == null)
                    {
                        throw new ArgumentException("No definition for output path.");
                    }
                }
            }
            else
            {
                if (!handle.TryGetValue("Database", out DatabaseName))
                {
                    throw new ArgumentException("Incorrect DataHandle.");
                }

                if (SourceDataMultiplicity == DataMultiplicity.Single)
                {
                    if (!handle.TryGetValue("ObjectId", out Id))
                    {
                        throw new ArgumentException("Incorrect DataHandle.");
                    }
                }
            }

            Host = configuration[$"Pins:{index}:AccessCredential:Host"];
            Port = configuration[$"Pins:{index}:AccessCredential:Port"];
            var user = configuration[$"Pins:{index}:AccessCredential:User"];
            var password = configuration[$"Pins:{index}:AccessCredential:Password"];
            ConnectionString = $"mongodb://{user}:{password}@{Host}:{Port}";
            IsTarget = isTarget;
        }

        protected void PrepareDatabase()
        {
            MongoClient = new MongoClient(ConnectionString);
            MongoDatabase = MongoClient.GetDatabase(DatabaseName);
        }
    }
}