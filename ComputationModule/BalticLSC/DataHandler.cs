using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ComputationModule.BalticLSC
{
	public class DataHandler : IDataHandler
	{

		private Dictionary<string, DataHandleOld> _dataHandles;
		private readonly TokensProxy _tokensProxy;
		private readonly JobRegistry _registry;
		private readonly IConfiguration _configuration;

		/// 
		/// <param name="registry"></param>
		/// <param name="configuration"></param>
		public DataHandler(JobRegistry registry, IConfiguration configuration)
		{
			_registry = registry;
			_tokensProxy = new TokensProxy();
			_configuration = configuration;
			_dataHandles = new Dictionary<string, DataHandleOld>();
		}

		public string ObtainDataItem(string pinName)
		{
			List<string> values;
			long[] sizes;
			(values, sizes) = ObtainDataItemsNDim(pinName);
			if (null == values || 0 == values.Count)
				return null;
			if (null == sizes && 1 == values.Count)
				return values[0];
			throw new Exception("Improper call - more than one data item exists for the pin");
		}

		public List<string> ObtainDataItems(string pinName)
		{
			List<string> values;
			long[] sizes;
			(values, sizes) = ObtainDataItemsNDim(pinName);
			if (1 == sizes?.Length)
				return values;
			throw new Exception("Improper call - more than one dimension exists for the pin");
		}

		/// 
		/// <param name="pinName"></param>
		public (List<string>, long[]) ObtainDataItemsNDim(string pinName)
		{
			string accessType = _registry.GetPinConfiguration(pinName).AccessType;

			switch (accessType)
			{
				case "Direct":
					return _registry.GetPinValuesNDim(pinName);
				case "MongoDB":
					List<string> values;
					long[] sizes;
					(values, sizes) = _registry.GetPinValuesNDim(pinName);
					List<Dictionary<string,string>> valuesObject = 
						values.Select(v => !string.IsNullOrEmpty(v) ? JsonConvert.DeserializeObject<Dictionary<string, string>>(v) : null).ToList();
					MongoDbHandle dbHandle = new MongoDbHandle(pinName, _configuration);
					List<string> dataItems = valuesObject.Select(vo => null != vo ?  dbHandle.Download(vo): null).ToList();
					//TODO tokens
					return (dataItems, sizes);
				default:
					throw new NotImplementedException(
						$"AccessType ({accessType}) not supported by the DataHandler, has to be handled manually");
			}
		}

		/// 
		/// <param name="pinName"></param>
		/// <param name="data"></param>
		/// <param name="isFinal"></param>
		/// <param name="msgUid"></param>
		public short SendDataItem(string pinName, string data, bool isFinal, string msgUid = null)
		{
			string accessType = _registry.GetPinConfiguration(pinName).AccessType;

			switch (accessType)
			{
				case "Direct":
					return SendToken(pinName, data, isFinal, msgUid);
				case "MongoDB":
					MongoDbHandle dbHandle = new MongoDbHandle(pinName, _configuration);
					Dictionary<string,string> newHandle = dbHandle.Upload(data);
					//TODO tokens
					return 0;
				default:
					throw new NotImplementedException(
						$"AccessType ({accessType}) not supported by the DataHandler, has to be handled manually");
			}
		}

		/// 
		/// <param name="pinName"></param>
		/// <param name="values"></param>
		/// <param name="isFinal"></param>
		/// <param name="msgUid"></param>
		public short SendToken(string pinName, object values, bool isFinal, string msgUid = null)
		{

			return 0;
		}

		public short FinishProcessing()
		{

			return 0;
		}

		/// 
		/// <param name="msgUids"></param>
		/// <param name="isFinal"></param>
		public short SendAckToken(List<string> msgUids, bool isFinal) {

			return 0;
		}

		/// 
		/// <param name="pinName"></param>
		public short CheckConnection(string pinName)
		{

			return 0;
		}

	}

}