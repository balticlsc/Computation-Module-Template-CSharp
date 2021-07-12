using System;
using ComputationModule.Messages;

namespace ComputationModule.BalticLSC {
	public class JobThread
	{

		private string _pinName;
		private TokenListener _listener;
		private JobRegistry _registry;
		private DataHandler _handler;

		public JobThread(string pinName, TokenListener listener, JobRegistry registry, DataHandler handler)
		{
			_pinName = pinName;
			_listener = listener;
			_registry = registry;
			_handler = handler;
		}

		public void Run(){
			try
			{
				_listener.DataReceived(_pinName);
				if ("true" == _registry.GetPinConfiguration(_pinName).IsRequired)
					_listener.OptionalDataReceived(_pinName);
				Status pinAggregatedStatus = Status.Completed;
				foreach (string pinName in _registry.GetStrongPinNames())
				{
					Status pinStatus = _registry.GetPinStatus(pinName);
					if (Status.Working == pinStatus)
						pinAggregatedStatus = Status.Working;
					else if (Status.Idle == pinStatus)
					{
						pinAggregatedStatus = Status.Idle;
						break;
					}
				}

				if (Status.Idle != pinAggregatedStatus)
					_listener.DataReady();
				if (Status.Completed == pinAggregatedStatus)
					_listener.DataComplete();
			}
			catch (Exception e)
			{
				_handler.FailProcessing(e.Message);
			}
		}
	}
}