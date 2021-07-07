using System.Collections.Generic;

namespace ComputationModule.BalticLSC
{
	public class DataHandlerImpl : DataHandler
	{

		private Dictionary<string, DataHandle> _dataHandles;
		private TokensProxy _tokensProxy;
		private JobRegistry _registry;

		public DataHandlerImpl()
		{

		}

		/// 
		/// <param name="registry"></param>
		public DataHandlerImpl(JobRegistry registry)
		{

		}

		/// 
		/// <param name="pinName"></param>
		public object ObtainData(string pinName)
		{

			return null;
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