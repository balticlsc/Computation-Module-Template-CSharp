using System.Collections.Generic;
using ComputationModule.Messages;

namespace ComputationModule.BalticLSC {
	public class JobRegistryImpl : JobRegistry {

		private Dictionary<string,List<InputTokenMessage>> _tokens;
		private Dictionary<string, object> _variables;
		private JobStatus _status;

		public JobRegistryImpl(){

		}

		~JobRegistryImpl(){

		}

		/// 
		/// <param name="pinName"></param>
		public Status GetPinStatus(string pinName){

			return Status.Idle;
		}

		/// 
		/// <param name="pinName"></param>
		public object GetPinValues(string pinName){

			return null;
		}

		/// 
		/// <param name="pinName"></param>
		public List<InputTokenMessage> GetPinTokens(string pinName){

			return null;
		}

		/// 
		/// <param name="progress"></param>
		public void SetProgress(long progress){

		}

		public long GetProgress(){

			return 0;
		}

		/// 
		/// <param name="status"></param>
		public void SetStatus(Status status){

		}

		/// 
		/// <param name="name"></param>
		/// <param name="value"></param>
		public void SetVariable(string name, object value){

		}

		/// 
		/// <param name="name"></param>
		public object GetVariable(string name){

			return null;
		}

	}

}