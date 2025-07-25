﻿using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using TwitchLeecher.Core.Models;
using TwitchLeecher.Gui.Interfaces;
using TwitchLeecher.Gui.Types;
using TwitchLeecher.Services.Interfaces;
using TwitchLeecher.Services.Services;
using TwitchLeecher.Shared.Commands;

namespace TwitchLeecher.Gui.ViewModels
{
    public partial class PreferencesViewModel : ViewModelBase, INavigationState
    {
        #region Fields

        private readonly IDialogService _dialogService;
        private readonly INotificationService _notificationService;
        private readonly IPreferencesService _preferencesService;
        private readonly IThemeService _themeService;

        private Preferences _currentPreferences;

        private ICommand _addFavouriteChannelCommand;
        private ICommand _removeFavouriteChannelCommand;
        private ICommand _chooseDownloadTempFolderCommand;
        private ICommand _chooseDownloadFolderCommand;
        private ICommand _chooseExternalPlayerCommand;
        private ICommand _clearExternalPlayerCommand;
        private ICommand _saveCommand;
        private ICommand _undoCommand;
        private ICommand _defaultsCommand;

        private readonly object _commandLockObject;
        private bool _isChannelDropDownOpen;

        #endregion Fields

        #region Constructors

        public PreferencesViewModel(
            IDialogService dialogService,
            INotificationService notificationService,
            IPreferencesService preferencesService,
            IThemeService themeService)
        {
            _dialogService = dialogService;
            _notificationService = notificationService;
            _preferencesService = preferencesService;
            _themeService = themeService;

            _commandLockObject = new object();
        }

        #endregion Constructors

        #region Properties
        
        public double ScrollPosition { get; set; }

        public Preferences CurrentPreferences
        {
            get
            {
                if (_currentPreferences == null)
                {
                    _currentPreferences = _preferencesService.CurrentPreferences.Clone();
                }

                return _currentPreferences;
            }

            private set { SetProperty(ref _currentPreferences, value); }
        }

        public IEnumerable<string> AvailableThemes => _preferencesService.AvailableThemes;

        public ICommand AddFavouriteChannelCommand
        {
            get
            {
                if (_addFavouriteChannelCommand == null)
                {
                    _addFavouriteChannelCommand = new DelegateCommand(AddFavouriteChannel);
                }

                return _addFavouriteChannelCommand;
            }
        }

        public ICommand RemoveFavouriteChannelCommand
        {
            get
            {
                if (_removeFavouriteChannelCommand == null)
                {
                    _removeFavouriteChannelCommand = new DelegateCommand(RemoveFavouriteChannel);
                }

                return _removeFavouriteChannelCommand;
            }
        }

        public ICommand ChooseDownloadTempFolderCommand
        {
            get
            {
                if (_chooseDownloadTempFolderCommand == null)
                {
                    _chooseDownloadTempFolderCommand = new DelegateCommand(ChooseDownloadTempFolder);
                }

                return _chooseDownloadTempFolderCommand;
            }
        }

        public ICommand ChooseDownloadFolderCommand
        {
            get
            {
                if (_chooseDownloadFolderCommand == null)
                {
                    _chooseDownloadFolderCommand = new DelegateCommand(ChooseDownloadFolder);
                }

                return _chooseDownloadFolderCommand;
            }
        }

        public ICommand ChooseExternalPlayerCommand
        {
            get
            {
                if (_chooseExternalPlayerCommand == null)
                {
                    _chooseExternalPlayerCommand = new DelegateCommand(ChooseExternalPlayer);
                }

                return _chooseExternalPlayerCommand;
            }
        }

        public ICommand ClearExternalPlayerCommand
        {
            get
            {
                if (_clearExternalPlayerCommand == null)
                {
                    _clearExternalPlayerCommand = new DelegateCommand(ClearExternalPlayer);
                }

                return _clearExternalPlayerCommand;
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new DelegateCommand(Save);
                }

                return _saveCommand;
            }
        }

        public ICommand UndoCommand
        {
            get
            {
                if (_undoCommand == null)
                {
                    _undoCommand = ReactiveCommand.CreateFromTask(async () => Undo());
                }

                return _undoCommand;
            }
        }

        public ICommand DefaultsCommand
        {
            get
            {
                if (_defaultsCommand == null)
                {
                    _defaultsCommand = ReactiveCommand.CreateFromTask(async () => Defaults());
                }

                return _defaultsCommand;
            }
        }

        #endregion Properties

        #region Methods

        private void AddFavouriteChannel()
        {
            try
            {
                lock (_commandLockObject)
                {
                    string currentChannel = CurrentPreferences.SearchChannelName;

                    if (!string.IsNullOrWhiteSpace(currentChannel))
                    {
                        string existingEntry = CurrentPreferences.SearchFavouriteChannels.FirstOrDefault(channel =>
                            channel.Equals(currentChannel, StringComparison.OrdinalIgnoreCase));

                        if (!string.IsNullOrWhiteSpace(existingEntry))
                        {
                            CurrentPreferences.SearchChannelName = existingEntry;
                        }
                        else
                        {
                            CurrentPreferences.SearchFavouriteChannels.Add(currentChannel);
                        }

                        _notificationService.ShowNotification($"'{currentChannel}' added!");
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void RemoveFavouriteChannel()
        {
            try
            {
                lock (_commandLockObject)
                {
                    string currentChannel = CurrentPreferences.SearchChannelName;

                    if (!string.IsNullOrWhiteSpace(currentChannel))
                    {
                        string existingEntry = CurrentPreferences.SearchFavouriteChannels.FirstOrDefault(channel =>
                            channel.Equals(currentChannel, StringComparison.OrdinalIgnoreCase));

                        if (!string.IsNullOrWhiteSpace(existingEntry))
                        {
                            CurrentPreferences.SearchFavouriteChannels.Remove(existingEntry);
                            CurrentPreferences.SearchChannelName =
                                CurrentPreferences.SearchFavouriteChannels.FirstOrDefault();
                            _notificationService.ShowNotification($"'{currentChannel}' removed!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void ChooseDownloadTempFolder()
        {
            try
            {
                lock (_commandLockObject)
                {
                    _dialogService.ShowFolderBrowserDialog(CurrentPreferences.DownloadTempFolder,
                        ChooseDownloadTempFolderCallback);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void ChooseDownloadTempFolderCallback(bool cancelled, string folder)
        {
            try
            {
                if (!cancelled)
                {
                    CurrentPreferences.DownloadTempFolder = folder;
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void ChooseDownloadFolder()
        {
            try
            {
                lock (_commandLockObject)
                {
                    _dialogService.ShowFolderBrowserDialog(CurrentPreferences.DownloadFolder,
                        ChooseDownloadFolderCallback);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void ChooseDownloadFolderCallback(bool cancelled, string folder)
        {
            try
            {
                if (!cancelled)
                {
                    CurrentPreferences.DownloadFolder = folder;
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void ChooseExternalPlayer()
        {
            try
            {
                lock (_commandLockObject)
                {
                    var filter = new CommonFileDialogFilter("exe", "Executables");
                    _dialogService.ShowFileBrowserDialog(filter, CurrentPreferences.MiscExternalPlayer,
                        ChooseExternalPlayerCallback);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void ClearExternalPlayer()
        {
            try
            {
                lock (_commandLockObject)
                {
                    CurrentPreferences.MiscExternalPlayer = null;
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void ChooseExternalPlayerCallback(bool cancelled, string file)
        {
            try
            {
                if (!cancelled)
                {
                    CurrentPreferences.MiscExternalPlayer = file;
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private void Save()
        {
            try
            {
                lock (_commandLockObject)
                {
                    _dialogService.SetBusy();
                    Validate();

                    if (!HasErrors)
                    {
                        _preferencesService.Save(_currentPreferences);
                        _themeService.SetTheme(_currentPreferences.Theme);
                        CurrentPreferences = null;
                        _notificationService.ShowNotification("Preferences saved");
                    }
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private async Task Undo()
        {
            try
            {
                MessageBoxResult result = await _dialogService.ShowMessageBox(
                    "Undo current changes and reload last saved preferences?", "Undo", MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _dialogService.SetBusy();
                    CurrentPreferences = null;
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        private async Task Defaults()
        {
            try
            {
                MessageBoxResult result = await _dialogService.ShowMessageBox("Load default preferences?", "Defaults",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _dialogService.SetBusy();
                    _preferencesService.Save(_preferencesService.CreateDefault());
                    _themeService.SetTheme(_preferencesService.CurrentPreferences.Theme);
                    CurrentPreferences = null;
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        public override void Validate(string propertyName = null)
        {
            base.Validate(propertyName);

            string currentProperty = nameof(CurrentPreferences);

            if (string.IsNullOrWhiteSpace(propertyName) || propertyName == currentProperty)
            {
                CurrentPreferences?.Validate();

                if (CurrentPreferences.HasErrors)
                {
                    AddError(currentProperty, "Invalid Preferences!");
                }
            }
        }

        public override void OnBeforeHidden()
        {
            try
            {
                CurrentPreferences = null;
            }
            catch (Exception ex)
            {
                _dialogService.ShowAndLogException(ex);
            }
        }

        public bool IsChannelDropDownOpen
        {
            get => _isChannelDropDownOpen;
            set => SetProperty(ref _isChannelDropDownOpen, value);
        }

        protected override List<MenuCommand> BuildMenu()
        {
            List<MenuCommand> menuCommands = base.BuildMenu();

            if (menuCommands == null)
            {
                menuCommands = new List<MenuCommand>();
            }

            menuCommands.Add(new MenuCommand(SaveCommand, "Save", "fa-solid fa-floppy-disk"));
            menuCommands.Add(new MenuCommand(UndoCommand, "Undo", "fa-solid fa-rotate-left"));
            menuCommands.Add(new MenuCommand(DefaultsCommand, "Default", "fa-solid fa-wrench"));

            return menuCommands;
        }

        [RelayCommand]
        private void OpenDropdown()
        {
            CurrentPreferences.SearchChannelName = string.Empty;
            IsChannelDropDownOpen = true;
        }

        #endregion Methods
    }
}