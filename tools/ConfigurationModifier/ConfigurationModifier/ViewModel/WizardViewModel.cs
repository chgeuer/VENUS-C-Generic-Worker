//Copyright (c) Microsoft
//This source is subject to the Microsoft Public License (Ms-PL).
//Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//All other rights reserved.





﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Input;
using System.ComponentModel.Composition;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Diagnostics;
using ConfigurationModifier.Command;
using ConfigurationModifier.Models;
using ConfigurationModifier.Data;

namespace ConfigurationModifier.ViewModel
{
    [Export(typeof(WizardViewModel))]
    class WizardViewModel : INotifyPropertyChanged, IPartImportsSatisfiedNotification
    {
        #region Fields

        ICommand moveNextCommand;
        ICommand cancelCommand;
        ICommand movePreviousCommand;
        WizardPageViewModelBase currentPage;
        ReadOnlyCollection<WizardPageViewModelBase> pages;

#pragma warning disable 0649
        [Import(typeof(XMLHandler))]
        XMLHandler xmlHandler;
#pragma warning restore 0649

        #endregion // Fields

        #region ImportSatisfied

        public void InitModel()
        {
            pages = null;
            xmlHandler.XMLVariables = null;
            this.CurrentPage = this.Pages[0];
            this.CurrentPagePercent = 0;
        }

        public void OnImportsSatisfied()
        {
            this.CurrentPage = this.Pages[0];
        }

        #endregion 

        #region Commands

        #region CancelCommand

        public ICommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                    cancelCommand = new RelayCommand(() => this.CancelOrder());

                return cancelCommand;
            }
        }

        void CancelOrder()
        {
            this.OnRequestClose(new DialogEventArgs(false));
        }

        #endregion

        #region MovePreviousCommand

        
        public ICommand MovePreviousCommand
        {
            get
            {
                if (movePreviousCommand == null)
                    movePreviousCommand = new RelayCommand(
                        () => this.MoveToPreviousPage(),
                        () => this.CanMoveToPreviousPage);

                return movePreviousCommand;
            }
        }

        bool CanMoveToPreviousPage
        {
            get { return 0 < this.CurrentPageIndex; }
        }

        void MoveToPreviousPage()
        {
            if (this.CanMoveToPreviousPage)
            {
                this.CurrentPage = this.Pages[this.CurrentPageIndex - 1];
                CurrentPagePercent = ((double)CurrentPageIndex / this.Pages.Count);
            }
        }

        #endregion 

        #region MoveNextCommand

        public ICommand MoveNextCommand
        {
            get
            {
                if (moveNextCommand == null)
                    moveNextCommand = new RelayCommand(
                        () => this.MoveToNextPage(),
                        () => this.CanMoveToNextPage);

                return moveNextCommand;
            }
        }

        bool CanMoveToNextPage
        {
            get { return this.CurrentPage != null && this.CurrentPage.IsValid(); }
        }

        void MoveToNextPage()
        {
            if (this.CanMoveToNextPage)
            {
                if (this.CurrentPageIndex < this.Pages.Count - 1)
                {
                    this.CurrentPage = this.Pages[this.CurrentPageIndex + 1];
                    CurrentPagePercent = ((double)CurrentPageIndex / this.Pages.Count);
                }
                else
                    this.OnRequestClose(new DialogEventArgs(true));
            }

        }

        #endregion 

        #endregion 

        #region Properties

     
        public WizardPageViewModelBase CurrentPage
        {
            get { return currentPage; }
            private set
            {
                if (value == currentPage)
                    return;

                if (currentPage != null)
                    currentPage.IsCurrentPage = false;

                currentPage = value;

                if (currentPage != null)
                    currentPage.IsCurrentPage = true;

                this.OnPropertyChanged("CurrentPage");
                this.OnPropertyChanged("IsOnLastPage");
            }
        }

     
        public bool IsOnLastPage
        {
            get { return this.CurrentPageIndex == this.Pages.Count - 1; }
        }

      
        public ReadOnlyCollection<WizardPageViewModelBase> Pages
        {
            get
            {
                if (pages == null)
                {
                    this.CreatePages();
                    this.OnPropertyChanged("Pages");
                }

                return pages;
            }
        }

        #endregion

        #region Events

        public event EventHandler RequestClose;

        #endregion

        #region Private Helpers

        void CreatePages()
        {
            var pagesList = new List<WizardPageViewModelBase>();

            PropertyInfo[] propertyInfos = typeof(Variables).GetProperties();
            foreach (var propertyInfo in propertyInfos)
            {
                WizardPageViewModelBase viewModelBase = null;
                if (propertyInfo.PropertyType == typeof(bool))
                {
                    viewModelBase = new BooleanPageViewModel(propertyInfo, xmlHandler.XMLVariables);
                }
                else if (propertyInfo.PropertyType == typeof(string) || propertyInfo.PropertyType == typeof(int))
                {
                    viewModelBase = new StringPageViewModel(propertyInfo, xmlHandler.XMLVariables);
                }

                if (viewModelBase != null)
                {
                    pagesList.Add(viewModelBase);
                }
            }

            pages = new ReadOnlyCollection<WizardPageViewModelBase>(pagesList);

        }

        private double currentPagePercent = 0;
        public double CurrentPagePercent
        {
            get
            {
                return currentPagePercent;
            }
            set
            {
                currentPagePercent = value;
                OnPropertyChanged("CurrentPagePercent");
            }
        }

        int CurrentPageIndex
        {
            get
            {

                if (this.CurrentPage == null)
                {
                    Debug.Fail("Why is the current page null?");
                    return -1;
                }
                return this.Pages.IndexOf(this.CurrentPage);
            }
        }

        void OnRequestClose(DialogEventArgs dlgEventArgs)
        {
            EventHandler handler = this.RequestClose;
            if (handler != null)
                handler(this, dlgEventArgs);
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


    }
}
