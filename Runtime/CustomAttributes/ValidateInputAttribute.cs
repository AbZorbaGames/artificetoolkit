using UnityEngine;

namespace ArtificeToolkit.Attributes
{
    public class ValidateInputAttribute : ValidatorAttribute, IArtifice_ArrayAppliedAttribute
    {
        public readonly string Condition;
        public readonly string Message = "Invalid Input";
        public readonly LogType LogType = LogType.Error;
        public readonly bool ReevaluateOnChange;

        public ValidateInputAttribute(string condition)
        {
            Condition = condition;
        }

        public ValidateInputAttribute(string condition, string message, bool reevaluateOnChange = true)
        {
            Condition = condition;
            Message = message;
        }

        public ValidateInputAttribute(string condition, LogType logType, bool reevaluateOnChange = true)
        {
            Condition = condition;
            LogType = logType;
        }

        public ValidateInputAttribute(string condition, string message, LogType logType, bool reevaluateOnChange)
        {
            Condition = condition;
            Message = message;
            LogType = logType;
            ReevaluateOnChange = reevaluateOnChange;
        }
    }
}