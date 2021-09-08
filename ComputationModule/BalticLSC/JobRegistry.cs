using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using ComputationModule.Messages;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace ComputationModule.BalticLSC {
	public class JobRegistry : IJobRegistry {

		private List<JobThread> _jobThreads;
		
		private Dictionary<string,List<InputTokenMessage>> _tokens;
		private Dictionary<string, object> _variables;
		private JobStatus _status;
		private List<PinConfiguration> _pins;
		
		private SemaphoreSlim _semaphore; 

		public JobRegistry(IConfiguration config){
			try
			{
				_pins = ConfigurationHandle.GetPinsConfiguration(config);
			}
			catch (Exception)
			{
				Log.Error("Error while parsing configuration.");
				throw;
			}

			_jobThreads = new List<JobThread>();
			_tokens = new Dictionary<string, List<InputTokenMessage>>();
			foreach (PinConfiguration pc in _pins.FindAll(p => "input" == p.PinType))
				_tokens[pc.PinName] = new List<InputTokenMessage>();
			_variables = new Dictionary<string, object>();
			_status = new JobStatus()
			{
				JobInstanceUid = Environment.GetEnvironmentVariable("SYS_MODULE_INSTANCE_UID")
			};
			_semaphore = new SemaphoreSlim(1,1);
		}

		public short RegisterThread(JobThread thread)
		{
			_semaphore.Wait();
			try
			{
				_jobThreads.Add(thread);
				return 0;
			}
			finally
			{
				_semaphore.Release();
			}
		}

		/// 
		/// <param name="pinName"></param>
		public Status GetPinStatus(string pinName)
		{
			_semaphore.Wait();
			try
			{
				if (0 == _tokens[pinName].Count)
					return Status.Idle;
				if (TokenMultiplicity.Single == GetPinConfigurationInternal(pinName).TokenMultiplicity)
					return Status.Completed;
				InputTokenMessage finalToken =
					_tokens[pinName].Find(t => !t.TokenSeqStack.ToList().Exists(s => !s.IsFinal));
				if (null != finalToken)
				{
					long maxCount = 1;
					foreach (SeqToken token in finalToken.TokenSeqStack)
						maxCount *= token.No + 1;
					if (_tokens[pinName].Count == maxCount)
						return Status.Completed;
				}

				return Status.Working;
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public string GetPinValue(string pinName)
		{
			List<string> values;
			long[] sizes;
			(values, sizes) = GetPinValuesNDim(pinName);
			if (null == values || 0 == values.Count)
				return null;
			if (null == sizes && 1 == values.Count)
				return values[0];
			throw new Exception("Improper call - more than one token exists for the pin");
		}

		public List<string> GetPinValues(string pinName)
		{
			List<string> values;
			long[] sizes;
			(values, sizes) = GetPinValuesNDim(pinName);
			if (1 == sizes?.Length)
				return values;
			throw new Exception("Improper call - more than one dimension exists for the pin");
		}

		/// 
		/// <param name="pinName"></param>
		public (List<string>, long[]) GetPinValuesNDim(string pinName)
		{
			_semaphore.Wait();
			try
			{
				if (0 == _tokens[pinName].Count)
					return (null, null);
				
				// Single token pin:

				if (TokenMultiplicity.Single == GetPinConfigurationInternal(pinName).TokenMultiplicity)
					return (new List<string>() {_tokens[pinName].FirstOrDefault().Values}, null);
				
				// Multiple token pin:
				
				long[] maxTableCounts = new long[_tokens[pinName].FirstOrDefault().TokenSeqStack.Count()];
				foreach (InputTokenMessage message in _tokens[pinName])
					for (int i = 0; i < message.TokenSeqStack.Count(); i++)
						if (maxTableCounts[i] < message.TokenSeqStack.ToList()[i].No)
							maxTableCounts[i] = message.TokenSeqStack.ToList()[i].No;
				
				long allTokenCount = 1;
				foreach (long index in maxTableCounts)
					allTokenCount *= index + 1;

				string[] result = new string[allTokenCount];

				foreach (InputTokenMessage message in _tokens[pinName])
				{
					long index = 0;
					long product = 1;
					for (int i = 0; i < message.TokenSeqStack.Count(); i++)
					{
						index += maxTableCounts[i] * product;
						product *= maxTableCounts[i];
					}

					result[index] = message.Values;
				}

				return (result.ToList(), maxTableCounts);
			}
			finally
			{
				_semaphore.Release();
			}
		}

		/// 
		/// <param name="pinName"></param>
		public List<InputTokenMessage> GetPinTokens(string pinName)
		{
			_semaphore.Wait();
			try
			{
				return _tokens[pinName];
			}
			finally
			{
				_semaphore.Release();
			}
		}

		/// 
		/// <param name="progress"></param>
		public void SetProgress(long progress)
		{
			_semaphore.Wait();
			try
			{
				_status.JobProgress = progress;
				if (0 <= progress && Status.Completed != _status.Status)
					_status.Status = Status.Working;
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public long GetProgress()
		{
			_semaphore.Wait();
			try
			{
				return _status.JobProgress;
			}
			finally
			{
				_semaphore.Release();
			}
		}

		/// 
		/// <param name="status"></param>
		public void SetStatus(Status status)
		{
			_semaphore.Wait();
			try
			{
				_status.Status = status;
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public JobStatus GetJobStatus()
		{
			_semaphore.Wait();
			try
			{
				return _status;
			}
			finally
			{
				_semaphore.Release();
			}
		}

		/// 
		/// <param name="name"></param>
		/// <param name="value"></param>
		public void SetVariable(string name, object value)
		{
			_semaphore.Wait();
			try
			{
				_variables[name] = value;
			}
			finally
			{
				_semaphore.Release();
			}
		}

		/// 
		/// <param name="name"></param>
		public object GetVariable(string name)
		{
			_semaphore.Wait();
			try
			{
				return _variables[name];
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public string GetEnvironmentVariable(string name)
		{
			return Environment.GetEnvironmentVariable(name);
		}

		public PinConfiguration GetPinConfiguration(string pinName)
		{
			_semaphore.Wait();
			try
			{
				return GetPinConfigurationInternal(pinName);
			}
			finally
			{
				_semaphore.Release();
			}
		}
		
		private PinConfiguration GetPinConfigurationInternal(string pinName)
		{
			return _pins.Find(x => x.PinName == pinName);
		}

		public List<string> GetStrongPinNames()
		{
			_semaphore.Wait();
			try
			{
				return _pins.FindAll(p => "true" == p.IsRequired).Select(p => p.PinName).ToList();
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public string GetBaseMsgUid()
		{
			_semaphore.Wait();
			try
			{
				return _tokens.Values.ToList().Find(ltm => 0 != ltm.Count)?.FirstOrDefault()?.MsgUid;
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public List<string> GetAllMsgUids()
		{
			_semaphore.Wait();
			try
			{
				return _tokens.Values.ToList().SelectMany(it => it).
					Select(it => it.MsgUid).ToList();
			}
			finally
			{
				_semaphore.Release();
			}
		}

		public void ClearMessages(List<string> msgUids)
		{
			foreach (string msgUid in msgUids)
			{
				List<InputTokenMessage> tokens = _tokens.Values.ToList().
					Find(l => l.Exists(it => msgUid == it.MsgUid));
				if (null != tokens)
				{
					InputTokenMessage message = tokens.Find(it => msgUid == it.MsgUid);
					tokens.Remove(message);
				}
			}
		}

		public void RegisterToken(InputTokenMessage msg)
		{
			_tokens[msg.PinName].Add(msg);
		}

	}
}