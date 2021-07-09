using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ComputationModule.Messages;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace ComputationModule.BalticLSC
{
    public abstract class DataHandleOld
    {
        protected readonly string LocalPath;
        private const string BalticDataPath = "/BalticLSC/data";
        private const string BalticDataPrefix = "BalticLSC-";
        private const int GuidLength = 6;
        
        protected readonly bool IsOutput;
        protected string SourceDataType;
        protected int SourceIndex;
        protected int TargetIndex;
        
        protected readonly string TargetDataType;
        protected readonly DataMultiplicity SourceDataMultiplicity;
        protected readonly DataMultiplicity TargetDataMultiplicity;
        public TokensProxy TokensProxy { get; set; }

        public abstract (string, bool) Download();
        public abstract bool Upload(string path);
        public abstract short CheckConnection();

        protected DataHandleOld(IConfiguration configuration)
        {
            LocalPath = Environment.GetEnvironmentVariable("LOCAL_TMP_PATH") ?? "/balticLSC_tmp";

            if (configuration["Pins:0:PinType"].ToLower().Trim() == "input"
                && (configuration["Pins:1:PinType"].ToLower().Trim() == "output"
                    || configuration["Pins:1:PinType"].ToLower().Trim() == "external output"))
            {
                SourceIndex = 0;
                TargetIndex = 1;
            }
            else if ((configuration["Pins:0:PinType"].ToLower().Trim() == "output" ||
                      configuration["Pins:0:PinType"].ToLower().Trim() == "external output")
                     && configuration["Pins:1:PinType"].ToLower().Trim() == "input")
            {
                SourceIndex = 1;
                TargetIndex = 0;
            }
            else
            {
                throw new ArgumentException("Not proper pins configuration");
            }

            var outputPinType = configuration[$"Pins:{TargetIndex}:PinType"].ToLower().Trim();
            IsOutput = outputPinType.Contains("external");

            SourceDataType = configuration[$"Pins:{SourceIndex}:DataType"];
            TargetDataType = configuration[$"Pins:{TargetIndex}:DataType"];
            SourceDataMultiplicity = (DataMultiplicity) Enum.Parse(typeof(DataMultiplicity),
                configuration[$"Pins:{SourceIndex}:DataMultiplicity"],true);
            TargetDataMultiplicity = (DataMultiplicity) Enum.Parse(typeof(DataMultiplicity),
                configuration[$"Pins:{TargetIndex}:DataMultiplicity"],true);

            Directory.CreateDirectory(LocalPath);
        }

        protected void ClearLocal()
        {
            try
            {
                if (Directory.Exists(LocalPath))
                {
                    Directory.Delete(LocalPath, true);
                }
                else if (File.Exists(LocalPath))
                {
                    File.Delete(LocalPath);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error while clearing local memory: {e}");
            }
        }

        protected void SendOutputToken(string pinName, Dictionary<string, string> handle, string baseMsgUid, bool isFinal)
        {
            if (IsOutput)
            {
                return;
            }

            try
            {
                var result = TokensProxy.SendOutputToken(pinName, handle, baseMsgUid, isFinal);
            }
            catch (Exception e)
            {
                Log.Error($"Error while sending output token: {e}");
            }
        }

        protected string GetRemotePathFromLocalPath(string localPath)
        {
            var name = Path.GetFileName(localPath);
            if (Path.GetExtension(localPath) != string.Empty && SourceDataMultiplicity == DataMultiplicity.Single && TargetDataMultiplicity == DataMultiplicity.Multiple)
            {
                name = Path.GetFileNameWithoutExtension(name);
            }

            return Path.Combine(BalticDataPath, GetNameWithGuid(name));
        }

        protected void AddGuidToFilesName(string directoryPath)
        {
            var files = new DirectoryInfo(directoryPath).GetFiles();
            foreach (var file in files)
            {
                var filePath = file.FullName;
                var fileName = Path.GetFileName(filePath);
                var newFileName = GetNameWithGuid(fileName);
                File.Move(filePath,Path.Combine(directoryPath, newFileName));
            }
        }

        private string GetNameWithGuid(string name)
        {
            if(name.StartsWith(BalticDataPrefix))
            {
                return name.Remove(BalticDataPrefix.Length,GuidLength).Insert(BalticDataPrefix.Length,Guid.NewGuid().ToString().Substring(0, GuidLength));
            }
            return BalticDataPrefix + Guid.NewGuid().ToString().Substring(0, GuidLength)+"-" + name;
        }
        
        protected List<FileInfo> GetAllFiles(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                return new List<FileInfo>();
            }

            var directoryInfo = new DirectoryInfo(directoryPath);
            var files = directoryInfo.GetFiles().ToList();
            var directories = directoryInfo.GetDirectories().ToList();

            directories.ForEach(x => files.AddRange(GetAllFiles(x.FullName)));

            return files;
        }
    }
}