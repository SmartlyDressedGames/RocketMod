using Rocket.API.Extensions;
using Rocket.Unturned.Events;
using SDG.Unturned;
using System;
using System.Reflection;
using UnityEngine;

namespace Rocket.Unturned.Player
{
    public sealed class UnturnedPlayerFeatures : UnturnedPlayerComponent
    {

        static MethodInfo onLanded = typeof(PlayerLife).GetMethod("onLanded", BindingFlags.NonPublic | BindingFlags.Instance);
        public DateTime Joined = DateTime.Now;

        internal Color? color = null;
        internal Color? Color
        {
            get { return color; }
            set { color = value; }
        }


        private bool vanishMode = false;
        public bool VanishMode
        {
            get { return vanishMode; }
            set
            {
                Player.GetComponent<UnturnedPlayerMovement>().VanishMode = value;
                PlayerMovement pMovement = Player.GetComponent<PlayerMovement>();
                pMovement.canAddSimulationResultsToUpdates = !value;
                if (vanishMode && !value)
                {
                    pMovement.updates.Add(new PlayerStateUpdate(pMovement.real, Player.Player.look.angle, Player.Player.look.rot));
                    pMovement.isUpdated = true;
                    PlayerManager.updates++;
                }
                vanishMode = value;
            }
        }

        private bool godMode = false;
        public bool GodMode
        {
            set
            {
                if (value == godMode)
                    return;

                var e = Player.Events;
                var landed = (Landed)Delegate.CreateDelegate(typeof(Landed), Player.Player.life, onLanded);
                if (value)
                {
                    e.OnUpdateHealth += e_OnPlayerUpdateHealth;
                    e.OnUpdateWater += e_OnPlayerUpdateWater;
                    e.OnUpdateFood += e_OnPlayerUpdateFood;
                    e.OnUpdateVirus += e_OnPlayerUpdateVirus;
                    Player.Player.life.onHurt += e_OnHurt;
                    Player.Player.movement.onLanded -= landed;
                }
                else
                {
                    e.OnUpdateHealth -= e_OnPlayerUpdateHealth;
                    e.OnUpdateWater -= e_OnPlayerUpdateWater;
                    e.OnUpdateFood -= e_OnPlayerUpdateFood;
                    e.OnUpdateVirus -= e_OnPlayerUpdateVirus;
                    Player.Player.life.onHurt -= e_OnHurt;
                    Player.Player.movement.onLanded += landed;
                }
                godMode = value;
            }
            get
            {
                return godMode;
            }
        }

        private bool initialCheck;

        Vector3 oldPosition = new Vector3();

        private void FixedUpdate()
        {
            if (oldPosition != Player.Position)
            {
                UnturnedPlayerEvents.fireOnPlayerUpdatePosition(Player);
                oldPosition = Player.Position;
            }
            if (!initialCheck && (DateTime.Now - Joined).TotalSeconds > 3)
            {
                Check();
            }
        }

        private void Check()
        {
            initialCheck = true;
           
            if (U.Settings.Instance.CharacterNameValidation)
            {
                string username = Player.CharacterName;
                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(U.Settings.Instance.CharacterNameValidationRule);
                System.Text.RegularExpressions.Match match = regex.Match(username);
                if (match.Groups[0].Length != username.Length)
                {
                    Provider.kick(Player.CSteamID, U.Translate("invalid_character_name"));
                }
            }
        }

        private static string reverse(string s)
        {
            string r = "";
            for (int i = s.Length; i > 0; i--) r += s[i - 1];
            return r;
        }

        protected override void Load()
        {

            if (godMode)
            {
                Player.Events.OnUpdateHealth += e_OnPlayerUpdateHealth;
                Player.Events.OnUpdateWater += e_OnPlayerUpdateWater;
                Player.Events.OnUpdateFood += e_OnPlayerUpdateFood;
                Player.Events.OnUpdateVirus += e_OnPlayerUpdateVirus;
                Player.Heal(100);
                Player.Infection = 0;
                Player.Hunger = 0;
                Player.Thirst = 0;
                Player.Bleeding = false;
                Player.Broken = false;
            }
        }

        private void e_OnPlayerUpdateVirus(UnturnedPlayer player, byte virus)
        {
            if (virus < 95) Player.Infection = 0;
        }

        private void e_OnPlayerUpdateFood(UnturnedPlayer player, byte food)
        {
            if (food < 95) Player.Hunger = 0;
        }

        private void e_OnPlayerUpdateWater(UnturnedPlayer player, byte water)
        {
            if (water < 95) Player.Thirst = 0;
        }

        private void e_OnPlayerUpdateHealth(UnturnedPlayer player, byte health)
        {
            if (health < 95)
            {
                player.Player.life.tellHealth(Provider.server, 100);
            }
        }

        private void e_OnHurt(SDG.Unturned.Player player, byte damage, Vector3 force, EDeathCause cause, ELimb limb, Steamworks.CSteamID killer)
        {
            player.life.askHeal(100, true, true);
        }
    }
}
