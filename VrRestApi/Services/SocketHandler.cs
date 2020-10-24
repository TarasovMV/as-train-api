using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using VrRestApi.Models;

namespace VrRestApi.Services
{
    public class SocketHandler
    {
        public Dictionary<string, string> vrDevices = new Dictionary<string, string>();
        public Dictionary<string, int> vrPano = new Dictionary<string, int>();

        public SocketHandler()
        {
        }

        public void SetVrPanoValue(string key, int value)
        {
            if (vrPano.ContainsKey(key))
            {
                vrPano.Remove(key);
            }
            vrPano.Add(key, value);
        }

        public string GetContextByDevice(string deviceId)
        {
            try
            {
                return vrDevices.First(x => x.Value == deviceId).Key;
            } catch
            {
                return null;
            }
            
        }

        public string JsonVrDevicesList()
        {
            if (vrDevices.Count == 0)
            {
                return null;
            }
            var obj = new JsonContainer<List<string>>(new List<string>(vrDevices.Values));
            return JsonConvert.SerializeObject(obj);
        }
    }
}
