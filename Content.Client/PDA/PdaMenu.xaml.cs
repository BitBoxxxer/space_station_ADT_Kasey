using Content.Client.GameTicking.Managers;
using Content.Shared.PDA;
using Robust.Shared.Utility;
using Content.Shared.CartridgeLoader;
using Content.Client.Message;
using Robust.Client.UserInterface;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.XAML;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;


namespace Content.Client.PDA
{
    [GenerateTypedNameReferences]
    public sealed partial class PdaMenu : PdaWindow
    {
        [Dependency] private readonly IClipboardManager _clipboard = null!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
        private readonly ClientGameTicker _gameTicker;

        public const int HomeView = 0;
        public const int ProgramListView = 1;
        public const int SettingsView = 2;
        public const int ProgramContentView = 3;

        private string _pdaOwner = Loc.GetString("comp-pda-ui-unknown");
        private string _owner = Loc.GetString("comp-pda-ui-unknown");
        private string _jobTitle = Loc.GetString("comp-pda-ui-unassigned");
        private string _stationName = Loc.GetString("comp-pda-ui-unknown");
        private string _alertLevel = Loc.GetString("comp-pda-ui-unknown");
        private string _instructions = Loc.GetString("comp-pda-ui-unknown");
        private string _instructionsForCommands = Loc.GetString("comp-pda-ui-for-commands-unknown"); // ADT START
        private string _instructionsForCapitan = Loc.GetString("comp-pda-ui-for-capitan-unknown");
        private string _instructionsForSecurity = Loc.GetString("comp-pda-ui-for-security-unknown"); 
        private string _instructionsForAnother = Loc.GetString("comp-pda-ui-for-another-unknown"); // ADT END

        private int _currentView;

        public event Action<EntityUid>? OnProgramItemPressed;
        public event Action<EntityUid>? OnUninstallButtonPressed;
        public event Action<EntityUid>? OnInstallButtonPressed;


        private bool IsAnother(string jobTitle) // ADT START Капитан
        {
            return jobTitle.Equals("Клоун", StringComparison.OrdinalIgnoreCase);

        } // ADT END

        private bool IsCommand(string jobTitle) // ADT START Главы
        {
            return jobTitle.Equals("Главный врач", StringComparison.OrdinalIgnoreCase) ||
                jobTitle.Equals("Квартирмейстер", StringComparison.OrdinalIgnoreCase) ||
                jobTitle.Equals("Старший инженер", StringComparison.OrdinalIgnoreCase) ||
                jobTitle.Equals("Научный руководитель", StringComparison.OrdinalIgnoreCase) ||
                jobTitle.Equals("Глава службы безопасности", StringComparison.OrdinalIgnoreCase) ||
                jobTitle.Equals("Глава персонала", StringComparison.OrdinalIgnoreCase) ||
                jobTitle.Equals("Магистрат", StringComparison.OrdinalIgnoreCase);
                
        } // ADT END

        private bool IsCapitan(string jobTitle) // ADT START Капитан
        {
            return jobTitle.Equals("Капитан", StringComparison.OrdinalIgnoreCase);

        } // ADT END

        private bool IsSecurity(string jobTitle) // ADT START Служба безопасности
        {
            return jobTitle.Equals("Надзиратель", StringComparison.OrdinalIgnoreCase) ||
                jobTitle.Equals("Офицер-Инструктор", StringComparison.OrdinalIgnoreCase) ||
                jobTitle.Equals("Бригмедик", StringComparison.OrdinalIgnoreCase) ||
                jobTitle.Equals("Детектив", StringComparison.OrdinalIgnoreCase) ||
                jobTitle.Equals("Пилот", StringComparison.OrdinalIgnoreCase) ||
                jobTitle.Equals("Кадет СБ", StringComparison.OrdinalIgnoreCase) ||
                jobTitle.Equals("Офицер СБ", StringComparison.OrdinalIgnoreCase)  ||
                jobTitle.Equals("Смотритель", StringComparison.OrdinalIgnoreCase);

        } // ADT END
        

        public PdaMenu()
        {
            IoCManager.InjectDependencies(this);
            _gameTicker = _entitySystem.GetEntitySystem<ClientGameTicker>();
            RobustXamlLoader.Load(this);

            ViewContainer.OnChildAdded += control => control.Visible = false;

            HomeButton.IconTexture = new SpriteSpecifier.Texture(new("/Textures/Interface/home.png"));
            FlashLightToggleButton.IconTexture = new SpriteSpecifier.Texture(new("/Textures/Interface/light.png"));
            EjectPenButton.IconTexture = new SpriteSpecifier.Texture(new("/Textures/Interface/pencil.png"));
            EjectIdButton.IconTexture = new SpriteSpecifier.Texture(new("/Textures/Interface/eject.png"));
            EjectPaiButton.IconTexture = new SpriteSpecifier.Texture(new("/Textures/Interface/pai.png"));
            ProgramCloseButton.IconTexture = new SpriteSpecifier.Texture(new("/Textures/Interface/Nano/cross.svg.png"));

            HomeButton.OnPressed += _ => ToHomeScreen();

            ProgramListButton.OnPressed += _ =>
            {
                HomeButton.IsCurrent = false;
                ProgramListButton.IsCurrent = true;
                SettingsButton.IsCurrent = false;
                ProgramTitle.IsCurrent = false;

                ChangeView(ProgramListView);
            };

            SettingsButton.OnPressed += _ =>
            {
                HomeButton.IsCurrent = false;
                ProgramListButton.IsCurrent = false;
                SettingsButton.IsCurrent = true;
                ProgramTitle.IsCurrent = false;

                ChangeView(SettingsView);
            };

            ProgramTitle.OnPressed += _ =>
            {
                HomeButton.IsCurrent = false;
                ProgramListButton.IsCurrent = false;
                SettingsButton.IsCurrent = false;
                ProgramTitle.IsCurrent = true;

                ChangeView(ProgramContentView);
            };

            ProgramCloseButton.OnPressed += _ =>
            {
                HideProgramHeader();
                ToHomeScreen();
            };

            PdaOwnerButton.OnPressed += _ =>
            {
                _clipboard.SetText(_pdaOwner);
            };

            IdInfoButton.OnPressed += _ =>
            {
                _clipboard.SetText(_owner + ", " + _jobTitle);
            };

            StationNameButton.OnPressed += _ =>
            {
                _clipboard.SetText(_stationName);
            };

            StationAlertLevelButton.OnPressed += _ =>
            {
                _clipboard.SetText(_alertLevel);
            };

            StationTimeButton.OnPressed += _ =>
            {
                var stationTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
                _clipboard.SetText((stationTime.ToString("hh\\:mm\\:ss")));
            };

            StationAlertLevelInstructionsButton.OnPressed += _ =>
            {
                _clipboard.SetText(_instructions);
            };

            HideAllViews();
            ToHomeScreen();
        }



        public void UpdateState(PdaUpdateState state)
        {
            FlashLightToggleButton.IsActive = state.FlashlightEnabled;

            if (state.PdaOwnerInfo.ActualOwnerName != null)
            {
                _pdaOwner = state.PdaOwnerInfo.ActualOwnerName;
                PdaOwnerLabel.SetMarkup(Loc.GetString("comp-pda-ui-owner",
                    ("actualOwnerName", _pdaOwner)));
            }

            if (state.PdaOwnerInfo.IdOwner != null || state.PdaOwnerInfo.JobTitle != null)
            {
                _owner = state.PdaOwnerInfo.IdOwner ?? Loc.GetString("comp-pda-ui-unknown");
                _jobTitle = state.PdaOwnerInfo.JobTitle ?? Loc.GetString("comp-pda-ui-unassigned");
                IdInfoLabel.SetMarkup(Loc.GetString("comp-pda-ui",
                    ("owner", _owner),
                    ("jobTitle", _jobTitle)));

                // Инструкции "comp-pda-ui-for-commands (-unknown)" для ролей
                if (IsCapitan(_jobTitle) || IsCommand(_jobTitle) || IsSecurity(_jobTitle))
                {
                    if (IsCapitan(_jobTitle))
                    {
                        _instructionsForCapitan = Loc.GetString("comp-pda-ui-for-capitan");
                    }

                    if (IsCommand(_jobTitle))
                    {
                        _instructionsForCommands = Loc.GetString("comp-pda-ui-for-commands");
                    }

                    if (IsSecurity(_jobTitle))
                    {
                        _instructionsForSecurity = Loc.GetString("comp-pda-ui-for-security");
                    }

                    if (IsAnother(_jobTitle))
                    {
                        _instructionsForAnother = Loc.GetString("comp-pda-ui-for-another");
                    }
                }
                else
                {
                    _instructionsForCommands = Loc.GetString("comp-pda-ui-for-commands-unknown");
                } // ADT END
            }
            
            else
            {
                IdInfoLabel.SetMarkup(Loc.GetString("comp-pda-ui-blank"));
            }

            _stationName = state.StationName ?? Loc.GetString("comp-pda-ui-unknown");
            StationNameLabel.SetMarkup(Loc.GetString("comp-pda-ui-station",
                ("station", _stationName)));

            var stationTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);

            StationTimeLabel.SetMarkup(Loc.GetString("comp-pda-ui-station-time",
                ("time", stationTime.ToString("hh\\:mm\\:ss"))));

            var alertLevel = state.PdaOwnerInfo.StationAlertLevel;
            var alertColor = state.PdaOwnerInfo.StationAlertColor;
            var alertLevelKey = alertLevel != null ? $"alert-level-{alertLevel}" : "alert-level-unknown";
            _alertLevel = Loc.GetString(alertLevelKey);

            StationAlertLevelLabel.SetMarkup(Loc.GetString(
                "comp-pda-ui-station-alert-level",
                ("color", alertColor),
                ("level", _alertLevel)
            ));

            if (IsCapitan(_jobTitle) || IsCommand(_jobTitle) || IsSecurity(_jobTitle) || IsAnother(_jobTitle)) // ADT START
            {
                if (IsCapitan(_jobTitle)) // ADT
                {
                    _instructionsForCapitan = Loc.GetString($"{alertLevelKey}-instructions-for-capitan");
                    StationAlertLevelInstructionsForCapitan.SetMarkup(Loc.GetString(
                        "comp-pda-ui-for-capitan",
                        ("instructionsForCapitan", _instructionsForCapitan))
                    );
                }

                if (IsCommand(_jobTitle)) // ADT
                {
                    // Установка текста инструкций для команд
                    _instructionsForCommands = Loc.GetString($"{alertLevelKey}-instructions-for-commands");
                    StationAlertLevelInstructionsForCommands.SetMarkup(Loc.GetString(
                        "comp-pda-ui-for-commands",
                        ("instructionsForCommands", _instructionsForCommands))
                    );
                } 

                if (IsSecurity(_jobTitle)) // ADT
                {
                    // Установка текста инструкций для команд
                    _instructionsForSecurity = Loc.GetString($"{alertLevelKey}-instructions-for-security");
                    StationAlertLevelInstructionsForSecurity.SetMarkup(Loc.GetString(
                        "comp-pda-ui-for-security",
                        ("instructionsForSecurity", _instructionsForSecurity))
                    );
                } 

                if (IsAnother(_jobTitle)) // ADT
                {
                    // Установка текста инструкций для команд
                    _instructionsForAnother = Loc.GetString($"{alertLevelKey}-instructions-for-another");
                    StationAlertLevelInstructionsForAnother.SetMarkup(Loc.GetString(
                        "comp-pda-ui-for-another",
                        ("instructionsForAnother", _instructionsForAnother))
                    );
                } 

                // Единичные _jobTitle.Equals:

                if (_jobTitle.Equals("Главный врач", StringComparison.OrdinalIgnoreCase) && alertLevel == "violet") // ADT пример как можно сделать "исключение" (Вирус)
                {
                    _instructionsForCommands = Loc.GetString($"{alertLevelKey}-instructions-for-another-med");
                    StationAlertLevelInstructionsForCommands.SetMarkup(Loc.GetString(
                        "comp-pda-ui-for-commands",
                        ("instructionsForCommands", _instructionsForCommands))
                    );
                }


                if (_jobTitle.Equals("Клоун", StringComparison.OrdinalIgnoreCase) && alertLevel == "epsilon") // ADT входит в команду another. (Мем)
                {
                    _instructionsForAnother = Loc.GetString($"{alertLevelKey}-instructions-for-another-clown");
                    StationAlertLevelInstructionsForAnother.SetMarkup(Loc.GetString(
                        "comp-pda-ui-for-another",
                        ("instructionsForAnother", _instructionsForAnother))
                    );
                }
            }

            else // ADT END
            {
                _instructions = Loc.GetString($"{alertLevelKey}-instructions");
                StationAlertLevelInstructions.SetMarkup(Loc.GetString(
                    "comp-pda-ui-station-alert-level-instructions",
                    ("instructions", _instructions))
                );
            }


            AddressLabel.Text = state.Address?.ToUpper() ?? " - ";

            EjectIdButton.IsActive = state.PdaOwnerInfo.IdOwner != null || state.PdaOwnerInfo.JobTitle != null;
            EjectPenButton.IsActive = state.HasPen;
            EjectPaiButton.IsActive = state.HasPai;
            ActivateMusicButton.Visible = state.CanPlayMusic;
            ShowUplinkButton.Visible = state.HasUplink;
            LockUplinkButton.Visible = state.HasUplink;
        }

        public void UpdateAvailablePrograms(List<(EntityUid, CartridgeComponent)> programs)
        {
            ProgramList.RemoveAllChildren();

            if (programs.Count == 0)
            {
                ProgramList.AddChild(new Label()
                {
                    Text = Loc.GetString("comp-pda-io-no-programs-available"),
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    VerticalExpand = true
                });

                return;
            }

            var row = CreateProgramListRow();
            var itemCount = 1;
            ProgramList.AddChild(row);

            foreach (var (uid, component) in programs)
            {
                //Create a new row every second program item starting from the first
                if (itemCount % 2 != 0)
                {
                    row = CreateProgramListRow();
                    ProgramList.AddChild(row);
                }

                var item = new PdaProgramItem();

                if (component.Icon is not null)
                    item.Icon.SetFromSpriteSpecifier(component.Icon);

                item.OnPressed += _ => OnProgramItemPressed?.Invoke(uid);

                switch (component.InstallationStatus)
                {
                    case InstallationStatus.Cartridge:
                        item.InstallButton.Visible = true;
                        item.InstallButton.Text = Loc.GetString("cartridge-bound-user-interface-install-button");
                        item.InstallButton.OnPressed += _ => OnInstallButtonPressed?.Invoke(uid);
                        break;
                    case InstallationStatus.Installed:
                        item.InstallButton.Visible = true;
                        item.InstallButton.Text = Loc.GetString("cartridge-bound-user-interface-uninstall-button");
                        item.InstallButton.OnPressed += _ => OnUninstallButtonPressed?.Invoke(uid);
                        break;
                }

                item.ProgramName.Text = Loc.GetString(component.ProgramName);
                item.SetHeight = 20;
                row.AddChild(item);

                itemCount++;
            }

            //Add a filler item to the last row when it only contains one item
            if (itemCount % 2 == 0)
                row.AddChild(new Control() { HorizontalExpand = true });
        }

        /// <summary>
        /// Changes the current view to the home screen (view 0) and sets the tabs `IsCurrent` flag accordingly
        /// </summary>
        public void ToHomeScreen()
        {
            HomeButton.IsCurrent = true;
            ProgramListButton.IsCurrent = false;
            SettingsButton.IsCurrent = false;
            ProgramTitle.IsCurrent = false;

            ChangeView(HomeView);
        }

        /// <summary>
        /// Hides the program title and close button.
        /// </summary>
        public void HideProgramHeader()
        {
            ProgramTitle.IsCurrent = false;
            ProgramTitle.Visible = false;
            ProgramCloseButton.Visible = false;
            ProgramListButton.Visible = true;
            SettingsButton.Visible = true;
        }

        /// <summary>
        /// Changes the current view to the program content view (view 3), sets the program title and sets the tabs `IsCurrent` flag accordingly
        /// </summary>
        public void ToProgramView(string title)
        {
            HomeButton.IsCurrent = false;
            ProgramListButton.IsCurrent = false;
            SettingsButton.IsCurrent = false;
            ProgramTitle.IsCurrent = false;
            ProgramTitle.IsCurrent = true;
            ProgramTitle.Visible = true;
            ProgramCloseButton.Visible = true;
            ProgramListButton.Visible = false;
            SettingsButton.Visible = false;

            ProgramTitle.LabelText = title;
            ChangeView(ProgramContentView);
        }

        /// <summary>
        /// Changes the current view to the given view number
        /// </summary>
        public void ChangeView(int view)
        {
            if (ViewContainer.ChildCount <= view)
                return;

            ViewContainer.GetChild(_currentView).Visible = false;
            ViewContainer.GetChild(view).Visible = true;
            _currentView = view;
        }

        private static BoxContainer CreateProgramListRow()
        {
            return new BoxContainer()
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true
            };
        }

        private void HideAllViews()
        {
            var views = ViewContainer.Children;
            foreach (var view in views)
            {
                view.Visible = false;
            }
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            var stationTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);

            StationTimeLabel.SetMarkup(Loc.GetString("comp-pda-ui-station-time",
                ("time", stationTime.ToString("hh\\:mm\\:ss"))));
        }
    }
}
