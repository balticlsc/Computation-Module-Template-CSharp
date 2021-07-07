using System.Collections.Generic;

namespace ComputationModule.BalticLSC {
	public interface DataHandler  {

		/// 
		/// <param name="pinName"></param>
		object ObtainData(string pinName);

		/// 
		/// <param name="pinName"></param>
		/// <param name="data"></param>
		short SendData(string pinName, object data);

		/// 
		/// <param name="pinName"></param>
		/// <param name="data"></param>
		/// <param name="isFinal"></param>
		short SendData(string pinName, object data, bool isFinal);

		/// 
		/// <param name="pinName"></param>
		/// <param name="data"></param>
		/// <param name="isFinal"></param>
		/// <param name="msgUid"></param>
		short SendData(string pinName, object data, bool isFinal, string msgUid);

		/// 
		/// <param name="pinName"></param>
		/// <param name="values"></param>
		/// <param name="isFinal"></param>
		short SendToken(string pinName, object values, bool isFinal);

		/// 
		/// <param name="pinName"></param>
		/// <param name="values"></param>
		/// <param name="isFinal"></param>
		/// <param name="msgUid"></param>
		short SendToken(string pinName, object values, bool isFinal, string msgUid);

		short FinishProcessing();

		/// 
		/// <param name="msgUids"></param>
		/// <param name="isFinal"></param>
		short SendAckToken(List<string> msgUids, bool isFinal);
	}

}