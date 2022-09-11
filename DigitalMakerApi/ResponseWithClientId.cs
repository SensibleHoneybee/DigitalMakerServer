using DigitalMakerApi.Responses;

namespace DigitalMakerApi
{
    public class ResponseWithClientId
    {
        public ResponseWithClientId(IResponse response, string clientId)
        {
            Response = response;
            ClientId = clientId;
        }

        public IResponse Response { get; }

        public string ClientId { get; }
    }
}
