using Godot;
using System;
using System.Collections.Generic;
using System.Text;

namespace DamselsGambit;

public partial class ConsoleWindow : Control
{
	[Export] public RichTextLabel OutputLabel { get; private set; }
	[Export] public TextEdit TextEdit { get; private set; }
	[Export] public TextEdit Autofill { get; private set; }

	private string _autofillSuggestion = "";

	private List<string> _history = [ "" ];
	private int _historyIndex = 0;

	public override void _EnterTree() {
		Console.OnPrint += OnPrint;
		Console.OnClear += OnClear;
		VisibilityChanged += OnVisibilityChanged;
	}
	public override void _ExitTree() {
		Console.OnPrint -= OnPrint;
		Console.OnClear -= OnClear;
		VisibilityChanged -= OnVisibilityChanged;
	}

	public override void _Ready() {
		TextEdit.Text = ""; OutputLabel.Text = ""; Autofill.Text = "";
		TextEdit.Connect(Control.SignalName.GuiInput, new Callable(this, MethodName.OnTextEditGuiInput));
		TextEdit.Connect(TextEdit.SignalName.CaretChanged, new Callable(this, MethodName.UpdateAutofillSuggestion));
		var editMenu = TextEdit.GetMenu();
		for (int i = 13; i > 8; --i) { editMenu.RemoveItem(i); }

		_fontHeight = Theme.GetFont("normal_font", "RichTextLabel").GetHeight(Theme.GetFontSize("normal_font_size", "RichTextLabel")) + Theme.GetConstant("line_padding", "RichTextLabel");
	}

	private float _fontHeight;

	private void OnPrint(string text) {
		if (!Visible) return;
		var scrollBar = OutputLabel.GetVScrollBar();
		bool shouldReadjustScroll = scrollBar.Value + _fontHeight + OutputLabel.Size.Y >= OutputLabel.GetContentHeight();

		OutputLabel.Text = Console.LogText;

		if (shouldReadjustScroll) { Callable.From(() => { scrollBar.Value = OutputLabel.GetContentHeight(); }).CallDeferred(); }
	}

	private void OnClear() { OutputLabel.Text = Console.LogText; }

	private void OnVisibilityChanged() {
		if (!Visible) return;
		OutputLabel.Text = Console.LogText;
		UpdateAutofillSuggestion();
	}

	private void UpdateAutofillSuggestion() {
		if (TextEdit.GetCaretCount() > 1) { _autofillSuggestion = Autofill.Text = ""; return; }

		int caretLine = TextEdit.GetCaretLine(), caretColumn = TextEdit.GetCaretColumn();

		var priorSb = new StringBuilder();
		for (int i = 0; i < caretLine; ++i) { priorSb.AppendLine(TextEdit.GetLine(i)); }
		var lineString = TextEdit.GetLine(caretLine);
		var lastWordBeginning = lineString[..caretColumn].LastIndexOf(' ') + 1;
		var nextSpace = lineString.Find(' ', caretColumn); if (nextSpace == -1) { nextSpace = lineString.Length; }
		var wordUnderCaret = TextEdit.GetWordUnderCaret(); if (wordUnderCaret == "") { wordUnderCaret = lineString[lastWordBeginning..nextSpace]; }
		if (lineString.Length > caretColumn) {
			priorSb.Append(lineString[..lastWordBeginning]);
			priorSb.Append(wordUnderCaret);
		}
		else { priorSb.Append(lineString); }
		
		_autofillSuggestion = Console.GetAutofillSuggestion(priorSb.ToString());
		if (wordUnderCaret == _autofillSuggestion) { _autofillSuggestion = ""; }
		if (_autofillSuggestion == "") { Autofill.Text = ""; return; }
		
		var fillSb = new StringBuilder();
		fillSb.Append('\n', caretLine);
		fillSb.Append(' ', lastWordBeginning);
		fillSb.Append(_autofillSuggestion);

		Autofill.Text = fillSb.ToString();
	}

	private void AcceptAutofill() {
		int caretLine = TextEdit.GetCaretLine(), caretColumn = TextEdit.GetCaretColumn();

		var sb = new StringBuilder();
		var lineString = TextEdit.GetLine(caretLine);
		var lastWordBeginning = lineString[..caretColumn].LastIndexOf(' ') + 1;
		var nextSpace = lineString.Find(' ', caretColumn);
		sb.Append(lineString[0..lastWordBeginning]);

		sb.Append(_autofillSuggestion);

		if (lineString.Length > caretColumn && nextSpace != -1) { sb.Append(lineString[nextSpace..]); }

		var newLine = sb.ToString();
		TextEdit.SetLine(caretLine, newLine);
		var newNextSpace = newLine.Find(' ', caretColumn); if (newNextSpace == -1) { newNextSpace = newLine.Length; }
		TextEdit.SetCaretColumn(newNextSpace);
		Callable.From(() => {
		}).CallDeferred();

		UpdateAutofillSuggestion();
	}

	private StringName ConsoleNewlineName = "console_newline";
	private StringName UiTextNewlineName = "ui_text_newline";
	private StringName UiTextCompletionReplaceName = "ui_text_completion_replace";
	private StringName UiTextCaretLeft = "ui_text_caret_left";
	private StringName UiTextCaretRight = "ui_text_caret_right";
	private StringName UiTextCaretUp = "ui_text_caret_up";
	private StringName UiTextCaretDown = "ui_text_caret_down";

	private void OnTextEditGuiInput(InputEvent @event) {
		if (@event.IsActionPressed(ConsoleNewlineName)) {
			TextEdit.InsertTextAtCaret("\n");
			TextEdit.AcceptEvent();
		}
		else if (@event.IsActionPressed(UiTextNewlineName)) {
			if (TextEdit.Text != "") {
				_history[_historyIndex] = TextEdit.Text;
				_history.Insert(_historyIndex, "");
			}
			Console.ParseCommand(TextEdit.Text);
			TextEdit.Text = "";
			OutputLabel.GetVScrollBar().Value = OutputLabel.GetContentHeight();
			TextEdit.AcceptEvent();
		}

		if (@event.IsActionPressed(UiTextCompletionReplaceName)) {
			if (_autofillSuggestion != "") {
				AcceptAutofill();
				TextEdit.AcceptEvent();
			}
		}

		if (@event.IsActionPressed(UiTextCaretUp)) {
			if (TextEdit.GetCaretLine() == 0 && _historyIndex < _history.Count - 1) {
				_history[_historyIndex] = TextEdit.Text;
				_historyIndex++;
				TextEdit.Text = _history[_historyIndex];
				TextEdit.AcceptEvent();
			}
		}
		if (@event.IsActionPressed(UiTextCaretDown)) {
			if (TextEdit.GetCaretLine() == TextEdit.GetVisibleLineCount() - 1 && _historyIndex > 0) {
				_history[_historyIndex] = TextEdit.Text;
				_historyIndex--;
				TextEdit.Text = _history[_historyIndex];
				TextEdit.AcceptEvent();
			}
		}
	}
}
