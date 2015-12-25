﻿using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned.Permissions;
using SDG.Unturned;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;

namespace Freenex.AccountLimiter
{
    public class AccountLimiter : RocketPlugin<AccountLimiterConfiguration>
    {
        public static AccountLimiter Instance;

        protected override void Load()
        {
            Instance = this;
            UnturnedPermissions.OnJoinRequested += UnturnedPermissions_OnJoinRequested;
            Logger.Log("Freenex's AccountLimiter has been loaded!");
        }

        protected override void Unload()
        {
            UnturnedPermissions.OnJoinRequested -= UnturnedPermissions_OnJoinRequested;
            Logger.Log("Freenex's AccountLimiter has been unloaded!");
        }

        public ESteamRejection GetSteamRejection()
        {
            var rejection = ESteamRejection.AUTH_VERIFICATION;

            try
            {
                rejection = (ESteamRejection) Enum.Parse(typeof(ESteamRejection), Configuration.Instance.accRejectionReason, true);
            }
            catch (ArgumentException)
            {
                // ignore, will return default (AUTH_VERIFICATION)
            }
            
            return rejection;
        }

        private void UnturnedPermissions_OnJoinRequested(Steamworks.CSteamID player, ref ESteamRejection? rejectionReason)
        {
            for (int i = 0; i < Configuration.Instance.Whitelist.Length; i++)
            {
                if (Configuration.Instance.Whitelist[i].WhitelistUser == player.ToString()){ return; }
            }

            WebClient WC = new WebClient();

            bool UserHasSetUpProfile = false;
            bool NonLimitedOverwrites = false;

            using (XmlReader xreader = XmlReader.Create(new StringReader(WC.DownloadString("http://steamcommunity.com/profiles/" + player + "?xml=1"))))
            {
                while (xreader.Read())
                {
                    if (xreader.IsStartElement())
                    {
                        if (xreader.Name == "vacBanned")
                        {
                            if (xreader.Read())
                            {
                                if (xreader.Value == "1")
                                {
                                    if (Configuration.Instance.accKickVACBannedAccounts)
                                    {
                                        rejectionReason = GetSteamRejection();
                                    }
                                }
                            }
                        }
                        else if (xreader.Name == "isLimitedAccount")
                        {
                            if (xreader.Read())
                            {
                                if (xreader.Value == "1")
                                {
                                    if (Configuration.Instance.accKickLimitedAccounts)
                                    {
                                        rejectionReason = GetSteamRejection();
                                    }
                                }
                                else if (xreader.Value == "0")
                                {
                                    if (Configuration.Instance.accNonLimitedOverwrites)
                                    {
                                        NonLimitedOverwrites = true;
                                    }
                                }
                                UserHasSetUpProfile = true;
                            }
                        }
                        else if (xreader.Name == "memberSince")
                        {
                            if (xreader.Read())
                            {
                                try
                                {
                                    string[] MemberSince = xreader.Value.Split(' ');
                                    MemberSince[1] = Regex.Match(MemberSince[1], @"\d+").Value;
                                    DateTime dtSteamUser = DateTime.ParseExact(MemberSince[1] + MemberSince[0] + MemberSince[2], "dMMMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None);
                                    TimeSpan tsSteamUser = DateTime.Now - dtSteamUser;
                                    if (tsSteamUser.Days <= Configuration.Instance.accMinimumDays && !NonLimitedOverwrites)
                                    {
                                        rejectionReason = GetSteamRejection();
                                    }
                                }
                                catch
                                {
                                    Logger.LogWarning("DateTimeParseError: " + player + " // " + xreader.Value);
                                }
                                UserHasSetUpProfile = true;
                                xreader.Close();
                            }
                        }
                    }
                }
            }

            if (!UserHasSetUpProfile)
            {
                rejectionReason = GetSteamRejection();
            }

        }
    }
}
