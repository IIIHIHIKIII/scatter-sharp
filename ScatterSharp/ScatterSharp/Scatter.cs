﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;

namespace ScatterSharp
{
    public class Scatter : IDisposable
    {
        private readonly string WSURI = "ws://{0}/socket.io/?EIO=3&transport=websocket";
        private readonly string SCATTER_API_PREAMBLE = "42/scatter";
        private ClientWebSocket Socket { get; set; }
        private Dictionary<string, TaskCompletionSource<string>> OpenTasks { get; set; }
        private Task ReceiverTask { get; set; }

        public Scatter()
        {
            Socket = new ClientWebSocket();
            OpenTasks = new Dictionary<string, TaskCompletionSource<string>>();
        }

        public void Dispose()
        {
            Socket.Dispose();
        }

        #region Api

        public async Task Connect(string host, CancellationToken cancellationToken)
        {
            if (Socket.State != WebSocketState.Open && Socket.State != WebSocketState.Connecting)
            {
                await Socket.ConnectAsync(new Uri(string.Format(WSURI, host)), cancellationToken);
            }

            if (Socket.State == WebSocketState.Open)
                await Socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("40/scatter")), WebSocketMessageType.Text, true, cancellationToken);
            else
                throw new Exception("Socket closed.");

            var receiverTask = Receive();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
            //return Socket.disconnect();
        }

        public bool IsConnected()
        {
            throw new NotImplementedException();
            //return SocketService.isConnected();
        }

        public bool IsPaired()
        {
            throw new NotImplementedException();
            //return SocketService.isPaired();
        }

        public string GetVersion()
        {
            throw new NotImplementedException();
            //return SocketService.sendApiRequest({
            //    type: 'getVersion',
            //payload: { }
            //});
        }

        public void GetIdentity(/*requiredFields*/)
        {
            throw new NotImplementedException();
            //throwNoAuth();
            //return SocketService.sendApiRequest({
            //    type: 'getOrRequestIdentity',
            //payload:
            //    {
            //        fields: requiredFields
            //}
            //}).then(id => {
            //    if (id) this.identity = id;
            //    return id;
            //});
        }

        public void GetIdentityFromPermissions()
        {
            throw new NotImplementedException();
            //throwNoAuth();
            //return SocketService.sendApiRequest({
            //    type: 'identityFromPermissions',
            //payload: { }
            //}).then(id => {
            //    if (id) this.identity = id;
            //    return id;
            //});
        }

        public void ForgetIdentity()
        {
            throw new NotImplementedException();
            //throwNoAuth();
            //return SocketService.sendApiRequest({
            //    type: 'forgetIdentity',
            //payload: { }
            //}).then(res => {
            //    this.identity = null;
            //    return res;
            //});
        }

        public void Authenticate(/*nonce*/)
        {
            throw new NotImplementedException();
            //throwNoAuth();
            //return SocketService.sendApiRequest({
            //    type: 'authenticate',
            //payload: { nonce }
            //});
        }

        public void GetArbitrarySignature(/*publicKey, data, whatfor = '', isHash = false*/)
        {
            throw new NotImplementedException();
            //throwNoAuth();
            //return SocketService.sendApiRequest({
            //    type: 'requestArbitrarySignature',
            //payload:
            //    {
            //        publicKey,
            //    data,
            //    whatfor,
            //    isHash
            //}
            //});
        }

        public void GetPublicKey(/*blockchain*/)
        {
            throw new NotImplementedException();
            //throwNoAuth();
            //return SocketService.sendApiRequest({
            //    type: 'getPublicKey',
            //payload: { blockchain }
            //});
        }

        public void LinkAccount(/*publicKey, network*/)
        {
            throw new NotImplementedException();
            //throwNoAuth();
            //return SocketService.sendApiRequest({
            //    type: 'linkAccount',
            //payload: { publicKey, network }
            //});
        }

        public void HasAccountFor(/*network*/)
        {
            throw new NotImplementedException();
            //throwNoAuth();
            //return SocketService.sendApiRequest({
            //    type: 'hasAccountFor',
            //payload:
            //    {
            //        network
            //}
            //});
        }

        public void SuggestNetwork(/*network*/)
        {
            throw new NotImplementedException();
            //throwNoAuth();
            //return SocketService.sendApiRequest({
            //    type: 'requestAddNetwork',
            //payload:
            //    {
            //        network
            //}
            //});
        }

        public void RequestTransfer(/*network, to, amount, options = { }*/)
        {
            throw new NotImplementedException();
            //const payload = { network, to, amount, options };
            //return SocketService.sendApiRequest({
            //    type:'requestTransfer',
            //    payload
            //});
        }

        public void RequestSignature(/*payload*/)
        {
            throw new NotImplementedException();
            //throwNoAuth();
            //return SocketService.sendApiRequest({
            //    type: 'requestSignature',
            //        payload
            //    });
        }

        public void CreateTransaction(/*blockchain, actions, account, network*/)
        {
            throw new NotImplementedException();
            //throwNoAuth();
            //return SocketService.sendApiRequest({
            //    type: 'createTransaction',
            //        payload:
            //    {
            //        blockchain,
            //            actions,
            //            account,
            //            network
            //        }
            //});
        }

        #endregion

        #region Utils

        private async Task Receive()
        {
            byte[] frame = new byte[4096];
            byte[] preamble = new byte[SCATTER_API_PREAMBLE.Length];
            ArraySegment<byte> segment = new ArraySegment<byte>(frame, 0, frame.Length);
            ScatterApiResponse apiResponse = null;

            while (Socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                MemoryStream ms = null;

                do
                {
                    result = await Socket.ReceiveAsync(segment, CancellationToken.None);

                    if (ms == null)
                        ms = new MemoryStream();

                    ms.Write(segment.Array, segment.Offset, result.Count);
                }
                while (result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close response received", CancellationToken.None);
                    continue;
                }

                ms.Read(preamble, 0, preamble.Length);

                // Disregarding Handshaking/Upgrading
                if (Encoding.UTF8.GetString(preamble) != SCATTER_API_PREAMBLE)
                    continue;

                //skip , from preamble
                ms.Read(preamble, 0, 1);

                using (var sr = new StreamReader(ms))
                using (var jtr = new JsonTextReader(sr))
                {
                    apiResponse = JsonSerializer.Create().Deserialize<ScatterApiResponse>(jtr);
                }

                ms.Dispose();

                switch (apiResponse.Type)
                {
                    case "paired":
                        HandlePairedResponse(apiResponse.Data);
                        break;
                    case "rekey":
                        HandleRekeyResponse();
                        break;
                    case "api":
                        HandleApiResponse(apiResponse.Data);
                        break;
                }
            }
        }

        private void HandleApiResponse(object data)
        {
            throw new NotImplementedException();
            //const openRequest = openRequests.find(x => x.id === response.id);
            //if (!openRequest) return;

            //openRequests = openRequests.filter(x => x.id !== response.id);

            //const isErrorResponse = typeof response.result === 'object'
            //    && response.result !== null
            //    && response.result.hasOwnProperty('isError');

            //if (isErrorResponse) openRequest.reject(response.result);
            //else openRequest.resolve(response.result);
        }

        private void HandleRekeyResponse()
        {
            throw new NotImplementedException();
            //appkey = 'appkey:' + random();

            //const json = {
            //plugin:'YOUR_APP',
            //data:
            //    {
            //        origin: 'YOUR_APP',
            //    appkey
            //}
            //};

            //socket.send('42/scatter,' + JSON.stringify(['rekeyed', json]);
        }

        private void HandlePairedResponse(object data)
        {
            throw new NotImplementedException();
            //// You should have a state variable on
            //// your socket class that knows the "paired" state.
            //paired = result;

            //if (paired)
            //{
            //    const savedKey = getStoredHashedAppkey();
            //    const hashed = appkey.indexOf('appkey:') > -1 ? sha256(appkey) : appkey;

            //    if (!savedKey || savedKey !== hashed)
            //    {
            //        setStoredHashedAppkey(hashed);
            //        appkey = hashed;
            //    }
            //}
        }

        #endregion
    }
}
