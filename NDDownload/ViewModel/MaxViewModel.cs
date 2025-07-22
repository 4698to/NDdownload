using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;

namespace NDDownload.ViewModel
{
    public class MaxViewModel : INotifyPropertyChanged
    {
        private string _name;
        private string _path;
        private string _version;
        private bool _selected;
        public MaxViewModel(string p)
        {
            _path = p;
            _name = System.IO.Path.GetDirectoryName(_path);
            _name = System.IO.Path.GetFileName(_name);
            string[] v = _name.Split(' ');
            _version = v[v.Length - 1];
            //_selected = true;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; 
                this.OnPropertyChanged("Name");
            }
        }
        public string Path
        {
            get { return _path; }
            set
            {
                _path = value;
                this.OnPropertyChanged("Path");
            }
        }

        public bool isSelected
        {
            get { return _selected; }
            set
            {
                if (value != _selected)
                {
                    _selected = value;
                    this.OnPropertyChanged("isSelected");
                }
            }
        }
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            else
                Console.WriteLine($"{propertyName} MaxViewModel PropertyChanged null ");
        }
        #endregion // INotifyPropertyChanged Members
    }
}
