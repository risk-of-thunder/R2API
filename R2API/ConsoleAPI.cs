using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using UnityEngine;

namespace R2API {
    /// <summary>
    /// Provides an API to easily extend upon the in game console system.
    /// Could be useful for easily testing out functions in your mods/API
    /// </summary>
    public static class ConsoleAPI {
        //Logging infrastructure.
        private const string TAG = "ConsoleAPI";
        private static ManualLogSource _logger = R2API.Logger;

        //Stores custom registered commands, in the future we cold also implement a help registry, based on the command name
        private static Dictionary<string, Func<R2APIConCommand, bool>> _commands = new Dictionary<string, Func<R2APIConCommand, bool>>();
        private static Dictionary<string, string> _commandHelp = new Dictionary<string, string>();

        static ConsoleAPI() {
            _registerR2APICommandHelp();
        }

        public static void InitHooks() {
            On.RoR2.Console.SubmitCmd += (orig, self, sender, cmd, submit) => {
                _logger.LogInfo("Processing Command... " + cmd);

                //Commands may be invalid/valid simply a wrapper class to provide helper props
                var cmdObj = new R2APIConCommand(cmd, sender);

                //extend on help
                
                //So we try handle.
                if (TryHandleR2APICmd(cmdObj) && cmdObj.Name.ToLower() != "help") {
                    return;
                }

                
                orig.Invoke(self, sender, cmd, false);
                if (cmdObj.Name.ToLower() == "help") {
                    var headerMsg = "R2API Console Help:\nRegistered Commands:\n";
                    var body = _gatherCmdHelp();
                    
                    LogUtils.Log(headerMsg);
                    LogUtils.Log(body);
                    
                }
            };

        }

        

        public static bool TryHandleR2APICmd(R2APIConCommand cmd) {
            uint howMuch;
            switch (cmd.Name.ToLower()) {
                case "help":
                    return false;
                case "give-money":
                    howMuch = cmd.Args != null ? UInt32.Parse(cmd.Args[0]) : 100;
                    PlayerAPI.GiveMoney(howMuch, cmd.Sender);
                    return true;
                case "give-xp":
                    howMuch = cmd.Args != null ? UInt32.Parse(cmd.Args[0]) : 100;
                    PlayerAPI.GiveXp(howMuch, cmd.Sender);
                    return true;
                case "set-move-speed":
                    var toSpeed = cmd.Args != null ? float.Parse(cmd.Args[0]) : 1.0f;
                    PlayerAPI.SetPlayerMoveSpeed(toSpeed, cmd.Sender);
                    return true;
                case "set-attack-speed":
                    var attackSpeed = cmd.Args != null ? float.Parse(cmd.Args[0]) : 1.0f;
                    PlayerAPI.SetPlayerAttackSpeed(attackSpeed, cmd.Sender);
                    return true;
                case "give":
                    var itemIndex = cmd.Args?[0];
                    var howMany = cmd.Args != null ? Int32.Parse(cmd.Args[1]) : 1;
                    PlayerAPI.GiveItem(itemIndex, howMany, cmd.Sender);
                    return true;
                case "print-stats":
                    PlayerAPI.LogPlayerStats(cmd.Sender);
                    return true;
                default:
                    if (_commands.ContainsKey(cmd.Name)) {
                        return _commands[cmd.Name].Invoke(cmd);
                    }
                    else {
                        var badMsg = $"No such command, {cmd.Name}.";
                        R2API.Logger.LogWarning("No such command " + cmd.Name);
                        LogUtils.Log(badMsg);
                        return false;
                    }
            }
        }

        /// <summary>
        /// Used to register a custom command into the ConsoleAPI.
        /// </summary>
        /// <param name="name">The name of the command, determines what triggers it.</param>
        /// <param name="helpTxt">Must be longer than 10 characters. See default helptext for a good base.</param>
        /// <param name="command">The actual command function.</param>
        public static void RegisterCommand(string name, string helpTxt, Func<R2APIConCommand, bool> command) {
            if (_commands.ContainsKey(name)) {
                var badMsg = $"Cannot register duplicate command name: {name}";
                LogUtils.LogToAll(badMsg);
                return;
            }

            if (helpTxt == null || helpTxt.Trim().Length < 6) {
                var badMsg = "Please provide a helpful message as helpTxt!";
                LogUtils.LogToAll(badMsg);
                return;
            }

            _commands[name] = command;
            _commandHelp[name] = helpTxt;
        }

        private static string _gatherCmdHelp() {
            var builder = new StringBuilder();
            foreach (var helpLine in _commandHelp) {
                var line = $"{helpLine.Key}: {helpLine.Value}";
                builder.AppendLine(line);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Register help text for built in R2APIMethods
        /// Should group by API. 
        /// </summary>
        private static void _registerR2APICommandHelp() {
            _commandHelp["give-money"] = $"give-money [uint]: Gives the calling player the specified amount of money, default is 100";
            _commandHelp["give-xp"] = $"give-xp [uint]: Gives the calling player the specified amount of xp, default is 100";
            _commandHelp["set-move-speed"] = $"set-move-speed [decimal]: Sets the move speed of the calling player to the given decimal, default is 1.0";
            _commandHelp["set-attack-speed"] = $"set-attack-speed [decimal]: Sets the attack speed of the calling player to the given decimal, default is 1.0";
            _commandHelp["give"] = $"give [ItemIdx(enum name/enum idxNumber)] [count]: Gives the calling player the specified item, count times. Default is 1. The item list is parsed from ItemIdx Class. You can use names or the numerical index. ";
            _commandHelp["print-stats"] = $"print-stats: Prints the stats for the calling player.";
        }
    }
    /// <summary>
    /// Helpful class for logging to the ingame console
    /// </summary>
    public static class LogUtils {
        /// <summary>
        /// Defacto method for writing to the ingame console.
        /// </summary>
        /// <param name="message">the message to display</param>
        /// <param name="type">(optional) the LogType, LogType.Log by default.</param>
        public static void Log(string message, LogType type = LogType.Log) {
            var log = new RoR2.Console.Log {
                message = message,
                logType = type,
                stackTrace = StackTraceUtility.ExtractStackTrace()
            };

            RoR2.Console.HandleLog(log.message, log.stackTrace, log.logType);
        }
        /// <summary>
        /// Defacto method for writing to the ingame console AND bepinex console.
        /// </summary>
        /// <param name="message">the message to display</param>
        /// <param name="level">(optional) the LogLevel, LogLevel.Info by default</param>
        /// <param name="type">(optional) the LogType, LogType.Log by default.</param>
        public static void LogToAll(string message, LogLevel level = LogLevel.Info, LogType type = LogType.Log) {
            var log = new RoR2.Console.Log {
                message = message,
                logType = type,
                stackTrace = StackTraceUtility.ExtractStackTrace()
            };

            RoR2.Console.HandleLog(log.message, log.stackTrace, log.logType);
            R2API.Logger.Log(level, log.message);
        }
    }

}
