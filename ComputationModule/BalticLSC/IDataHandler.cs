using System.Collections.Generic;

namespace ComputationModule.BalticLSC {
	public interface IDataHandler  {

		/// 
		/// <param name="pinName"></param>
		string ObtainDataItem(string pinName);
		
		/// 
		/// <param name="pinName"></param>
		List<string> ObtainDataItems(string pinName);
		
		/// 
		/// <param name="pinName"></param>
		(List<string>, long[]) ObtainDataItemsNDim(string pinName);

		/// 
		/// <param name="pinName"></param>
		/// <param name="data"></param>
		/// <param name="isFinal"></param>
		/// <param name="msgUid"></param>
		short SendDataItem(string pinName, string data, bool isFinal, string msgUid = null);

		/// 
		/// <param name="pinName"></param>
		/// <param name="values"></param>
		/// <param name="isFinal"></param>
		/// <param name="msgUid"></param>
		short SendToken(string pinName, string values, bool isFinal, string msgUid = null);

		short FinishProcessing();

		/// 
		/// <param name="msgUids"></param>
		/// <param name="isFinal"></param>
		short SendAckToken(List<string> msgUids, bool isFinal);
	}

}