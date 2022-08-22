using DigitalMakerApi.Responses;

namespace DigitalMakerApi
{
    public class RootResponse
    {
        public RootResponse(IResponse response, string clientId)
        {
            Response = response;
            ClientId = clientId;
        }

        public IResponse Response { get; }

        public string ClientId { get; }
    }
}
