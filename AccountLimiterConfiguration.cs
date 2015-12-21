﻿using Rocket.API;
using System.Xml.Serialization;

namespace Freenex.AccountLimiter
{
    public sealed class Whitelist
    {
        [XmlAttribute("Steam64ID")]
        public string WhitelistUser;

        public Whitelist(string steamid)
        {
            WhitelistUser = steamid;
        }
        public Whitelist()
        {
            WhitelistUser = string.Empty;
        }
    }

    public class AccountLimiterConfiguration : IRocketPluginConfiguration
    {
        public int accMinimumDays;
        public bool accNonLimitedOverwrites;
        public bool accKickVACBannedAccounts;
        public bool accKickLimitedAccounts;

        [XmlArrayItem("WhitelistUser")]
        [XmlArray(ElementName = "Whitelist")]
        public Whitelist[] Whitelist;

        public void LoadDefaults()
        {
            accMinimumDays = 30;
            accNonLimitedOverwrites = true;
            accKickVACBannedAccounts = false;
            accKickLimitedAccounts = false;

            Whitelist = new Whitelist[]{
                new Whitelist("76561198187138313")
            };
        }
    }
}