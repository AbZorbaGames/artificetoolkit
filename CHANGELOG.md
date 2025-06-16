# Change Log:
## 1.3.16
- Enhancement: Refactored `Artifice_EditorWindow_Validator`'s logs to set selection of inspector with one click only (previously was two).
- Enhancement: Fallback rendering from 2022.2 and newer, is now UI Toolkit rather than IMGUI. 
- Enhancement: When using SerializedReference for interfaces, when no properties exist in the interface, it does not render the extra foldout.
- Fix: Now custom attributes are applied to the serialized reference it self.

## 1.3.15
- Enhancement: Added `Artifice_ValidatorModule_ScriptableObject` which parses scriptable objects in the Assets and checks for custom attribute validations.

## 1.3.14
- Fix: SerializedReference with ForceArtifice for interface instantiation now skips interface and abstract types since they cannot be instantiated.

## 1.3.13
- Fix: Specifically for Unity 6, `EnableIf` was not working for elements created after the initial rendering.

## 1.3.12
- Fix: ArtificeToolkit no longer causes initial errors upon installation or reimport all.

## 1.3.11
- Enhancement: Now group attributes can be used with the `[Button]` attributes to affect the placement of method buttons.

## 1.3.10
- Enhancement: Added `ValidateInput` attribute which allows for easy in-script validations to be created on the spot.
- Fix: Fixed bug with `IArtifice_ArrayAppliedAttribute`
- Fix: Allowed `Artifice_CustomAttributeDrawer_Validator_BASE` inheritors to update the InfoBox as they see fit.
- Fix: Fixed bug with `Artifice_Validator` which would first cache the log and then call validate. That would was problematic in case of dynamic log messages per drawer. 

## 1.3.8
- Change: Added `IArtifice_ArrayAppliedAttribute` which is used to indicate whether an attribute should be applied to a property array or be injected on its children.

## 1.3.7
- Enhancement: Great performance boost for artifice drawer.
- Enhancement: Added `[ArtificeIgnore]` attribute.
- Enhancement: Added context action to ignore rendering specific scripts with `ArtificeDrawer`.
- Fix: Fixed problem where validator logs would glitch between appearing and disappearing.

## 1.3.5
- Fix: Hotfixed problem with CustomPropertyDrawer utility returning null in rare cases (like InputAction from Unity's InputSystem package). In this case, we fallback to a default `PropertyField` now.

## 1.3.4
- Fix: Copying and pasting an entire artifice list now works even if original copy has been disposed.

## 1.3.3
- Fix: SerializedDictionary did not work with long or some other types due to randmomization not being defined.

## 1.3.2
- Enhancement: Added [Sort] attribute which allows you to reorder rendering order of properties.

## 1.3.1
- Fix: Validator had an unnecessary force stop on hierarchy change to avoid disappearing gameObjects, but this was already covered with a targeted if clause on the same iteration.
- Enhancement: Added the option on the validator settings, to set a custom batching priority value.

## 1.3.0
- Enhancement: Heavily refactored `Artifice_Validator` in order to centralize batching and gathering of target objects to validate. To make the parsing logic simpler, `Artifice_ValidatorModule_GameObjectBatching` and `Artifice_ValidatorModule_SerializedPropertyBatching` requiring a single method to be overriden to apply validations.
- Enhancement: Added Null Script checker validation module, to immediately know when a script reference is lost.
- Change: Removed from validator the assets folders. They were draining performance and the attributes are not designed to support them. A common problem would be having a Required attribute on a prefab property which would have, by design, have to be filled after being placed inside another prefab.

## 1.2.0
- Enhancement: Added interface and abstract types serialization solution based on `SerializeReference` and `ForceArtifice``.
- Enhancement: Added OnValueChanged
- Fix: Corrected position of delete-element on artifice lists.

## 1.1.13
- Enhancement: Refactored Artifice Valdiator to be independent from the EditorWindow and runs persistently in the background while in autorun.
- Enhancement: Injected toolbar indicators for Artifice Validator on the top-left corner of Unity. On click, it toggles on/off the editor window of the validator for more details.

## 1.1.12
 - Fix: Added support on ChildGameObjectOnly attributes to also work on list elements.   

## 1.1.11
 - Enhancement: Added SerializedDictionary to runtime which works with all serializable types. Works with a specialized custom property drawer inherting from AbstractListView.
 - Enhancement: Updated documentation to include extra features section.
 - Fix: Added null checks and minor serialized property verification extension to avoid some errors.
 - Fix: Added persistency to Artifice_EditorWindow_Validator two pane split view. 


## 1.1.10
 - Fix: Bad namespace for Artifice_VisualElement_SlidingGroup caused conflicts with UnityEditor.Editor namespace.

## 1.1.9
 - Fix: Abstract List View would not apply attributes to children.
 - Fix: ChildGameObjectOnly would cause visual bug after list redraw.
 - Enhancement: Removed from Validator the drawer of scenes. It did not contribute to anything.
 - Enhancement: Updated Artifice_VisualElement_ToggleButton to support BindProperty.
 - Enhancement: Reversed Button parameter usage to be more usable, and fixed bug were it would not be able to close sliding panel afterwards.
 - Fix: Documentation menu item will now redirect user at the github page, showing the README.md
 - Change: Max/Min attributes have been converted to validations.
 - Change: ArtificeEditorWindow now has virtual method for CreateGUI, allowing you to extend it. It also immedietelly filters out unwanted unity serialized field.


## 1.1.8
 - Enhancement: Refactored ButtonAttribute to work with methods instead of proxy properties. 
 - Enhancement: Added sliging group visual element. Used in ButtonAttribute for cleaner inspector view
 - Enhancement: Some improvement on artifice list view performance 

## 1.1.7
 - Enhancement: Previously ArtificeDrawer would completely ignore custom property drawers in the project. Now, it queries and uses them if they exist!
 - Bug Fix: Previously in version 2022.X, when openning the validator window a bunch of warnings would show up. This is now fixed.
 - Enhancement: OnPropertyBound override for custom property drawers now works with 100% consistency.
 - Enhancement: Now ChildGameObjectOnly deletes the default unity object selector.
 - New Attributes: HideInArtifice, ReadOnly 
 - Documentation: Added documentation section on why order matterns + how to create your own custom attribute drawers.

## 1.1.5
 - Enhancement: Added ListElementNameAttribute which allows you to set a custom naming extension to your list elements based on sub-property string values.
 - Enhancement: Added context menu options (apply/revert to prefab, copy and paste) to Artifice's list view. Now, it also indicates with the blue indicator if lists have been detected on the list.

## 1.1.4
 - Refactor: Changed MenuItem name from ArtificeDrawer to ArtificeToolkit
 - Enhancement: Updated README.md with complete documentation and examples for each tested and used attribute with images and gifs.
 - Fix: Empty array using custom attributes was not rendering with artifice fixed.

## 1.1.3

- Enhancement: Now toggle button visual element can receive different sprites for each of its states. A example of this was implemented in the validator.

## 1.1.2

- Enhancement: Artifice Off now truly disables the toolkit. It disables the CustomEditor attribute on the artifice inspector, disabling its automatic replacement of the default editor. In addition, it will enforce the toggle option upon every domain reload. This ensures consistency when initializing or updating the package.