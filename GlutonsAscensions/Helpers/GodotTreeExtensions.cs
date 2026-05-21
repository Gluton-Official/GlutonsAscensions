using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace GlutonsAscensions.Helpers;

public static class GodotTreeExtensions {
    public static void AddSiblingSafely(this Node node, Node? sibling) {
        if (sibling == null) return;
        if (NGame.IsMainThread()) {
            node.AddSibling(sibling);
        }
        else {
            node.CallDeferred(Node.MethodName.AddSibling, (Variant)(GodotObject)sibling);
        }
    }
}