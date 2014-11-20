﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Mvc;

using Player = VF_RealmPlayersDatabase.PlayerData.Player;
using WowRealm = VF_RealmPlayersDatabase.WowRealm;
using Guild = VF_RealmPlayersDatabase.GeneratedData.Guild;

using WowBoss = VF_RealmPlayersDatabase.WowBoss;
namespace RealmPlayersServer
{
    public partial class GuildList : System.Web.UI.Page
    {
        private class GuildProgress
        {
            //ZZZZ AAA MMMM O WWW BBBB QQQQ NNNN

            private static int ZGBitsMul = 0x01;
            private static int ZGBitsMask = 0x0F;

            private static int AQ20BitsMul = 0x10;
            private static int AQ20BitsMask = 0x70;

            private static int MCBitsMul = 0x80;
            private static int MCBitsMask = 0x780;

            private static int OnyBitsMul = 0x800;
            private static int OnyBitsMask = 0x800;

            private static int WorldBossBitsMul = 0x1000;
            private static int WorldBossBitsMask = 0x7000;

            private static int BWLBitsMul = 0x8000;
            private static int BWLBitsMask = 0x78000;

            private static int AQ40BitsMul = 0x80000;
            private static int AQ40BitsMask = 0x780000;

            private static int NaxxBitsMul = 0x800000;
            private static int NaxxBitsMask = 0x7800000;

            public static int CreateProgressInt(int _ZG, int _AQ20, int _MC, int _Ony, int _World, int _BWL, int _AQ40, int _Naxx)
            {
                return _Naxx * NaxxBitsMul
                    + _AQ40 * AQ40BitsMul
                    + _BWL * BWLBitsMul
                    + _World * WorldBossBitsMul
                    + _Ony * OnyBitsMul
                    + _MC * MCBitsMul
                    + _AQ20 * AQ20BitsMul
                    + _ZG * ZGBitsMul;
            }
            public static int GetProgressZG(int _ProgressInt)
            {
                return (_ProgressInt & ZGBitsMask) / ZGBitsMul;
            }
            public static int GetProgressAQ20(int _ProgressInt)
            {
                return (_ProgressInt & AQ20BitsMask) / AQ20BitsMul;
            }
            public static int GetProgressMC(int _ProgressInt)
            {
                return (_ProgressInt & MCBitsMask) / MCBitsMul;
            }
            public static int GetProgressOny(int _ProgressInt)
            {
                return (_ProgressInt & OnyBitsMask) / OnyBitsMul;
            }
            public static int GetProgressWorldBosses(int _ProgressInt)
            {
                return (_ProgressInt & WorldBossBitsMask) / WorldBossBitsMul;
            }
            public static int GetProgressBWL(int _ProgressInt)
            {
                return (_ProgressInt & BWLBitsMask) / BWLBitsMul;
            }
            public static int GetProgressAQ40(int _ProgressInt)
            {
                return (_ProgressInt & AQ40BitsMask) / AQ40BitsMul;
            }
            public static int GetProgressNaxx(int _ProgressInt)
            {
                return (_ProgressInt & NaxxBitsMask) / NaxxBitsMul;
            }
        }
        //private static int ZGAQ20Multiplier = 1; //0-10 + 0-6
        //private static int MCONYWBMultiplier = 100; //0-10 + 0-1 + 0-6
        //private static int BWLMultiplier = 10000; //0-8
        //private static int AQ40Multiplier = 100000; //0-9
        //private static int NaxxMultiplier = 1000000; //0-15

        static GuildColumn[] Table_Columns = new GuildColumn[]{
            GuildColumn.Number,
            GuildColumn.Name,
            GuildColumn.Progress,
            GuildColumn.MemberCount,
            GuildColumn.Level60MemberCount,
            GuildColumn.TotalHKs,
            GuildColumn.AverageMemberHKs,
        };
        static GuildColumn[] Table_Columns_TBC = new GuildColumn[]{
            GuildColumn.Number,
            GuildColumn.Name,
            GuildColumn.Progress,
            GuildColumn.MemberCount,
            GuildColumn.Level70MemberCount,
            GuildColumn.TotalHKs,
            GuildColumn.AverageMemberHKs,
        };
        public MvcHtmlString m_BreadCrumbHTML = null;
        public MvcHtmlString m_GuildListInfoHTML = null;
        public MvcHtmlString m_TableHeadHTML = null;
        public MvcHtmlString m_TableBodyHTML = null;
        public MvcHtmlString m_PaginationHTML = null;

        public MvcHtmlString m_GuildScriptData = null;
        public string CreateGuildListInfo(int _GuildCount)
        {
            string guildListInfo
                = "<h1>Guilds<span class='badge badge-inverse'>" + _GuildCount + " Guilds</span></h1>"
                + "<p>Guilds on the realm. Sorted by current progress.</p><p>The progress is automatically generated by looking at instance-boss specific items players have within the guild.</p>";

            return guildListInfo;
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            int pageNr = PageUtility.GetQueryInt(Request, "page", 1);
            int pageIndex = pageNr - 1;//Change range from 0 to * instead of 1 to *
            int count = PageUtility.GetQueryInt(Request, "count", 50);
            if (count > 500) count = 500;

            var realm = RealmControl.Realm;
            if (realm == WowRealm.Unknown)
                return;

            this.Title = "Guilds @ " + StaticValues.ConvertRealmParam(realm) + " | RealmPlayers";

            var realmDB = DatabaseAccess.GetRealmPlayers(this, realm);

            var guildSummaryDB = Hidden.ApplicationInstance.Instance.GetGuildSummaryDatabase();
            //;// DatabaseAccess.GetRealmGuilds(this, realm, NotLoadedDecision.RedirectAndWait).Where((guild) => { return guild.Value.GetTotalPlayers() > 0; })
            var guildArray = guildSummaryDB.GetGuilds(realm);//.OrderByDescending((guild) => { return guild.Value.Players.Count; });
            string page = "";
            int nr = 0;

            string guildProgressData = "";

            List<Tuple<int, Tuple<VF_RPDatabase.GuildSummary, string>>> progressGuilds = new List<Tuple<int, Tuple<VF_RPDatabase.GuildSummary, string>>>();

            foreach (var guild in guildArray)
            {
                guild.Value.GenerateCache(realmDB);
                if (guild.Value.GetMembers().Count > 0)
                {
                    string thisGuildProgressData = guild.Value.m_GuildProgressData.Item1;
                    int progressComparisonValue = guild.Value.m_GuildProgressData.Item2;
                    if (thisGuildProgressData == "" && guild.Value.Stats_GetTotalMaxLevels() >= 25)
                    {
                        thisGuildProgressData = CreateProgressStr(this, guild.Value, realm, out progressComparisonValue);
                        guild.Value.m_GuildProgressData = Tuple.Create(thisGuildProgressData, progressComparisonValue);
                    }
                    progressGuilds.Add(Tuple.Create(progressComparisonValue, Tuple.Create(guild.Value, thisGuildProgressData)));
                }
            }
            var orderedProgressGuilds = progressGuilds.OrderByDescending(_Value =>
                {
                    UInt64 sortValue = (((UInt64)_Value.Item1) << 32);
                    if(sortValue != 0)
                    {
                        //sortValue |= (((UInt64)(UInt32)_Value.Item2.Item1.Stats_GetAveragePVPRank()) << 16) | ((UInt64)(UInt32)_Value.Item2.Item1.GetMembers().Count);
                        sortValue |= (((UInt64)(UInt32)_Value.Item2.Item1.Stats_GetAverageMemberHKs()) << 16) | ((UInt64)(UInt32)_Value.Item2.Item1.GetMembers().Count);
                    }
                    else
                    {
                        sortValue |= (UInt64)(UInt32)_Value.Item2.Item1.Stats_GetTotalMaxLevels();
                    }
                    return sortValue;
                });
            //#warning implement this feature below in comments
            /*
             It's about the guild page: 
             * as far as I can get, it currently has guilds sorted by the following parameters: 
             * PvE progress -> Level 60 Members Count -> Average PvP Rank (up to the first 47 guilds, 
             * after which only the amount of 60s seems to be taken into account to determine a guild standing, 
             * as for the remaining parameters they're randomly sorted).

            According to me, as a day 1 player who's always been active, 
             * the average player tends to be mainly after the PvE, 
             * then the PvP, then the social aspects of a community,
             * so the sort order would end up providing a more actual indication of a guild's standing by prioritizing 
             * those values according to this order: 
             * PvE Progress -> Average PvP Rank -> Members Count -> Level 60 Members Count -> Total HKs. 
             * Provided it's possible at all to have the system sort guilds by these parameters.
             */
            if(orderedProgressGuilds.Count() > 0)
            {
                if (GuildProgress.GetProgressAQ40(orderedProgressGuilds.First().Item1) == 0) //AQ Content not released
                {
                    guildProgressData += "g_AQReleased = false;";
                }
                else
                {
                    guildProgressData += "g_AQReleased = true;";
                }
                if (GuildProgress.GetProgressNaxx(orderedProgressGuilds.First().Item1) == 0) //Naxx Content not released
                {
                    guildProgressData += "g_NaxxReleased = false;";
                }
                else
                {
                    guildProgressData += "g_NaxxReleased = true;";
                }
            }
            foreach (var guild in orderedProgressGuilds)
            {
                nr++;
                if (nr > pageIndex * count && nr <= (pageIndex + 1) * count)
                {
                    page += PageUtility.CreateGuildRow(nr, guild.Item2.Item1, Table_Columns);
                    guildProgressData += guild.Item2.Item2;
                }
                if (nr >= (pageIndex + 1) * count)
                    break;
            }
            if (nr != 0 && nr <= pageIndex * count)
            {
                pageIndex = (nr - 1) / count;
                Response.Redirect(PageUtility.CreateUrlWithNewQueryValue(Request, "page", (pageIndex + 1).ToString()));
            }
            m_BreadCrumbHTML = new MvcHtmlString(PageUtility.BreadCrumb_AddHome()
                + PageUtility.BreadCrumb_AddRealm(RealmControl.Realm)
                + PageUtility.BreadCrumb_AddFinish("Guilds"));
            m_GuildListInfoHTML = new MvcHtmlString(CreateGuildListInfo(progressGuilds.Count));
            m_TableHeadHTML = new MvcHtmlString(PageUtility.CreateGuildTableHeaderRow(Table_Columns));
            m_TableBodyHTML = new MvcHtmlString(page);

            m_PaginationHTML = new MvcHtmlString(PageUtility.CreatePagination(Request, pageNr, ((guildArray.Count() - 1) / count) + 1));
            m_GuildScriptData = new MvcHtmlString("<script>var guildProgress = new Array();"
            //+ "guildProgress['Dreamstate'] = { MC: '111111111', Ony: '1', BWL: '11111110', ZG: '0000000000', AQ20: '000000', AQ40: '00000000000', Naxx: '000000000000000', WB: '000000' };"
            //+ "guildProgress['Delirium'] = { MC: '111101011', Ony: '1', BWL: '11011011', ZG: '0000000000', AQ20: '000000', AQ40: '00000000000', Naxx: '000000000000000', WB: '000000' };"
            //+ "guildProgress['Team_Plague'] = { MC: '111101011', Ony: '1', BWL: '10011010', ZG: '0000000000', AQ20: '000000', AQ40: '00000000000', Naxx: '000000000000000', WB: '010010' };"
            + guildProgressData
            + "</script>");


        }

        public static string CreateProgressStr(System.Web.UI.Page _Page, VF_RPDatabase.GuildSummary _Guild, WowRealm _Realm, out int _RetProgressComparisonValue)
        {
            var wowVersion = StaticValues.GetWowVersion(_Realm);
            Dictionary<WowBoss, int> m_MembersWithBossItems = new Dictionary<WowBoss, int>();
            for (int i = (int)WowBoss.MCFirst; i <= (int)WowBoss.MCLast; ++i) m_MembersWithBossItems.Add((WowBoss)i, 0);
            for (int i = (int)WowBoss.OnyFirst; i <= (int)WowBoss.OnyLast; ++i) m_MembersWithBossItems.Add((WowBoss)i, 0);
            for (int i = (int)WowBoss.BWLFirst; i <= (int)WowBoss.BWLLast; ++i) m_MembersWithBossItems.Add((WowBoss)i, 0);
            for (int i = (int)WowBoss.ZGFirst; i <= (int)WowBoss.ZGLast; ++i) m_MembersWithBossItems.Add((WowBoss)i, 0);
            for (int i = (int)WowBoss.AQ20First; i <= (int)WowBoss.AQ20Last; ++i) m_MembersWithBossItems.Add((WowBoss)i, 0);
            for (int i = (int)WowBoss.AQ40First; i <= (int)WowBoss.AQ40Last; ++i) m_MembersWithBossItems.Add((WowBoss)i, 0);
            for (int i = (int)WowBoss.NaxxFirst; i <= (int)WowBoss.NaxxLast; ++i) m_MembersWithBossItems.Add((WowBoss)i, 0);
            for (int i = (int)WowBoss.WBFirst; i <= (int)WowBoss.WBLast; ++i) m_MembersWithBossItems.Add((WowBoss)i, 0);

            var itemDropdatabase = DatabaseAccess.GetItemDropDatabase(_Page, wowVersion, NotLoadedDecision.RedirectAndWait).GetDatabase();
            var playersHistory = DatabaseAccess.GetRealmPlayersHistory(_Page, _Realm, NotLoadedDecision.RedirectAndWait);
            foreach (var guildPlayer in _Guild.Players)
            {
                if (guildPlayer.Value.IsInGuild == false)
                    continue;
                List<int> ignoredItems = new List<int>();
                VF_RealmPlayersDatabase.PlayerData.PlayerHistory playerHistory = null;
                if (playersHistory.TryGetValue(guildPlayer.Value.PlayerName, out playerHistory) == true)
                {
                    try
                    {
                        if (playerHistory.HaveValidHistory() == false)
                            continue;
                        foreach (var gear in playerHistory.GearHistory)
                        {
                            bool isInGuild = (playerHistory.GetGuildItemAtTime(gear.Uploader.GetTime()).Data.GuildName == _Guild.GuildName);
                            foreach (var item in gear.Data.Items)
                            {
                                if (ignoredItems.Contains(item.Value.ItemID))
                                    continue;

                                if (isInGuild == false)
                                {
                                    ignoredItems.Add(item.Value.ItemID);
                                    continue;
                                }

                                List<VF_RealmPlayersDatabase.ItemDropDataItem> itemDropDataList = null;
                                if (itemDropdatabase.TryGetValue(item.Value.ItemID, out itemDropDataList) == true)
                                {
                                    foreach (var itemDropData in itemDropDataList)
                                    {
                                        WowBoss itemBoss = itemDropData.m_Boss;
                                        if (itemBoss == WowBoss.Renataki_Of_The_Thousand_Blades
                                        || itemBoss == WowBoss.Wushoolay_the_Storm_Witch
                                        || itemBoss == WowBoss.Gri_Lek_Of_The_Iron_Blood
                                        || itemBoss == WowBoss.Hazzarah_The_Dreamweaver)
                                            itemBoss = WowBoss.Edge_Of_Madness;
                                        if (m_MembersWithBossItems.ContainsKey(itemBoss))
                                        {
                                            m_MembersWithBossItems[itemBoss] = m_MembersWithBossItems[itemBoss] + 1;
                                            ignoredItems.Add(item.Value.ItemID);
                                        }
                                        /*if (itemBoss >= WowBoss.MCFirst && itemBoss <= WowBoss.MCLast)
                                        {}
                                        else if (itemBoss >= WowBoss.OnyFirst && itemBoss <= WowBoss.OnyLast)
                                        {}
                                        else if (itemBoss >= WowBoss.BWLFirst && itemBoss <= WowBoss.BWLLast)
                                        {}
                                        else if (itemBoss >= WowBoss.ZGFirst && itemBoss <= WowBoss.ZGLast)
                                        {}
                                        else if (itemBoss >= WowBoss.AQ20First && itemBoss <= WowBoss.AQ20Last)
                                        {}
                                        else if (itemBoss >= WowBoss.AQ40First && itemBoss <= WowBoss.AQ40Last)
                                        {}
                                        else if (itemBoss >= WowBoss.NaxxFirst && itemBoss <= WowBoss.NaxxLast)
                                        {}*/
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.ConsoleWriteLine("GuildList.aspx could not look through gear for player \"" + guildPlayer.Value.PlayerName + "\" Exception:" + ex.ToString(), ConsoleColor.Red);
                    }
                }
            }

            string mcString = "";
            string onyString = "";
            string bwlString = "";
            string zgString = "";
            string aq20String = "";
            string aq40String = "";
            string naxxString = "";
            string wbString = "";
            for (int i = (int)WowBoss.MCFirst; i <= (int)WowBoss.MCLast; ++i) mcString += ((m_MembersWithBossItems[(WowBoss)i] >= 2) ? "1" : "0");
            for (int i = (int)WowBoss.OnyFirst; i <= (int)WowBoss.OnyLast; ++i) onyString += ((m_MembersWithBossItems[(WowBoss)i] >= 2) ? "1" : "0");
            for (int i = (int)WowBoss.BWLFirst; i <= (int)WowBoss.BWLLast; ++i) bwlString += ((m_MembersWithBossItems[(WowBoss)i] >= 2) ? "1" : "0");
            for (int i = (int)WowBoss.ZGFirst; i <= (int)WowBoss.ZGLast; ++i) zgString += ((m_MembersWithBossItems[(WowBoss)i] >= 2) ? "1" : "0");
            for (int i = (int)WowBoss.AQ20First; i <= (int)WowBoss.AQ20Last; ++i) aq20String += ((m_MembersWithBossItems[(WowBoss)i] >= 2) ? "1" : "0");
            for (int i = (int)WowBoss.AQ40First; i <= (int)WowBoss.AQ40Last; ++i) aq40String += ((m_MembersWithBossItems[(WowBoss)i] >= 2) ? "1" : "0");
            for (int i = (int)WowBoss.NaxxFirst; i <= (int)WowBoss.NaxxLast; ++i) naxxString += ((m_MembersWithBossItems[(WowBoss)i] >= 2) ? "1" : "0");
            for (int i = (int)WowBoss.WBFirst; i <= (int)WowBoss.WBLast; ++i) wbString += ((m_MembersWithBossItems[(WowBoss)i] >= 2) ? "1" : "0");

            string oldMCstring = mcString;
            string oldBWLstring = bwlString;
            string oldZGstring = zgString;
            string oldAQ20string = aq20String;
            string oldAQ40string = aq40String;
            string oldNaxxstring = naxxString;

            if (mcString[9] == '1') //If Ragnaros is killed, everything is killed
                mcString = "1111111111";
            else if (mcString[8] == '1') //If Majordomo is killed, everything up to Majordomo is killed
                mcString = "1111111110";

            if (bwlString[7] == '1') //If Nefarian is killed, everything is killed
                bwlString = "11111111";
            else if (bwlString.LastIndexOf('1') != -1)//If boss X is killed, everything up to boss X is killed
                bwlString = new string('1', bwlString.LastIndexOf('1') + 1) + bwlString.Substring(bwlString.LastIndexOf('1')+1);

            if (zgString[5] == '1') //If Hakkar is killed, everything except optionals is killed
                zgString = "111111" + zgString.Substring(6);

            if (aq20String[2] == '1') //If Ossirian is killed, everything except optionals is killed
                aq20String = "111" + aq20String.Substring(3);

            if (aq40String[5] == '1') //If C'thun is killed, everything except optionals is killed
                aq40String = "111111" + aq40String.Substring(6);
            else if (aq40String.LastIndexOf('1', 5, 6) != -1) //If boss X is killed, everything up to boss X is killed
                aq40String = new string('1', aq40String.LastIndexOf('1', 5, 6) + 1) + aq40String.Substring(aq40String.LastIndexOf('1', 5, 6) + 1);
            
            if(naxxString[14] == '1') 
                naxxString = "111111111111111";
            else if (naxxString[13] == '1') 
                naxxString = "111111111111110";

            //if (oldMCstring != mcString)
            //{
            //    //take notes
            //    System.Diagnostics.Debugger.Break();
            //}
            //if (oldBWLstring != bwlString)
            //{
            //    //take notes
            //    System.Diagnostics.Debugger.Break();
            //}
            //if (oldZGstring != zgString)
            //{
            //    //take notes
            //    System.Diagnostics.Debugger.Break();
            //}
            //if (oldAQ20string != aq20String)
            //{
            //    //take notes
            //    System.Diagnostics.Debugger.Break();
            //}
            //if (oldAQ40string != aq40String)
            //{
            //    //take notes
            //    System.Diagnostics.Debugger.Break();
            //}
            //if (oldNaxxstring != naxxString)
            //{
            //    //take notes
            //    System.Diagnostics.Debugger.Break();
            //}


            _RetProgressComparisonValue = GuildProgress.CreateProgressInt(
                    zgString.Count(_Char => _Char == '1'),
                    aq20String.Count(_Char => _Char == '1'),
                    mcString.Count(_Char => _Char == '1'),
                    onyString.Count(_Char => _Char == '1'),
                    wbString.Count(_Char => _Char == '1'),
                    bwlString.Count(_Char => _Char == '1'),
                    aq40String.Count(_Char => _Char == '1'),
                    naxxString.Count(_Char => _Char == '1')
                );

            return "guildProgress['" + _Guild.GuildName.Replace(' ', '_') + "'] = { MC: '" + mcString
                + "', Ony: '" + onyString
                + "', BWL: '" + bwlString
                + "', ZG: '" + zgString
                + "', AQ20: '" + aq20String
                + "', AQ40: '" + aq40String
                + "', Naxx: '" + naxxString
                + "', WB: '" + wbString + "' };";
        }
    }
}