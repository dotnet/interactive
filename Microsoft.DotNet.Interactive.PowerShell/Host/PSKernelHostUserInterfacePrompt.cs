// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using System.Text;

namespace Microsoft.DotNet.Interactive.PowerShell.Host
{
    public partial class PSKernelHostUserInterface
    {
        // Note: the prompt handling code is from the PowerShell ConsoleHost implementation,
        // with the necessary refactoring and updates for it to work with the PowerShell kernel.

        private const char PromptCommandPrefix = '!';

        /// <summary>
        /// Guarantee a contrasting color for the prompt...
        /// </summary>
        private ConsoleColor PromptColor
        {
            get
            {
                switch (RawUI.BackgroundColor)
                {
                    case ConsoleColor.White: return ConsoleColor.Black;
                    case ConsoleColor.Cyan: return ConsoleColor.Black;
                    case ConsoleColor.DarkYellow: return ConsoleColor.Black;
                    case ConsoleColor.Yellow: return ConsoleColor.Black;
                    case ConsoleColor.Gray: return ConsoleColor.Black;
                    case ConsoleColor.Green: return ConsoleColor.Black;
                    default: return ConsoleColor.Magenta;
                }
            }
        }

        /// <summary>
        /// Guarantee a contrasting color for the default prompt that is slightly
        /// different from the other prompt elements.
        /// </summary>
        private ConsoleColor DefaultPromptColor
        {
            get
            {
                if (PromptColor == ConsoleColor.Magenta)
                    return ConsoleColor.Yellow;
                else
                    return ConsoleColor.Blue;
            }
        }

        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            string paramName = nameof(descriptions);
            if (descriptions == null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (descriptions.Count < 1)
            {
                throw new ArgumentException(
                    $"The '{paramName}' collection must have at least one element.",
                    paramName);
            }

            // we lock here so that multiple threads won't interleave the various reads and writes here.
            lock (_instanceLock)
            {
                var results = new Dictionary<string, PSObject>();

                if (!string.IsNullOrEmpty(caption))
                {
                    WriteLine();
                    WriteLine(PromptColor, RawUI.BackgroundColor, caption);
                }

                if (!string.IsNullOrEmpty(message))
                {
                    WriteLine(message);
                }

                if (descriptions.Any(d => d != null && !string.IsNullOrEmpty(d.HelpMessage)))
                {
                    WriteLine("(Type !? for Help.)");
                }

                for (int index = 0; index < descriptions.Count; index ++)
                {
                    FieldDescription fieldDescription = descriptions[index]
                        ?? throw new ArgumentException($"'{paramName}[{index}]' cannot be null.", paramName);

                    PSObject inputPSObject = null;
                    string fieldName = fieldDescription.Name;

                    if (!LanguagePrimitives.TryConvertTo(fieldDescription.ParameterAssemblyFullName, out Type fieldType) &&
                        !LanguagePrimitives.TryConvertTo(fieldDescription.ParameterTypeFullName, out fieldType))
                    {
                        fieldType = typeof(string);
                    }

                    if (typeof(IList).IsAssignableFrom(fieldType))
                    {
                        var inputList = new List<object>();
                        Type elementType = fieldType.IsArray ? fieldType.GetElementType() : typeof(object);

                        var listInputPrompt = new StringBuilder(fieldName);
                        while (true)
                        {
                            listInputPrompt.Append($"[{inputList.Count}]: ");
                            string inputString = PromptForSingleItem(
                                elementType,
                                fieldDescription,
                                listInputPrompt.ToString(),
                                isListInput: true,
                                out bool listInputDone,
                                out object convertedObj);

                            if (listInputDone)
                            {
                                break;
                            }

                            inputList.Add(convertedObj);
                            // Remove the indices from the prompt
                            listInputPrompt.Length = fieldName.Length;
                        }

                        inputPSObject = LanguagePrimitives.TryConvertTo(inputList, fieldType, out object tryConvertResult)
                            ? PSObject.AsPSObject(tryConvertResult)
                            : PSObject.AsPSObject(inputList);
                    }
                    else
                    {
                        // field is not a list
                        string inputPrompt = $"{fieldName}: ";

                        PromptForSingleItem(
                            fieldType,
                            fieldDescription,
                            inputPrompt,
                            isListInput: false,
                            out bool _,
                            out object convertedObj);

                        inputPSObject = PSObject.AsPSObject(convertedObj);
                    }

                    results.Add(fieldName, inputPSObject);
                }

                return results;
            }
        }

        private string PromptForSingleItem(
            Type fieldType,
            FieldDescription fieldDescription,
            string fieldPrompt,
            bool isListInput,
            out bool listInputDone,
            out object convertedObj)
        {
            listInputDone = false;
            convertedObj = null;

            if (fieldType == typeof(SecureString))
            {
                SecureString secureString = ReadPassword(fieldPrompt).GetSecureStringPassword();
                convertedObj = secureString;

                if (isListInput && secureString != null && secureString.Length == 0)
                {
                    listInputDone = true;
                }
            }
            else if (fieldType == typeof(PSCredential))
            {
                WriteLine(fieldPrompt);
                PSCredential credential = PromptForCredential(
                    caption: null,   // caption already written
                    message: null,   // message already written
                    userName: null,
                    targetName: string.Empty);

                convertedObj = credential;
                if (isListInput && credential != null && credential.Password.Length == 0)
                {
                    listInputDone = true;
                }
            }
            else
            {
                string inputString = null;
                do
                {
                    inputString = PromptReadInput(
                        fieldDescription,
                        fieldPrompt,
                        isListInput,
                        out listInputDone);
                }
                while (!listInputDone && !PromptTryConvertTo(fieldType, inputString, out convertedObj));
                return inputString;
            }

            return null;
        }

        private string PromptReadInput(
            FieldDescription fieldDescription,
            string fieldPrompt,
            bool isListInput,
            out bool listInputDone)
        {
            listInputDone = false;
            string processedInputString = null;

            while (true)
            {
                string rawInputString = ReadInput(fieldPrompt);
                if (!string.IsNullOrEmpty(fieldDescription.Label) && rawInputString.StartsWith(PromptCommandPrefix))
                {
                    processedInputString = PromptCommandMode(rawInputString, fieldDescription, out bool inputDone);

                    if (inputDone)
                    {
                        break;
                    }
                }
                else
                {
                    if (isListInput && rawInputString.Length == 0)
                    {
                        listInputDone = true;
                    }

                    processedInputString = rawInputString;
                    break;
                }
            }

            return processedInputString;
        }

        private string PromptCommandMode(string input, FieldDescription desc, out bool inputDone)
        {
            string command = input.Substring(1);
            inputDone = true;

            if (command.StartsWith(PromptCommandPrefix))
            {
                return command;
            }

            if (command.Length == 1)
            {
                if (command[0] == '?')
                {
                    WriteLine(string.IsNullOrEmpty(desc.HelpMessage)
                        ? $"No help is available for {desc.Name}."
                        : desc.HelpMessage);
                }
                else
                {
                    WriteLine($"'{input}' cannot be recognized as a valid Prompt command.");
                }

                inputDone = false;
                return null;
            }

            if (string.Equals(command, "\"\"", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }
            else if (string.Equals(command, "$null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            else
            {
                WriteLine($"'{input}' cannot be recognized as a valid Prompt command.");
                inputDone = false;
                return null;
            }
        }

        private bool PromptTryConvertTo(Type fieldType, string inputString, out object convertedObj)
        {
            try
            {
                convertedObj = LanguagePrimitives.ConvertTo(inputString, fieldType, CultureInfo.InvariantCulture);
                return true;
            }
            catch (PSInvalidCastException e)
            {
                WriteLine(e.InnerException?.Message ?? e.Message);
            }
            catch (Exception e)
            {
                WriteLine(e.Message);
            }

            convertedObj = inputString;
            return false;
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            string choicesParamName = nameof(choices);
            string defaultChoiceParamName = nameof(defaultChoice);

            if (choices == null)
            {
                throw new ArgumentNullException(choicesParamName);
            }

            if (choices.Count == 0)
            {
                throw new ArgumentException(
                    $"'{choicesParamName}' should have at least one element.",
                    choicesParamName);
            }

            if (defaultChoice < -1 || defaultChoice >= choices.Count)
            {
                throw new ArgumentOutOfRangeException(
                    defaultChoiceParamName,
                    defaultChoice,
                    $"'{defaultChoiceParamName}' must be a valid index into '{choicesParamName}' or -1 for no default choice.");
            }

            // we lock here so that multiple threads won't interleave the various reads and writes here.

            lock (_instanceLock)
            {
                if (!string.IsNullOrEmpty(caption))
                {
                    WriteLine();
                    WriteLine(PromptColor, RawUI.BackgroundColor, caption);
                }

                if (!string.IsNullOrEmpty(message))
                {
                    WriteLine(message);
                }

                int result = defaultChoice;
                string[,] hotkeysAndPlainLabels = BuildHotkeysAndPlainLabels(choices);

                // Add the default choice key only if it is valid.
                // The value '-1' is used to specify no default.
                var defaultChoiceKeys = new Dictionary<int, bool>();
                if (defaultChoice >= 0)
                {
                    defaultChoiceKeys.Add(defaultChoice, true);
                }

                do
                {
                    WriteChoicePrompt(
                        hotkeysAndPlainLabels,
                        defaultChoiceKeys,
                        shouldEmulateForMultipleChoiceSelection: false);

                    string response = ReadInput("Select: ").Trim();
                    if (response.Length == 0)
                    {
                        // Just hit 'Enter', so pick the default if there is one.
                        if (defaultChoice >= 0)
                        {
                            break;
                        }

                        continue;
                    }

                    // Decide which choice they made.
                    if (response == "?")
                    {
                        // Show the help content.
                        ShowChoiceHelp(choices, hotkeysAndPlainLabels);
                        continue;
                    }

                    result = DetermineChoicePicked(response, choices, hotkeysAndPlainLabels);
                    if (result >= 0)
                    {
                        break;
                    }

                    // The input matched none of the choices, so prompt again
                }
                while (true);

                return result;
            }
        }

        public Collection<int> PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, IEnumerable<int> defaultChoices)
        {
            string choicesParamName = nameof(choices);
            string defaultChoicesParamName = nameof(defaultChoices);

            if (choices == null)
            {
                throw new ArgumentNullException(choicesParamName);
            }

            if (choices.Count == 0)
            {
                throw new ArgumentException(
                    $"'{choicesParamName}' should have at least one element.",
                    choicesParamName);
            }

            var defaultChoiceKeys = new Dictionary<int, bool>();
            if (defaultChoices != null)
            {
                foreach (int defaultChoice in defaultChoices)
                {
                    if (defaultChoice < 0 || defaultChoice >= choices.Count)
                    {
                        throw new ArgumentOutOfRangeException(
                            defaultChoicesParamName,
                            defaultChoice,
                            string.Format(
                                "The element of '{0}' must be a valid index into '{1}'. '{2}' is not a valid index.",
                                defaultChoicesParamName,
                                choicesParamName,
                                defaultChoice));
                    }

                    defaultChoiceKeys.TryAdd(defaultChoice, true);
                }
            }

            var result = new Collection<int>();
            // Lock here so that multiple threads won't interleave the various reads and writes here.
            lock (_instanceLock)
            {
                // Write caption on the console, if present.
                if (!string.IsNullOrEmpty(caption))
                {
                    // Should be a skin lookup
                    WriteLine();
                    WriteLine(PromptColor, RawUI.BackgroundColor, caption);
                }

                // Write message.
                if (!string.IsNullOrEmpty(message))
                {
                    WriteLine(message);
                }

                string[,] hotkeysAndPlainLabels = BuildHotkeysAndPlainLabels(choices);

                WriteChoicePrompt(
                    hotkeysAndPlainLabels,
                    defaultChoiceKeys,
                    shouldEmulateForMultipleChoiceSelection: true);

                if (defaultChoiceKeys.Count > 0)
                {
                    WriteLine();
                }

                // Display ChoiceMessage like Choice[0],Choice[1] etc
                int choicesSelected = 0;
                do
                {
                    // write the current prompt
                    string choiceMsg = string.Format("Choice[{0}]: ", choicesSelected);
                    Write(PromptColor, RawUI.BackgroundColor, choiceMsg);

                    string response = ReadInput("Select: ").Trim();

                    // Just hit 'Enter'.
                    if (response.Length == 0)
                    {
                        // This may happen when
                        // 1. User wants to go with the defaults
                        // 2. User selected some choices and wanted those choices to be picked.

                        if (result.Count == 0 && defaultChoiceKeys.Keys.Count >= 0)
                        {
                            // User did not pick up any choices. If there's a default, pick that one.
                            foreach (int defaultChoice in defaultChoiceKeys.Keys)
                            {
                                result.Add(defaultChoice);
                            }
                        }

                        // allow for no choice selection.
                        break;
                    }

                    // Decide which choice the user made.
                    if (response == "?")
                    {
                        // Show the help content.
                        ShowChoiceHelp(choices, hotkeysAndPlainLabels);
                        continue;
                    }

                    int choicePicked = DetermineChoicePicked(response, choices, hotkeysAndPlainLabels);
                    if (choicePicked >= 0)
                    {
                        result.Add(choicePicked);
                        choicesSelected++;
                    }
                    // prompt for multiple choices
                }
                while (true);

                return result;
            }
        }

        internal static string[,] BuildHotkeysAndPlainLabels(Collection<ChoiceDescription> choices)
        {
            // we will allocate the result array
            const char NullChar = (char)0;
            var hotkeysAndPlainLabels = new string[2, choices.Count];

            for (int i = 0; i < choices.Count; ++i)
            {
                string label = choices[i].Label;

                char hotKeyChar = NullChar;
                string hotKeyString = label;

                int andIndex = label.IndexOf('&');
                if (andIndex >= 0)
                {
                    if (andIndex + 1 < label.Length)
                    {
                        char hotKeyCandidate = label[andIndex + 1];
                        if (!char.IsWhiteSpace(hotKeyCandidate))
                        {
                            hotKeyChar = char.ToUpper(hotKeyCandidate);
                        }
                    }

                    hotKeyString = label.Remove(andIndex, 1).Trim();
                }

                // The question mark character is already taken for diplaying help.
                if (hotKeyChar == '?')
                {
                    throw new ArgumentException(
                        "Cannot process the hot key because a question mark ('?') cannot be used as a hot key.",
                        $"choices[{i}].Label");
                }

                hotkeysAndPlainLabels[0, i] = hotKeyChar == NullChar ? string.Empty : hotKeyChar.ToString();
                hotkeysAndPlainLabels[1, i] = hotKeyString;
            }

            return hotkeysAndPlainLabels;
        }

        private static int DetermineChoicePicked(string response, Collection<ChoiceDescription> choices, string[,] hotkeysAndPlainLabels)
        {
            // Check the full label first, as this is the least ambiguous.
            for (int i = 0; i < choices.Count; i++)
            {
                // pick the one that matches either the hot key or the full label
                if (string.Equals(response, hotkeysAndPlainLabels[1, i], StringComparison.CurrentCultureIgnoreCase))
                {
                    return i;
                }
            }

            // Now check the hotkeys.
            for (int i = 0; i < choices.Count; ++i)
            {
                // Ignore labels with empty hotkeys
                var hotKey = hotkeysAndPlainLabels[0, i];
                if (hotKey.Length == 0)
                {
                    continue;
                }

                if (string.Equals(response, hotKey, StringComparison.CurrentCultureIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private void ShowChoiceHelp(Collection<ChoiceDescription> choices, string[,] hotkeysAndPlainLabels)
        {
            for (int i = 0; i < choices.Count; ++i)
            {
                string key = hotkeysAndPlainLabels[0, i];
                if (key.Length == 0)
                {
                    // If there's no hotkey, use the label as the help.
                    key = hotkeysAndPlainLabels[1, i];
                }

                WriteLine(string.Format("{0} - {1}", key, choices[i].HelpMessage));
            }
        }

        private void WriteChoicePrompt(
            string[,] hotkeysAndPlainLabels,
            Dictionary<int, bool> defaultChoiceKeys,
            bool shouldEmulateForMultipleChoiceSelection)
        {
            ConsoleColor fg = RawUI.ForegroundColor;
            ConsoleColor bg = RawUI.BackgroundColor;

            string choiceTemplate = "[{0}] {1}  ";
            int lineLen = 0;
            int choiceCount = hotkeysAndPlainLabels.GetLength(1);

            for (int i = 0; i < choiceCount; ++i)
            {
                ConsoleColor cfg = PromptColor;
                if (defaultChoiceKeys.ContainsKey(i))
                {
                    cfg = DefaultPromptColor;
                }

                string choice = string.Format(
                    CultureInfo.InvariantCulture,
                    choiceTemplate,
                    hotkeysAndPlainLabels[0, i],
                    hotkeysAndPlainLabels[1, i]);

                WriteChoiceHelper(choice, cfg, bg, ref lineLen);
                if (shouldEmulateForMultipleChoiceSelection)
                {
                    WriteLine();
                }
            }

            WriteChoiceHelper("[?] Help", fg, bg, ref lineLen);

            if (shouldEmulateForMultipleChoiceSelection)
            {
                WriteLine();
            }

            string defaultPrompt = string.Empty;
            if (defaultChoiceKeys.Count > 0)
            {
                string prepend = string.Empty;
                var defaultChoicesBuilder = new StringBuilder();
                foreach (int defaultChoice in defaultChoiceKeys.Keys)
                {
                    string defaultStr = hotkeysAndPlainLabels[0, defaultChoice];
                    if (string.IsNullOrEmpty(defaultStr))
                    {
                        defaultStr = hotkeysAndPlainLabels[1, defaultChoice];
                    }

                    defaultChoicesBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}{1}", prepend, defaultStr);
                    prepend = ",";
                }

                string defaultChoices = defaultChoicesBuilder.ToString();
                defaultPrompt = defaultChoiceKeys.Count == 1
                    ? string.Format("(default is '{0}')", defaultChoices)
                    : string.Format("(default choices are {0})", defaultChoices);
            }

            WriteChoiceHelper(defaultPrompt, fg, bg, ref lineLen);
        }

        private void WriteChoiceHelper(string text, ConsoleColor fg, ConsoleColor bg, ref int lineLen)
        {
            int lineLenMax = RawUI.WindowSize.Width - 1;
            int textLen = RawUI.LengthInBufferCells(text);
            bool trimEnd = false;

            if (lineLen + textLen > lineLenMax)
            {
                WriteLine();
                trimEnd = true;
                lineLen = textLen;
            }
            else
            {
                lineLen += textLen;
            }

            Write(fg, bg, trimEnd ? text.TrimEnd() : text);
        }
    }
}