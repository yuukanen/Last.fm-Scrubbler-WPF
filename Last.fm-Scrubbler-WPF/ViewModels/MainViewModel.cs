﻿using Caliburn.Micro;
using IF.Lastfm.Core.Api;
using Scrubbler.Interfaces;
using Scrubbler.Properties;
using Scrubbler.ViewModels.ExtraFunctions;
using Scrubbler.ViewModels.ScrobbleViewModels;
using Scrubbler.Views;
using System;
using System.IO;
using System.Reflection;

namespace Scrubbler.ViewModels
{
  /// <summary>
  /// ViewModel for the <see cref="MainView"/>.
  /// </summary>
  public class MainViewModel : Conductor<Screen>.Collection.OneActive
  {
    #region Properties

    /// <summary>
    /// String containing application name and version.
    /// </summary>
    public string TitleString
    {
      get { return _titleString; }
      private set
      {
        _titleString = value;
        NotifyOfPropertyChange();
      }
    }
    private string _titleString;

    /// <summary>
    /// ViewModel for the <see cref="UserView"/>.
    /// </summary>
    public UserViewModel UserViewModel
    {
      get { return _userViewModel; }
      private set
      {
        _userViewModel = value;
        NotifyOfPropertyChange();
      }
    }
    private UserViewModel _userViewModel;

    /// <summary>
    /// The minimum DateTime for all DateTimePickers.
    /// </summary>
    public static DateTime MinimumDateTime = DateTime.Now.Subtract(TimeSpan.FromDays(14));

    #region StatusBar Properties

    /// <summary>
    /// Gets the username displayed in the status bar.
    /// </summary>
    public string StatusBarUsername
    {
      get { return _client.Auth.Authenticated ? _client.Auth.UserSession.Username : "Not logged in."; }
    }

    /// <summary>
    /// Gets the current status displayed in the status bar.
    /// </summary>
    public string CurrentStatus
    {
      get { return _currentStatus; }
      private set
      {
        _currentStatus = value;
        NotifyOfPropertyChange(() => CurrentStatus);
      }
    }
    private string _currentStatus;

    #endregion StatusBar Properties

    #endregion Properties

    #region Private Member

    /// <summary>
    /// The client used for all last.fm actions.
    /// </summary>
    private ILastFMClient _client;

    /// <summary>
    /// The api key of this application.
    /// </summary>
    private const string APIKEY = "9d226e3ff2fcd55a42c553fef62a7553";

    /// <summary>
    /// The api secret of this application.
    /// </summary>
    private const string APISECRET = "c779028b480f6c63449c380aeebbbd63";

    /// <summary>
    /// ViewModel managing all scrobble ViewModels.
    /// </summary>
    private ScrobblerViewModel _scrobblerVM;

    /// <summary>
    /// ViewModel managing all extra function ViewModels.
    /// </summary>
    private ExtraFunctionsViewModel _extraFunctionsVM;

    /// <summary>
    /// Window manager used to display dialogs etc.
    /// </summary>
    private IExtendedWindowManager _windowManager;

    /// <summary>
    /// Factory used for creating clients.
    /// </summary>
    private static ILastFMClientFactory _lastFMClientFactory;

    /// <summary>
    /// Factory used for creating scrobblers.
    /// </summary>
    private IScrobblerFactory _scrobblerFactory;

    /// <summary>
    /// FileOperator for interfacing with the hard disk.
    /// </summary>
    private IFileOperator _fileOperator;

    #endregion Private Member

    /// <summary>
    /// Constructor.
    /// </summary>
    public MainViewModel(IExtendedWindowManager windowManager, ILastFMClientFactory clientFactory, IScrobblerFactory scrobblerFactory, ILocalFileFactory localFileFactory,
                         IFileOperator fileOperator)
    {
      _windowManager = windowManager;
      _lastFMClientFactory = clientFactory;
      _scrobblerFactory = scrobblerFactory;
      _fileOperator = fileOperator;
      TitleString = "Last.fm Scrubbler WPF " + Assembly.GetExecutingAssembly().GetName().Version;
      _client = _lastFMClientFactory.CreateClient(APIKEY, APISECRET);
      SetupViewModels(localFileFactory);
      CurrentStatus = "Waiting to scrobble...";
    }

    #region Setup

    /// <summary>
    /// Creates the ViewModels.
    /// </summary>
    private void SetupViewModels(ILocalFileFactory localFileFactory)
    {
      _scrobblerVM = new ScrobblerViewModel(_windowManager, localFileFactory, _fileOperator, _client);
      _scrobblerVM.StatusUpdated += StatusUpdated;
      CreateScrobblers();
      ActivateItem(_scrobblerVM);

      _extraFunctionsVM = new ExtraFunctionsViewModel(_windowManager, _client.User, _fileOperator);
      _extraFunctionsVM.StatusUpdated += StatusUpdated;
      ActivateItem(_extraFunctionsVM);

      // should be active
      ActivateItem(_scrobblerVM);

      UserViewModel = new UserViewModel(_windowManager, _client.Auth, _fileOperator);
      UserViewModel.ActiveUserChanged += UserViewModel_ActiveUserChanged;
      UserViewModel.LoadLastUser();
    }

    #endregion Setup

    /// <summary>
    /// Triggers when the <see cref="UserViewModel.ActiveUser"/> changes.
    /// </summary>
    /// <param name="sender">Ignored.</param>
    /// <param name="e">Ignored.</param>
    private void UserViewModel_ActiveUserChanged(object sender, EventArgs e)
    {
      NotifyOfPropertyChange(() => StatusBarUsername);
      CreateScrobblers();
    }

    /// <summary>
    /// Recreates the scrobblers of the ScrobbleViewModels.
    /// </summary>
    private void CreateScrobblers()
    {
      IAuthScrobbler scrobbler;
      IAuthScrobbler cachingScrobbler;
      if (_client.Auth.UserSession != null)
      {
        string dbFile = _client.Auth.UserSession.Username + ".db";

        try
        {
          if (!File.Exists(dbFile))
            File.Create(dbFile);
        }
        catch (Exception ex)
        {
          CurrentStatus = "Error creating cache database. Error: " + ex.Message;
        }

        scrobbler = _scrobblerFactory.CreateScrobbler(_client.Auth);
        cachingScrobbler = _scrobblerFactory.CreateSQLiteScrobbler(_client.Auth, dbFile);
      }
      else
      {
        scrobbler = null;
        cachingScrobbler = null;
      }

      _scrobblerVM.UpdateScrobblers(scrobbler, cachingScrobbler);
    }

    /// <summary>
    /// Updates the status.
    /// </summary>
    /// <param name="sender">Ignored.</param>
    /// <param name="e">Contains the new status.</param>
    private void StatusUpdated(object sender, UpdateStatusEventArgs e)
    {
      CurrentStatus = e.NewStatus;
    }

    /// <summary>
    /// Shows the <see cref="UserView"/>.
    /// </summary>
    public void HyperlinkClicked()
    {
      _windowManager.ShowDialog(UserViewModel);
      NotifyOfPropertyChange(() => StatusBarUsername);
    }

    /// <summary>
    /// Dispose iTunes com object before exit.
    /// </summary>
    public void MainView_Closing()
    {
      //ITunesScrobbleVM.Dispose();
      Settings.Default.Save();
    }
  }
}