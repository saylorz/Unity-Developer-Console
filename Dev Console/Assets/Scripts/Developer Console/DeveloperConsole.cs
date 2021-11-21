using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Console
{
    public abstract class ConsoleCommand
    {
        public abstract string Name { get; protected set; }
        public abstract string Command { get; protected set; }
        public abstract string Description { get; protected set; }
        public abstract string Help { get; protected set; }

        public void AddCommandToConsole()
        {
            string addMessage = " command has been added to the console.";

            DeveloperConsole.AddCommandsToConsole(Command, this);
            Debug.Log(Name + addMessage);
        }

        public abstract void RunCommand(string[] args);
    }

    public class DeveloperConsole : MonoBehaviour
    {
        public static DeveloperConsole Instance { get; private set; }
        public static Dictionary<string, ConsoleCommand> Commands { get; private set; }
 
        [Header("UI Settings")]
        [SerializeField]
        private Canvas consoleCanvas;

        [SerializeField]
        private Text consoleText;

        [SerializeField]
        private Text inputText;

        [SerializeField]
        private InputField consoleInput;

        [Header("Clipboard Settings")]
        [SerializeField]
        [Tooltip("How many commands can be held in the clipboard, 0 = off")]
        private int clipboardSize = 5;
        private string[] clipboard;
        private int clipboardIndexer = 0;
        private int clipboardCursor = 0;

        [Header("Auto-Complete Settings")]
        [SerializeField]
        [Tooltip("How many chars before you can tab to autocomplete")]
        private int tabMinCharLength = 3;

        #region Colors
        public static string requiredColor = "#FA8072";
        public static string optionalColor = "#00FF7F";
        public static string warningColor = "#ffcc00";
        public static string executedColor = "e600e6";

        #endregion

        #region Typical Messages
        public static string NotRecognized = $"Commond not <color={warningColor}>recognized</color>";

        public static string ExecutedSuccessfully = $"Command executed <color={executedColor}>successfully</color>";

        public static string ParametersAmount = $"Wrong <color={warningColor}>amount of parameters</color>";

        public static string TypeNotSupported = $"Type of command <color={warningColor}>not supported</color>";

        public static string SceneNotFound = $"Scene <color={warningColor}>not found</color>." +
                                             $" Make sure that you have placed it inside <color={warningColor}>build settings</color>";

        public static string ClipboardCleared = $"\nConsole clipboard <color={optionalColor}>cleared</color>";
        #endregion

        private void Awake()
        {
            if(Instance != null)
            {
                return;
            }

            Instance = this;
            Commands = new Dictionary<string, ConsoleCommand>();
        }

        private void Start()
        {
            clipboard = new string[clipboardSize];

            consoleCanvas.gameObject.SetActive(false);
            CreateCommands();
        }

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void HandleLog(string _logMessage, string _stackTrace, LogType _type)
        {
            string message = "[" + _type.ToString() + "] " + _logMessage;
            AddMessageToConsole(message);
        }

        private void CreateCommands()
        {
            CommandQuit.CreateCommand();
        }

        public static void AddCommandsToConsole(string _name, ConsoleCommand _command)
        {
            if(!Commands.ContainsKey(_name))
            {
                Commands.Add(_name, _command);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                consoleCanvas.gameObject.SetActive(!consoleCanvas.gameObject.activeInHierarchy);

                consoleInput.ActivateInputField();
                consoleInput.Select();
            }

            if(consoleCanvas.gameObject.activeInHierarchy)
            {
                if(Input.GetKeyDown(KeyCode.Return))
                {
                    if (string.IsNullOrEmpty(inputText.text) == false)
                    {
                        AddMessageToConsole(inputText.text);
                        ParseInput(inputText.text);

                        if(clipboardSize != 0)
                        {
                            StoreCommandInClipboard(inputText.text);
                        }
                    }

                    //clear input
                    consoleInput.text = "";
                    consoleInput.ActivateInputField();
                    consoleInput.Select();
                }

                if(Input.GetKeyDown(KeyCode.UpArrow))
                {
                    if(clipboardSize != 0 && clipboardIndexer != 0)
                    {
                        if(clipboardCursor == clipboardIndexer)
                        {
                            clipboardCursor--;
                            consoleInput.text = clipboard[clipboardCursor];
                        }
                        else
                        {
                            if(clipboardCursor > 0)
                            {
                                clipboardCursor--;
                                consoleInput.text = clipboard[clipboardCursor];
                            }
                            else
                            {
                                consoleInput.text = clipboard[0];
                            }
                        }
                    }
                    consoleInput.caretPosition = consoleInput.text.Length;
                }

                if(Input.GetKeyDown(KeyCode.DownArrow))
                {
                    if(clipboardSize != 0 && clipboardIndexer != 0)
                    {
                        if (clipboardCursor < clipboardIndexer)
                        {
                            clipboardCursor++;
                            consoleInput.text = clipboard[clipboardCursor];
                            consoleInput.caretPosition = consoleInput.text.Length;
                        }
                    }
                }

                if(Input.GetKeyDown(KeyCode.Tab))
                {
                    int inputLength = consoleInput.text.Length;

                    if (inputLength >= tabMinCharLength && consoleInput.text.Any(char.IsWhiteSpace) == false)
                    {
                        foreach (var command in Commands)
                        {
                            string commandKey = command.Key.Length <= inputLength ? command.Key : command.Key.Substring(0, inputLength);

                            if (consoleInput.text.ToLower().StartsWith(commandKey.ToLower()))
                            {
                                consoleInput.text = command.Key;
                                consoleInput.caretPosition = command.Key.Length;
                                break;
                            }
                        }
                    }
                }
            }

            if(consoleCanvas.gameObject.activeInHierarchy == false)
            {
                    consoleInput.text = "";
            }
        }

        private void StoreCommandInClipboard(string _command)
        {
            clipboard[clipboardIndexer] = _command;

            if(clipboardIndexer < clipboardSize - 1)
            {
                clipboardIndexer++;
                clipboardCursor = clipboardIndexer;
            }
            else if(clipboardIndexer == clipboardSize - 1)
            {
                //Clear clipboard and reset
                clipboardIndexer = 0;
                clipboardCursor = 0;

                for(int i = 0; i < clipboardSize; i++)
                {
                    clipboard[i] = string.Empty;
                }
            }
        }

        private void AddMessageToConsole(string _msg)
        {
            consoleText.text += _msg + "\n";
        }

        private void ParseInput(string _input)
        {
            string[] input = _input.Split(' ');

            if(input.Length == 0 || input == null)
            {
                Debug.LogWarning(NotRecognized);
                return;
            }

            if(!Commands.ContainsKey(input[0]))
            {
                Debug.LogWarning(NotRecognized);
            }
            else
            {
                //Create a list to leverage Linq
                List<string> args = input.ToList();

                //Remove command 
                args.RemoveAt(0);

                Commands[input[0]].RunCommand(args.ToArray());
            }
        }
    }
}