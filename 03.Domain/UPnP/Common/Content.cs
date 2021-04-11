using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace UPnP.Common
{
    public class Content
    {
        public Content(DIDLLite didllite, string go_up_id, string presentation_url)
        {
            this.Elements = new ObservableCollection<Element>();
            this.GoUpId = go_up_id;
            foreach (Container container in didllite.Containers)
            {
                Elements.Add(new Element(container));
                this.FoldersCount++;
            }
            foreach (Item item in didllite.Items)
            {
                Elements.Add(new Element(item, presentation_url));
                this.FilesCount++;
            }
        }

        public ObservableCollection<Element> Elements { get; private set; }
        public int FoldersCount { get; private set; }
        public int FilesCount { get; private set; }
        public string GoUpId { get; private set; }
    }

    public class Element
    {
        public Element()
        {
        }

        public Element(Container container)
        {
            this.Id = container.Id;
            this.Title = container.Title;
            this.IsFolder = true;
            this.ParentId = container.ParentID;
            this.IconUrl = "Resources/folder.png";
            this.Icon = new Image();
            if (!string.IsNullOrEmpty(this.IconUrl))
            {
                BitmapImage img = new BitmapImage();
                img.UriSource = new Uri("ms-appx:///Resources/folder.png");
                this.Icon.Source = img;
            }
        }

        public Element(Item item, string presentation_url)
        {
            this.Id = item.Id;
            this.Title = item.Title;
            this.IsFolder = false;
            this.ParentId = item.ParentID;
            this.Class = item.Class;
            foreach (ResData res in item.Res)
            {
                if (res.Duration != null && this.Duration == null)
                {
                    this.Duration = res.Duration;
                    this.FileUrl = res.Value;
                }
                if (res.Resolution != null && this.Resolution == null)
                    this.Resolution = res.Resolution;
                if (res.Size != null && this.Size == null)
                    this.Size = res.Size;
                if (res.Duration == null && this.IconUrl == null)
                    this.IconUrl = res.Value;
            }
            if (!string.IsNullOrEmpty(this.FileUrl))
                if (this.FileUrl.IndexOf(presentation_url) == -1)
                {
                    int pos = this.FileUrl.IndexOf("/disk/");
                    if (pos != -1)
                    {
                        this.FileUrl = presentation_url + this.FileUrl.Substring(pos + 1);
                    }
                }
            if (!string.IsNullOrEmpty(this.IconUrl))
                if (this.IconUrl.IndexOf(presentation_url) == -1)
                {
                    int pos = this.IconUrl.IndexOf("/disk/");
                    if (pos != -1)
                    {
                        this.IconUrl = presentation_url + this.IconUrl.Substring(pos + 1);
                    }
                }
            this.Icon = new Image();
            if (!string.IsNullOrEmpty(this.IconUrl))
                this.Icon.Source = new BitmapImage(new Uri(this.IconUrl));
        }

        public string Id { get; set; }
        public string ParentId { get; set; }
        public string Title { get; set; }
        public bool IsFolder { get; private set; }
        public string Class { get; set; }
        public string IconUrl { get; set; }
        public Image Icon { get; set; }
        public string Duration { get; private set; }
        public string FileUrl { get; set; }
        public string Resolution { get; set; }
        public string Size { get; private set; }
    }
}
