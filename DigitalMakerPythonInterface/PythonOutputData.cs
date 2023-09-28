using DigitalMakerApi.Models;

namespace DigitalMakerPythonInterface
{
    public class PythonOutputData
    {
        public PythonOutputData(List<OutputAction> outputActions)
        {
            OutputActions = outputActions;
        }

        public List<OutputAction> OutputActions { get; }
    }
}
