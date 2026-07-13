// Copyright (C) 2026 Chubby Hippo
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the Free
// Software Foundation, either version 3 of the License, or (at your option)
// any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
// more details.
//
// You should have received a copy of the GNU General Public License along
// with this program. If not, see <https://www.gnu.org/licenses/>.
//
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Collections.Generic;
using Notemeow.Core;
using Xunit;

namespace Notemeow.Core.Tests
{
    public class TreeMeowSpec : SpecDsl
    {
        private sealed class TreeNode
        {
            public readonly string Name;
            public readonly TreeNode Parent;
            public readonly List<TreeNode> Children = new List<TreeNode>();
            public bool Expanded;

            public TreeNode(string name, TreeNode parent)
            {
                Name = name;
                Parent = parent;
            }

            public TreeNode Add(string childName)
            {
                var child = new TreeNode(childName, this);
                Children.Add(child);
                return child;
            }
        }

        private sealed class FakeTree
        {
            public readonly TreeNode Root = new TreeNode("root", null);
            public TreeNode Focus;
            public readonly List<string> Ran = new List<string>();

            public FakeTree()
            {
                Focus = Root;
            }

            public void Run(string id)
            {
                List<TreeNode> rows = VisibleRows();
                int at = rows.IndexOf(Focus);
                switch (id)
                {
                    case "notemeow.tree.focusDown":
                        Focus = rows[System.Math.Min(at + 1, rows.Count - 1)];
                        break;
                    case "notemeow.tree.focusUp":
                        Focus = rows[System.Math.Max(at - 1, 0)];
                        break;
                    case "notemeow.tree.collapse":
                        if (Focus.Expanded) Focus.Expanded = false;
                        else if (Focus.Parent != null) Focus = Focus.Parent;
                        break;
                    case "notemeow.tree.expand":
                        if (Focus.Children.Count > 0 && !Focus.Expanded) Focus.Expanded = true;
                        else if (Focus.Children.Count > 0) Focus = Focus.Children[0];
                        break;
                    default:
                        Ran.Add(id);
                        break;
                }
            }

            private List<TreeNode> VisibleRows()
            {
                var rows = new List<TreeNode>();
                Walk(Root, rows);
                return rows;
            }

            private void Walk(TreeNode n, List<TreeNode> rows)
            {
                rows.Add(n);
                if (n.Expanded)
                {
                    foreach (TreeNode c in n.Children) Walk(c, rows);
                }
            }

            public void Select(string name)
            {
                Focus = Find(Root, name);
            }

            private TreeNode Find(TreeNode n, string name)
            {
                if (n.Name == name) return n;
                foreach (TreeNode c in n.Children)
                {
                    TreeNode r = Find(c, name);
                    if (r != null) return r;
                }
                return null;
            }

            public string SelectedText()
            {
                return Focus.Name;
            }

            public bool IsExpanded(string name)
            {
                TreeNode prior = Focus;
                Select(name);
                bool e = Focus.Expanded;
                Focus = prior;
                return e;
            }
        }

        private static FakeTree GivenTree()
        {
            var tree = new FakeTree();
            TreeNode a = tree.Root.Add("a");
            a.Add("a1");
            a.Add("a2");
            tree.Root.Add("b");
            tree.Root.Expanded = true;
            return tree;
        }

        [Fact(DisplayName = "given the bundled rc then it binds the tree keys")]
        public void BundledRcBindsTreeKeys()
        {
            Rc.Config d = Rc.Defaults();
            Assert.Equal("meow-next", d.Motion['j'].Command);
            Assert.Equal("meow-prev", d.Motion['k'].Command);
            Assert.Equal("meow-left", d.Motion['h'].Command);
            Assert.Equal("meow-right", d.Motion['l'].Command);
            Assert.Equal("notemeow.hideView", d.Motion['q'].Action);
        }

        [Fact(DisplayName = "given a tree when j and k then the selection moves like the arrow keys")]
        public void JAndKMoveSelection()
        {
            FakeTree tree = GivenTree();
            TreeMeow.Dispatch(tree.Run, 'j');
            Assert.Equal("a", tree.SelectedText());
            TreeMeow.Dispatch(tree.Run, 'j');
            Assert.Equal("b", tree.SelectedText());
            TreeMeow.Dispatch(tree.Run, 'k');
            Assert.Equal("a", tree.SelectedText());
        }

        [Fact(DisplayName = "given a collapsed node when l then it expands, and l again enters it")]
        public void LExpandsThenEnters()
        {
            FakeTree tree = GivenTree();
            tree.Select("a");
            TreeMeow.Dispatch(tree.Run, 'l');
            Assert.True(tree.IsExpanded("a"), "l on a collapsed node expands it");
            Assert.Equal("a", tree.SelectedText());
            TreeMeow.Dispatch(tree.Run, 'l');
            Assert.Equal("a1", tree.SelectedText());
        }

        [Fact(DisplayName = "given an expanded node when h then it collapses, then goes to the parent")]
        public void HCollapsesThenParent()
        {
            FakeTree tree = GivenTree();
            tree.Select("a");
            tree.Focus.Expanded = true;
            tree.Select("a1");
            TreeMeow.Dispatch(tree.Run, 'h');
            Assert.Equal("a", tree.SelectedText());
            TreeMeow.Dispatch(tree.Run, 'h');
            Assert.False(tree.IsExpanded("a"), "h on an expanded node collapses it");
            Assert.Equal("a", tree.SelectedText());
            TreeMeow.Dispatch(tree.Run, 'h');
            Assert.Equal("root", tree.SelectedText());
        }

        [Fact(DisplayName = "given an editor-only command in the mmap then it is inert on trees")]
        public void EditorOnlyCommandInert()
        {
            GivenRc("mmap w meow-next-word");
            FakeTree tree = GivenTree();
            TreeMeow.Dispatch(tree.Run, 'w');
            Assert.Equal("root", tree.SelectedText());
            Assert.Empty(tree.Ran);
        }

        [Fact(DisplayName = "given a user mmap override then it shadows the bundled defaults")]
        public void UserMmapOverrideShadows()
        {
            GivenRc("mmap j ignore");
            FakeTree tree = GivenTree();
            TreeMeow.Dispatch(tree.Run, 'j');
            Assert.Equal("root", tree.SelectedText());
        }

        [Fact(DisplayName = "given a keys mapping then the replay resolves every key through the motion map")]
        public void KeysReplayResolvesEachKey()
        {
            GivenRc("mmap g jj");
            FakeTree tree = GivenTree();
            TreeMeow.Dispatch(tree.Run, 'g');
            Assert.Equal("b", tree.SelectedText());
        }

        [Fact(DisplayName = "given a noremap replay then it skips user maps like the engine")]
        public void NoremapReplaySkipsUserMaps()
        {
            GivenRc("mnoremap g jj\nmmap j ignore");
            FakeTree tree = GivenTree();
            TreeMeow.Dispatch(tree.Run, 'j');
            Assert.Equal("root", tree.SelectedText());
            TreeMeow.Dispatch(tree.Run, 'g');
            Assert.Equal("b", tree.SelectedText());
        }

        [Fact(DisplayName = "given an <action> mmap then it dispatches with the tree as context")]
        public void ActionMmapDispatches()
        {
            GivenRc("mmap z <action>(notemeow.test.probe)");
            FakeTree tree = GivenTree();
            TreeMeow.Dispatch(tree.Run, 'z');
            Assert.Equal(new List<string> { "notemeow.test.probe" }, tree.Ran);
        }

        [Fact(DisplayName = "given defaults and user maps then boundChars merges them")]
        public void BoundCharsMerges()
        {
            GivenRc("mmap w meow-next-word");
            HashSet<char> bound = TreeMeow.BoundChars();
            foreach (char c in "jkhlqw")
            {
                Assert.True(bound.Contains(c), "'" + c + "' must be bound");
            }
            Assert.False(bound.Contains('z'), "unmapped letters stay native (type-to-find)");
        }

        [Fact(DisplayName = "given mmap q ignore then the key returns to the tree")]
        public void MmapQIgnoreReturnsToTree()
        {
            GivenRc("mmap q ignore");
            Assert.False(TreeMeow.BoundChars().Contains('q'), "an ignored key leaves the shortcut set");
            Assert.True(TreeMeow.BoundChars().Contains('j'), "the other defaults stay");
        }
    }
}
