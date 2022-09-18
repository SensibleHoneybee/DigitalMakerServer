using System.Text;
using Microsoft.Extensions.Logging;

namespace DigitalMakerPythonInterface
{
    public interface IPythonScriptRunner
    {
        Task<PythonOutputData> RunPythonProcessAsync(string userSuppliedPythonCode, PythonInputData pythonData);
    }

    ////public class PythonScriptRunner : IPythonScriptRunner
    ////{
    ////    private readonly IPythonScriptProvider _pythonScriptProvider;

    ////    private readonly IPythonScriptGateway _pythonScriptGateway;

    ////    private readonly IPythonVariableDefinitionProvider _pythonVariableDefinitionProvider;

    ////    private readonly IPythonResultParser _pythonResultParser;

    ////    private readonly ILogger<PythonScriptRunner> _logger;

    ////    public PythonScriptRunner(
    ////        IPythonScriptProvider pythonScriptProvider,
    ////        IPythonScriptGateway pythonScriptGateway,
    ////        IPythonVariableDefinitionProvider pythonVariableDefinitionProvider,
    ////        IPythonResultParser pythonResultParser,
    ////        ILogger<PythonScriptRunner> logger)
    ////    {
    ////        this._pythonScriptProvider = pythonScriptProvider;
    ////        this._pythonScriptGateway = pythonScriptGateway;
    ////        this._pythonVariableDefinitionProvider = pythonVariableDefinitionProvider;
    ////        this._pythonResultParser = pythonResultParser;
    ////        this._logger = logger;
    ////    }

    ////    public async Task<string> RunPythonProcessAsync(string userSuppliedPythonCode, PythonInputData pythonInputData)
    ////    {
    ////        var defaultPythonScript = this._pythonScriptProvider.GetPythonScript();

    ////        // Construct python initializers for the variables
    ////        var variableDefinitions = new StringBuilder();
    ////        foreach (var variable in pythonInputData.Variables)
    ////        {
    ////            var definition = this._pythonVariableDefinitionProvider.GetPythonVariableDefinition(variable);
    ////            variableDefinitions.AppendLine(definition);
    ////        }

    ////        var actualPythonScript = defaultPythonScript
    ////            .Replace("{{{VARIABLE_DEFINITIONS}}}", variableDefinitions.ToString())
    ////            .Replace("{{{USER_CODE}}}", userSuppliedPythonCode);

    ////        var pythonResult = await this._pythonScriptGateway.RunPythonProcessAsync(actualPythonScript);

    ////        var parsedResults = _pythonResultParser.Parse(pythonResult, pythonInputData.Variables);

    ////        return pythonResult;
    ////    }
    ////}
}
