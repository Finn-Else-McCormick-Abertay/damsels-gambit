#if TOOLS
using Godot;
using System;
using DamselsGambit.Util;
using Godot.Collections;
using System.Linq;

namespace DamselsGambit.Editor.FocusTagPlugin;

[Tool]
public partial class TaggedNodePathProperty : EditorProperty
{
    private Button _openButton;
    private TextEdit _textEdit;

    private string _currentValue;
    private bool _isNodePathSelected = false;

    private bool _updating = false;
    private bool _hasInitialised = false;

    private enum RightClickMenuButtons {
        TextWritingDirection = 7,
        DisplayControlCharacters = 12,
        InsertControlCharacter = 13,
        EmojiAndSymbols = 30,
        InsertNodePath = 100
    }

    public TaggedNodePathProperty() {
        _openButton = new() { Alignment = HorizontalAlignment.Left, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
        _textEdit = new() { CustomMinimumSize = new (0, 100), DrawTabs = true, IndentWrappedLines = true, EmojiMenuEnabled = false, CaretMoveOnRightClick = false, AutowrapMode = TextServer.AutowrapMode.WordSmart };
        var textEditMenu = _textEdit.GetMenu();

        // Remove pointless options
        textEditMenu.RemoveItem(textEditMenu.GetItemIndex((int)RightClickMenuButtons.TextWritingDirection));
        textEditMenu.RemoveItem(textEditMenu.GetItemIndex((int)RightClickMenuButtons.DisplayControlCharacters));
        textEditMenu.RemoveItem(textEditMenu.GetItemIndex((int)RightClickMenuButtons.InsertControlCharacter));
        textEditMenu.RemoveItem(textEditMenu.GetItemIndex((int)RightClickMenuButtons.EmojiAndSymbols));

        // Remove first and last two separators
        textEditMenu.RemoveItem(RangeOf<int>.UpTo(textEditMenu.ItemCount).First(textEditMenu.IsItemSeparator));
        textEditMenu.RemoveItem(RangeOf<int>.UpTo(textEditMenu.ItemCount).Last(textEditMenu.IsItemSeparator));
        textEditMenu.RemoveItem(RangeOf<int>.UpTo(textEditMenu.ItemCount).Last(textEditMenu.IsItemSeparator));
        
        // Add options
        textEditMenu.AddSeparator();
        textEditMenu.AddItem("Insert NodePath", (int)RightClickMenuButtons.InsertNodePath);
        textEditMenu.SetItemTooltip(textEditMenu.GetItemIndex((int)RightClickMenuButtons.InsertNodePath), "Insert relative path to a node.");
        textEditMenu.Connect(PopupMenu.SignalName.IdPressed, this, MethodName.OnRightClickMenuIdPressed);

        AddChild(_openButton); AddFocusable(_openButton);
        AddChild(_textEdit); AddFocusable(_textEdit); _textEdit.Hide();
        _openButton.Connect(Button.SignalName.Pressed, this, MethodName.OnButtonPressed);
        _textEdit.Connect(TextEdit.SignalName.TextChanged, this, MethodName.OnTextEditChanged);
        _textEdit.Connect(TextEdit.SignalName.TextSet, this, MethodName.OnTextEditSet);
        _textEdit.Connect(TextEdit.SignalName.CaretChanged, this, MethodName.OnCaretChanged);

        Refresh();
    }

    public override void _UpdateProperty() {
        var newValue = GetEditedObject().Get(GetEditedProperty()).AsNodePath();
        if (newValue.ToString() == _currentValue) return;

        _updating = true;
        _currentValue = newValue; Refresh();
        _updating = false; _hasInitialised = true;
    }

    public override void _SetReadOnly(bool readOnly) {
        _textEdit.Editable = !readOnly;
    }

    private void OnRightClickMenuIdPressed(int id) {
        if (id == (int)RightClickMenuButtons.InsertNodePath) {
            void OnNodeSelected(NodePath nodePath) {
                if (nodePath.IsEmpty) return;

                var pointedToNode = EditorInterface.Singleton.GetEditedSceneRoot().GetNode(nodePath);
                var pathFromEditedToPointedTo = (GetEditedObject() as Node)?.GetPathTo(pointedToNode, true);
                
                _textEdit.InsertTextAtCaret(pathFromEditedToPointedTo);
            }
            EditorInterface.Singleton.PopupNodeSelector(Callable.From<NodePath>(OnNodeSelected), [ "Control" ], (GetEditedObject() as Node)?.GetNodeOrNull(_textEdit.GetSelectedText()));
        }
    }

    private void OnCaretChanged() {
        bool isNodePathSelected = (GetEditedObject() as Node)?.GetNodeOrNull(_textEdit.GetSelectedText()) is not null;
        if (isNodePathSelected != _isNodePathSelected) {
            var textEditMenu = _textEdit.GetMenu();
            textEditMenu.SetItemText(textEditMenu.GetItemIndex((int)RightClickMenuButtons.InsertNodePath), isNodePathSelected ? "Edit NodePath" : "Insert NodePath");
        }
        _isNodePathSelected = isNodePathSelected;
    }

    private void OnButtonPressed() {
        _textEdit.Visible = !_textEdit.Visible;
        SetBottomEditor(_textEdit.Visible ? _textEdit : null);
    }

    private void OnTextEditChanged() {
        if (_updating) return;
        _currentValue = _textEdit.Text.TrimEnd();
        RefreshButton();
        EmitChanged(GetEditedProperty(), _currentValue);
    }

    private void OnTextEditSet() { if (_hasInitialised) OnTextEditChanged(); }

    private void Refresh() {
        RefreshButton();
        var caretPositions = RangeOf<int>.UpTo(_textEdit.GetCaretCount()).Select(i => (_textEdit.GetCaretLine(i), _textEdit.GetCaretColumn(i))).ToArray();
        _textEdit.Text = _currentValue;
        RangeOf<int>.UpTo(Math.Min(caretPositions.Length, _textEdit.GetCaretCount())).ForEach(i => {
            var (line, col) = caretPositions[i];
            _textEdit.SetCaretLine(line, true, true, 0, i); _textEdit.SetCaretColumn(col, true, i);
        });
    }

    private void RefreshButton() {
        _openButton.Text = _currentValue?.Replace('\n', ' ');
    }
}

#endif
