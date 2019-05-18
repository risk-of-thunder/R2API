using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using JetBrains.Annotations;
using RoR2;

namespace R2API {
    public class R2APIConCommand {
        private string _cmd;

        private string[] _cmdParts => _cmd.Split(' ');

        public string Name => _cmdParts[0];

        /// <summary>
        /// Returns the parts of a command, returns null if no commands
        /// </summary>
        [CanBeNull]
        public string[] Args {
            get {
                return _cmdParts.Length > 1 ? _cmdParts.Where((val, idx) => idx != 0).ToArray() : null;
            }
        }

        public NetworkUser Sender;

        public R2APIConCommand(string cmdString, NetworkUser sender) {
            //set the command string
            _cmd = cmdString.Trim();

            R2API.Logger.LogInfo($"cmd parts length {_cmdParts.Length} cmd txt {Name}");
            //Set sender, this can be null
            Sender = sender;
        }
    }
}
