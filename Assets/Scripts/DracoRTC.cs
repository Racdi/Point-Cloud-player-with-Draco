using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;
using NativeWebSocket;
using Draco;

public class DracoRTC : MonoBehaviour
{
    private Mesh currentMesh;
    private DracoToParticles particlesScript;

#pragma warning disable 0649
    [SerializeField] private Button callButton;
    [SerializeField] private Button hangupButton;
#pragma warning restore 0649

    private byte[] receivedFrame;

    private RTCPeerConnection pc1, pc2;
    private RTCDataChannel dataChannel, remoteDataChannel;

    private DelegateOnIceConnectionChange pc1OnIceConnectionChange;
    private DelegateOnIceConnectionChange pc2OnIceConnectionChange;
    private DelegateOnIceCandidate pc1OnIceCandidate;
    private DelegateOnIceCandidate pc2OnIceCandidate;
    private DelegateOnMessage onDataChannelMessage;
    private DelegateOnOpen onDataChannelOpen;
    private DelegateOnClose onDataChannelClose;
    private DelegateOnDataChannel onDataChannel;

    private WebSocket websocket;

    private void Awake()
    {
        callButton.onClick.AddListener(() => { StartCoroutine(Call()); });
        hangupButton.onClick.AddListener(() => { Hangup(); });
    }

    private async void Start()
    {
        callButton.interactable = true;
        hangupButton.interactable = false;

        websocket = new WebSocket("ws://10.1.2.159:8080");

        websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            HandleSignalingMessage(message);
        };

        pc1OnIceConnectionChange = state => { OnIceConnectionChange(pc1, state); };
        pc2OnIceConnectionChange = state => { OnIceConnectionChange(pc2, state); };
        pc1OnIceCandidate = candidate => { OnIceCandidate(pc1, candidate); };
        pc2OnIceCandidate = candidate => { OnIceCandidate(pc2, candidate); };
        onDataChannel = channel =>
        {
            Debug.Log("OnDataChannel");
            remoteDataChannel = channel;
            remoteDataChannel.OnMessage = onDataChannelMessage;
        };
        onDataChannelMessage = async bytes =>
        {
            Debug.Log("OnDataCHannelMessage");
            receivedFrame = bytes;
            var meshDataArray = Mesh.AllocateWritableMeshData(1);
            var result = await DracoDecoder.DecodeMesh(meshDataArray[0], receivedFrame);
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, currentMesh);
            var verticesList = new List<Vector3>(currentMesh.vertices);
            var colorsList = new List<Color32>(currentMesh.colors32);
            await particlesScript.Set(verticesList, colorsList);
        };
        onDataChannelOpen = () =>
        {
            hangupButton.interactable = true;
        };
        onDataChannelClose = () =>
        {
            hangupButton.interactable = false;
        };

        await websocket.Connect();
    }

    private void OnEnable()
    {
        currentMesh = new Mesh();
        particlesScript = gameObject.GetComponent<DracoToParticles>();
    }

    RTCConfiguration GetSelectedSdpSemantics()
    {
        RTCConfiguration config = default;
        config.iceServers = new RTCIceServer[]
        {
            new RTCIceServer { urls = new string[] { "stun:stun1.l.google.com:19302" } }
        };
        return config;
    }

    void OnIceConnectionChange(RTCPeerConnection pc, RTCIceConnectionState state)
    {
        Debug.Log($"{GetName(pc)} IceConnectionState: {state}");
    }

    void OnIceCandidate(RTCPeerConnection pc, RTCIceCandidate candidate)
    {
        if (candidate != null)
        {
            Debug.Log($"{GetName(pc)} ICE candidate: {candidate.Candidate}");
            string candidateMessage = $"ice:candidate\n{candidate.Candidate}\nsdpMid:{candidate.SdpMid}\nsdpMLineIndex:{candidate.SdpMLineIndex}";
            SendSignalingMessage(candidateMessage);
        }
        else
        {
            Debug.Log("ICE candidate is null!");
        }
    }

    string GetName(RTCPeerConnection pc)
    {
        return (pc == pc1) ? "pc1" : "pc2";
    }

    RTCPeerConnection GetOtherPc(RTCPeerConnection pc)
    {
        return (pc == pc1) ? pc2 : pc1;
    }

    IEnumerator Call()
    {
        callButton.interactable = false;
        var configuration = GetSelectedSdpSemantics();

        pc1 = new RTCPeerConnection(ref configuration);
        pc1.OnIceCandidate = pc1OnIceCandidate;
        pc1.OnIceConnectionChange = pc1OnIceConnectionChange;

        pc2 = new RTCPeerConnection(ref configuration);
        pc2.OnIceCandidate = pc2OnIceCandidate;
        pc2.OnIceConnectionChange = pc2OnIceConnectionChange;
        pc2.OnDataChannel = onDataChannel;

        RTCDataChannelInit conf = new RTCDataChannelInit();
        dataChannel = pc1.CreateDataChannel("data", conf);
        dataChannel.OnOpen = onDataChannelOpen;

        var op = pc1.CreateOffer();
        yield return op;

        if (!op.IsError)
        {
            yield return StartCoroutine(OnCreateOfferSuccess(op.Desc));
        }
        else
        {
            OnCreateSessionDescriptionError(op.Error);
        }
    }

    void Hangup()
    {
        pc1.Close();
        pc2.Close();
        pc1 = null;
        pc2 = null;
        receivedFrame = null;
        hangupButton.interactable = false;
        callButton.interactable = true;
    }

    IEnumerator OnCreateOfferSuccess(RTCSessionDescription desc)
    {
        var op = pc1.SetLocalDescription(ref desc);
        yield return op;

        if (!op.IsError)
        {
            OnSetLocalSuccess(pc1);

            // Send the offer SDP to the signaling server via WebSocket
            string offerMessage = $"sdp:offer\n{desc.sdp}\r\n";
            SendSignalingMessage(offerMessage);
        }
        else
        {
            Debug.LogError("Failed to set local description for offer: " + op.Error);
        }
    }

    IEnumerator OnCreateAnswerSuccess(RTCSessionDescription desc)
    {
        var op = pc2.SetLocalDescription(ref desc);
        yield return op;

        if (!op.IsError)
        {
            OnSetLocalSuccess(pc2);
        }

        var op2 = pc1.SetRemoteDescription(ref desc);
        yield return op2;

        if (!op2.IsError)
        {
            OnSetRemoteSuccess(pc1);
        }
    }

    void OnSetLocalSuccess(RTCPeerConnection pc)
    {
        Debug.Log($"{GetName(pc)} SetLocalDescription complete");
    }

    void OnSetRemoteSuccess(RTCPeerConnection pc)
    {
        Debug.Log($"{GetName(pc)} SetRemoteDescription complete");
    }

    void OnCreateSessionDescriptionError(RTCError error)
    {
        Debug.LogError("Error during SDP negotiation: " + error);
    }

    private async void SendSignalingMessage(string message)
    {
        Debug.Log("Sending message:" + message);
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(message);
        }
    }

    private void HandleSignalingMessage(string message)
    {
        Debug.Log("Received signaling message");
        string[] lines = message.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);

        if (lines[0].StartsWith("sdp:"))
        {
            string sdpType = lines[0].Substring(4);
            string sdp = string.Join("\n", lines, 1, lines.Length - 1);

            if (sdpType == "offer")
            {
                Debug.Log("Received offer");
                StartCoroutine(OnReceivedOffer(sdp));
            }
            else if (sdpType == "answer")
            {
                Debug.Log("Received answer");
                StartCoroutine(OnReceivedAnswer(sdp));
            }
        }
        else if (lines[0].StartsWith("ice:"))
        {
            Debug.Log("Received ice");
            string candidateStr = string.Join("\n", lines, 1, lines.Length - 1);
            RTCIceCandidateInit initStr = new RTCIceCandidateInit();
            initStr.candidate = candidateStr;
            RTCIceCandidate candidate = new RTCIceCandidate(initStr);
            GetOtherPc(pc1).AddIceCandidate(candidate);
        }
    }

    private IEnumerator OnReceivedOffer(string sdp)
    {
        RTCSessionDescription offer = new RTCSessionDescription();
        offer.type = RTCSdpType.Offer;
        offer.sdp = sdp;

        var op = pc2.SetRemoteDescription(ref offer);
        yield return op;

        if (!op.IsError)
        {
            OnSetRemoteSuccess(pc2);
        }

        var answerOp = pc2.CreateAnswer();
        yield return answerOp;

        if (!answerOp.IsError)
        {
            yield return OnCreateAnswerSuccess(answerOp.Desc);
            SendSignalingMessage($"sdp:answer\n{answerOp.Desc.sdp}");
        }
    }

    private IEnumerator OnReceivedAnswer(string sdp)
    {
        RTCSessionDescription answer = new RTCSessionDescription();
        answer.type = RTCSdpType.Answer;
        answer.sdp = sdp;

        var op = pc1.SetRemoteDescription(ref answer);
        yield return op;

        if (!op.IsError)
        {
            OnSetRemoteSuccess(pc1);
        }
    }

    private void OnApplicationQuit()
    {
        if (websocket != null)
        {
            websocket.Close();
        }
    }
}
