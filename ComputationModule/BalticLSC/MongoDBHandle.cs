using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using ComputationModule.Messages;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Serilog;

namespace ComputationModule.BalticLSC
{
    public class MongoDBHandle : MongoCommon
    {
        private string _collectionName;
        private IMongoCollection<BsonDocument> _mongoCollection;

        public MongoDBHandle(Dictionary<string, string> handle, bool isTarget, IConfiguration configuration) : base(
            handle, isTarget, configuration)
        {
            var index = isTarget ? TargetIndex : SourceIndex;
            if (isTarget)
            {
                if (IsOutput)
                {
                    _collectionName = configuration[$"Pins:{index}:AccessPath:Collection"];
                    if (_collectionName == null)
                    {
                        throw new ArgumentException("No definition for output path.");
                    }
                }
            }
            else
            {
                if (!handle.TryGetValue("Collection", out _collectionName))
                {
                    throw new ArgumentException("Incorrect DataHandle.");
                }
            }
        }

        private void Prepare()
        {
            PrepareDatabase();
            _mongoCollection = MongoDatabase.GetCollection<BsonDocument>(_collectionName);
        }

        public override (string, bool) Download()
        {
            Prepare();

            var status = false;
            var localPath = "";
            switch (SourceDataMultiplicity)
            {
                case DataMultiplicity.Single:
                {
                    try
                    {
                        Log.Information($"Downloading object with id: {Id}");
                        var id = new ObjectId(Id);
                        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                        var document = _mongoCollection.Find(filter).FirstOrDefault();

                        if (document != null)
                        {
                            localPath = DownloadSingleFile(document, LocalPath);
                            status = true;
                            Log.Information($"Downloading object with id: {Id} successful.");
                        }
                        else
                        {
                            Log.Information($"Can not find object with id {Id}");
                        }

                        break;
                    }
                    catch (Exception)
                    {
                        Log.Error($"Downloading object with id {Id} failed.");
                        ClearLocal();
                        throw;
                    }
                }
                case DataMultiplicity.Multiple:
                {
                    try
                    {
                        Log.Information($"Downloading all files from {_collectionName}.");
                        localPath = $"{LocalPath}/{_collectionName}";
                        Directory.CreateDirectory(localPath);
                        var filter = Builders<BsonDocument>.Filter.Empty;
                        var documents = _mongoCollection.Find(filter).ToList();

                        foreach (var document in documents)
                        {
                            DownloadSingleFile(document, localPath);
                        }

                        AddGuidToFilesName(localPath);
                        status = true;
                        Log.Information($"Downloading all files from {_collectionName} successful.");
                    }
                    catch (Exception)
                    {
                        Log.Error($"Downloading all files from collection {_collectionName} failed.");
                        ClearLocal();
                        throw;
                    }

                    break;
                }
                default:
                    status = false;
                    break;
            }

            return (localPath, status);
        }

        public override bool Upload(string localPath)
        {
            if (!IsOutput)
            {
                GenerateCollectionName();
            }

            var status = false;
            try
            {
                Prepare();
                switch (SourceDataMultiplicity)
                {
                    case DataMultiplicity.Single when TargetDataMultiplicity == DataMultiplicity.Single:
                    {
                        Log.Information($"Uploading file from {localPath} to collection {_collectionName}");
                        UploadSingleFile(localPath, true);
                        status = true;
                        Log.Information($"Upload file from {localPath} successful.");
                        break;
                    }
                    case DataMultiplicity.Single when TargetDataMultiplicity == DataMultiplicity.Multiple:
                    {
                        Log.Information($"Uploading file from {localPath} to collection {_collectionName}");
                        UploadSingleFile(localPath, true);
                        status = true;
                        Log.Information($"Upload file from {localPath} successful.");
                        break;
                    }
                    case DataMultiplicity.Multiple when TargetDataMultiplicity == DataMultiplicity.Multiple:
                    {
                        Log.Information($"Uploading directory from {localPath} to collection {_collectionName}");
                        var files = GetAllFiles(localPath);
                        var handleList = new List<Dictionary<string, string>>();
                        var newHandle = new Dictionary<string, string>();

                        foreach (var file in files)
                        {
                            var bsonDocument = GetBsonDocument(file.FullName);
                            _mongoCollection.InsertOne(bsonDocument);
                            handleList.Add(GetTokenHandle(bsonDocument));
                        }


                        newHandle.Add("Files", JsonConvert.SerializeObject(handleList));
                        newHandle.Add("Database", DatabaseName);
                        newHandle.Add("Collection", _collectionName);

                        SendOutputToken(newHandle, true);
                        status = true;
                        Log.Information($"Upload directory from {localPath} successful.");
                        break;
                    }
                    case DataMultiplicity.Multiple when TargetDataMultiplicity == DataMultiplicity.Single:
                    {
                        Log.Information($"Uploading directory from {localPath} to collection {_collectionName}");
                        var files = GetAllFiles(localPath);
                        for (var i = 0; i < files.Count; i++)
                        {
                            UploadSingleFile(files[i].FullName, i == files.Count - 1);
                        }

                        status = true;
                        Log.Information($"Upload directory from {localPath} successful.");
                        break;
                    }
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error: {e} \n Uploading from {localPath} failed.");
                ClearLocal();
                throw;
            }

            ClearLocal();
            SendAckToken();

            return status;
        }

        private void UploadSingleFile(string localPath, bool isFinal)
        {
            if (!CheckFileSize(localPath))
            {
                return;
            }

            var bsonDocument = GetBsonDocument(localPath);
            _mongoCollection.InsertOne(bsonDocument);

            var newHandle = GetTokenHandle(bsonDocument);
            newHandle.Add("Database", DatabaseName);
            newHandle.Add("Collection", _collectionName);

            SendOutputToken(newHandle, isFinal);
        }

        private bool CheckFileSize(string localPath)
        {
            var size = new FileInfo(localPath).Length;
            var maxSize = 16 * 1024 * 1024;

            return size < maxSize;
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

        private string DownloadSingleFile(BsonDocument document, string localPath)
        {
            var fileName = document.GetElement("fileName").Value.AsString;
            var fileContent = document.GetElement("fileContent").Value.AsBsonBinaryData;
            var filePath = $"{localPath}/{fileName}";
            using var fileStream = File.OpenWrite(filePath);
            fileStream.Write(fileContent.Bytes);
            fileStream.Dispose();

            return filePath;
        }

        private void GenerateCollectionName()
        {
            DatabaseName = $"baltic_database_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            _collectionName = $"baltic_collection_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        public override short CheckConnection()
        {
            try
            {
                using var tcpClient = new TcpClient();
                tcpClient.Connect(Host, int.Parse(Port));
            }
            catch (Exception)
            {
                Log.Error($"Unable to reach {Host}:{Port}");
                return 1;
            }

            try
            {
                MongoClient = new MongoClient(ConnectionString);
                MongoClient.ListDatabases();
            }
            catch (MongoAuthenticationException)
            {
                Log.Error("Unable to authenticate to MongoDB");
                return 2;
            }
            catch (Exception e)
            {
                Log.Error($"Error {e} while trying to connect to MongoDB");
                return 1;
            }

            try
            {
                if (!IsTarget && SourceDataMultiplicity == DataMultiplicity.Single)
                {
                    MongoDatabase = MongoClient.GetDatabase(DatabaseName);
                    if (MongoDatabase == null)
                    {
                        Log.Error($"No database {DatabaseName}");
                        return 3;
                    }
                    _mongoCollection = MongoDatabase.GetCollection<BsonDocument>(_collectionName);
                    if (_mongoCollection == null)
                    {
                        Log.Error($"No collection {_collectionName}");
                        return 3;
                    }

                    var id = new ObjectId(Id);
                    var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                    var document = _mongoCollection.Find(filter).FirstOrDefault();

                    if (document == null)
                    {
                        Log.Error($"No document with id {Id}");
                        return 3;
                    }
                }
            }
            catch (Exception)
            {
                Log.Error($"Error while trying to get object {Id} from database {DatabaseName} from collection {_collectionName}");
                return 3;
            }

            return 0;
        }
    }
}