using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor
{
    /// <summary>Provides utilities to other Editor scripts.</summary>
    /// <example>Get the style for a specific <see cref="VisualElement"/>.</example>
    public class Artifice_Utilities
    {
        #region FIELDS
        
        public static bool ArtificeDrawerEnabled
        {
            get => EditorPrefs.GetBool("artificeDrawerEnabled");
            set => EditorPrefs.SetBool("artificeDrawerEnabled", value);
        }
        private StylesHolder _soStylesHolder;
        private Dictionary<Type, Type> _drawerMap;
        
        #endregion

        #region SINGLETON

        private Artifice_Utilities()
        {
        }

        private static Artifice_Utilities _instance;

        private static Artifice_Utilities Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Artifice_Utilities();
                    _instance.InitializeSingleton();
                }

                return _instance;
            }
        }
        
        private void InitializeSingleton()
        {
            var styleHolderPaths = AssetDatabase.FindAssets($"Artifice_StylesHolder t:{nameof(StylesHolder)}");
            
            if(styleHolderPaths.Length != 1)
                Debug.LogError($"Exactly one asset of this kind should get fetched, not {styleHolderPaths.Length}");
            
            _soStylesHolder = AssetDatabase.LoadAssetAtPath<StylesHolder>(AssetDatabase.GUIDToAssetPath(styleHolderPaths[0]));
            InitializeDrawerMap();
        }

        #endregion
        
        #region MenuItems

        private const string ArtificeInspectorOn = "Artifice Toolkit/" + "\u2712 Toggle ArtificeInspector/On";
        private const string ArtificeInspectorOff = "Artifice Toolkit/" +"\u2712 Toggle ArtificeInspector/Off";
        private const string ArtificeDocumentation = "Artifice Toolkit/" +"\ud83d\udcd6 Documentation...";
        private const string ArtificeIgnoreList = "Artifice Toolkit/" + "\u2718 Preview Ignore List";
        private const string ArtificeDocumentationURL = "https://github.com/AbZorbaGames/artificetoolkit";
        
        [MenuItem(ArtificeInspectorOn, true, 0)]
        private static bool ToggleOnCheckmark()
        {
            Menu.SetChecked(ArtificeInspectorOn, ArtificeDrawerEnabled);
            return true;
        }

        /// <summary> Creates a MenuItem to enable and disable the Artifice system. </summary>
        [MenuItem(ArtificeInspectorOn, priority = 11)]
        private static void ToggleArtificeDrawerOn()
        {
            ToggleArtificeDrawer(true);
            Debug.Log("<color=lime>[Artifice Inspector]</color> Enabled");
        }
        
        /// <summary> Creates a MenuItem to enable and disable the Artifice system. </summary>
        [MenuItem(ArtificeInspectorOff, priority = 11)]
        private static void ToggleArtificeDrawerOff()
        {
            ToggleArtificeDrawer(false);
            Debug.Log($"<color=orange>[Artifice Inspector]</color> Disabled");
        }
        
        [MenuItem(ArtificeInspectorOff, true, 0)]
        private static bool ToggleOffCheckmark()
        {
            Menu.SetChecked(ArtificeInspectorOff, !ArtificeDrawerEnabled);
            return true;
        }
        
        [MenuItem(ArtificeDocumentation)]
        private static void OpenArtificeDocumentationURL()
        {
            Application.OpenURL(ArtificeDocumentationURL);
        }
        
        [MenuItem(ArtificeIgnoreList)]
        private static void OpenArtificeIgnoreList()
        {
            Artifice_EditorWindow_IgnoreList.ShowWindow();
        }
        
        public static void ToggleArtificeDrawer(bool toggle)
        {
            var guid = AssetDatabase.FindAssets("ArtificeInspector").FirstOrDefault();
            if (guid == null)
            {
                Debug.Log("ArtificeToolkit: Cannot find ArtificeInspector script. This makes it unable to turn on/off the ArtificeToolkit.");
                return;
            }
            
            var filePath = AssetDatabase.GUIDToAssetPath(guid);
            if (File.Exists(filePath))
            {
                var hasChangedFile = false;
                var lines = File.ReadAllLines(filePath);

                // Set Regex pattern
                var customEditorAttributePattern = @"^\s*(//\s*)?\[CustomEditor\(typeof\(Object\), true\), CanEditMultipleObjects\]\s*$";
                for (var i = 0; i < lines.Length; i++)
                {
                    if (!Regex.IsMatch(lines[i], customEditorAttributePattern)) 
                        continue;
                    
                    // Check if the line is already commented
                    if (toggle && lines[i].TrimStart().Contains("//"))
                    {
                        // Uncomment the line
                        lines[i] = lines[i].Substring(2);
                        hasChangedFile = true;
                    }
                    else if(!toggle && !lines[i].TrimStart().StartsWith("//"))
                    {
                        // Comment out the line
                        lines[i] = "//" + lines[i];
                        hasChangedFile = true;
                    }
                    
                    break;
                }
                
                if (hasChangedFile)
                {
                    Selection.activeGameObject = null;
                    
                    // Change toggle and write/refresh.
                    ArtificeDrawerEnabled = toggle;
                    File.WriteAllLines(filePath, lines);
                    AssetDatabase.Refresh();
                }
            }
        }

        #endregion
        
        #region Reflection

        /// <summary>
        /// Resolves a nested member, and the object that contains it(if not fully static)
        /// </summary>
        /// <param name="nestedMember">Path to member</param>
        /// <param name="rootObject">Root object (null for fully static paths)</param>
        /// <returns>Tuple of (containing object, member info)</returns>
        public static (object, MemberInfo) ResolveNestedMember(
            string nestedMember, object rootObject)
        {
            if (string.IsNullOrEmpty(nestedMember))
                throw new ArgumentNullException(nameof(nestedMember),
                                                "Nested member can't be null or empty");

            var parts         = nestedMember.Split('.');
            var currentObject = rootObject;
            var currentType   = rootObject?.GetType();
            var startIndex    = 0;

            // Attempt to find the longest matching type name
            Type matchedType     = null;
            var  typePartsLength = 0;

            for (int i = parts.Length; i > 0; i--)
            {
                var typeCandidate = string.Join(".", parts.Take(i));
                matchedType = FindType(typeCandidate);
                if (matchedType == null) continue;
                typePartsLength = i;
                break;
            }

            if (matchedType != null)
            {
                currentType   = matchedType;
                currentObject = null;
                startIndex    = typePartsLength;
            }
            else if (rootObject == null)
            {
                throw new TypeLoadException(
                    $"Unable to resolve type from path: {parts[0]} and rootObject is null");
            }

            for (int i = startIndex; i < parts.Length; i++)
            {
                var name = parts[i];
                var member = currentType.GetMember(name,
                                                   BindingFlags.Instance |
                                                   BindingFlags.Static   |
                                                   BindingFlags.Public   |
                                                   BindingFlags.NonPublic).FirstOrDefault();

                if (member == null)
                    throw new MemberAccessException(
                        $"Failed to resolve '{name}' in type '{currentType.FullName}'");

                if (i == parts.Length - 1)
                    return (currentObject, member);

                switch (member)
                {
                    case FieldInfo field:
                        currentObject = field.GetValue(field.IsStatic ? null : currentObject);
                        break;

                    case PropertyInfo property:
                        if (!property.CanRead)
                            throw new InvalidOperationException(
                                $"Property '{property.Name}' is not readable");
                        currentObject =
                            property.GetValue(property.GetMethod.IsStatic ? null : currentObject);
                        break;

                    case MethodInfo method:
                        if (method.GetParameters().Length > 0)
                            throw new InvalidOperationException(
                                $"Method '{method.Name}' in path must have no parameters");
                        currentObject = method.Invoke(method.IsStatic ? null : currentObject, null);
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Member '{name}' is not a field, property, or parameterless method");
                }

                if (currentObject == null)
                    throw new NullReferenceException(
                        $"Path member '{name}' in '{nestedMember}' returned null");

                currentType = currentObject.GetType();
            }

            throw new InvalidOperationException($"Failed to fully resolve '{nestedMember}'");
        }

        /// <summary>
        /// Attempts to find a type by name in loaded assemblies
        /// </summary>
        private static Type FindType(string name)
        {
            // Try direct type lookup
            var type = Type.GetType(name);
            if (type != null) return type;

            // Try in loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(name);
                if (type != null) return type;

                // Try by simple name
                var match = assembly.GetTypes().FirstOrDefault(t => t.Name == name);
                if (match != null) return match;
            }

            return null;
        }

        #endregion
        
        public static Dictionary<Type, Type> GetDrawerMap()
        {
            return Instance._drawerMap;
        }
        
        /* Uses singleton privately, to allow access with static method */
        public static StyleSheet GetStyle(Type type)
        {
            return Instance._soStylesHolder.GetStyle(type);
        }
        
        /* Uses singleton privately, to allow access with static method */
        public static StyleSheet GetStyleByName(string name)
        {
            return Instance._soStylesHolder.GetStyleByName(name);
        }
        
        /* Uses singleton privately, to allow access with static method */
        public static StyleSheet GetGlobalStyle()
        {
            return Instance._soStylesHolder.GetGlobalStyle();
        }

        /* Method Dictating the equality of certain objects */
        public static bool AreEqual(object object1, object object2)
        {
            if (object1 == null && object2 == null)
            {
                return true;
            }

            if (object1 == null || object2 == null)
            {
                return false;
            }

            // Special case for enums
            if (object1 is Enum || object2 is Enum)
            {
                var enumType = object1 is Enum ? object1.GetType() : object2.GetType();
                var supportsFlags = enumType.GetCustomAttribute(typeof(FlagsAttribute)) != null;

                // Easier to debug instead of inline
                var v1 = Convert.ToInt64(object1);
                var v2 = Convert.ToInt64(object2);

                if (supportsFlags)
                {
                    var result = v1 & v2;
                    return result != 0;
                }

                return v1 == v2;
            }

            // Compare the values based on their types
            if (object1.GetType() == object2.GetType())
            {
                return object1.Equals(object2);
            }


            // Convert to strings and compare if the types are different
            var stringValue1 = object1.ToString();
            var stringValue2 = object2.ToString();

            return stringValue1.Equals(stringValue2);
        }
        
        /* Converts global PC path to Unity relative for AssetDatabase usage */
        public static string ConvertGlobalToRelativePath(string globalPath)
        {
            var dataPath = Application.dataPath;
            
            // DataPath by default includes Assets. So remove if from the the dataPath before extracting.
            dataPath = dataPath.Replace("Assets", "");
            
            // Extract dataPath completely. Whats left, is our relative path.
            return globalPath.Replace(dataPath, "");
        }
        
        /* Returns a map of all the AttributeDrawers */
        private void InitializeDrawerMap()
        {
            _drawerMap = new Dictionary<Type, Type>();

            var allDrawersTypes = TypeCache.GetTypesDerivedFrom<Artifice_CustomAttributeDrawer>();
            foreach (var drawerType in allDrawersTypes)
            {
                if(drawerType.IsAbstract)
                    continue;
                
                var customDrawerAttribute = drawerType.GetCustomAttribute<Artifice_CustomAttributeDrawerAttribute>();
                _drawerMap[customDrawerAttribute.Type] = drawerType;
            }
        }
        
        #region Reselection Utility
        
        private IEnumerator OnNextFrame(Action action)
        {
            yield return null;
            action.Invoke();
        }

        public static void TriggerNextFrameReselection()
        {
            var selection = Selection.objects;
            Selection.objects = null;
            EditorCoroutineUtility.StartCoroutine(Instance.OnNextFrame(() =>
            {
                Selection.objects = selection;
            }), Instance);
        }
        
        #endregion
    }
}