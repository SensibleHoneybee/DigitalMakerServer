using DigitalMakerApi.Models;

namespace DigitalMakerWorkerApp.PythonInterface
{
    public class PythonOutputData
    {
        public List<Variable> Variables { get; set; } = new List<Variable>();

        public List<OutputAction> OutputActions { get; set; } = new List<OutputAction>();
    }
}
