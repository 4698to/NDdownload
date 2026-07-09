using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace NDDownload.ViewModel
{
    
    public class ScriptCollectPathsViewModel : INotifyPropertyChanged
    {

        private string path;
        private bool isSelect;
        private bool canedit;
        private bool exists;
        public ScriptCollectPathsViewModel()
        {
            isSelect = false;
        }
        public ScriptCollectPathsViewModel(string path)
        {
            this.path = path;
        }
        public ScriptCollectPathsViewModel(string path, bool select)
        {
            this.path = path;
            this.isSelect = select;
            this.canedit = true;
            
        }
        public bool GetFileExists()
        {
            if (System.IO.Path.HasExtension(this.Path))
            {
                return File.Exists(this.Path);
            }
            else { 
                return System.IO.Directory.Exists(this.Path);
            }
        }
        public bool Exists
        {
            get {
                return this.GetFileExists();
            }
            set {
                exists = value;
                this.OnPropertyChanged("Exists");
            }
        }
        public string Path
        {
            get { return path; }
            set { 
                path = value;   
                this.OnPropertyChanged("Path");
            } 
        }
        public bool Edit
        {
            get { return canedit; }
            set {
                canedit = value;
                this.OnPropertyChanged("Eidt");
            }
        }
        public bool Select
        {
            get { return isSelect; }
            set {
                isSelect = value;
                this.OnPropertyChanged("Select");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            else
                Console.WriteLine("InstallViewModel PropertyChanged is null");
        }
    }
}
