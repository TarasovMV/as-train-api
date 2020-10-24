using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using VrRestApi.Models;

namespace VrRestApi.Services
{
    public class SocketHub: Hub
    {
        SocketHandler socketHandler;

        public SocketHub(SocketHandler socketHandler)
        {
            this.socketHandler = socketHandler;
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.All.SendAsync("Send", $"{Context.ConnectionId} вошел в чат");
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            socketHandler.vrDevices.Remove(Context.ConnectionId);
            await GetVrDevices();
            await Clients.All.SendAsync("Send", $"{Context.ConnectionId} покинул в чат");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task Send(string message)
        {
            await this.Clients.All.SendAsync("Send", message);
        }

        public async Task SetDeviceId(string deviceId)
        {
            socketHandler.vrDevices.Add(Context.ConnectionId, deviceId);
            await this.Clients.All.SendAsync("Send", $"{deviceId} add to vr devices");
            //await VrDeviceConnect(deviceId);
            await GetVrDevices();
        }

        public async Task SetExperienceVrState(string connectionId, string message)
        {

            await this.Clients.Client(connectionId).SendAsync("SetExperienceVrState", message);
        }

        public async Task SetActivePano(string deviceId, int panoIdx)
        {
            socketHandler.SetVrPanoValue(deviceId, panoIdx);
            var connectionId = socketHandler.GetContextByDevice(deviceId);
            if (connectionId == null)
            {
                return;
            }
            await Clients.Client(connectionId).SendAsync("PanoActiveHub", panoIdx);
        }

        public async Task GetVrDevices()
        {
            var devices = socketHandler.JsonVrDevicesList();
            if (devices == null)
            {
                return;
            }
            await this.Clients.All.SendAsync("VrDevicesHub", devices);
        }

        // TODO: remove
        public async Task VrDeviceConnect(string deviceId)
        {
            int? panoIdx = socketHandler.vrPano.GetValueOrDefault(deviceId);
            int idx = panoIdx ?? -1;
            if (idx == -1)
            {
                var connectionId = socketHandler.GetContextByDevice(deviceId);
                if (connectionId == null)
                {
                    return;
                }
                await Clients.Client(connectionId).SendAsync("PanoMenu");
                return;
            }
            await SetActivePano(deviceId, idx);
        }

        public async Task ClosePano(string deviceId)
        {
            //socketHandler.vrPano.Remove(deviceId);
            var connectionId = socketHandler.GetContextByDevice(deviceId);
            if (connectionId == null)
                return;
            await Clients.Client(connectionId).SendAsync("ClosePano");
        }

        // From PC
        public async Task StartVrHeadset(string deviceId, string msg)
        {
            var connectionId = socketHandler.GetContextByDevice(deviceId);
            if (connectionId == null)
            {
                return;
            }
            var sourceId = Context.ConnectionId;
            await Clients.Client(connectionId).SendAsync("StartVrHeadset", sourceId, msg);
        }

        // From PC
        public async Task CloseVr(string deviceId, string msg)
        {
            var connectionId = socketHandler.GetContextByDevice(deviceId);
            if (connectionId == null)
            {
                return;
            }
            await Clients.Client(connectionId).SendAsync("CloseVr", msg);
        }

        // From Mobile
        public async Task SendResult(string connectionId, string code)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
                return;
            await Clients.Client(connectionId).SendAsync("SendResult", code);
        }

        // From Mobile
        public async Task StartVrView(string connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
                return;
            await Clients.Client(connectionId).SendAsync("StartVrView");
        }
    }
}
