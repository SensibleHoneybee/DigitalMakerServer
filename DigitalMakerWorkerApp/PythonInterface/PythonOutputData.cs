using DigitalMakerApi.Models;

namespace DigitalMakerWorkerApp.PythonInterface
{
    public class PythonOutputData
    {
        public PythonOutputData(List<Variable> variables, List<OutputAction> outputActions)
        {
            Variables = variables;
            OutputActions = outputActions;
        }

        public List<Variable> Variables { get; }

        public List<OutputAction> OutputActions { get; }
    }
}
