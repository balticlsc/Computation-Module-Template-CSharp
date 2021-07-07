namespace ComputationModule.BalticLSC {
	public class DataHandle {

		public DataHandle(){

		}

		public short CheckConnection(){

			return 0;
		}

		public (string, bool) Download(){

			return (null, true);
		}

		/// 
		/// <param name="localPath"></param>
		public bool Upload(string localPath){

			return false;
		}

	}

}