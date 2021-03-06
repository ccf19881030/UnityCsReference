// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Configuration;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    internal interface ITextInputField : IEventHandler, ITextElement
    {
        bool hasFocus { get; }

        bool doubleClickSelectsWord { get; }
        bool tripleClickSelectsLine { get; }

        void SyncTextEngine();
        bool AcceptCharacter(char c);
        string CullString(string s);
        void UpdateText(string value);
    }

    public abstract class TextInputBaseField<TValueType> : BaseField<TValueType>
    {
        static CustomStyleProperty<Color> s_SelectionColorProperty = new CustomStyleProperty<Color>("--unity-selection-color");
        static CustomStyleProperty<Color> s_CursorColorProperty = new CustomStyleProperty<Color>("--unity-cursor-color");

        public new class UxmlTraits : BaseFieldTraits<string, UxmlStringAttributeDescription>
        {
            UxmlIntAttributeDescription m_MaxLength = new UxmlIntAttributeDescription { name = "max-length", obsoleteNames = new[] { "maxLength" }, defaultValue = kMaxLengthNone };
            UxmlBoolAttributeDescription m_Password = new UxmlBoolAttributeDescription { name = "password" };
            UxmlStringAttributeDescription m_MaskCharacter = new UxmlStringAttributeDescription { name = "mask-character", obsoleteNames = new[] { "maskCharacter" }, defaultValue = kMaskCharDefault.ToString()};
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };
            UxmlBoolAttributeDescription m_IsReadOnly = new UxmlBoolAttributeDescription { name = "readonly" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var field = ((TextInputBaseField<TValueType>)ve);
                field.maxLength = m_MaxLength.GetValueFromBag(bag, cc);
                field.isPasswordField = m_Password.GetValueFromBag(bag, cc);
                field.isReadOnly = m_IsReadOnly.GetValueFromBag(bag, cc);
                string maskCharacter = m_MaskCharacter.GetValueFromBag(bag, cc);
                if (maskCharacter != null && maskCharacter.Length > 0)
                {
                    field.maskChar = maskCharacter[0];
                }
                field.text = m_Text.GetValueFromBag(bag, cc);
            }
        }

        TextInputBase m_TextInputBase;
        protected TextInputBase textInputBase => m_TextInputBase;

        internal const int kMaxLengthNone = -1;
        internal const char kMaskCharDefault = '*';

        public new static readonly string ussClassName = "unity-base-text-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public static readonly string textInputUssName = "unity-text-input";

        public string text
        {
            get { return m_TextInputBase.text; }
            protected set
            {
                m_TextInputBase.text = value;
            }
        }

        public bool isReadOnly
        {
            get { return m_TextInputBase.isReadOnly; }
            set { m_TextInputBase.isReadOnly = value; }
        }

        // Password field (indirectly lossy behaviour when activated via multiline)
        public bool isPasswordField
        {
            get { return m_TextInputBase.isPasswordField; }
            set { m_TextInputBase.isPasswordField = value; }
        }

        public Color selectionColor => m_TextInputBase.selectionColor;
        public Color cursorColor => m_TextInputBase.cursorColor;


        public int cursorIndex => m_TextInputBase.cursorIndex;
        public int selectIndex => m_TextInputBase.selectIndex;
        public int maxLength
        {
            get { return m_TextInputBase.maxLength; }
            set { m_TextInputBase.maxLength = value; }
        }

        public bool doubleClickSelectsWord
        {
            get { return m_TextInputBase.doubleClickSelectsWord; }
            set { m_TextInputBase.doubleClickSelectsWord = value; }
        }
        public bool tripleClickSelectsLine
        {
            get { return m_TextInputBase.tripleClickSelectsLine; }
            set { m_TextInputBase.tripleClickSelectsLine = value; }
        }

        public bool isDelayed { get; set; }

        public char maskChar
        {
            get { return m_TextInputBase.maskChar; }
            set { m_TextInputBase.maskChar = value; }
        }

        /* internal for VisualTree tests */
        internal TextEditorEventHandler editorEventHandler => m_TextInputBase.editorEventHandler;

        /* internal for VisualTree tests */
        internal TextEditorEngine editorEngine  => m_TextInputBase.editorEngine;

        internal bool hasFocus => m_TextInputBase.hasFocus;

        public void SelectAll()
        {
            m_TextInputBase.SelectAll();
        }

        internal void SyncTextEngine()
        {
            m_TextInputBase.SyncTextEngine();
        }

        internal void DrawWithTextSelectionAndCursor(MeshGenerationContext mgc, string newText)
        {
            m_TextInputBase.DrawWithTextSelectionAndCursor(mgc, newText);
        }

        protected TextInputBaseField(int maxLength, char maskChar, TextInputBase textInputBase)
            : this(null, maxLength, maskChar, textInputBase) {}

        protected TextInputBaseField(string label, int maxLength, char maskChar, TextInputBase textInputBase)
            : base(label, textInputBase)
        {
            tabIndex = 0;
            delegatesFocus = false;

            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            m_TextInputBase = textInputBase;
            m_TextInputBase.maxLength = maxLength;
            m_TextInputBase.maskChar = maskChar;
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt == null)
            {
                return;
            }

            if (evt.eventTypeId == KeyDownEvent.TypeId())
            {
                KeyDownEvent keyDownEvt = evt as KeyDownEvent;

                // We must handle the ETX (char 3) or the \n instead of the KeypadEnter or Return because the focus will
                //     have the drawback of having the second event to be handled by the focused field.
                if ((keyDownEvt?.character == 3) ||     // KeyCode.KeypadEnter
                    (keyDownEvt?.character == '\n'))    // KeyCode.Return
                {
                    visualInput?.Focus();
                }
            }
        }

        protected abstract class TextInputBase : VisualElement, ITextInputField
        {
            string m_OriginalText;

            void SaveValueAndText()
            {
                // When getting the FocusIn, we must keep the value in case of Escape...
                m_OriginalText = text;
            }

            void RestoreValueAndText()
            {
                text = m_OriginalText;
            }

            public void SelectAll()
            {
                editorEngine?.SelectAll();
            }

            internal void SelectNone()
            {
                editorEngine?.SelectNone();
            }

            private void UpdateText(string value)
            {
                if (text != value)
                {
                    // Setting the VisualElement text here cause a repaint since it dirty the layout flag.
                    using (InputEvent evt = InputEvent.GetPooled(text, value))
                    {
                        evt.target = parent;
                        text = value;
                        parent?.SendEvent(evt);
                    }
                }
            }

            public int cursorIndex
            {
                get { return editorEngine.cursorIndex; }
            }

            public int selectIndex
            {
                get { return editorEngine.selectIndex; }
            }

            public bool isReadOnly { get; set; }
            public int maxLength { get; set; }
            public char maskChar { get; set; }

            public virtual bool isPasswordField { get; set; }

            public bool doubleClickSelectsWord { get; set; }
            public bool tripleClickSelectsLine { get; set; }

            internal bool isDragging { get; set; }

            bool touchScreenTextField
            {
                get { return TouchScreenKeyboard.isSupported; }
            }


            Color m_SelectionColor = Color.clear;
            Color m_CursorColor = Color.grey;

            public Color selectionColor => m_SelectionColor;
            public Color cursorColor => m_CursorColor;


            internal bool hasFocus
            {
                get { return elementPanel != null && elementPanel.focusController.GetLeafFocusedElement() == this; }
            }

            /* internal for VisualTree tests */
            internal TextEditorEventHandler editorEventHandler { get; private set; }

            /* internal for VisualTree tests */
            internal TextEditorEngine editorEngine { get; private set; }


            private string m_Text;

            public string text
            {
                get { return m_Text; }
                set
                {
                    if (m_Text == value)
                        return;

                    m_Text = value;
                    editorEngine.text = value;
                    IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
                }
            }

            internal TextInputBase()
            {
                isReadOnly = false;
                focusable = true;

                AddToClassList(inputUssClassName);
                m_Text = string.Empty;
                name = TextField.textInputUssName;


                requireMeasureFunction = true;

                editorEngine = new TextEditorEngine(OnDetectFocusChange, OnCursorIndexChange);

                if (touchScreenTextField)
                {
                    editorEventHandler = new TouchScreenTextEditorEventHandler(editorEngine, this);
                }
                else
                {
                    // TODO: Default values should come from GUI.skin.settings
                    doubleClickSelectsWord = true;
                    tripleClickSelectsLine = true;

                    editorEventHandler = new KeyboardTextEditorEventHandler(editorEngine, this);
                }

                // Make the editor style unique across all textfields
                editorEngine.style = new GUIStyle(editorEngine.style);

                RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
                this.generateVisualContent += OnGenerateVisualContent;
            }

            DropdownMenuAction.Status CutCopyActionStatus(DropdownMenuAction a)
            {
                return (editorEngine.hasSelection && !isPasswordField) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
            }

            DropdownMenuAction.Status PasteActionStatus(DropdownMenuAction a)
            {
                return (editorEngine.CanPaste() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            void ProcessMenuCommand(string command)
            {
                using (ExecuteCommandEvent evt = ExecuteCommandEvent.GetPooled(command))
                {
                    evt.target = this;
                    SendEvent(evt);
                }
            }

            void Cut(DropdownMenuAction a)
            {
                ProcessMenuCommand(EventCommandNames.Cut);
            }

            void Copy(DropdownMenuAction a)
            {
                ProcessMenuCommand(EventCommandNames.Copy);
            }

            void Paste(DropdownMenuAction a)
            {
                ProcessMenuCommand(EventCommandNames.Paste);
            }

            private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
            {
                Color selectionValue = Color.clear;
                Color cursorValue = Color.clear;

                ICustomStyle customStyle = e.customStyle;
                if (customStyle.TryGetValue(s_SelectionColorProperty, out selectionValue))
                    m_SelectionColor = selectionValue;

                if (customStyle.TryGetValue(s_CursorColorProperty, out cursorValue))
                    m_CursorColor = cursorValue;

                SyncGUIStyle(this, editorEngine.style);
            }

            internal virtual void SyncTextEngine()
            {
                editorEngine.text = CullString(text);

                editorEngine.SaveBackup();

                editorEngine.position = layout;

                editorEngine.DetectFocusChange();
            }

            internal string CullString(string s)
            {
                if (maxLength >= 0 && s != null && s.Length > maxLength)
                    return s.Substring(0, maxLength);
                return s;
            }

            internal void OnGenerateVisualContent(MeshGenerationContext mgc)
            {
                // When this is used, we can get rid of the content.text trick and use mask char directly in the text to print
                if (touchScreenTextField)
                {
                    var touchScreenEditor = editorEventHandler as TouchScreenTextEditorEventHandler;
                    if (touchScreenEditor != null && editorEngine.keyboardOnScreen != null)
                    {
                        UpdateText(CullString(editorEngine.keyboardOnScreen.text));

                        if (editorEngine.keyboardOnScreen.status != TouchScreenKeyboard.Status.Visible)
                        {
                            editorEngine.keyboardOnScreen = null;
                            GUI.changed = true;
                        }
                    }

                    // if we use system keyboard we will have normal text returned (hiding symbols is done inside os)
                    // so before drawing make sure we hide them ourselves
                    string drawText = text;
                    if (touchScreenEditor != null && !string.IsNullOrEmpty(touchScreenEditor.secureText))
                        drawText = "".PadRight(touchScreenEditor.secureText.Length, maskChar);

                    text = drawText;
                }
                else
                {
                    if (!hasFocus)
                    {
                        mgc.Text(MeshGenerationContextUtils.TextParams.MakeStyleBased(this, text));
                    }
                    else
                    {
                        DrawWithTextSelectionAndCursor(mgc, text);
                    }
                }
            }

            internal void DrawWithTextSelectionAndCursor(MeshGenerationContext mgc, string newText)
            {
                var keyboardTextEditor = editorEventHandler as KeyboardTextEditorEventHandler;
                if (keyboardTextEditor == null)
                    return;

                keyboardTextEditor.PreDrawCursor(newText);

                int cursorIndex = editorEngine.cursorIndex;
                int selectIndex = editorEngine.selectIndex;
                Rect localPosition = editorEngine.localPosition;
                var scrollOffset = editorEngine.scrollOffset;

                float textScaling = TextNative.ComputeTextScaling(worldTransform, GUIUtility.pixelsPerPoint);

                var textParams = MeshGenerationContextUtils.TextParams.MakeStyleBased(this, text);
                textParams.text = " ";
                textParams.wordWrapWidth = 0.0f;
                textParams.wordWrap = false;

                var textNativeSettings = MeshGenerationContextUtils.TextParams.GetTextNativeSettings(textParams, textScaling);
                float lineHeight = TextNative.ComputeTextHeight(textNativeSettings);

                float wordWrapWidth = 0.0f;

                // Make sure to take into account the word wrap style...
                if (editorEngine.multiline && (resolvedStyle.whiteSpace == WhiteSpace.Normal))
                {
                    wordWrapWidth = contentRect.width;

                    // Since the wrapping is enabled, there is no need to offset the text... It will always fit the space on screen !
                    scrollOffset = Vector2.zero;
                }

                GUIUtility.compositionCursorPos = editorEngine.graphicalCursorPos - scrollOffset +
                    new Vector2(localPosition.x, localPosition.y + lineHeight);

                Color drawCursorColor = cursorColor;

                int selectionEndIndex = string.IsNullOrEmpty(GUIUtility.compositionString)
                    ? selectIndex
                    : cursorIndex + GUIUtility.compositionString.Length;

                CursorPositionStylePainterParameters cursorParams;

                // Draw highlighted section, if any
                if ((cursorIndex != selectionEndIndex) && !isDragging)
                {
                    int min = cursorIndex < selectionEndIndex ? cursorIndex : selectionEndIndex;
                    int max = cursorIndex > selectionEndIndex ? cursorIndex : selectionEndIndex;

                    cursorParams = CursorPositionStylePainterParameters.GetDefault(this, text);
                    cursorParams.text = editorEngine.text;
                    cursorParams.wordWrapWidth = wordWrapWidth;
                    cursorParams.cursorIndex = min;

                    textNativeSettings = cursorParams.GetTextNativeSettings(textScaling);
                    Vector2 minPos = TextNative.GetCursorPosition(textNativeSettings, cursorParams.rect, min);
                    Vector2 maxPos = TextNative.GetCursorPosition(textNativeSettings, cursorParams.rect, max);

                    minPos -= scrollOffset;
                    maxPos -= scrollOffset;

                    if (Mathf.Approximately(minPos.y, maxPos.y))
                    {
                        mgc.Rectangle(new MeshGenerationContextUtils.RectangleParams()
                        {
                            rect = new Rect(minPos.x, minPos.y, maxPos.x - minPos.x, lineHeight),
                            color = selectionColor
                        });
                    }
                    else
                    {
                        // Draw first line
                        mgc.Rectangle(new MeshGenerationContextUtils.RectangleParams()
                        {
                            rect = new Rect(minPos.x, minPos.y, contentRect.xMax - minPos.x, lineHeight),
                            color = selectionColor
                        });

                        var inbetweenHeight = (maxPos.y - minPos.y) - lineHeight;
                        if (inbetweenHeight > 0f)
                        {
                            // Draw all lines in-between
                            mgc.Rectangle(new MeshGenerationContextUtils.RectangleParams()
                            {
                                rect = new Rect(contentRect.xMin, minPos.y + lineHeight, contentRect.width, inbetweenHeight),
                                color = selectionColor
                            });
                        }

                        // Draw last line if not empty
                        if (maxPos.x != contentRect.x)
                        {
                            mgc.Rectangle(new MeshGenerationContextUtils.RectangleParams()
                            {
                                rect = new Rect(contentRect.xMin, maxPos.y, maxPos.x, lineHeight),
                                color = selectionColor
                            });
                        }
                    }
                }

                // Draw the text with the scroll offset
                if (!string.IsNullOrEmpty(editorEngine.text) && contentRect.width > 0.0f && contentRect.height > 0.0f)
                {
                    textParams = MeshGenerationContextUtils.TextParams.MakeStyleBased(this, text);
                    textParams.rect = new Rect(contentRect.x - scrollOffset.x, contentRect.y - scrollOffset.y, contentRect.width + scrollOffset.x, contentRect.height + scrollOffset.y);
                    textParams.text = editorEngine.text;

                    mgc.Text(textParams);
                }

                // Draw the cursor
                if (!isReadOnly && !isDragging)
                {
                    if (cursorIndex == selectionEndIndex && computedStyle.unityFont.value != null)
                    {
                        cursorParams = CursorPositionStylePainterParameters.GetDefault(this, text);
                        cursorParams.text = editorEngine.text;
                        cursorParams.wordWrapWidth = wordWrapWidth;
                        cursorParams.cursorIndex = cursorIndex;

                        textNativeSettings = cursorParams.GetTextNativeSettings(textScaling);
                        Vector2 cursorPosition = TextNative.GetCursorPosition(textNativeSettings, cursorParams.rect, cursorParams.cursorIndex);
                        cursorPosition -= scrollOffset;
                        mgc.Rectangle(new MeshGenerationContextUtils.RectangleParams
                        {
                            rect = new Rect(cursorPosition.x, cursorPosition.y, 1f, lineHeight),
                            color = drawCursorColor
                        });
                    }

                    // Draw alternate cursor, if any
                    if (editorEngine.altCursorPosition != -1)
                    {
                        cursorParams = CursorPositionStylePainterParameters.GetDefault(this, text);
                        cursorParams.text = editorEngine.text.Substring(0, editorEngine.altCursorPosition);
                        cursorParams.wordWrapWidth = wordWrapWidth;
                        cursorParams.cursorIndex = editorEngine.altCursorPosition;

                        textNativeSettings = cursorParams.GetTextNativeSettings(textScaling);
                        Vector2 altCursorPosition = TextNative.GetCursorPosition(textNativeSettings, cursorParams.rect, cursorParams.cursorIndex);
                        altCursorPosition -= scrollOffset;
                        mgc.Rectangle(new MeshGenerationContextUtils.RectangleParams
                        {
                            rect = new Rect(altCursorPosition.x, altCursorPosition.y, 1f, lineHeight),
                            color = drawCursorColor
                        });
                    }
                }

                keyboardTextEditor.PostDrawCursor();
            }

            internal virtual bool AcceptCharacter(char c)
            {
                // when readonly, we do not accept any character
                return !isReadOnly;
            }

            protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
            {
                if (evt?.target is TextInputBase)
                {
                    evt.menu.AppendAction("Cut", Cut, CutCopyActionStatus);
                    evt.menu.AppendAction("Copy", Copy, CutCopyActionStatus);
                    evt.menu.AppendAction("Paste", Paste, PasteActionStatus);
                }
            }

            private void OnDetectFocusChange()
            {
                if (editorEngine.m_HasFocus && !hasFocus)
                {
                    editorEngine.OnFocus();
                }

                if (!editorEngine.m_HasFocus && hasFocus)
                    editorEngine.OnLostFocus();
            }

            private void OnCursorIndexChange()
            {
                IncrementVersion(VersionChangeType.Repaint);
            }

            protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
            {
                // If the text is empty, we should make sure it returns at least the height/width of 1 character...
                var textToUse = m_Text;
                if (string.IsNullOrEmpty(textToUse))
                {
                    textToUse = " ";
                }

                return TextElement.MeasureVisualElementTextSize(this, textToUse, desiredWidth, widthMode, desiredHeight, heightMode);
            }

            protected override void ExecuteDefaultActionAtTarget(EventBase evt)
            {
                base.ExecuteDefaultActionAtTarget(evt);

                if (elementPanel != null && elementPanel.contextualMenuManager != null)
                {
                    elementPanel.contextualMenuManager.DisplayMenuIfEventMatches(evt, this);
                }

                if (evt?.eventTypeId == ContextualMenuPopulateEvent.TypeId())
                {
                    ContextualMenuPopulateEvent e = evt as ContextualMenuPopulateEvent;
                    int count = e.menu.MenuItems().Count;
                    BuildContextualMenu(e);

                    if (count > 0 && e.menu.MenuItems().Count > count)
                    {
                        e.menu.InsertSeparator(null, count);
                    }
                }
                else if (evt.eventTypeId == FocusInEvent.TypeId())
                {
                    SaveValueAndText();
                }
                else if (evt.eventTypeId == KeyDownEvent.TypeId())
                {
                    KeyDownEvent keyDownEvt = evt as KeyDownEvent;

                    if (keyDownEvt?.keyCode == KeyCode.Escape)
                    {
                        RestoreValueAndText();
                        parent.Focus();
                    }
                }

                editorEventHandler.ExecuteDefaultActionAtTarget(evt);
            }

            protected override void ExecuteDefaultAction(EventBase evt)
            {
                base.ExecuteDefaultAction(evt);

                editorEventHandler.ExecuteDefaultAction(evt);
            }

            bool ITextInputField.hasFocus => hasFocus;

            void ITextInputField.SyncTextEngine()
            {
                SyncTextEngine();
            }

            bool ITextInputField.AcceptCharacter(char c)
            {
                return AcceptCharacter(c);
            }

            string ITextInputField.CullString(string s)
            {
                return CullString(s);
            }

            void ITextInputField.UpdateText(string value)
            {
                UpdateText(value);
            }

            private void DeferGUIStyleRectSync()
            {
                RegisterCallback<GeometryChangedEvent>(OnPercentResolved);
            }

            private void OnPercentResolved(GeometryChangedEvent evt)
            {
                UnregisterCallback<GeometryChangedEvent>(OnPercentResolved);

                var guiStyle = editorEngine.style;
                int left = (int)resolvedStyle.marginLeft;
                int top = (int)resolvedStyle.marginTop;
                int right = (int)resolvedStyle.marginRight;
                int bottom = (int)resolvedStyle.marginBottom;
                AssignRect(guiStyle.margin, left, top, right, bottom);

                left = (int)resolvedStyle.paddingLeft;
                top = (int)resolvedStyle.paddingTop;
                right = (int)resolvedStyle.paddingRight;
                bottom = (int)resolvedStyle.paddingBottom;
                AssignRect(guiStyle.padding, left, top, right, bottom);
            }

            private static void SyncGUIStyle(TextInputBase textInput, GUIStyle style)
            {
                var computedStyle = textInput.computedStyle;
                style.alignment = computedStyle.unityTextAlign.GetSpecifiedValueOrDefault(style.alignment);
                style.wordWrap = computedStyle.whiteSpace.specificity != StyleValueExtensions.UndefinedSpecificity
                    ? computedStyle.whiteSpace.value == WhiteSpace.Normal
                    : style.wordWrap;
                bool overflowVisible = computedStyle.overflow.specificity != StyleValueExtensions.UndefinedSpecificity
                    ? computedStyle.overflow.value == Overflow.Visible
                    : style.clipping == TextClipping.Overflow;
                style.clipping = overflowVisible ? TextClipping.Overflow : TextClipping.Clip;
                if (computedStyle.unityFont.value != null)
                {
                    style.font = computedStyle.unityFont.value;
                }

                style.fontSize = (int)computedStyle.fontSize.GetSpecifiedValueOrDefault((float)style.fontSize);
                style.fontStyle = computedStyle.unityFontStyleAndWeight.GetSpecifiedValueOrDefault(style.fontStyle);

                int left = computedStyle.unitySliceLeft.value;
                int top = computedStyle.unitySliceTop.value;
                int right = computedStyle.unitySliceRight.value;
                int bottom = computedStyle.unitySliceBottom.value;
                AssignRect(style.border, left, top, right, bottom);

                if (IsLayoutUsingPercent(textInput))
                {
                    textInput.DeferGUIStyleRectSync();
                }
                else
                {
                    left = (int)computedStyle.marginLeft.value.value;
                    top = (int)computedStyle.marginTop.value.value;
                    right = (int)computedStyle.marginRight.value.value;
                    bottom = (int)computedStyle.marginBottom.value.value;
                    AssignRect(style.margin, left, top, right, bottom);

                    left = (int)computedStyle.paddingLeft.value.value;
                    top = (int)computedStyle.paddingTop.value.value;
                    right = (int)computedStyle.paddingRight.value.value;
                    bottom = (int)computedStyle.paddingBottom.value.value;
                    AssignRect(style.padding, left, top, right, bottom);
                }
            }

            private static bool IsLayoutUsingPercent(VisualElement ve)
            {
                var computedStyle = ve.computedStyle;

                // Margin
                if (computedStyle.marginLeft.value.unit == LengthUnit.Percent ||
                    computedStyle.marginTop.value.unit == LengthUnit.Percent ||
                    computedStyle.marginRight.value.unit == LengthUnit.Percent ||
                    computedStyle.marginBottom.value.unit == LengthUnit.Percent)
                    return true;

                // Padding
                if (computedStyle.paddingLeft.value.unit == LengthUnit.Percent ||
                    computedStyle.paddingTop.value.unit == LengthUnit.Percent ||
                    computedStyle.paddingRight.value.unit == LengthUnit.Percent ||
                    computedStyle.paddingBottom.value.unit == LengthUnit.Percent)
                    return true;

                return false;
            }

            private static void AssignRect(RectOffset rect, int left, int top, int right, int bottom)
            {
                rect.left = left;
                rect.top = top;
                rect.right = right;
                rect.bottom = bottom;
            }
        }
    }
}
