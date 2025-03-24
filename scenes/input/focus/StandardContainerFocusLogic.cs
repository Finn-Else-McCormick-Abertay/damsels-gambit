using System.Collections.Generic;
using System.Linq;
using Godot;

namespace DamselsGambit;

public static class StandardContainerFocusLogic
{
	private static bool IsHorizontal(FocusDirection direction) => direction switch { FocusDirection.Left or FocusDirection.Right => true, _ => false };
	private static bool IsVertical(FocusDirection direction) => direction switch { FocusDirection.Up or FocusDirection.Down => true, _ => false };

	private static bool IsAxisAnd<LeftRightType, UpDownType>(Node node, FocusDirection direction) =>
		direction switch { FocusDirection.Left or FocusDirection.Right when node is LeftRightType => true, FocusDirection.Up or FocusDirection.Down when node is UpDownType => true, _ => false };

    public static Node GetNextFocus(Control root, Node container, FocusDirection direction, Node child) {
        if (IsAxisAnd<HBoxContainer, VBoxContainer>(container, direction)) {
            var indexDirection = direction switch {
                FocusDirection.Left or FocusDirection.Up => -1,
                FocusDirection.Right or FocusDirection.Down => 1,
                FocusDirection.None => 0
            };
            int index = child.GetIndex() + indexDirection;
            while (index >= 0 && index < container.GetChildCount()) {
                var nextFocus = InputManager.FindFocusableWithin(container.GetChild(index), direction);
                if (nextFocus is not null) return nextFocus;
                index += indexDirection;
            }
        }

        if (container is GridContainer gridContainer && gridContainer.Columns > 0) {
            int originalIndex = child.GetIndex();
            var indexJump = direction switch {
                FocusDirection.Up => -gridContainer.Columns, FocusDirection.Down => gridContainer.Columns,
                FocusDirection.Left => -1, FocusDirection.Right => 1,
                FocusDirection.None => 0
            };
            int index = originalIndex + indexJump;
            while (index >= 0 && index < container.GetChildCount() && direction switch { FocusDirection.Left => (index + 1) % gridContainer.Columns != 0, FocusDirection.Right => index % gridContainer.Columns != 0, _ => true }) {
                var nextFocus = InputManager.FindFocusableWithin(container.GetChild(index), direction);
                if (nextFocus is not null) return nextFocus;
                index += indexJump;
            }
            if (index >= container.GetChildCount() && originalIndex < container.GetChildCount() - (container.GetChildCount() % gridContainer.Columns)) {
                var nextFocus = InputManager.FindFocusableWithin(container.GetChildren().Last(), direction);
                if (nextFocus is not null) return nextFocus;
            }
        }

        if (container is TabContainer tabContainer) {
            if (direction == FocusDirection.Down && root is TabBar && InputManager.FindFocusableWithin(tabContainer.GetCurrentTabControl(), direction) is Control nextFocus) return nextFocus;
            if (direction == FocusDirection.Up) return tabContainer.GetTabBar();
        }
        return null;
    }

    public static bool UseDirectionalInput(Control control, FocusDirection direction) {
        if (control is Slider slider && slider.Editable && slider.Value >= slider.MinValue && slider.Value <= slider.MaxValue && IsAxisAnd<HSlider, VSlider>(control, direction)) {
			slider.Value += slider.Step * direction switch { FocusDirection.Left or FocusDirection.Down => -1f, FocusDirection.Right or FocusDirection.Up => 1f, _ => 0f }; return true;
		}
		if (control is TabBar tabBar && IsHorizontal(direction) && direction switch { FocusDirection.Left => tabBar.SelectPreviousAvailable(), FocusDirection.Right => tabBar.SelectNextAvailable(), _ => false }) return true;
        return false;
    }
}