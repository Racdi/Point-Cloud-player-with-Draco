using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.WebRTC;
using UnityEngine;
using Draco;
using WebSocketSharp;
using Newtonsoft.Json;
using WebRTCTutorial.DTO;

namespace WebRTCTutorial
{
    public class WebRTCCloudManager : MonoBehaviour
    {
        private WebRTCDataTransfer _webSocketClient;
        private RTCPeerConnection _peerConnection;

        private Mesh currentMesh;
        public DracoToParticles particlesScript;
        private List<byte[]> receivedFrame;

        public bool CanConnect =>
            _peerConnection?.ConnectionState == RTCPeerConnectionState.New || 
            _peerConnection?.ConnectionState == RTCPeerConnectionState.Disconnected;

        public bool IsConnected => _peerConnection?.ConnectionState == RTCPeerConnectionState.Connecting;

        private RTCDataChannel dataChannel, remoteDataChannel;

        private DelegateOnMessage onDataChannelMessage;
        private DelegateOnDataChannel onDataChannel;

        private void OnNegotiationNeeded()
        {
            Debug.Log("SDP Offer <-> Answer exchange requested by the webRTC client.");
            StartCoroutine(CreateAndSendLocalSdpOffer());
        }

        private void SendMessageToOtherPeer<TType>(TType obj, DtoType type)
        {
            try
            {
                var serializedPayload = JsonConvert.SerializeObject(obj); ;

                var dtoWrapper = new DTOWrapper
                {
                    Type = (int)type,
                    Payload = serializedPayload
                };
                var serializedDto = JsonConvert.SerializeObject(dtoWrapper); ;
                _webSocketClient.SendWebSocketMessage(serializedDto);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void SendIceCandidateToOtherPeer(RTCIceCandidate iceCandidate)
        {
            var iceDto = new DTOice
            {
                Candidate = iceCandidate.Candidate,
                SdpMid = iceCandidate.SdpMid,
                SdpMLineIndex = iceCandidate.SdpMLineIndex
            };
            SendMessageToOtherPeer(iceDto, DtoType.ICE);
        }

        private void SendSdpToOtherPeer(RTCSessionDescription sdp)
        {
            var sdpDto = new DTOsdp
            {
                Type = (int)sdp.type,
                Sdp = sdp.sdp
            };
            SendMessageToOtherPeer(sdpDto, DtoType.SDP);
        }

        private void OnWebSocketMessageReceived(string message)
        {
            var dtoWrapper = JsonConvert.DeserializeObject<DTOWrapper>(message);
            switch ((DtoType)dtoWrapper.Type)
            {
                case DtoType.ICE:
                    var iceDto = JsonConvert.DeserializeObject<DTOice>(dtoWrapper.Payload);

                    var ice = new RTCIceCandidate(new RTCIceCandidateInit
                    {
                        candidate = iceDto.Candidate,
                        sdpMid = iceDto.SdpMid,
                        sdpMLineIndex = iceDto.SdpMLineIndex
                    });

                    _peerConnection.AddIceCandidate(ice);
                    Debug.Log($"Received ICE Candidate: {ice.Candidate}");
                    break;
                case DtoType.SDP:
                    var sdpDto = JsonConvert.DeserializeObject<DTOsdp>(dtoWrapper.Payload);
                    var sdp = new RTCSessionDescription
                    {
                        type = (RTCSdpType)sdpDto.Type,
                        sdp = sdpDto.Sdp
                    };
                    Debug.Log($"Received SDP offer of type: {sdp.type} and SDP details: {sdp.sdp}");
                    switch (sdp.type)
                    {
                        case RTCSdpType.Offer:
                            //StartCoroutine(OnRemoteSdpOfferReceived(sdp));
                            break;
                        case RTCSdpType.Answer:
                            StartCoroutine(OnRemoteSdpAnswerReceived(sdp));
                            break;
                        case RTCSdpType.Pranswer:
                            StartCoroutine(OnRemoteSdpAnswerReceived(sdp));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("Unhandled type of SDP message: " + sdp.type);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IEnumerator CreateAndSendLocalSdpOffer()
        {
            // 1. Create local SDP offer
            var createOfferOperation = _peerConnection.CreateOffer();
            yield return createOfferOperation;

            if (createOfferOperation.IsError)
            {
                Debug.LogError("Failed to create offer");
                yield break;
            }
            var sdpOffer = createOfferOperation.Desc;

            // 2. Set the offer as a local SDP 
            var setLocalSdpOperation = _peerConnection.SetLocalDescription(ref sdpOffer);
            yield return setLocalSdpOperation;

            if (setLocalSdpOperation.IsError)
            {
                Debug.LogError("Failed to set local description");
                yield break;
            }
            // 3. Send the SDP Offer to the other Peer
            SendSdpToOtherPeer(sdpOffer);
            Debug.Log("Sent Sdp Offer");
        }

        private IEnumerator OnRemoteSdpAnswerReceived(RTCSessionDescription remoteSdpAnswer)
        {
            // 1. Set the received answer as remote description
            var setRemoteSdpOperation = _peerConnection.SetRemoteDescription(ref remoteSdpAnswer);
            yield return setRemoteSdpOperation;
            if (setRemoteSdpOperation.IsError)
            {
                Debug.LogError("Failed to set remote description");
            }
        }

        private void OnIceCandidate(RTCIceCandidate candidate)
        {
            Debug.Log("Found ICE candidate");
            SendIceCandidateToOtherPeer(candidate);
            Debug.Log("Sent ICE Candidate to the other peer THREAD " + Thread.CurrentThread.ManagedThreadId);
        }

    protected void Awake()
        {
            receivedFrame = new List<byte[]>();
            currentMesh = new Mesh();
            // FindObjectOfType is used for the demo purpose only. In a real production it's better to avoid it for performance reasons
            _webSocketClient = FindObjectOfType<WebRTCDataTransfer>();
            StartCoroutine(WebRTC.Update());

            var config = new RTCConfiguration
            {
                iceServers = new RTCIceServer[]
                {
                    new RTCIceServer
                    {
                        urls = new string[]
                        {
                        // Google Stun server
                        "stun:stun.l.google.com:19302"
                        },
                    }
                },
                iceTransportPolicy = RTCIceTransportPolicy.All
            };
            _peerConnection = new RTCPeerConnection(ref config);
            // "Negotiation" is the exchange of SDP Offer/Answer. Peers describe what media they want to send and agree on, for example, what codecs to use
            // In this tutorial we exchange the SDP Offer/Answer only once when connecting.
            // But in a real production you'd have to repeat the exchange every time the OnNegotiationNeeded event is triggered
            _peerConnection.OnNegotiationNeeded += OnNegotiationNeeded;
            // Triggered when a new network endpoint is found that could potentially be used to establish the connection
            _peerConnection.OnIceCandidate += OnIceCandidate;
            // Triggered when a new message is received from the other peer via WebSocket
            _webSocketClient.MessageReceived += OnWebSocketMessageReceived;
        }

        public void Connect()
        {
            dataChannel = _peerConnection.CreateDataChannel("LocalDataChannel", new RTCDataChannelInit());
            Debug.Log("Created local data channel:" + dataChannel.Id);
            _peerConnection.OnDataChannel = channel =>
            {
                Debug.Log("OnDataChannel:" + channel.Id);
                remoteDataChannel = channel;
                remoteDataChannel.OnMessage = onDataChannelMessage;
            };
            onDataChannelMessage = bytes =>
            {
                if (System.Text.Encoding.UTF8.GetString(bytes) == "EOF")
                {
                    Debug.Log("End of File received");
                    DecodeMessage(Combine(receivedFrame));
                    receivedFrame = new List<byte[]>();
                }
                else
                {
                    Debug.Log("Piece of PC received");
                    receivedFrame.Add(bytes);
                }
            };
        }

        private async void DecodeMessage(byte[] fullFrame)
        {
            var meshDataArray = Mesh.AllocateWritableMeshData(1);
            var result = await DracoDecoder.DecodeMesh(meshDataArray[0], fullFrame);
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, currentMesh);
            var verticesList = new List<Vector3>(currentMesh.vertices);
            var colorsList = new List<Color32>(currentMesh.colors32);
            await particlesScript.Set(verticesList, colorsList);
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                return;
            }
            _peerConnection.Close();
            _peerConnection.Dispose();
        }

        public void SendDebugMessage()
        {
            remoteDataChannel?.Send("Debug message on remote data channel");
        }

        private byte[] Combine(List<byte[]> arrays)
        {
            int size = 0;
            foreach (byte[] array in arrays)
            {
                size += array.Length;
            }
            byte[] rv = new byte[size];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
    }
}