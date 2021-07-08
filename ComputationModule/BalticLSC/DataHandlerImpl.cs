using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ComputationModule.BalticLSC
{
	public class DataHandlerImpl : DataHandler
	{

		private Dictionary<string, DataHandle> _dataHandles;
		private TokensProxy _tokensProxy;
		private JobRegistryImpl _registry;
		private IConfiguration _configuration;

		/// 
		/// <param name="registry"></param>
		public DataHandlerImpl(JobRegistryImpl registry, IConfiguration configuration)
		{
			_registry = registry;
			_tokensProxy = new TokensProxy();
			_configuration = configuration;
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
						values.Select(v => string.IsNullOrEmpty(v) ? JsonConvert.DeserializeObject<Dictionary<string, string>>(v) : null).ToList();
					List<MongoDBHandle> dbHandles =
						valuesObject.Select(vo => null != vo ? new MongoDBHandle(vo, false, _configuration) : null).ToList();
					List<string> dataItems = dbHandles.Select(h => h.Download().Item1).ToList();
					return (dataItems, sizes);
				default:
					throw new NotImplementedException(
						$"AccessType ({accessType}) not supported by the DataHandler, has to be handled manually");
			}
		}

		/// 
		/// <param name="pinName"></param>
		/// <param name="data"></param>
		public short SendData(string pinName, object data)
		{

			return 0;
		}

		/// 
		/// <param name="pinName"></param>
		/// <param name="data"></param>
		/// <param name="isFinal"></param>
		public short SendData(string pinName, object data, bool isFinal)
		{

			return 0;
		}

		/// 
		/// <param name="pinName"></param>
		/// <param name="data"></param>
		/// <param name="isFinal"></param>
		/// <param name="msgUid"></param>
		public short SendData(string pinName, object data, bool isFinal, string msgUid)
		{

			return 0;
		}

		/// 
		/// <param name="pinName"></param>
		/// <param name="values"></param>
		/// <param name="isFinal"></param>
		public short SendToken(string pinName, object values, bool isFinal)
		{

			return 0;
		}

		/// 
		/// <param name="pinName"></param>
		/// <param name="values"></param>
		/// <param name="isFinal"></param>
		/// <param name="msgUid"></param>
		public short SendToken(string pinName, object values, bool isFinal, string msgUid)
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