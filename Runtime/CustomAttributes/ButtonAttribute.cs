using System;

namespace ArtificeToolkit.Attributes
{
    /// <summary>
    /// Add this attribute to a method and it will create a button which invokes the method.
    /// Αny number of given parameters found in the relative scope will be used for parameters.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute : CustomAttribute
    {
        public readonly bool ShouldAddOnSlidingPanel;
        public readonly string[] ParameterNames;

        public ButtonAttribute(params string[] parameters)
        {
            ShouldAddOnSlidingPanel = true;
            ParameterNames = parameters;
        }
        
        public ButtonAttribute(bool shouldAddOnSlidingPanel = true, params string[] parameters)
        {
            ShouldAddOnSlidingPanel = shouldAddOnSlidingPanel;
            ParameterNames = parameters;
        }
    }
}
