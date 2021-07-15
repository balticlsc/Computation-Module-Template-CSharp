using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using ComputationModule.Messages;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ComputationModule.BalticLSC
{
	public class DataHandler : IDataHandler
	{

		private Dictionary<string, DataHandle> _dataHandles;
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
			_dataHandles = new Dictionary<string, DataHandle>();
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
			try
			{
				List<string> values;
				long[] sizes;
				(values, sizes) = _registry.GetPinValuesNDim(pinName);
				List<Dictionary<string,string>> valuesObject = 
					values.Select(v => !string.IsNullOrEmpty(v) ? JsonConvert.DeserializeObject<Dictionary<string, string>>(v) : null).ToList();
				DataHandle dHandle = GetDataHandle(pinName);
				List<string> dataItems = valuesObject.Select(vo => null != vo ? dHandle.Download(vo): null).ToList();
				return (dataItems, sizes);
			}
			catch (ArgumentException)
			{
				return _registry.GetPinValuesNDim(pinName);
			}
		}

		/// 
		/// <param name="pinName"></param>
		/// <param name="data"></param>
		/// <param name="isFinal"></param>
		/// <param name="msgUid"></param>
		public short SendDataItem(string pinName, string data, bool isFinal, string msgUid = null)
		{
			if("Direct" == _registry.GetPinConfiguration(pinName).AccessType)
				return SendToken(pinName, data, isFinal, msgUid);
			DataHandle dHandle = GetDataHandle(pinName);
			Dictionary<string,string> newHandle = dHandle.Upload(data);
			return SendToken(pinName, JsonConvert.SerializeObject(newHandle), isFinal, msgUid);
		}

		/// 
		/// <param name="pinName"></param>
		/// <param name="values"></param>
		/// <param name="isFinal"></param>
		/// <param name="msgUid"></param>
		public short SendToken(string pinName, string values, bool isFinal, string msgUid = null)
		{
			if (null == msgUid)
				msgUid = _registry.GetBaseMsgUid();
			return HttpStatusCode.OK == _tokensProxy.SendOutputToken(pinName, values, msgUid, isFinal) ? (short)0 : (short)-1;
		}

		public short FinishProcessing()
		{
			List<string> msgUids = _registry.GetAllMsgUids();
			_registry.SetStatus(Status.Completed);
			return SendAckToken(msgUids, true);
		}

		public short FailProcessing(string note)
		{
			List<string> msgUids = _registry.GetAllMsgUids();
			_registry.SetStatus(Status.Failed);
			if (HttpStatusCode.OK == _tokensProxy.SendAckToken(msgUids, true, true, note))
			{
				_registry.ClearMessages(msgUids);
				return 0;
			}
			return -1;
		}

		/// 
		/// <param name="msgUids"></param>
		/// <param name="isFinal"></param>
		public short SendAckToken(List<string> msgUids, bool isFinal)
		{
			if (HttpStatusCode.OK == _tokensProxy.SendAckToken(msgUids, isFinal))
			{
				_registry.ClearMessages(msgUids);
				return 0;
			}
			return -1;
		}

		/// 
		/// <param name="pinName"></param>
		/// <param name="handle"></param>
		public short CheckConnection(string pinName, Dictionary<string,string> handle = null)
		{
			try
			{
				DataHandle dHandle = GetDataHandle(pinName);
				return dHandle.CheckConnection(handle);
			}
			catch (ArgumentException)
			{
				throw new ArgumentException(
					"Cannot check connection for a pin of type \"Direct\"");
			}
		}

		private DataHandle GetDataHandle(string pinName)
		{
			if (_dataHandles.ContainsKey(pinName))
				return _dataHandles[pinName];
			string accessType = _registry.GetPinConfiguration(pinName).AccessType;
			DataHandle handle;
			switch (accessType)
			{
				case "Direct":
					throw new ArgumentException(
						"Cannot create a data handle for a pin of type \"Direct\"");
				case "MongoDB":
					handle = new MongoDbHandle(pinName, _configuration);
					break;
				default:
					throw new NotImplementedException(
						$"AccessType ({accessType}) not supported by the DataHandler, has to be handled manually");
			}
			_dataHandles[pinName] = handle;
			return handle;
		}

	}

}