using UnityEditor;

namespace ArtificeToolkit.Editor
{
    public class Artifice_ValidatorModule_CustomAttributeChecker : Artifice_ValidatorModule_SerializedPropertyBatching
    {
        #region FIELDS

        public override string DisplayName { get; protected set; } = "CustomAttributes Checker";
        
        #endregion
        
        // Override for each batched property
        protected override void ValidateSerializedProperty(SerializedProperty property)
        {
            Artifice_ValidatorExtensions.GenerateValidatorLogs(property, Logs, GetType());
        }
    }
}