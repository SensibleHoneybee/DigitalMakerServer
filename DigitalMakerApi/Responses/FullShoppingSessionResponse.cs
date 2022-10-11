using DigitalMakerApi.Models;
using Newtonsoft.Json;

namespace DigitalMakerApi.Responses
{
    public class FullShoppingSessionResponse : IResponse
    {
        public Instance Instance { get; set; } = null!;

        public ShoppingSession ShoppingSession { get; set; } = null!;

        [JsonIgnore]
        public string ResponseType => DigitalMakerResponseType.FullShoppingSession;
    }
}
