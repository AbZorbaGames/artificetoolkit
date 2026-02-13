using System.Collections.Generic;
using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEngine;

namespace Editor.Artifice_ArtificeMenuEditorWindow
{
    public class TestMenuEditorWindow : ArtificeMenuEditorWindow
    {
        [MenuItem("Test/Test")]
        private static void OpenWindow()
        {
            GetWindow<TestMenuEditorWindow>().Show();
        }
        
        protected override List<ArtificeTreeNode> BuildMenuTree()
        {
            var list = new List<ArtificeTreeNode>();
            
            var menuItem1 = new ArtificeTreeNode("Item 1", CreateInstance<Page1>());
            menuItem1.AddChild(new ArtificeTreeNode("subItem1", CreateInstance<Page2>()));
            menuItem1.AddChild(new ArtificeTreeNode("subItem2", CreateInstance<Page1>()));
            
            var menuItem2 = new ArtificeTreeNode("Item 2", CreateInstance<Page2>());
            
            list.Add(menuItem1);
            list.Add(menuItem2);

            return list;
        }

        private class Page1 : ScriptableObject
        {
            [BoxGroup]
            public int x;
            
            [BoxGroup]
            public int y;

            public Vector3 vector;
            
            [Button]
            private void  PerformCalculations()
            {
                
            }
        }

        private class Page2 : ScriptableObject
        {
            public string x;
            public string y;
        }
    }   
}


