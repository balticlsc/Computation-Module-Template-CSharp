using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComputationModule.BalticLSC;
using ComputationModule.Messages;
using ComputationModule.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;

namespace ComputationModule.Controllers
{
    [ApiController]
    [Route("/")]
    public class JobController : ControllerBase
    {
        private Status _status;
        private long _progress;
        private List<PinConfiguration> _pins;
        private List<JobTask> _jobTasks;
        
        public JobController(IConfiguration config)
        {
            try
            {
                _pins = ConfigurationHandle.GetPinsConfiguration(config);
            }
            catch (Exception)
            {
                Log.Error("Error while parsing configuration.");
            }

            _jobTasks = new List<JobTask>();
        }

        [HttpPost]
        [Route("token")]
        public IActionResult ProcessTokenMessage([FromBody] object value)
        {
            if (_status != Status.Failed)
            {
                try
                {
                    var inputToken = JsonConvert.DeserializeObject<InputTokenMessage>(value.ToString() ?? "");
                    var tokens = new TokensProxy(inputToken.MsgUid, inputToken.PinName);
                    try
                    {
                        var jobTask = new JobTask(_pins.FirstOrDefault(x => x.PinName == inputToken.PinName),
                            tokens, inputToken.Values);
                        var result = jobTask.DataStoreProxy.CheckDataConnections();
                        _jobTasks.Add(jobTask);
                        var task = new Task(() => jobTask.StartDataProcessing());

                        switch (result)
                        {
                            case 0:
                                task.Start();
                                return Ok();
                            case 1:
                                tokens.SendAckToken(true, $"Data store proxy no response.");
                                return NotFound("no-response");
                            case 2:
                                tokens.SendAckToken(true, $"Data store proxy unauthorized.");
                                return Ok("ok-bad-dataset");
                            case 3:
                                tokens.SendAckToken(true, $"Data store proxy invalid path: {inputToken.Values}.");
                                return Ok("ok-bad-dataset");
                        }

                        return BadRequest();
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error of type {e.GetType()}: {e.Message}\n{e.StackTrace}");
                        tokens.SendAckToken(true, e.Message);
                        return Ok(e);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Corrupted token: {e.Message}");
                    return BadRequest(e);
                }
            }

            return StatusCode(503);
        }

        [HttpGet]
        [Route("/status")]
        public IActionResult GetStatus()
        {
            //Update overall job progress and job status based on status and progress of each job task
            var jobStatus = new JobStatus
            {
                JobInstanceUid = Environment.GetEnvironmentVariable("SYS_MODULE_INSTANCE_UID"),
                JobProgress = _progress,
                Status = _status
            };

            return Ok(jobStatus);
        }
    }
}