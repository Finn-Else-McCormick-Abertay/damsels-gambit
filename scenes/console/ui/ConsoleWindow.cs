using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DamselsGambit.Util;

namespace DamselsGambit;

public partial class ConsoleWindow : Control
{
	[Export] public RichTextLabel OutputLabel { get; private set; }
	[Export] public TextEdit TextEdit { get; private set; }
	[Export] public TextEdit Autofill { get; private set; }

	private IEnumerable<string> _autofillSuggestions;
	private string _autofillSuggestion = "";

	private readonly List<string> _history = [ "" ];
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
		TextEdit.ConnectAll((Control.SignalName.GuiInput, Callable.From<InputEvent>(OnTextEditGuiInput)), (TextEdit.SignalName.CaretChanged, Callable.From(UpdateAutofillSuggestion)));
		var editMenu = TextEdit.GetMenu();
		for (int i = 13; i > 8; --i) editMenu.RemoveItem(i);

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
		OutputLabel.GetVScrollBar().Value = OutputLabel.GetContentHeight();
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
		
		_autofillSuggestions = Console.GetAutofillSuggestions(priorSb.ToString());
		if (_autofillSuggestion != "<<null>>" && (_autofillSuggestion == "" || !_autofillSuggestions.Contains(_autofillSuggestion))) { _autofillSuggestion = _autofillSuggestions.Any() ? _autofillSuggestions.First() : ""; }
		if (wordUnderCaret == _autofillSuggestion) { _autofillSuggestion = ""; }
		if (_autofillSuggestion == "" || _autofillSuggestion == "<<null>>") { Autofill.Text = ""; return; }
		
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

		_autofillSuggestions = [];
		_autofillSuggestion = "";
		UpdateAutofillSuggestion();
	}

	private static readonly StringName ConsoleNewline = "console_newline";
	private static readonly StringName UiTextNewline = "ui_text_newline";
	private static readonly StringName UiTextCompletionReplace = "ui_text_completion_replace";
	private static readonly StringName UiTextCompletionEscape = "console_text_completion_escape";
	private static readonly StringName UiTextCaretLeft = "ui_text_caret_left";
	private static readonly StringName UiTextCaretRight = "ui_text_caret_right";
	private static readonly StringName UiTextCaretUp = "ui_text_caret_up";
	private static readonly StringName UiTextCaretDown = "ui_text_caret_down";

	private void OnTextEditGuiInput(InputEvent @event) {
		if (@event.IsActionPressed(ConsoleNewline)) {
			TextEdit.InsertTextAtCaret("\n");
			TextEdit.AcceptEvent();
		}
		else if (@event.IsActionPressed(UiTextNewline)) {
			if (TextEdit.Text != "") {
				_history[_historyIndex] = TextEdit.Text;
				_history.Insert(_historyIndex, "");
			}
			Console.ParseCommand(TextEdit.Text);
			TextEdit.Text = "";
			_autofillSuggestions = [];
			_autofillSuggestion = "";
			OutputLabel.GetVScrollBar().Value = OutputLabel.GetContentHeight();
			TextEdit.AcceptEvent();
		}

		if (@event.IsActionPressed(UiTextCompletionReplace)) {
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
				TextEdit.SetCaretColumn(TextEdit.Text.Length);
				TextEdit.AcceptEvent();
			}
		}
		if (@event.IsActionPressed(UiTextCaretDown)) {
			if (TextEdit.GetCaretLine() == TextEdit.GetVisibleLineCount() - 1 && _historyIndex > 0) {
				_history[_historyIndex] = TextEdit.Text;
				_historyIndex--;
				TextEdit.Text = _history[_historyIndex];
				TextEdit.SetCaretColumn(TextEdit.Text.Length);
				TextEdit.AcceptEvent();
			}
		}

		if (@event.IsActionPressed(UiTextCompletionEscape) && _autofillSuggestions.Any()) {
			if (_autofillSuggestion != "<<null>>") { _autofillSuggestion = "<<null>>"; } else { _autofillSuggestion = ""; }
			UpdateAutofillSuggestion();
			TextEdit.AcceptEvent();
		}

		if (@event.IsActionPressed(UiTextCaretLeft) && _autofillSuggestions.Any() && _autofillSuggestion != "" && _autofillSuggestion != "<<null>>") {
			if (_autofillSuggestions.Contains(_autofillSuggestion)) {
				var index = _autofillSuggestions.ToList().IndexOf(_autofillSuggestion) - 1;
				if (index < 0) index += _autofillSuggestions.Count();
				if (index >= _autofillSuggestions.Count()) index -= _autofillSuggestions.Count();
				_autofillSuggestion = _autofillSuggestions.ElementAt(index);
				UpdateAutofillSuggestion();
				TextEdit.AcceptEvent();
			}
		}
		if (@event.IsActionPressed(UiTextCaretRight) && _autofillSuggestions.Any() && _autofillSuggestion != "" && _autofillSuggestion != "<<null>>") {
			if (_autofillSuggestions.Contains(_autofillSuggestion)) {
				var index = _autofillSuggestions.ToList().IndexOf(_autofillSuggestion) + 1;
				if (index < 0) index += _autofillSuggestions.Count();
				if (index >= _autofillSuggestions.Count()) index -= _autofillSuggestions.Count();
				_autofillSuggestion = _autofillSuggestions.ElementAt(index);
				UpdateAutofillSuggestion();
				TextEdit.AcceptEvent();
			}
		}
	}
}
