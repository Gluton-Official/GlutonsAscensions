using Godot;

namespace GlutonsAscensions.Helpers;

public static class CanvasItemExtensions {
    extension(CanvasItem canvasItem) {
        public void SetModulateAlpha(float value) {
            var color = canvasItem.Modulate;
            color.A = value;
            canvasItem.Modulate = color;
        }
    }
}