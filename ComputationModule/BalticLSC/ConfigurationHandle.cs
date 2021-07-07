using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ComputationModule.BalticLSC
{
    public class ConfigurationHandle
    {
        public static List<PinConfiguration> GetPinsConfiguration(IConfiguration configuration)
        {
            var pins = new List<PinConfiguration>();
            var pinsSections = configuration.GetSection("Pins").GetChildren().ToList();
            foreach (var configurationSection in pinsSections)
            {
                pins.Add(new PinConfiguration(configurationSection));
            }

            return pins;
        }
    }
}