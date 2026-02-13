using System.Collections.Generic;
using UnityEditor;

namespace Editor.Artifice_ArtificeMenuEditorWindow
{
    public class TestMenuEditorWindow : ArtificeMenuEditorWindow
    {
        [MenuItem("Test/Test")]
        private static void OpenWindow()
        {
            GetWindow<TestMenuEditorWindow>().Show();
        }
        
        protected override List<ArtificeMenuNode> BuildMenuTree()
        {
            var list = new List<ArtificeMenuNode>();
            
            var menuItem1 = new ArtificeMenuNode("Item 1", new Page1());
            menuItem1.AddChild(new ArtificeMenuNode("subItem1", new Page2()));
            menuItem1.AddChild(new ArtificeMenuNode("subItem2", new Page1()));
            
            var menuItem2 = new ArtificeMenuNode("Item 2", new Page2());
            
            list.Add(menuItem1);
            list.Add(menuItem2);

            return list;
        }

        private class Page1
        {
            public int x;
            public int y;
        }

        private class Page2
        {
            public string x;
            public string y;
        }
    }   
}
