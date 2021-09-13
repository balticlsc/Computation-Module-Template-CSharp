using System.Collections.Generic;
using System.Linq;
using ComputationModule.DataModel;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ComputationModule.BalticLSC
{
    public static class ConfigurationHandle
    {
        public static List<PinConfiguration> GetPinsConfiguration(IConfiguration configuration)
        {
            var pinsSections = configuration.GetSection("Pins").GetChildren();
            return pinsSections.Select(configurationSection => new PinConfiguration(configurationSection)).ToList();
        }
    }
}