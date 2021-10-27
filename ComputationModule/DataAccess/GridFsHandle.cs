using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using ComputationModule.BalticLSC;
using ComputationModule.Messages;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Newtonsoft.Json;
using Serilog;

namespace ComputationModule.DataAccess
{
    public class GridFsHandle : DataHandle
    {
        private readonly string _connectionString;
        private IMongoClient _mongoClient;
        private IMongoDatabase _mongoDatabase;
        private IGridFSBucket _mongoBucket;

        public GridFsHandle(string pinName, IConfiguration configuration) : base(pinName, configuration)
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
            if (!handle.TryGetValue("Database", out var databaseName))
                throw new ArgumentException("Incorrect DataHandle (Database).");
            if (!handle.TryGetValue("Collection", out var collectionName))
                throw new ArgumentException("Incorrect DataHandle (Collection).");

            Prepare(databaseName, collectionName);

            var localPath = "";
            switch (PinConfiguration.DataMultiplicity)
            {
                case DataMultiplicity.Single:
                {
                    if (!handle.TryGetValue("ObjectId", out string id))
                        throw new ArgumentException("Incorrect DataHandle (ObjectId).");
                    if (!handle.TryGetValue("FileName", out string fileName))
                        throw new ArgumentException("Incorrect DataHandle (FileName).");
                    try
                    {
                        Log.Information($"Downloading object with id: {id}");

                        localPath = $"{LocalPath}/{fileName}";
                        DownloadOneFile(ObjectId.Parse(id), localPath);
                        
                        Log.Information($"Downloading object with id: {id} successful.");
                    }
                    catch (Exception)
                    {
                        Log.Error($"Downloading object with id {id} failed.");
                        ClearLocal();
                        throw;
                    }

                    break;
                }
                case DataMultiplicity.Multiple:
                {
                    try
                    {
                        Log.Information($"Downloading all files from {collectionName}.");
                        localPath = $"{LocalPath}/{collectionName}";
                        Directory.CreateDirectory(localPath);

                        using var cursor = _mongoBucket.Find(Builders<GridFSFileInfo>.Filter.Empty);
                        
                        foreach (var file in cursor.ToList())
                            DownloadOneFile(file.Id, localPath + "/" + file.Filename);

                        AddGuidToFilesName(localPath);
                        Log.Information($"Downloading all files from {collectionName} successful.");
                    }
                    catch (Exception)
                    {
                        Log.Error($"Downloading all files from bucket {collectionName} failed.");
                        ClearLocal();
                        throw;
                    }

                    break;
                }
            }

            return localPath;
        }
        
        private void DownloadOneFile(ObjectId id, string localPath)
        {
            FileStream file = new FileStream(localPath,FileMode.Create);
            _mongoBucket.DownloadToStream(id, file);
            file.Close();
        }

        public override Dictionary<string, string> Upload(string localPath)
        {
            if ("input" == PinConfiguration.PinType)
                throw new Exception("Upload cannot be called for input pins");
            if (!File.Exists(localPath) && !Directory.Exists(localPath))
                throw new ArgumentException($"Invalid path ({localPath})");
            var isDirectory = File.GetAttributes(localPath).HasFlag(FileAttributes.Directory);
            if (DataMultiplicity.Multiple == PinConfiguration.DataMultiplicity && !isDirectory)
                throw new ArgumentException("Multiple data pin requires path pointing to a directory, not a file");
            if (DataMultiplicity.Single == PinConfiguration.DataMultiplicity && isDirectory)
                throw new ArgumentException("Single data pin requires path pointing to a file, not a directory");

            Dictionary<string, string> handle = null;
            try
            {
                var (databaseName, collectionName) = Prepare();

                switch (PinConfiguration.DataMultiplicity)
                {
                    case DataMultiplicity.Single:
                    {
                        Log.Information($"Uploading file from {localPath} to collection {collectionName}");

                        handle = UploadOneFile(localPath);
                        handle.Add("Database", databaseName);
                        handle.Add("Collection", collectionName);
                        
                        Log.Information($"Upload file from {localPath} successful.");
                        break;
                    }
                    case DataMultiplicity.Multiple:
                    {
                        Log.Information($"Uploading directory from {localPath} to bucket {collectionName}");
                        var files = GetAllFiles(localPath);
                        var handleList = new List<Dictionary<string, string>>();

                        foreach (FileInfo fileInfo in GetAllFiles(localPath))
                            handleList.Add(UploadOneFile(fileInfo.FullName));

                        handle = new Dictionary<string, string>
                        {
                            {"Files", JsonConvert.SerializeObject(handleList)},
                            {"Database", databaseName},
                            {"Collection", collectionName}
                        };

                        Log.Information($"Upload directory from {localPath} successful.");
                        break;
                    }
                }

                return handle;
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
        }

        private Dictionary<string, string> UploadOneFile(string localPath)
        {
            FileStream file = new FileStream(localPath, FileMode.Open);
            string fileName = new FileInfo(localPath).Name;
            ObjectId id = _mongoBucket.UploadFromStream(fileName, file);
            file.Close();
                        
            return new Dictionary<string, string>()
            {
                {"ObjectId", id.ToString()}, 
                {"FileName", fileName}
            };
        }

        public override short CheckConnection(Dictionary<string, string> handle = null)
        {
            string host = PinConfiguration.AccessCredential["Host"],
                port = PinConfiguration.AccessCredential["Port"];
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
                if (!handle.TryGetValue("Database", out var databaseName))
                    throw new ArgumentException("Incorrect DataHandle (Database).");
                if (!handle.TryGetValue("Collection", out var collectionName))
                    throw new ArgumentException("Incorrect DataHandle (Collection).");
                string id = null;
                if (PinConfiguration.DataMultiplicity == DataMultiplicity.Single
                    && !handle.TryGetValue("ObjectId", out id))
                    throw new ArgumentException("Incorrect DataHandle (ObjectId).");
                try
                {
                    _mongoDatabase = _mongoClient.GetDatabase(databaseName);
                    if (_mongoDatabase == null)
                    {
                        Log.Error($"No database {databaseName}");
                        return -3;
                    }

                    _mongoBucket = new GridFSBucket(_mongoDatabase,
                        new GridFSBucketOptions() {BucketName = collectionName});
                    var cursor = _mongoBucket.Find(Builders<GridFSFileInfo>.Filter.Empty);
                    if (0 == cursor.ToList().Count)
                    {
                        Log.Error($"No or empty bucket {collectionName}");
                        return -3;
                    }

                    if (PinConfiguration.DataMultiplicity == DataMultiplicity.Single)
                    {
                        cursor = _mongoBucket.Find(Builders<GridFSFileInfo>.Filter.Eq(x => x.Id, ObjectId.Parse(id)));

                        if (0 == cursor.ToList().Count)
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

        private (string, string) Prepare(string databaseName = null, string collectionName = null)
        {
            databaseName ??= $"baltic_database_{Guid.NewGuid().ToString("N")[..8]}";
            collectionName ??= $"baltic_collection_{Guid.NewGuid().ToString("N")[..8]}";
            //TODO to reset or not to reset
            _mongoClient = new MongoClient(_connectionString);
            _mongoDatabase = _mongoClient.GetDatabase(databaseName);
            _mongoBucket = new GridFSBucket(_mongoDatabase, new GridFSBucketOptions(){BucketName = collectionName});
            return (databaseName, collectionName);
        }

        private static BsonDocument GetBsonDocument(string localPath)
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

        private static Dictionary<string, string> GetTokenHandle(BsonDocument document)
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