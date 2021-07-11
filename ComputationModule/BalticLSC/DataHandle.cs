using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ComputationModule.Messages;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace ComputationModule.BalticLSC {
	public abstract class DataHandle
	{
		protected readonly PinConfiguration PinConfiguration;
		protected readonly string LocalPath;
		
		private const string BalticDataPath = "/BalticLSC/data";
		private const string BalticDataPrefix = "BalticLSC-";
		private const int GuidLength = 6;

		/// 
		/// <param name="pinName"></param>
		/// <param name="configuration"></param>
		protected DataHandle(string pinName, IConfiguration configuration)
		{
			LocalPath = Environment.GetEnvironmentVariable("LOCAL_TMP_PATH") ?? "/balticLSC_tmp";

			try
			{
				PinConfiguration = 
					ConfigurationHandle.GetPinsConfiguration(configuration).Find(x => x.PinName == pinName);

			}
			catch (Exception)
			{
				Log.Error("Error while parsing configuration.");
			}
			
			Directory.CreateDirectory(LocalPath);

		}

		public abstract short CheckConnection(Dictionary<string, string> handle = null);

		/// 
		/// <param name="handle"></param>
		public abstract string Download(Dictionary<string, string> handle);

		/// 
		/// <param name="localPath"></param>
		public abstract Dictionary<string, string> Upload(string localPath);

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