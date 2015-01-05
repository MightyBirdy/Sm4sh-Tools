﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace AnimCmd
{
    /// <summary>
    /// Provides an extended RichTextBox with Intellisense like code completion and syntax highlighting.
    /// </summary>
    class ITSCodeBox : RichTextBox
    {
        #region Members
        private static bool _render = true;
        private List<EventInfo> dictionary;
        private ListBox AutocompleteBox;
        #endregion

        #region External Methods
        [DllImport("user32")]
        private extern static int GetCaretPos(out Point p);
        #endregion

        #region Constructors

        public ITSCodeBox()
            : base()
        {
            AutocompleteBox = new ListBox();
            AutocompleteBox.Parent = this;
            AutocompleteBox.KeyUp += OnKeyUp;
            AutocompleteBox.Visible = false;
            this.dictionary = new List<EventInfo>();
        }
        #endregion

        #region Properties
        /// <summary>
        /// The autocomplete dictionary.
        /// </summary>
        public List<EventInfo> Dictionary
        {
            get { return this.dictionary; }
            set { this.dictionary = value; }
        }
        /// <summary>
        /// The text of the current line.
        /// </summary>
        public string CurrentLineText
        {
            get
            {
                if (Lines.Length > 0)
                    return Lines[CurrentLineIndex];
                else return "";
            }
        }
        /// <summary>
        /// The index of the current line.
        /// </summary>
        public int CurrentLineIndex
        {
            get
            {
                Point cp;
                GetCaretPos(out cp);
                return GetLineFromCharIndex(GetCharIndexFromPosition(cp));
            }
        }
        #endregion

        #region Methods
        protected override void OnKeyDown(KeyEventArgs e)
        {   
            if ((e.KeyCode == Keys.Enter | e.KeyCode == Keys.Down | e.KeyCode == Keys.Space) && AutocompleteBox.Visible == true)
            {
                AutocompleteBox.Focus();
                if (e.KeyCode == Keys.Down && !(AutocompleteBox.SelectedIndex >= AutocompleteBox.Items.Count))
                    AutocompleteBox.SelectedIndex++;
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape | CurrentLineText == "" && AutocompleteBox.Visible == true)
            {
                AutocompleteBox.Visible = false;
                e.Handled = true;
            }
        }
        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter | e.KeyCode == Keys.Space)
            {
                Point cp;
                string TempStr = this.Text.Remove(GetFirstCharIndexFromLine(CurrentLineIndex), Lines[CurrentLineIndex].Length);
                this.Text = TempStr.Insert(GetFirstCharIndexFromLine(CurrentLineIndex), ((ListBox)sender).SelectedItem.ToString());
                GetCaretPos(out cp);
                this.Select(GetFirstCharIndexFromLine(CurrentLineIndex) + CurrentLineText.Length, 0);
                AutocompleteBox.Hide();
                this.Focus();
                ProcessAllLines();
            }

        }
        /// <summary>
        /// WndProc
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x00f)
            {
                if (_render)
                    base.WndProc(ref m);
                else
                    m.Result = IntPtr.Zero;
            }
            else
                base.WndProc(ref m);
        }
        /// <summary>
        /// OnTextChanged event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnTextChanged(EventArgs e)
        {
            if (!_render)
                return;

            Point cp;
            GetCaretPos(out cp);
            AutocompleteBox.SetBounds(cp.X + this.Left, cp.Y + 10, 280, 70);

            List<string> FilteredList =
                dictionary.Where(s => s.Name.StartsWith(CurrentLineText)).Select(m => m.Name).ToList();

            if (FilteredList.Count != 0 && !CurrentLineText.EndsWith(")") &&
                !CurrentLineText.EndsWith("(") && CurrentLineText != "")
            {
                AutocompleteBox.DataSource = FilteredList;
                AutocompleteBox.Show();
            }
            else
                AutocompleteBox.Hide();

            // Process this line.
            FormatLine(CurrentLineIndex);
        }
        /// <summary>
        /// Process a line.
        /// <param name="LineIndex"> The index of the line to process.</param>
        /// </summary>
        private void FormatLine(int LineIndex)
        {
            if (Lines.Length == 0)
                return;

            _render = false;

            // Save the position and make the whole line black
            int nPosition = SelectionStart;
            SelectionStart = GetFirstCharIndexFromLine(LineIndex);
            SelectionLength = Lines[LineIndex].Length;
            SelectionColor = Color.Black;

            // Process numbers
            Format(LineIndex, "\\b(?:[0-9]*\\.)?[0-9]+\\b", Color.Red); // Decimal
            Format(LineIndex, @"\b0x[a-fA-F\d]+\b", Color.DarkCyan); // Hexadecimal
            // Process parenthesis
            Format(LineIndex, "[\x28-\x2c]", Color.Blue);
            // Process strings
            Format(LineIndex, "\"[^\"\\\\\\r\\n]*(?:\\\\.[^\"\\\\\\r\\n]*)*\"", Color.DarkRed);
            // Process comments
            Format(LineIndex, "//.*$", Color.DarkGreen);

            SelectionStart = nPosition;
            SelectionLength = 0;
            SelectionColor = Color.Black;

            _render = true;
            this.Invalidate();
        }
        /// <summary>
        /// Process a regular expression, painting the matched syntax.
        /// </summary>
        /// <param name="LineIndex"> The index of the line to process.</param>
        /// <param name="Regex">The regular expression to use in evaluation.</param>
        /// <param name="color">The color to paint matches.</param>
        private void Format(int LineIndex, string strRegex, Color color)
        {
            Regex regKeywords = new Regex(strRegex, RegexOptions.IgnoreCase);
            Match regMatch;


            for (regMatch = regKeywords.Match(Lines[LineIndex]); regMatch.Success; regMatch = regMatch.NextMatch())
            {
                // Process the words
                int nStart = GetFirstCharIndexFromLine(LineIndex) + regMatch.Index;
                int nLenght = regMatch.Length;
                SelectionStart = nStart;
                SelectionLength = nLenght;
                SelectionColor = color;
            }

        }
        /// <summary>
        /// Processes all lines in the code box.
        /// </summary>
        public void ProcessAllLines()
        {
            for (int i = 0; i < Lines.Length; i++)
                FormatLine(i);
        }
        #endregion

    }
}