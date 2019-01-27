using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Security;

namespace LoginTests
{
    public class MainWindowViewModel
    {
        private string _DbConnection;
        private RelayCommand _LoginCommand;
        private RelayCommand _RegisterCommand;
        private IPassword _PasswordContainer;
        private Server _Server = new Server();

        public MainWindowViewModel(IPassword passwordContainer)
        {
            _PasswordContainer = passwordContainer;
        }

        public string User { get; set; }
        
        public RelayCommand LoginCommand
        {
            get
            {
                if (_LoginCommand == null)
                    _LoginCommand = new RelayCommand(x => Login(User, _PasswordContainer.Password));
                return _LoginCommand;
            }
        }

        public RelayCommand RegisterCommand
        {
            get
            {
                if (_RegisterCommand == null)
                {
                    _RegisterCommand = new RelayCommand(x => Register(User, _PasswordContainer.Password));
                }
                return _RegisterCommand;
            }
        }

        public void Login(string username, string password)
        {
            var result = _Server.TryLogin(username, password, out string message);
            var img = result ? MessageBoxImage.Information : MessageBoxImage.Error;
            MessageBox.Show(message, "", MessageBoxButton.OK, img);
        }

        private void Register(string username, string password)
        {
            var result = _Server.TryRegister(username, password, out string message);
            var img = result ? MessageBoxImage.Information : MessageBoxImage.Error;
            MessageBox.Show(message, "", MessageBoxButton.OK, img);
        }
    }
}
