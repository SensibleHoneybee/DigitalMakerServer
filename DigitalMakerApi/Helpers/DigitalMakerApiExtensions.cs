using DigitalMakerApi.Models;

namespace DigitalMakerApi.Helpers
{
    public static class DigitalMakerApiExtensions
    {
        public static void SetValueFromString(this Variable variable, string value)
        {
            if (variable.VariableType == VariableType.String)
            {
                variable.Value = value;
            }
            else if (variable.VariableType == VariableType.Integer)
            {
                variable.Value = Convert.ToInt32(value);
            }
            else if (variable.VariableType == VariableType.Float)
            {
                variable.Value = Convert.ToDecimal(value);
            }
            else if (variable.VariableType == VariableType.List)
            {
                throw new NotImplementedException();
            }
            else if (variable.VariableType == VariableType.Boolean)
            {
                variable.Value = Convert.ToBoolean(value);
            }
            else
            {
                var msg = $"Unknown variable type {variable.VariableType} for variable {variable.Name}";
                throw new InvalidOperationException(msg);
            }
        }
    }
}
