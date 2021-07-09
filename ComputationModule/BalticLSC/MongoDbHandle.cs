using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using ComputationModule.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using Newtonsoft.Json;
using Serilog;

namespace ComputationModule.BalticLSC
{
    public class MongoDbHandle : DataHandle
    {
        private readonly string _connectionString;
        private IMongoClient _mongoClient;
        private IMongoDatabase _mongoDatabase;
        private IMongoCollection<BsonDocument> _mongoCollection;

        public MongoDbHandle(string pinName, IConfiguration configuration) : base(pinName, configuration)
        {
            _connectionString = $"mongodb://{PinConfiguration.AccessCredential["User"]}" +
                                $":{PinConfiguration.AccessCredential["Password"]}" +
                                $"@{PinConfiguration.AccessCredential["Host"]}" +
                                $":{PinConfiguration.AccessCredential["Port"]}";
        }

        public override string Download(Dictionary<string, string> handle)
        {
            if ("input" != PinConfiguration.PinType)
                throw new Exception("Download cannot be called for output pins");
            if (!handle.TryGetValue("Database", out string databaseName))
                throw new ArgumentException("Incorrect DataHandle.");
            if (!handle.TryGetValue("Collection", out string collectionName))
                throw new ArgumentException("Incorrect DataHandle.");
            
            Prepare(databaseName, collectionName);
            
            var localPath = "";
            switch (PinConfiguration.DataMultiplicity)
            {
                case DataMultiplicity.Single:
                    if (!handle.TryGetValue("ObjectId", out string id))
                        throw new ArgumentException("Incorrect DataHandle.");
                    try
                    {
                        Log.Information($"Downloading object with id: {id}");
                        var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(id));
                        var document = _mongoCollection.Find(filter).FirstOrDefault();
                        if (document != null)
                        {
                            localPath = DownloadSingleFile(document, LocalPath);
                            Log.Information($"Downloading object with id: {id} successful.");
                        }
                        else
                        {
                            Log.Information($"Can not find object with id {id}");
                        }
                    }
                    catch (Exception)
                    {
                        Log.Error($"Downloading object with id {id} failed.");
                        ClearLocal();
                        throw;
                    }
                    break;
                case DataMultiplicity.Multiple:
                    try
                    {
                        Log.Information($"Downloading all files from {collectionName}.");
                        localPath = $"{LocalPath}/{collectionName}";
                        Directory.CreateDirectory(localPath);
                        var filter = Builders<BsonDocument>.Filter.Empty;
                        var documents = _mongoCollection.Find(filter).ToList();

                        foreach (var document in documents)
                            DownloadSingleFile(document, localPath);

                        AddGuidToFilesName(localPath);
                        Log.Information($"Downloading all files from {collectionName} successful.");
                    }
                    catch (Exception)
                    {
                        Log.Error($"Downloading all files from collection {collectionName} failed.");
                        ClearLocal();
                        throw;
                    }
                    break;
            }
            return localPath;
        }

        public override Dictionary<string, string> Upload(string localPath)
        {
            if ("input" == PinConfiguration.PinType)
                throw new Exception("Upload cannot be called for input pins");
            
            Dictionary<string, string> handle = null;
            try
            {
                (string databaseName, string collectionName) = Prepare();

                switch (SourceDataMultiplicity)
                {
                    case DataMultiplicity.Single when TargetDataMultiplicity == DataMultiplicity.Single:
                    {
                        Log.Information($"Uploading file from {localPath} to collection {collectionName}");
                        UploadSingleFile(localPath, true);
                        Log.Information($"Upload file from {localPath} successful.");
                        break;
                    }
                    case DataMultiplicity.Single when TargetDataMultiplicity == DataMultiplicity.Multiple:
                    {
                        Log.Information($"Uploading file from {localPath} to collection {collectionName}");
                        UploadSingleFile(localPath, true);
                        Log.Information($"Upload file from {localPath} successful.");
                        break;
                    }
                    case DataMultiplicity.Multiple when TargetDataMultiplicity == DataMultiplicity.Multiple:
                    {
                        Log.Information($"Uploading directory from {localPath} to collection {collectionName}");
                        var files = GetAllFiles(localPath);
                        var handleList = new List<Dictionary<string, string>>();
                        handle = new Dictionary<string, string>();

                        foreach (var file in files)
                        {
                            var bsonDocument = GetBsonDocument(file.FullName);
                            _mongoCollection.InsertOne(bsonDocument);
                            handleList.Add(GetTokenHandle(bsonDocument));
                        }


                        handle.Add("Files", JsonConvert.SerializeObject(handleList));
                        handle.Add("Database", databaseName);
                        handle.Add("Collection", collectionName);

                        SendOutputToken(handle, true);
                        Log.Information($"Upload directory from {localPath} successful.");
                        break;
                    }
                    case DataMultiplicity.Multiple when TargetDataMultiplicity == DataMultiplicity.Single:
                    {
                        Log.Information($"Uploading directory from {localPath} to collection {collectionName}");
                        var files = GetAllFiles(localPath);
                        for (var i = 0; i < files.Count; i++)
                        {
                            UploadSingleFile(files[i].FullName, i == files.Count - 1);
                        }

                        Log.Information($"Upload directory from {localPath} successful.");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error: {e} \n Uploading from {localPath} failed.");
                throw;
            }
            finally
            {
                ClearLocal();
            }

            return handle;
        }

        public override short CheckConnection(Dictionary<string, string> handle = null)
        {
            var host = PinConfiguration.AccessCredential["Host"];
            var port = PinConfiguration.AccessCredential["Port"];
            try
            {
                using var tcpClient = new TcpClient();
                tcpClient.Connect(host, int.Parse(port));
            }
            catch (Exception)
            {
                Log.Error($"Unable to reach {host}:{port}");
                return -1;
            }

            try
            {
                _mongoClient = new MongoClient(_connectionString);
                _mongoClient.ListDatabases();
            }
            catch (MongoAuthenticationException)
            {
                Log.Error("Unable to authenticate to MongoDB");
                return -2;
            }
            catch (Exception e)
            {
                Log.Error($"Error {e} while trying to connect to MongoDB");
                return -1;
            }
            
            if ("input" == PinConfiguration.PinType && null != handle)
            {
                if (!handle.TryGetValue("Database", out string databaseName))
                    throw new ArgumentException("Incorrect DataHandle.");
                if (!handle.TryGetValue("Collection", out string collectionName))
                    throw new ArgumentException("Incorrect DataHandle.");
                string id = null;
                if (PinConfiguration.DataMultiplicity == DataMultiplicity.Single
                    && !handle.TryGetValue("ObjectId", out id))
                    throw new ArgumentException("Incorrect DataHandle.");
                try
                {
                    _mongoDatabase = _mongoClient.GetDatabase(databaseName);
                    if (_mongoDatabase == null)
                    {
                        Log.Error($"No database {databaseName}");
                        return -3;
                    }

                    _mongoCollection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
                    if (_mongoCollection == null)
                    {
                        Log.Error($"No collection {collectionName}");
                        return -3;
                    }

                    if (PinConfiguration.DataMultiplicity == DataMultiplicity.Single)
                    {
                        var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(id));
                        var document = _mongoCollection.Find(filter).FirstOrDefault();

                        if (document == null)
                        {
                            Log.Error($"No document with id {id}");
                            return -3;
                        }
                    }
                }
                catch (Exception)
                {
                    Log.Error("Error while trying to " +
                              (null != id ? $"get object {id}" : $"access collection {collectionName}") +
                              $" from database {databaseName}" +
                              (null != id ? $" from collection {collectionName}" : ""));
                    return -3;
                }
            }     
            return 0;
        }
        
        private (string,string) Prepare(string databaseName = null, string collectionName = null)
        {
            databaseName ??= $"baltic_database_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            collectionName ??= $"baltic_collection_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            //TODO to reset or not to reset
            _mongoClient = new MongoClient(_connectionString);
            _mongoDatabase = _mongoClient.GetDatabase(databaseName);
            _mongoCollection = _mongoDatabase.GetCollection<BsonDocument>(collectionName);
            return (databaseName, collectionName);
        }
        
        private BsonDocument GetBsonDocument(string localPath)
        {
            var objectId = ObjectId.GenerateNewId();
            var fileStream = File.OpenRead(localPath);
            var fileName = new FileInfo(localPath).Name;
            var memoryStream = new MemoryStream();
            fileStream.CopyTo(memoryStream);
            var fileByteArray = memoryStream.ToArray();

            var data = new Dictionary<string, object>()
            {
                {"_id", objectId},
                {"fileName", fileName},
                {"fileContent", new BsonBinaryData(fileByteArray)}
            };

            var bsonDocument = new BsonDocument(data);

            return bsonDocument;
        }

        private Dictionary<string, string> GetTokenHandle(BsonDocument document)
        {
            var newHandle = new Dictionary<string, string>()
            {
                {"FileName", document.GetElement("fileName").Value.AsString},
                {"ObjectId", document.GetElement("_id").Value.AsObjectId.ToString()}
            };

            return newHandle;
        }
        
    }
}