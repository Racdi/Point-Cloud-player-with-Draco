using Unity.WebRTC;

namespace WebRTCTutorial.DTO
{
    [System.Serializable]
    public class DTOice
    {
        public string Candidate;
        public string SdpMid;
        public int? SdpMLineIndex;
    }
}