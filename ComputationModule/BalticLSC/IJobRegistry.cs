using System.Collections.Generic;
using ComputationModule.Messages;

namespace ComputationModule.BalticLSC {
	public interface IJobRegistry  {

		/// 
		/// <param name="pinName"></param>
		Status GetPinStatus(string pinName);

		/// 
		/// <param name="pinName"></param>
		string GetPinValue(string pinName);
		
		/// 
		/// <param name="pinName"></param>
		List<string> GetPinValues(string pinName);
		
		/// 
		/// <param name="pinName"></param>
		(List<string>, long[]) GetPinValuesNDim(string pinName);

		/// 
		/// <param name="pinName"></param>
		List<InputTokenMessage> GetPinTokens(string pinName);

		/// 
		/// <param name="progress"></param>
		void SetProgress(long progress);

		long GetProgress();

		/// 
		/// <param name="status"></param>
		void SetStatus(Status status);

		/// 
		/// <param name="name"></param>
		/// <param name="value"></param>
		void SetVariable(string name, object value);

		/// 
		/// <param name="name"></param>
		object GetVariable(string name);
	}

}