using DigitalMakerApi;
using DigitalMakerApi.Models;
using DigitalMakerApi.Responses;

namespace DigitalMakerServer
{
    public class OutputMessageProvider
    {
        public List<ResponseWithClientId> ConvertToResponseMessages(
            IEnumerable<OutputAction> outputActions,
            string shoppingSessionId,
            string shopperClientId,
            string instanceAdminClientId)
        {
            var result = new List<ResponseWithClientId>();

            foreach (var outputAction in outputActions)
            {
                if (string.Equals(outputAction.ActionName, OutputActionType.SendMessageToShopper, StringComparison.OrdinalIgnoreCase))
                {
                    var messageForShopper = Convert.ToString(outputAction.Argument);
                    var response = new OutputActionResponse
                    {
                        ShoppingSessionId = shoppingSessionId,
                        OutputName = outputAction.ActionName
                    };

                    result.Add(new ResponseWithClientId(response, shopperClientId));
                }
            }

            return result;
        }
    }
}
