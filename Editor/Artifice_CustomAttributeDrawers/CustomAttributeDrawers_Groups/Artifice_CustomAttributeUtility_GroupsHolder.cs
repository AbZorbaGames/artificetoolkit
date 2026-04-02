using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups
{
    /// <summary> Singleton class to store shared <see cref="Artifice_VisualElement_Group"/> between properties </summary>
    public class Artifice_CustomAttributeUtility_GroupsHolder
    {
        #region FIELDS

        private readonly Dictionary<string, Dictionary<string, Artifice_VisualElement_Group>> _pathElementMap = new Dictionary<string, Dictionary<string, Artifice_VisualElement_Group>>();

        private readonly List<Artifice_VisualElement_Group> _openGroupStack = new();
        
        #endregion
        
        #region SINGLETON

        private Artifice_CustomAttributeUtility_GroupsHolder()
        {
        }

        private static Artifice_CustomAttributeUtility_GroupsHolder _instance;

        public static Artifice_CustomAttributeUtility_GroupsHolder Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Artifice_CustomAttributeUtility_GroupsHolder();
                }

                return _instance;
            }
        }

        #endregion

        /* Uses serializedObject and serializedProperty to generate a unique key based on parent's path */
        private string GetKeyPath(SerializedProperty property)
        {
            // Get elementKey from property
            var parentKey = $"{property.serializedObject.GetHashCode()}";
            
            if (property.depth > 0)
            {
                var propertyPathTokens = property.propertyPath.Split(".");
                var newTokens = new string[propertyPathTokens.Length - 1];
                Array.Copy(propertyPathTokens, newTokens, newTokens.Length);
                
                var keyPostfix = String.Join('.', newTokens);
                parentKey += $"-{keyPostfix}";
            }

            return parentKey;
        }
        
        /* Returns true if the elementKey is contained in the elementMap */
        private bool Contains(string parentKey, string elementKey)
        {
            if (_pathElementMap.ContainsKey(parentKey) == false)
                return false;
                
            var elementMap = _pathElementMap[parentKey];
            var tokens = elementKey.Split("/");
            return elementMap.ContainsKey(tokens[^1]);
        }
        
        /* Creates all the T (inherits from VisualElement_BoxGroup) to satisfy the elementKey chain. */
        private void Create(SerializedProperty property, string parentKey, string elementKey, Type elementType)
        {
            if (_pathElementMap.ContainsKey(parentKey) == false)
                _pathElementMap[parentKey] = new Dictionary<string, Artifice_VisualElement_Group>();

            var elementMap = _pathElementMap[parentKey];
            
            // Ex. "Group1/Subgroup1" and "Group1/Subgroup2"
            // Check if it has parent. if yes, attach there!
            var tokens = elementKey.Split("/");
            var currentKey = "";
            for(var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                currentKey += "/" + token;
                if (Contains(parentKey, token))
                    continue;
                
                // Create new element
                var elem = (Artifice_VisualElement_Group)Activator.CreateInstance(elementType, true);
                elem.name = $"{token}-group-container";
                // Set title
                elem.SetTitle(token);
                // Set persistence key and invoke load
                elem.ViewPersistenceKey = $"{property.propertyPath}" + currentKey;
                
                // Save to element map
                elementMap[token] = elem;
                
                // If we have checked for parent, add as child
                if (i > 0)
                {
                    var parent = elementMap[tokens[i - 1]];
                    // Reset content container because its possible for a previous one to have accessed it
                    parent.ResetContentContainer();
                    parent.Add(elem);
                }
            }
        }
        
        /* Clears all data regarding the serializedObject parameter*/
        public void ClearSerializedObject(SerializedObject serializedObject)
        {
            var hash = serializedObject.GetHashCode();

            var keys = _pathElementMap.Keys.Where(key => key.Contains(hash.ToString())).ToList();
            foreach (var key in keys)
            {
                var elementMap = _pathElementMap[key];
                var elementMapKeys = elementMap.Keys.ToList();
                foreach(var elementKey in elementMapKeys)
                {
                    elementMap[elementKey].Clear();
                    elementMap.Remove(elementKey);
                }

                _pathElementMap.Remove(key);
            }
        }

        #region Open Groups API
        
        public void PushOpenGroup(Artifice_VisualElement_Group group)
        {
            _openGroupStack.Add(group);
            _openGroupStack.First().SetContentContainer(_openGroupStack.Last());
        }
        
        public void PopOpenGroup()
        {
            if (HasOpenGroup())
            {
                _openGroupStack.Remove(_openGroupStack.Last());
                
                // if there are still elements left
                if (HasOpenGroup())
                {
                    _openGroupStack.First().SetContentContainer(_openGroupStack.Last());
                }
            }
            else
                Debug.LogWarning("Trying to pop the Open Group Stack but no elements are inside.");
        }
        
        public bool HasOpenGroup()
        {
            return _openGroupStack.Count > 0;
        }

        public void CloseOpenGroups()
        {
            while(HasOpenGroup())
                PopOpenGroup();
        }
        
        public Artifice_VisualElement_Group Get_OpenGroup()
        {
            // If correctly structured, First should always have a correct set of content containers.
            // It is important to return the first instead of the last so that it is correctly returned to the inspector.
            return _openGroupStack.First(); 
        }
        
        #endregion
        
        ///<summary> If the elementKey does not exist, it creates it. Then returns the base of the group chain. Lastly it sets the proper content container or resets it. </summary>
        public (Artifice_VisualElement_Group firstElem, Artifice_VisualElement_Group lastElem) Get(SerializedProperty property, string value, Type elementType)
        {
            var parentKey = GetKeyPath(property);
            
            if (Contains(parentKey, value) == false) // Create new
                Create(property, parentKey, value, elementType);
            
            var elementMap = _pathElementMap[parentKey];
            var tokens = value.Split("/");
            
            // Get reference to first and last in tokens-chain
            var firstElem = elementMap[tokens[0]];
            var lastElem = elementMap[tokens[^1]];
            
            // Set or Reset the content container to add your children
            if (firstElem.Equals(lastElem) == false)
                firstElem.SetContentContainer(lastElem);
            else
                firstElem.ResetContentContainer();

            // Always return the first in the chain, because inspector will change hierarchy otherwise.
            // Return the last element in case alterations need to occur
            return (firstElem, lastElem);
        }
    }
}