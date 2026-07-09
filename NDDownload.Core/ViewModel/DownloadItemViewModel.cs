using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NDDownload.Download;
namespace NDDownload.ViewModel
{
    public class DownloadItemViewModel : INotifyPropertyChanged
    {
        private DownloadItem _item;

        public DownloadItemViewModel(DownloadItem item, ObservableCollection<MaxViewModel> _maxInstallSelect)
        {
            this._item = item;
            this._item.SetWillPath(_maxInstallSelect,false);//设置资源包的安装路径
        }

        public bool FileNameContains(string _name)
        {
            if (_item.child != null)
            {
                foreach (DownloadItem i in _item.child)
                {
                    if (i.zipname.Contains(_name))
                    { 
                        return true;
                    }
                }
            }
            else {
                return FileName.Contains(_name);
            }
            return false;
        }
        public string About
        {
            get
            {
                if (string.IsNullOrEmpty(_item.about))
                { return ""; }
                else
                {

                    return _item.about;
                }
            }
            set { 
                _item.about = value;
                this.OnPropertyChanged("About");

            }
        }
        public bool isUpdate
        {
            get {
                if (float.Parse(_item.version) > float.Parse(_item.oldVersion))
                {
                    return true;
                }
                else {
                    return false;
                }
            }
        }
        public string oldVersion
        {
            get { return _item.oldVersion; }
            set {
                _item.oldVersion = value;
                this.OnPropertyChanged("oldVersion");
            }
        }
        public string Version
        {
            get { return _item.version; }
            set {
                _item.version = value;
                this.OnPropertyChanged("Version");
            }
        }
        public bool quick
        {
            get { return _item.quick; }
            set {
                _item.quick = value;
                this.OnPropertyChanged("Version");
            }
        }
        public bool IsEnabled
        {
            get { return _item.IsEnabled;}
            set {
                _item.IsEnabled = value;
                this.OnPropertyChanged("IsEnabled");
            }
        }
        public string helplink
        {
            get {
                if (string.IsNullOrEmpty(_item.helplink))
                { return @"https://sundaybox.cc"; }
                else
                {
                    return _item.helplink;
                }
            }
            set {
                _item.helplink = value;
                this.OnPropertyChanged("helplink");

            }
        }
        /*public string helpUrl {
            get { return _item.helpUrl; }
            set { _item.helpUrl = value;
                this.OnPropertyChanged("helpUrl");
            }
        }*/
        public bool hasUrl {
            get { return _item.hasUrl; }
        }
        public string Name
        {
            get { return _item.zipname; }
            set {
                _item.zipname = value;
                this.OnPropertyChanged("Name");
            }
        }
        public string FileName
        {
            get { return _item.FileName; }
            set { 
                //_item.FileName = value;
                this.OnPropertyChanged("FileName");
            }
        }
        public DownloadItem parent
        {
            get { return _item.parent; }
            set {
                _item.parent = value;
                this.OnPropertyChanged("parent");

            }
        }
        public DownloadItem Item
        { 
            get { return _item; }
            set { _item = value;
                this.OnPropertyChanged("Item");
            }
        }
        public bool isDone {
            get { return _item.isDone; }
            set { _item.isDone = value;
                this.OnPropertyChanged("isDone");
            }
        }
        public bool isSelected
        {
            get {
                return _item.selected; }
            set
            {
                if (value != _item.selected)
                {
                    _item.selected = value;
                    this.OnPropertyChanged("isSelected");
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            else
                Console.WriteLine("DownloadItemViewModel PropertyChanged is null");
        }
    }
}
