using System;
using System.Text;
using VRageMath;

using RichHudFramework.Client;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using RichHudFramework.UI.Rendering;

namespace ScenariumAPI
{
    public class ScenariumHUD
    {
        readonly ScenariumSaveData _data;
        readonly Action<string> _addEvent;
        readonly Action _save;
        ScenariumViewModel _viewModel;

        TexturedBox _root;
        TexturedBox _titleBar;
        TexturedBox _titleSep;
        TexturedBox _tabBar;
        TexturedBox _tabSep;
        TexturedBox _sidePanel;
        TexturedBox _sideSep;
        TexturedBox _mainPanel;
        TexturedBox _statusBar;
        TexturedBox _statusSep;

        TexturedBox _summaryBox;
        TexturedBox _summarySep;
        TexturedBox _detailBox;
        TexturedBox _activityBox;
        TexturedBox _activitySep;
        TexturedBox _sideBoxA;
        TexturedBox _sideBoxB;
        TexturedBox _sideBoxC;
        TexturedBox _sideBoxD;

        Label _title;
        Label _subTitle;
        Label _sideTitle;
        Label _summaryTitle;
        Label _summaryBody;
        Label _detailTitle;
        Label _detailBody;
        Label _activityTitle;
        Label _activityBody;
        Label _status;

        LabelBoxButton _tabScenario;
        LabelBoxButton _tabQuests;
        LabelBoxButton _tabFactions;
        LabelBoxButton _tabIntel;

        LabelBoxButton _sideOverview;
        LabelBoxButton _sidePrimary;
        LabelBoxButton _sideSecondary;
        LabelBoxButton _sideTertiary;

        LabelBoxButton _closeButton;

        bool _created;
        int _lastHash;

        // SE terminal palette — dark neutral steel with cyan accent
        readonly Color RootColor       = new Color(14, 20, 26, 220);
        readonly Color HeaderColor     = new Color(22, 32, 41, 245);
        readonly Color SepColor        = new Color(42, 60, 74,  160);
        readonly Color PanelColor      = new Color(16, 24, 31, 210);
        readonly Color PanelAltColor   = new Color(18, 27, 34, 230);
        readonly Color CardColor       = new Color(26, 37, 47, 220);
        readonly Color CardActiveColor = new Color(54, 78, 96, 250);
        readonly Color ButtonHoverColor= new Color(48, 70, 86, 250);
        readonly Color CloseColor      = new Color(36, 26, 26, 240);
        readonly Color CloseHoverColor = new Color(110, 40, 40, 255);
        readonly Color TextColor       = new Color(222, 230, 236, 255);
        readonly Color MutedTextColor  = new Color(148, 168, 182, 210);
        readonly Color AccentColor     = new Color(120, 188, 212, 255);
        readonly Color AccentDimColor  = new Color(90, 148, 168, 255);

        public ScenariumHUD(ScenariumSaveData data, Action<string> addEvent, Action save)
        {
            _data = data;
            _addEvent = addEvent;
            _save = save;
            _viewModel = new ScenariumViewModel();
        }

        public void SetViewModel(ScenariumViewModel viewModel)
        {
            _viewModel = viewModel ?? new ScenariumViewModel();
            Refresh(true);
        }

        public void Create()
        {
            if (_created)
                return;

            RichHudClient.Init("ScenariumAPI", HudInit, ClientReset);
        }

        void HudInit()
        {
            // Root panel
            _root      = Box(HudMain.HighDpiRoot, new Vector2(1240f, 790f), new Vector2(0f,    0f),    RootColor,    10);

            // Title bar + separator
            _titleBar  = Box(_root, new Vector2(1240f,  62f), new Vector2(0f,  356f), HeaderColor,   11);
            _titleSep  = Box(_root, new Vector2(1240f,   2f), new Vector2(0f,  325f), SepColor,      12);

            // Tab bar + separator
            _tabBar    = Box(_root, new Vector2(1240f,  50f), new Vector2(0f,  299f), PanelAltColor, 11);
            _tabSep    = Box(_root, new Vector2(1240f,   2f), new Vector2(0f,  274f), SepColor,      12);

            // Side panel + vertical separator
            _sidePanel = Box(_root, new Vector2(338f,  614f), new Vector2(-431f, -32f), PanelColor,  11);
            _sideSep   = Box(_root, new Vector2(  2f,  614f), new Vector2(-261f, -32f), SepColor,    12);

            // Main content panel
            _mainPanel = Box(_root, new Vector2(820f,  614f), new Vector2( 190f, -32f), PanelColor,  11);

            // Status bar + separator
            _statusSep = Box(_root, new Vector2(1240f,   2f), new Vector2(0f, -350f), SepColor,      12);
            _statusBar = Box(_root, new Vector2(1240f,  42f), new Vector2(0f, -374f), HeaderColor,   11);

            // Main content cards
            _summaryBox  = Box(_mainPanel, new Vector2(778f, 118f), new Vector2(0f,  210f), CardColor, 12);
            _summarySep  = Box(_mainPanel, new Vector2(778f,   1f), new Vector2(0f,  150f), SepColor,  13);
            _detailBox   = Box(_mainPanel, new Vector2(778f, 350f), new Vector2(0f,  -28f), new Color(12, 18, 24, 225), 12);
            _activitySep = Box(_mainPanel, new Vector2(778f,   1f), new Vector2(0f, -205f), SepColor,  13);
            _activityBox = Box(_mainPanel, new Vector2(778f, 100f), new Vector2(0f, -258f), CardColor, 12);

            // Sidebar nav cards
            _sideBoxA = Box(_sidePanel, new Vector2(290f, 60f), new Vector2(0f,  176f), CardColor, 12);
            _sideBoxB = Box(_sidePanel, new Vector2(290f, 60f), new Vector2(0f,  100f), CardColor, 12);
            _sideBoxC = Box(_sidePanel, new Vector2(290f, 60f), new Vector2(0f,   24f), CardColor, 12);
            _sideBoxD = Box(_sidePanel, new Vector2(290f, 60f), new Vector2(0f,  -52f), CardColor, 12);

            // Title bar labels
            _title     = Label(_titleBar, "SCENARIUM",                  new Vector2(-438f,  10f), new Vector2(360f, 32f), AccentColor,    1.08f, TextAlignment.Left,   13);
            _subTitle  = Label(_titleBar, "Campaign Control Interface", new Vector2(-438f, -14f), new Vector2(440f, 22f), MutedTextColor, 0.66f, TextAlignment.Left,   13);
            _closeButton = Button(_titleBar, "✕", new Vector2(578f, 0f), new Vector2(36f, 36f), CloseColor, CloseHoverColor, 13);
            _closeButton.MouseInput.LeftClicked += delegate { Close(); _addEvent("Scenarium panel closed."); _save(); };

            // Tab buttons — evenly spaced
            _tabScenario = TabButton("SCENARIO",  -420f, "SCENARIO");
            _tabQuests   = TabButton("QUESTS",    -140f, "QUESTS");
            _tabFactions = TabButton("FACTIONS",   140f, "FACTIONS");
            _tabIntel    = TabButton("INTEL LOG",  420f, "INTEL");

            // Sidebar nav
            _sideTitle     = Label(_sidePanel, "NAVIGATION", new Vector2(0f, 258f), new Vector2(300f, 30f), AccentDimColor, 0.78f, TextAlignment.Center, 13);
            _sideOverview  = SideButton("Overview",  176f, "OVERVIEW");
            _sidePrimary   = SideButton("Primary",   100f, "PRIMARY");
            _sideSecondary = SideButton("Secondary",  24f, "SECONDARY");
            _sideTertiary  = SideButton("Details",   -52f, "TERTIARY");

            // Summary card
            _summaryTitle = Label(_summaryBox, "SUMMARY", new Vector2(0f,  40f), new Vector2(710f, 24f), AccentDimColor, 0.78f, TextAlignment.Left, 13);
            _summaryBody  = Label(_summaryBox, "",         new Vector2(  0f, -12f), new Vector2(700f, 76f), TextColor,      0.84f, TextAlignment.Left, 13);
            _summaryBody.VertCenterText = false;

            // Detail card
            _detailTitle = Label(_detailBox, "DETAILS", new Vector2(-358f, 150f), new Vector2(220f, 20f), AccentDimColor, 0.58f, TextAlignment.Left,  13);
            _detailBody  = Label(_detailBox, "",         new Vector2(  0f, -12f), new Vector2(700f, 292f), TextColor,     0.80f, TextAlignment.Left,  13);
            _detailBody.VertCenterText = false;
            _detailBody.LineWrapWidth = 680f;

            // Activity card
            _activityTitle = Label(_activityBox, "RECENT ACTIVITY", new Vector2(0f, 32f), new Vector2(710f, 24f), AccentDimColor, 0.74f, TextAlignment.Left, 13);
            _activityBody  = Label(_activityBox, "",                  new Vector2(  0f, -18f), new Vector2(700f, 62f), TextColor,     0.74f, TextAlignment.Left, 13);
            _activityBody.VertCenterText = false;
            _activityBody.LineWrapWidth = 680f;

            // Status bar
            _status = Label(_statusBar, "SHIFT+Q  Open / Close   |   Click tabs and sidebar cards to navigate", new Vector2(0f, 0f), new Vector2(1120f, 30f), MutedTextColor, 0.72f, TextAlignment.Left, 13);

            _created = true;
            HudMain.EnableCursor = _data.PanelVisible;
            Refresh(true);
        }

        TexturedBox Box(HudParentBase parent, Vector2 size, Vector2 offset, Color color, int z)
        {
            TexturedBox box = new TexturedBox(parent);
            box.Size = size;
            box.Offset = offset;
            box.Color = color;
            box.ZOffset = (sbyte)z;
            box.Visible = _data.PanelVisible;
            return box;
        }

        Label Label(HudParentBase parent, string text, Vector2 offset, Vector2 size, Color color, float scale, TextAlignment align, int z)
        {
            Label label = new Label(parent);
            label.Text = text;
            label.Offset = offset;
            label.Size = size;
            label.AutoResize = false;
            label.VertCenterText = true;
            label.BuilderMode = TextBuilderModes.Wrapped;
            label.LineWrapWidth = size.X - 18f;
            label.TextBoard.Scale = scale;
            label.TextBoard.SetFormatting(new GlyphFormat(color, align, scale));
            label.ZOffset = (sbyte)z;
            label.Visible = _data.PanelVisible;
            return label;
        }

        LabelBoxButton Button(HudParentBase parent, string text, Vector2 offset, Vector2 size, Color color, Color highlight, int z)
        {
            LabelBoxButton button = new LabelBoxButton(parent);
            button.Text = text;
            button.Offset = offset;
            button.Size = size;
            button.Color = color;
            button.HighlightColor = highlight;
            button.TextBoard.Scale = 0.82f;
            button.TextBoard.SetFormatting(new GlyphFormat(TextColor, TextAlignment.Center, 0.82f));
            button.AutoResize = false;
            button.VertCenterText = true;
            button.MouseInput.RequestCursor = true;
            button.ZOffset = (sbyte)z;
            button.Visible = _data.PanelVisible;
            return button;
        }

        LabelBoxButton TabButton(string text, float x, string tab)
        {
            LabelBoxButton button = Button(_tabBar, text, new Vector2(x, 0f), new Vector2(245f, 42f), TabColor(tab), ButtonHoverColor, 13);
            button.TextBoard.SetFormatting(new GlyphFormat(TabTextColor(tab), TextAlignment.Center, 0.82f));
            button.MouseInput.LeftClicked += delegate
            {
                _data.PanelTab = tab;
                _data.SelectedItemId = "OVERVIEW";
                _addEvent("Opened " + text + " tab.");
                Refresh(true);
                _save();
            };
            return button;
        }

        LabelBoxButton SideButton(string text, float y, string id)
        {
            LabelBoxButton button = Button(_sidePanel, text, new Vector2(0f, y), new Vector2(290f, 54f), CardColor, ButtonHoverColor, 18);
            button.TextBoard.SetFormatting(new GlyphFormat(TextColor, TextAlignment.Left, 0.68f));
            button.MouseInput.RequestCursor = true;

            button.MouseInput.LeftClicked += delegate
            {
                _data.SelectedItemId = id;
                Refresh(true);
                _save();
            };

            return button;
        }

        Color TabColor(string tab)
        {
            return _data.PanelTab == tab ? CardActiveColor : CardColor;
        }

        Color TabTextColor(string tab)
        {
            return _data.PanelTab == tab ? AccentColor : MutedTextColor;
        }

        void ClientReset()
        {
            _created = false;
        }

        public void Open()
        {
            _data.PanelVisible = true;
            SetVisible(true);
            HudMain.EnableCursor = true;
            Refresh(true);
        }

        public void Close()
        {
            _data.PanelVisible = false;
            HudMain.EnableCursor = false;
            SetVisible(false);
        }

        public void Refresh(bool force = false)
        {
            if (!_created || _detailBody == null)
                return;

            UpdateTabVisuals();
            UpdateSidebar();

            string hashText = _data.PanelTab + "|" + _data.SelectedItemId + "|" + _data.Events.Count + "|" + CountActiveQuests() + "|" + CountCompletedQuests() + "|" + (_viewModel != null ? _viewModel.Version : 0);
            int hash = hashText.GetHashCode();

            if (force || hash != _lastHash)
            {
                _lastHash = hash;
                PopulateContent();
                _status.Text = "SHIFT+Q  Open / Close   |   Tab: " + PrettyTab(_data.PanelTab) + "   |   Card: " + FormatSelection(_data.SelectedItemId);
            }

            SetVisible(_data.PanelVisible);
            HudMain.EnableCursor = _data.PanelVisible;
        }

        void UpdateTabVisuals()
        {
            if (_tabScenario != null)
            {
                _tabScenario.Color = TabColor("SCENARIO");
                _tabScenario.TextBoard.SetFormatting(new GlyphFormat(TabTextColor("SCENARIO"), TextAlignment.Center, 0.82f));
            }
            if (_tabQuests != null)
            {
                _tabQuests.Color = TabColor("QUESTS");
                _tabQuests.TextBoard.SetFormatting(new GlyphFormat(TabTextColor("QUESTS"), TextAlignment.Center, 0.82f));
            }
            if (_tabFactions != null)
            {
                _tabFactions.Color = TabColor("FACTIONS");
                _tabFactions.TextBoard.SetFormatting(new GlyphFormat(TabTextColor("FACTIONS"), TextAlignment.Center, 0.82f));
            }
            if (_tabIntel != null)
            {
                _tabIntel.Color = TabColor("INTEL");
                _tabIntel.TextBoard.SetFormatting(new GlyphFormat(TabTextColor("INTEL"), TextAlignment.Center, 0.82f));
            }
        }

        void UpdateSidebar()
        {
            if (_data.PanelTab == "SCENARIO")
            {
                _sideTitle.Text = "SCENARIO";
                _sideOverview.Text  = "Campaign Overview";
                _sidePrimary.Text   = "Current Objective";
                _sideSecondary.Text = "Progression Chain";
                _sideTertiary.Text  = "Sector Status";
            }
            else if (_data.PanelTab == "QUESTS")
            {
                _sideTitle.Text = "QUESTS";
                _sideOverview.Text  = "Quest Overview";
                _sidePrimary.Text   = "Active Objectives";
                _sideSecondary.Text = "Completed";
                _sideTertiary.Text  = "Locked";
            }
            else if (_data.PanelTab == "FACTIONS")
            {
                _sideTitle.Text = "FACTIONS";
                _sideOverview.Text  = "Faction Overview";
                _sidePrimary.Text   = "UTD Status";
                _sideSecondary.Text = "Conquest Chain";
                _sideTertiary.Text  = "Doctrine";
            }
            else
            {
                _sideTitle.Text = "INTEL LOG";
                _sideOverview.Text  = "Recent Events";
                _sidePrimary.Text   = "Operational Intel";
                _sideSecondary.Text = "System Notes";
                _sideTertiary.Text  = "Debug Commands";
            }

            HighlightSide(_sideOverview,  _sideBoxA, "OVERVIEW");
            HighlightSide(_sidePrimary,   _sideBoxB, "PRIMARY");
            HighlightSide(_sideSecondary, _sideBoxC, "SECONDARY");
            HighlightSide(_sideTertiary,  _sideBoxD, "TERTIARY");
        }

        void HighlightSide(LabelBoxButton button, TexturedBox box, string id)
        {
            bool active = _data.SelectedItemId == id;
            Color bg = active ? CardActiveColor : CardColor;
            Color fg = active ? AccentColor : MutedTextColor;

            if (button != null)
            {
                button.Color = bg;
                button.TextBoard.SetFormatting(new GlyphFormat(fg, TextAlignment.Left, 0.68f));
            }

            if (box != null)
                box.Color = CardColor;
        }
            if (box != null)
                box.Color = bg;
        }

        void PopulateContent()
        {
            _summaryTitle.Text  = "SUMMARY";
            _detailTitle.Text   = PrettyTab(_data.PanelTab).ToUpper() + "  •  " + FormatSelection(_data.SelectedItemId).ToUpper();
            _activityTitle.Text = "RECENT ACTIVITY";

            if (_data.PanelTab == "SCENARIO") PopulateScenario();
            else if (_data.PanelTab == "QUESTS") PopulateQuests();
            else if (_data.PanelTab == "FACTIONS") PopulateFactions();
            else PopulateIntel();

            _activityBody.Text = BuildRecentEvents(2);
        }

        void PopulateScenario()
        {
            _summaryBody.Text =
                "Campaign: " + GetCampaignName() + "     " +
                "Sector: " + GetSectorName() + "     " +
                "State: " + GetCampaignState();

            if (_data.SelectedItemId == "PRIMARY")
            {
                _detailBody.Text =
                    "CURRENT OBJECTIVE\n\n" +
                    GetActiveObjectiveTitle() + "\n\n" +
                    GetActiveObjectiveDescription();
                return;
            }

            if (_data.SelectedItemId == "SECONDARY")
            {
                _detailBody.Text =
                    "PROGRESSION CHAIN\n\n" +
                    "01  Conquer regional factions on Earth.\n" +
                    "02  Recover jump-gate components.\n" +
                    "03  Build and activate Jump Gate Alpha.\n" +
                    "04  Travel to the next sector.\n" +
                    "05  Escalate through later campaign sectors.";
                return;
            }

            if (_data.SelectedItemId == "TERTIARY")
            {
                _detailBody.Text =
                    "SECTOR STATUS\n\n" +
                    "Current Sector:     " + GetSectorName() + "\n" +
                    "Campaign State:      " + GetCampaignState() + "\n" +
                    "Tracked Factions:   " + GetFactionCount() + "\n" +
                    "Tracked Nodes:      " + GetNodeCount();
                return;
            }

            _detailBody.Text =
                "CAMPAIGN OVERVIEW\n\n" +
                "Scenarium tracks campaign state, conquest chains, faction defeat, " +
                "quest unlocks, rewards, and sector progression.\n\n" +
                "The API owns strategic state. Campaign packs provide content.";
        }

        void PopulateQuests()
        {
            _summaryBody.Text =
                "Active: " + CountActiveQuests() +
                "     Completed: " + CountCompletedQuests() +
                "     Locked: " + CountLockedQuests();

            if (_data.SelectedItemId == "PRIMARY")
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("ACTIVE OBJECTIVES\n");
                foreach (ScenariumQuestState q in _data.Quests)
                    if (q.Revealed && !q.Completed)
                        sb.AppendLine("[ ] " + q.Title + "\n    " + q.Description + "\n");
                _detailBody.Text = sb.ToString();
                return;
            }

            if (_data.SelectedItemId == "SECONDARY")
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("COMPLETED OBJECTIVES\n");
                foreach (ScenariumQuestState q in _data.Quests)
                    if (q.Completed)
                        sb.AppendLine("[X] " + q.Title + " (" + q.Id + ")");
                _detailBody.Text = sb.ToString();
                return;
            }

            if (_data.SelectedItemId == "TERTIARY")
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("LOCKED OBJECTIVES\n");
                foreach (ScenariumQuestState q in _data.Quests)
                    if (!q.Revealed && !q.Completed)
                        sb.AppendLine("[?] " + q.Id);
                _detailBody.Text = sb.ToString();
                return;
            }

            _detailBody.Text =
                "QUEST OVERVIEW\n\n" +
                "Use the sidebar cards to inspect active, completed, and locked objectives.\n\n" +
                "The next runtime pass will bind this page to campaign-pack quest definitions.";
        }

        void PopulateFactions()
        {
            _summaryBody.Text =
                "Tracked Factions: " + GetFactionCount() + "     UTD: " + GetFactionState("UTD");

            if (_data.SelectedItemId == "PRIMARY")
            {
                _detailBody.Text =
                    "UTD STATUS\n\n" +
                    "State: " + GetFactionState("UTD") + "\n\n" +
                    "The United Terran Directorate is the current proof-of-concept " +
                    "faction for Scenarium conquest state.";
                return;
            }

            if (_data.SelectedItemId == "SECONDARY")
            {
                _detailBody.Text = BuildNodeStatusLines();
                return;
            }

            if (_data.SelectedItemId == "TERTIARY")
            {
                _detailBody.Text =
                    "DOCTRINE STATES\n\n" +
                    "Peacetime    Mining, transport, economy, patrols.\n" +
                    "Alert        QRF, interceptors, guarded convoys.\n" +
                    "War          Military outposts, defended sites, strike packages.\n" +
                    "Defeated     Faction spawns disabled by campaign logic.";
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("FACTION OVERVIEW\n");

            if (_viewModel != null && _viewModel.FactionLines != null && _viewModel.FactionLines.Count > 0)
            {
                foreach (string line in _viewModel.FactionLines)
                    sb.AppendLine(line);
            }
            else
            {
                foreach (ScenariumFactionState f in _data.Factions)
                    sb.AppendLine(f.Tag + "     " + f.State + (f.Defeated ? "     DEFEATED" : ""));
            }

            _detailBody.Text = sb.ToString();
        }

        void PopulateIntel()
        {
            _summaryBody.Text =
                "Events: " + _data.Events.Count + "     Nodes: " + GetNodeCount() + "     Runtime command feedback";

            if (_data.SelectedItemId == "PRIMARY")
            {
                _detailBody.Text =
                    "OPERATIONAL INTEL\n\n" +
                    "Intel entries will later include coordinates, discovered bases, " +
                    "faction alerts, and unlocked objective chains.";
                return;
            }

            if (_data.SelectedItemId == "SECONDARY")
            {
                _detailBody.Text =
                    "SYSTEM NOTES\n\n" +
                    "RichHud interface active.\n" +
                    "Mouse cursor is enabled while the Scenarium panel is open.\n" +
                    "Tab buttons and sidebar cards are clickable.";
                return;
            }

            if (_data.SelectedItemId == "TERTIARY")
            {
                _detailBody.Text =
                    "DEBUG COMMANDS\n\n" +
                    "/scen reload\n" +
                    "/scen validate\n" +
                    "/scen nodes\n" +
                    "/scen query campaign\n" +
                    "/scen query faction UTD\n" +
                    "/scen destroy UTD_EARTH_OUTPOST_01";
                return;
            }

            if (_viewModel != null && _viewModel.NodeLines != null && _viewModel.NodeLines.Count > 0)
            {
                _detailBody.Text = BuildNodeStatusLines() + "\n" + BuildRecentEvents(5);
            }
            else
            {
                _detailBody.Text = BuildRecentEvents(12);
            }
        }


        string GetCampaignName()
        {
            if (_viewModel != null && !string.IsNullOrWhiteSpace(_viewModel.CampaignDisplayName))
                return _viewModel.CampaignDisplayName;

            return _data.CampaignId;
        }

        string GetSectorName()
        {
            if (_viewModel != null && !string.IsNullOrWhiteSpace(_viewModel.CurrentSectorId))
                return _viewModel.CurrentSectorId;

            return _data.CurrentSector;
        }

        string GetCampaignState()
        {
            if (_viewModel != null && !string.IsNullOrWhiteSpace(_viewModel.CampaignState))
                return _viewModel.CampaignState;

            return _data.CampaignStage;
        }

        int GetFactionCount()
        {
            if (_viewModel != null && _viewModel.FactionLines != null && _viewModel.FactionLines.Count > 0)
                return _viewModel.FactionLines.Count;

            return _data.Factions.Count;
        }

        int GetNodeCount()
        {
            if (_viewModel != null && _viewModel.NodeLines != null)
                return _viewModel.NodeLines.Count;

            return 0;
        }

        string BuildNodeStatusLines()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CONQUEST NODES\n");

            if (_viewModel != null && _viewModel.NodeLines != null && _viewModel.NodeLines.Count > 0)
            {
                foreach (string line in _viewModel.NodeLines)
                    sb.AppendLine(line);
            }
            else
            {
                sb.AppendLine("No conquest nodes loaded.");
                sb.AppendLine("Run /scen reload.");
            }

            return sb.ToString();
        }

        string BuildRecentEvents(int max)
        {
            StringBuilder sb = new StringBuilder();
            int start = Math.Max(0, _data.Events.Count - max);

            for (int i = start; i < _data.Events.Count; i++)
                sb.AppendLine("• " + _data.Events[i].Message);

            if (sb.Length == 0)
                sb.AppendLine("No recent events.");

            return sb.ToString();
        }

        string PrettyTab(string tab)
        {
            if (tab == "SCENARIO") return "Scenario";
            if (tab == "QUESTS")   return "Quests";
            if (tab == "FACTIONS") return "Factions";
            if (tab == "INTEL")    return "Intel Log";
            return "Scenario";
        }

        string FormatSelection(string id)
        {
            if (id == "PRIMARY")   return "Primary";
            if (id == "SECONDARY") return "Secondary";
            if (id == "TERTIARY")  return "Details";
            return "Overview";
        }

        string GetActiveObjectiveTitle()
        {
            foreach (ScenariumQuestState q in _data.Quests)
                if (q.Revealed && !q.Completed && q.Active)
                    return q.Title;

            foreach (ScenariumQuestState q in _data.Quests)
                if (q.Revealed && !q.Completed)
                    return q.Title;

            return "None";
        }

        string GetActiveObjectiveDescription()
        {
            foreach (ScenariumQuestState q in _data.Quests)
                if (q.Revealed && !q.Completed && q.Active)
                    return q.Description;

            foreach (ScenariumQuestState q in _data.Quests)
                if (q.Revealed && !q.Completed)
                    return q.Description;

            return "No active objective.";
        }

        string GetFactionState(string tag)
        {
            foreach (ScenariumFactionState f in _data.Factions)
            {
                if (string.Equals(f.Tag, tag, StringComparison.OrdinalIgnoreCase))
                    return f.State + (f.Defeated ? " / DEFEATED" : "");
            }

            return "Unknown";
        }

        string GetQuestMark(string id)
        {
            foreach (ScenariumQuestState q in _data.Quests)
            {
                if (string.Equals(q.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    if (q.Completed) return "[X]";
                    if (q.Revealed)  return "[ ]";
                    return "[?]";
                }
            }

            return "[?]";
        }

        int CountActiveQuests()
        {
            int count = 0;
            foreach (ScenariumQuestState q in _data.Quests)
                if (q.Revealed && !q.Completed)
                    count++;
            return count;
        }

        int CountCompletedQuests()
        {
            int count = 0;
            foreach (ScenariumQuestState q in _data.Quests)
                if (q.Completed)
                    count++;
            return count;
        }

        int CountLockedQuests()
        {
            int count = 0;
            foreach (ScenariumQuestState q in _data.Quests)
                if (!q.Revealed && !q.Completed)
                    count++;
            return count;
        }

        void SetVisible(bool visible)
        {
            SetVisibleElement(_root,        visible);
            SetVisibleElement(_titleBar,    visible);
            SetVisibleElement(_titleSep,    visible);
            SetVisibleElement(_tabBar,      visible);
            SetVisibleElement(_tabSep,      visible);
            SetVisibleElement(_sidePanel,   visible);
            SetVisibleElement(_sideSep,     visible);
            SetVisibleElement(_mainPanel,   visible);
            SetVisibleElement(_statusBar,   visible);
            SetVisibleElement(_statusSep,   visible);
            SetVisibleElement(_summaryBox,  visible);
            SetVisibleElement(_summarySep,  visible);
            SetVisibleElement(_detailBox,   visible);
            SetVisibleElement(_activityBox, visible);
            SetVisibleElement(_activitySep, visible);
            SetVisibleElement(_sideBoxA,    visible);
            SetVisibleElement(_sideBoxB,    visible);
            SetVisibleElement(_sideBoxC,    visible);
            SetVisibleElement(_sideBoxD,    visible);

            SetVisibleElement(_title,         visible);
            SetVisibleElement(_subTitle,      visible);
            SetVisibleElement(_sideTitle,     visible);
            SetVisibleElement(_summaryTitle,  visible);
            SetVisibleElement(_summaryBody,   visible);
            SetVisibleElement(_detailTitle,   visible);
            SetVisibleElement(_detailBody,    visible);
            SetVisibleElement(_activityTitle, visible);
            SetVisibleElement(_activityBody,  visible);
            SetVisibleElement(_status,        visible);

            SetVisibleElement(_tabScenario,   visible);
            SetVisibleElement(_tabQuests,     visible);
            SetVisibleElement(_tabFactions,   visible);
            SetVisibleElement(_tabIntel,      visible);
            SetVisibleElement(_sideOverview,  visible);
            SetVisibleElement(_sidePrimary,   visible);
            SetVisibleElement(_sideSecondary, visible);
            SetVisibleElement(_sideTertiary,  visible);
            SetVisibleElement(_closeButton,   visible);
        }

        void SetVisibleElement(HudElementBase element, bool visible)
        {
            if (element != null)
                element.Visible = visible;
        }

        public void CloseAndDispose()
        {
            Close();
            _created = false;
        }
    }
}
