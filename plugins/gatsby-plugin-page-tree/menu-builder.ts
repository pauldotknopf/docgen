import treeify from 'treeify-paths'
import MenuItem from './menu-item'

class TreeNode {
    path: string
    name: string
    children: Array<TreeNode>
}

const normalizePath = (p: string) => {
    if(p.endsWith('/'))  {
        p = p.substr(0, p.length - 1);
    }
    if(!p.startsWith('/')) {
        p = '/' + p;
    }
    return p;
}

export function buildMenuFromNodes(nodes: Array<GatsbyNode>, selectedPath: string, ignorePaths: string[]): Array<MenuItem> {
    selectedPath = normalizePath(selectedPath);
    let pages = nodes.filter(x => x.internal.type == 'SitePage');
    if (!ignorePaths) {
        ignorePaths = [];
    }
    ignorePaths = ignorePaths.map(normalizePath);
    let treePaths = pages
        .map(x => normalizePath(x.path))
        .filter(x => x !== '/')
        .filter(x => {
            return ignorePaths.findIndex(ignorePath => ignorePath == x) == -1;
        });
    let tree = treeify(treePaths) as TreeNode;
    
    let rootNode = tree.children[0];

    let result: Array<MenuItem> = [];
    
    const walkTreeNode = (node: TreeNode, parents: MenuItem[]): MenuItem => {
        let normalizedPath = normalizePath(node.path);
        let menuItem = new MenuItem();
        menuItem.path = normalizedPath;

        let page = pages.find(x => normalizePath(x.path) == normalizedPath);
        if (page) {
            menuItem.title = page.context.title;
        } else {
            menuItem.title = node.name;
            menuItem.isEmptyParent = true;
        }

        if(normalizedPath == selectedPath) {
            menuItem.selected = true;
            for(let parent of parents) {
                parent.active = true;
            }
        }

        if (node.children && node.children.length > 0) {
            let newParents = [
                ...parents,
                menuItem
            ];
            menuItem.children = node.children.map(child => {
                return walkTreeNode(child, newParents)
            })
        }
        return menuItem;
    };

    for(let child of rootNode.children) {
        result.push(walkTreeNode(child, []));
    }

    return result;
}