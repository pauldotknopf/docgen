﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DocGen.Web.Requirements.Impl
{
    public class MenuStore : IMenuStore
    {
        readonly List<Page> _pages = new List<Page>();

        private readonly TreeNode _tree = new TreeNode
        {
            Path = "/"
        };
        
        public void AddPage(string path, string title, int order)
        {
            if(string.IsNullOrEmpty(path)) throw new ArgumentNullException();
            if(!path.StartsWith("/")) throw new ArgumentOutOfRangeException();

            if (_pages.Any(x => x.Path == path))
            {
                throw new InvalidOperationException();
            }

            var page = new Page
            {
                Path = path,
                Title = title,
                Order = order
            };
            
            _pages.Add(page);

            var current = _tree;
            foreach (var part in page.Path.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries))
            {
                current = current.Resolve(part, true);
            }
            
            if(current.Page != null) throw new InvalidOperationException(); // shouldn't happen

            current.Page = page;
        }

        public MenuItem BuildMenu(string currentPath, int alwaysEpandToLevel = 0)
        {
            // Find the furthest leaf node
            var parents = new List<TreeNode> { _tree };
            Page selectedPage = null;
            
            {
                // Get all the parent nodes for this path
                var current = parents.Single();
                foreach (var part in currentPath.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries))
                {
                    var potential = current.Resolve(part);
                    if (potential == null) continue;
                    current = potential;
                    selectedPage = current.Page;
                    parents.Add(potential);
                }
            }

            {
                // Get first parent that has a page.
                var directParentPage = parents.Last(x => x.Page != null) ?? _tree;
                
                MenuItem BuildMenuItem(TreeNode treeNode, int level)
                {
                    var newMenuItem = new MenuItem
                    {
                        Path = string.IsNullOrEmpty(treeNode.FullPath) ? "/" : treeNode.FullPath,
                        IsEmptyParent = treeNode.Page == null,
                        Title = treeNode.Page?.Title
                    };

                    if (parents.Contains(treeNode))
                    {
                        newMenuItem.Active = true;
                    }

                    if (selectedPage != null)
                    {
                        if (selectedPage.Path.Equals(treeNode.FullPath, StringComparison.InvariantCultureIgnoreCase))
                        {
                            newMenuItem.Selected = true;
                        }
                    }
                    
                    if (treeNode == directParentPage || newMenuItem.IsEmptyParent)
                    {
                        // This is the most direct parent, so let's render all children.
                        var children = treeNode.Children.Values
                            .OrderBy(x => x.Page?.Order ?? int.MaxValue)
                            .Select(x => BuildMenuItem(x, level + 1));
                        newMenuItem.Children.AddRange(children);
                    }
                    else
                    {
                            foreach (var child in treeNode.Children.Values
                                .OrderBy(x => x.Page?.Order ?? int.MaxValue))
                            {
                                if (parents.Contains(child) || level <= alwaysEpandToLevel)
                                {
                                    newMenuItem.Children.Add(BuildMenuItem(child, level + 1));
                                }
                            }
                    }

                    return newMenuItem;
                }

                return BuildMenuItem(_tree, 1);
            }
        }

        class Page
        {
            public string Path { get; set; }
            public string Title { get; set; }
            public int Order { get; set; }
        }

        class TreeNode
        {
            public TreeNode()
            {
                Children = new Dictionary<string, TreeNode>();
            }
            
            public string FullPath { get; set; }
            
            public string Path { get; set; }
            
            public Page Page { get; set; }
            
            public TreeNode Parent { get; set; }
            
            public Dictionary<string, TreeNode> Children { get; }
            
            public TreeNode Resolve(string path, bool create = false)
            {
                if (Children.TryGetValue(path, out var result))
                {
                    return result;
                }

                if (!create) return null;
                
                result = new TreeNode
                {
                    Path = path,
                    Parent = this,
                    FullPath = FullPath + "/" + path
                };
                Children.Add(path, result);

                return result;
            }
        }
    }
}