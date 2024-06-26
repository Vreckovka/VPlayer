﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Events;
using VCore;
using VCore.Standard.Factories.ViewModels;
using VCore.WPF;
using VCore.WPF.Interfaces.Managers;
using VCore.WPF.Managers;
using VCore.WPF.Misc;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPLayer.Domain.Contracts.IPTV;
using VPlayer.IPTV.ViewModels.Prompts;
using VPlayer.IPTV.Views.Prompts;

namespace VPlayer.IPTV.ViewModels
{
  public class M3UTvSourceViewModel : TVSourceViewModel
  {
    public M3UTvSourceViewModel(
      TvSource tVSource,
      IStorageManager storageManager,
      IViewModelsFactory viewModelsFactory,
      IEventAggregator eventAggregator,
      IWindowManager windowManager) : base(tVSource, storageManager, viewModelsFactory, eventAggregator, windowManager)
    {
      if (tVSource.TvSourceType != TVSourceType.M3U)
      {
        throw new ArgumentException("Wrong source type");
      }
    }

    #region FilePath

    private string filePath;

    public string FilePath
    {
      get { return filePath; }
      set
      {
        if (value != filePath)
        {
          filePath = value;

          ValidateIsValid();


          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Commands

    #region AddNewSource

    private ActionCommand pickFile;

    public ICommand PickFile
    {
      get
      {
        if (pickFile == null)
        {
          pickFile = new ActionCommand(OnPickFile);
        }

        return pickFile;
      }
    }

    public void OnPickFile()
    {
      CommonOpenFileDialog dialog = new CommonOpenFileDialog();

      dialog.AllowNonFileSystemItems = true;
      dialog.IsFolderPicker = false;
      dialog.Title = "Select M3U file";
      dialog.Filters.Add(new CommonFileDialogFilter("m3u", "m3u"));

      if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
      {
        FilePath = dialog.FileName;
      }
    }

    #endregion

    #endregion

    #region PrepareEntityForDb

    public override Task PrepareEntityForDb()
    {
      return Task.Run(async () =>
      {
        var fileData = File.ReadAllText(FilePath);

        Model.TvChannels = new List<TvChannel>();

        foreach (Match m in Regex.Matches(fileData, @"#EXTINF:-1,(.+)\r?\n(https?\S+)"))
        {
          var channelName = m.Groups[1].Value.Replace("\r", null);
          var url = m.Groups[2].Value;

          var tvItem = new TvItem()
          {
            Source = url,
            Name = channelName
          };

          Model.TvChannels.Add(new TvChannel()
          {
            TvSource = Model,
            TvItem = tvItem
          });
        }

         await VSynchronizationContext.InvokeOnDispatcherAsync(() =>
         {
           foreach (var channel in Model.TvChannels)
           {
             TvChannels.Add(viewModelsFactory.Create<TvChannelViewModel>(channel));
           }
         });

        Model.SourceConnection = null;
      });

    }

    #endregion

    #region ValidateIsValid

    public override void ValidateIsValid()
    {
      if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(Name))
      {
        IsValid = true;
      }
      else
      {
        IsValid = false;
      }
    }

    #endregion

  }
}