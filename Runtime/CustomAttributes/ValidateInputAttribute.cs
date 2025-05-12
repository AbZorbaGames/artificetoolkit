using UnityEngine;

namespace ArtificeToolkit.Attributes
{
    public class ValidateInputAttribute : ValidatorAttribute, IArtifice_ArrayAppliedAttribute
    {
        public string Condition;
        public string Message = "Invalid Input";
        public LogType LogType = LogType.Error;

        public ValidateInputAttribute(string condition)
        {
            Condition = condition;
        }

        public ValidateInputAttribute(string condition, string message)
        {
            Condition = condition;
            Message = message;
        }

        public ValidateInputAttribute(string condition, LogType logType)
        {
            Condition = condition;
            LogType = logType;
        }

        public ValidateInputAttribute(string condition, string message, LogType logType)
        {
            Condition = condition;
            Message = message;
            LogType = logType;
        }
    }
}