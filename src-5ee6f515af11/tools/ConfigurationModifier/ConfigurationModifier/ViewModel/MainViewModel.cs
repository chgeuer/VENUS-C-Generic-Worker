//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel.Composition;
using System.ComponentModel;
using ConfigurationModifier.Data;
using System.Windows.Input;
using ConfigurationModifier.Command;
using Microsoft.Win32;
using System.Diagnostics.CodeAnalysis;


namespace ConfigurationModifier.ViewModel
{
    [Export(typeof(MainViewModel))]
    class MainViewModel : IPartImportsSatisfiedNotification, INotifyPropertyChanged
    {

#pragma warning disable 0649
        [Import(typeof(XMLHandler))]
        private XMLHandler xmlHandler;

        [Import(typeof(WizardDialog))]
        private WizardDialog _dlg;

        [Import(typeof(WizardViewModel))]
        private WizardViewModel _wizardModel;
#pragma warning restore 0649

        private ICommand _moveNextCommand;
        private ICommand _saveAsCommand;
        private bool _isFileModified;

        public string SourceFileForWebBrowser
        {
            get
            {
                return xmlHandler.FileName;
            }
            set
            {
                xmlHandler.FileName = value;
                this.OnPropertyChanged("SourceFileForWebBrowser");
            }
        }

        private string _sourcefileForTextBox = "Browse for cscfg files";
        public string SourceFileForTextBox
        {
            get
            {
                return _sourcefileForTextBox;
            }
            set
            {
                _sourcefileForTextBox = value;
                this.OnPropertyChanged("SourceFileForTextBox");
            }
        }

        public string FilterStr
        {
            get
            {
                return "Configuration Files|*.cscfg";
            }
        }

        public bool MultiSelect
        {
            get
            {
                return false;
            }
        }

        public Func<string, CancellationTokenSource> BrowseFunction
        {
            get
            {
                return fileName =>
                {
                    this.SourceFileForTextBox = fileName;
                    this.SourceFileForWebBrowser = fileName;
                    _isFileModified = false;
                    return new System.Threading.CancellationTokenSource();
                };
            }
        }

        #region MoveNextCommand

        public ICommand MoveNextCommand
        {
            get
            {
                if (_moveNextCommand == null)
                    _moveNextCommand = new RelayCommand(
                        () => this.MoveToDialog(),
                        () => this.CanMoveToDialog);

                return _moveNextCommand;
            }
        }

        bool CanMoveToDialog
        {
            get { return !String.IsNullOrEmpty(SourceFileForWebBrowser); }
        }

        void MoveToDialog()
        {
            if (this.CanMoveToDialog)
            {
                _wizardModel.InitModel();
                _dlg.ShowDialog();
                if (_dlg.Result)
                {
                    string folderName = System.IO.Path.GetDirectoryName(System.IO.Path.GetTempPath());
                    string fileName = xmlHandler.WriteTempXML(folderName);
                    SourceFileForWebBrowser = fileName;
                    _isFileModified = true;
                }
            }
        }

        #endregion // MovePreviousCommand

        #region SaveAsCommand

        public ICommand SaveAsCommand
        {
            get
            {
                if (_saveAsCommand == null)
                    _saveAsCommand = new RelayCommand(
                        () => this.SaveAs(),
                        () => this.CanSaveAs);

                return _saveAsCommand;
            }
        }

        bool CanSaveAs
        {
            get { return _isFileModified; }
        }

        void SaveAs()
        {
            if (this.CanSaveAs)
            {
                SaveFileDialog dlg = new SaveFileDialog
                {
                    Filter = FilterStr
                };

                if (dlg.ShowDialog() == true)
                {
                    xmlHandler.WriteToXML(dlg.FileName);
                    SourceFileForTextBox = dlg.FileName;
                    SourceFileForWebBrowser = dlg.FileName;
                    _isFileModified = false;
                }
            }
        }

        #endregion

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion


        public void OnImportsSatisfied()
        {

        }
    }
}
