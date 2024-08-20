﻿using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using SFactions;
using TCRTShock;
using TCRCore;
using TShockAPI.Hooks;
using System.Reflection;
using Microsoft.Xna.Framework;

namespace SFactionsTCRConnector
{
    [ApiVersion(2, 1)]
    public class SFactionsTCRConnector : TerrariaPlugin
    {
        public override string Name => "SFactionsTCRConnector";
        public override string Author => "Soofa";
        public override string Description => "Connects SFactions with TCR";
        public override Version Version => new Version(0, 0, 1);
        public SFactionsTCRConnector(Main game) : base(game) { }
        public TerrariaChatRelayTShock TCR = null;
        public Configuration Config = Configuration.Reload();
        public override void Initialize()
        {
            ServerApi.Hooks.GamePostInitialize.Register(this, OnGamePostInitialize);
            TShockAPI.Hooks.GeneralHooks.ReloadEvent += OnReload;
        }

        private void OnReload(ReloadEventArgs e)
        {
            Config = Configuration.Reload();
            e.Player.SendSuccessMessage("[SFactionsTCRConnector] Plugin has been reloaded.");
        }

        private void OnGamePostInitialize(EventArgs args)
        {
            foreach (var pc in ServerApi.Plugins)
            {
                if (pc.Plugin.Name == "TerrariaChatRelay")
                {
                    TCR = (TerrariaChatRelayTShock)pc.Plugin;

                    // Get the OnChatReceived method
                    var onChatReceived = TCR.GetType().GetMethod("OnChatReceived", BindingFlags.NonPublic | BindingFlags.Instance);

                    // Create a delegate for the method
                    HookHandler<ServerChatEventArgs> serverChatHandler = (HookHandler<ServerChatEventArgs>)Delegate.CreateDelegate(
                        typeof(HookHandler<ServerChatEventArgs>), TCR, onChatReceived!);


                    // Deregister the handler
                    ServerApi.Hooks.ServerChat.Deregister(TCR, serverChatHandler);
                    ServerApi.Hooks.ServerChat.Register(this, OnServerChat);

                }
            }
        }

        private void OnServerChat(ServerChatEventArgs args)
        {
            string text = args.Text;

            // Terraria's client side commands remove the command prefix, 
            // which results in arguments of that command show up on the Discord.
            // Thus, it needs to be reversed
            foreach (var item in Terraria.UI.Chat.ChatManager.Commands._localizedCommands)
            {
                if (item.Value._name == args.CommandId._name)
                {
                    if (!string.IsNullOrEmpty(text))
                    {
                        text = item.Key.Value + ' ' + text;
                    }
                    else
                    {
                        text = item.Key.Value;
                    }
                    break;
                }
            }

            if (text.StartsWith(TShock.Config.Settings.CommandSpecifier) || text.StartsWith(TShock.Config.Settings.CommandSilentSpecifier))
                return;

            if (text == "" || text == null)
                return;

            if (TShock.Players[args.Who].mute == true)
                return;

            TCR.ChatHolder.Add(new TerrariaChatRelayTShock.Chatter()
            {
                Player = Main.player[args.Who].ToTCRPlayer(args.Who),
                Text = text
            });

            TSPlayer plr = TShock.Players[args.Who];

            text = string.Format(Config.ChatFormat,
                                           "",                                       // {0}
                                           plr.Group.Prefix,                         // {1}
                                           plr.Name,                                 // {2}
                                           plr.Group.Suffix,                         // {3}
                                           text,                                     // {4}
                                           GetFactionNameWithParanthesis(plr.Index)  // {5}
                                           );

            Core.RaiseTerrariaMessageReceived(this, Main.player[args.Who].ToTCRPlayer(args.Who), text);
        }


        private static string GetFactionNameWithParanthesis(int playerIndex)
        {
            string result = "";

            if (FactionService.IsPlayerInAnyFaction(playerIndex))
            {
                result = SFactions.SFactions.Config.ChatFactionNameOpeningParenthesis;
                result += FactionService.GetPlayerFaction(playerIndex).Name;
                result += SFactions.SFactions.Config.ChatFactionNameClosingParanthesis;
            }

            return result;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.ServerChat.Deregister(this, OnServerChat);
                TShockAPI.Hooks.GeneralHooks.ReloadEvent -= OnReload;
            }
        }
    }
}
