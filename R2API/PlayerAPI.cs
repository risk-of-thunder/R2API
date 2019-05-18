using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using IL.RoR2.UI;
using MonoMod.Utils;
using UnityEngine;
using UnityEngine.Networking;
using Console = RoR2.Console;


namespace R2API {

    public class R2APIConsole : RoR2.Console {

    }
    /**
     * Important Classes in RoR2
     * CharacterBody
     * BuffIndex
     * BuffCatalog
     * ..Exploring some more
     */
    // ReSharper disable once InconsistentNaming
    public static class PlayerAPI {
        //Logging infrastructure.
        private const string TAG = "PlayerAPI";
        private static ManualLogSource _logger = R2API.Logger;

        //debug, move to enum later
        private const string TAG_PLAYER = "player";

        private static CharacterMaster _player = null;



        public static List<Action<PlayerStats>> CustomEffects { get; private set; }

        public static void InitHooks() {
            //Get the player object once the game starts.
            On.RoR2.CharacterMaster.OnBodyStart += (orig, master, body) => {
                orig.Invoke(master,body);
                if (_player != null) {
                    return;
                }
                var player = GameObject.FindWithTag("Player");
                if (player == null) {
                    return;
                }
                _logger.LogInfo($"player name {player.GetComponent<CharacterBody>().master.name} tag {player.GetComponent<CharacterBody>().master.tag}");
                if (master == null) {
                    _logger.LogWarning("Skipping null master...");
                    return;
                }

                OnPlayerLoaded(player.GetComponent<CharacterBody>().master);
                
            };
        }

       


        public static void OnPlayerLoaded(CharacterMaster master) {
            R2API.Logger.LogInfo($"Player {master.GetBody().GetUserName()} is active.");
            _player = master;
        }

        public static void OnPlayerUnLoaded(CharacterMaster master) {
            R2API.Logger.LogInfo($"Player {master.GetBody().GetUserName()} is inactive.");
        }

        

        public static void RecalcStats(CharacterBody characterBody) {
            characterBody.SetFieldValue("experience",
                TeamManager.instance.GetTeamExperience(characterBody.teamComponent.teamIndex));
            characterBody.SetFieldValue("level",
                TeamManager.instance.GetTeamLevel(characterBody.teamComponent.teamIndex));

            /* Calculate Vanilla items effects
            *
            * TODO
            */
            PlayerStats playerStats = null; //TODO: initialize this from characterBody

            foreach (var effectAction in CustomEffects) {
                effectAction(playerStats);
            }

            characterBody.statsDirty = false;
        }

        public static void GiveItem(string itemIndex, int count = 1, NetworkUser offender = null) {
            if (!_player) {
                return;
            }

            ItemIndex item;
            try {
                ItemIndex.TryParse(itemIndex, true, out item);
                _player.inventory.GiveItem(item, count);
            }
            catch {
                var msg = "Was unable to find item " + itemIndex;

                _logger.LogWarning(msg);
                LogUtils.Log(msg);
            }

            
        }

        public static void TakeItem(string itemIndex, int count = 1, NetworkUser offender = null) {
            if (!_player) {
                return;
            }
            ItemIndex item;
            try {
                ItemIndex.TryParse(itemIndex, true, out item);
                _player.inventory.RemoveItem(item,count);
                if(offender != null)
                    LogUtils.Log($"Removing x{count} {item} from user: {offender?.userName} ");
            }
            catch {
                var badMsg = $"Could not find item {itemIndex}";
                //Log to bep and in-game consoles.
                _logger.LogWarning(badMsg);
                LogUtils.Log(badMsg);
            }


        }


        public static void LogPlayerStats(NetworkUser offender = null) {
            if (_player == null) {
                LogUtils.Log("No active player! You must be in a game to use this command.");
                return;
            }
            var msg = $"Stats for {offender.userName}" +
                      $"\nMax Health:{_player.GetBody().maxHealth} Current Regen:{_player.GetBody().regen}" +
                      $"\nMax Shield:{_player.GetBody().maxShield} Base Move Speed:{_player.GetBody().baseMoveSpeed}" +
                      $"\nAcceleration:{_player.GetBody().acceleration} Jump Power:{_player.GetBody().jumpPower}" +
                      $"\nMax Jump Height:{_player.GetBody().maxJumpHeight} JumpCount:{_player.GetBody().maxJumpHeight}" +
                      $"\nAttack Speed (base):{_player.GetBody().attackSpeed} ({_player.GetBody().baseAttackSpeed})" +
                      $"\nDamage (base): {_player.GetBody().damage} ({_player.GetBody().baseDamage})" +
                      $"\nCrit (base): {_player.GetBody().crit} ({_player.GetBody().baseCrit})" +
                      $"\nArmor (base): {_player.GetBody().armor} ({_player.GetBody().baseArmor})";

            _logger.LogInfo(msg);
            LogUtils.Log(msg);
        }
        public static void SetJumpPower(NetworkUser offender = null) {
            if (_player == null) {
                _logger.LogWarning("No active player");
            }
        }
        public static void GiveMoney(uint howMuch, NetworkUser offender = null) {

            if (offender == null) return;
            
             if (_player == null) {
                var logAct = new Console.Log {
                    message = "No-one to give money to!"
                };
                Console.HandleLog(logAct.message, logAct.stackTrace, logAct.logType);
                return;
            }
            var log = new Console.Log {
                message = offender != null
                    ? $"Giving {howMuch} currency to {offender.userName}!"
                    : $"Giving {howMuch} currency to anon :o "
            };
            Console.HandleLog(log.message,log.stackTrace, log.logType);

            _player.GiveMoney(howMuch);



        }
        public static void GiveXp(uint howMuch, NetworkUser offender = null) {

            if (offender == null) return;
            if (_player == null) {
                var logAct = new Console.Log {
                    message = "No-one to give xp to!"
                };
                Console.logs.Add(logAct);
                return;
            }
            var log = new Console.Log {
                message = $"Giving {howMuch} xp to {offender.userName}!"
            };
            Console.logs.Add(log);
            _player.GiveExperience(howMuch);



        }
        public static void SetPlayerMoveSpeed(float toSpeed, NetworkUser offender = null) {
            if (_player == null) {
                return;
            }
            _player.GetBody().baseMoveSpeed = toSpeed;
            LogUtils.Log($"Setting {offender.userName} base speed to {toSpeed}");
        }
        public static void SetPlayerAttackSpeed(float toSpeed, NetworkUser offender = null) {
            if (_player == null) {
                return;
            }
            _player.GetBody().baseAttackSpeed = toSpeed;
        }



    }


    /// <summary>
    /// Holds player modifiers.
    /// </summary>
    public class PlayerStats {
        //May be more in CharacterBody. Ie. baseAttackSpeed, baseRegen... 
        //Character Stats
        public int maxHealth = 0;
        public int healthRegen = 0;
        public bool isElite = false;
        public int maxShield = 0;
        public float movementSpeed = 0;
        public float acceleration = 0;
        public float jumpPower = 0;
        public float maxJumpHeight = 0;
        public float maxJumpCount = 0;
        public float attackSpeed = 0;
        public float damage = 0;
        public float Crit = 0;
        public float Armor = 0;
        public float critHeal = 0;

        //Primary Skill
        public float PrimaryCooldownScale = 0;
        public float PrimaryStock = 0;

        //Secondary Skill
        public float SecondaryCooldownScale = 0;
        public float SecondaryStock = 0;

        //Utility Skill
        public float UtilityCooldownScale = 0;
        public float UtilityStock = 0;

        //Special Skill
        public float SpecialCooldownScale = 0;
        public float SpecialStock = 0;
    }

    public static class MethodInfoHelper {
        public static MethodInfo GetMethodInfo<T>(Expression<Action<T>> expression) {
            var member = expression.Body as MethodCallExpression;

            if (member != null)
                return member.Method;

            throw new ArgumentException("Expression is not a method", "expression");
        }
    }


    


}
