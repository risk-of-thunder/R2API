using R2API.Networking.Interfaces;
using R2API.Networking.Messages;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API.Networking {
    /// <summary>
    /// Helper functions for various RoR2 networking needs
    /// </summary>
    public static class NetworkingHelpers {
        public static void DealDamage(this DamageInfo? damage, HurtBox? target,
            bool callDamage, bool callHitEnemy, bool callHitWorld) {
            if (NetworkServer.active) {
                if (callDamage) {
                    if (target != null && target.healthComponent != null) {
                        target.healthComponent.TakeDamage(damage);
                    }
                }

                if (callHitEnemy) {
                    if (target != null && target.healthComponent != null) {
                        GlobalEventManager.instance.OnHitEnemy(damage, target.healthComponent.gameObject);
                    }
                }

                if (callHitWorld) {
                    GlobalEventManager.instance.OnHitAll(damage,
                        target && target.healthComponent ? target.healthComponent.gameObject : null);
                }
            }
            else {
                new DamageMessage(damage, target, callDamage, callHitEnemy, callHitWorld)
                    .Send(NetworkDestination.Server);
            }
        }

        public static void ApplyBuff(this CharacterBody? body,
            BuffIndex buff, int stacks = 1, float duration = -1f) {
            if (NetworkServer.active) {
                if (duration < 0f) {
                    body.SetBuffCount(buff, stacks);
                } else {
                    if (stacks < 0) {
                        R2API.Logger.LogError("Cannot remove duration from a buff");
                        return;
                    }

                    for (int i = 0; i < stacks; ++i) {
                        body.AddTimedBuff(buff, duration);
                    }
                }
            }
            else {
                new BuffMessage(body, buff, stacks, duration)
                    .Send(NetworkDestination.Server);
            }
        }

        public static void ApplyDot(this HealthComponent victim, GameObject attacker,
            DotController.DotIndex dotIndex, float duration = 8f, float damageMultiplier = 1f) {
            if (NetworkServer.active) {
                DotController.InflictDot(victim.gameObject, attacker, dotIndex, duration, damageMultiplier);
            }
            else {
                new DotMessage(victim.gameObject, attacker, dotIndex, duration, damageMultiplier)
                    .Send(NetworkDestination.Server);
            }
        }
    }
}
