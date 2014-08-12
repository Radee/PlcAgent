﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using _PlcAgent.Visual.Interfaces;

namespace _PlcAgent.Visual.Gui
{
    /// <summary>
    /// Interaction logic for GuiAnalyzerMainFrame.xaml
    /// </summary>
    public partial class GuiAnalyzerMainFrame : IRefreshableGui, IResizableGui
    {
        #region Variables

        private readonly List<GuiComponent> _channelList;

        #endregion


        #region Properties

        public GuiAnalyzerDataCursor AnalyzerDataCursorRed;
        public GuiAnalyzerDataCursor AnalyzerDataCursorBlue;

        #endregion


        #region Constructors

        public GuiAnalyzerMainFrame(Analyzer.Analyzer analyzer) : base(analyzer)
        {
            InitializeComponent();

            Analyzer.AnalyzerChannels.OnChannelListModified += RefreshGui;
            Analyzer.ObservableTime.OnPointCreated += OnPointCreated;

            _channelList = new List<GuiComponent>();

            AnalyzerDataCursorRed = new GuiAnalyzerDataCursor(Analyzer)
            {
                ParentGrid = PlotGrid,
                Brush = Brushes.Red,
                ActualPosition = 246.0
            };
            AnalyzerDataCursorBlue = new GuiAnalyzerDataCursor(Analyzer)
            {
                ParentGrid = PlotGrid,
                Brush = Brushes.Blue,
                ActualPosition = 206.0
            };

            GeneralGrid.Children.Add(AnalyzerDataCursorRed);
            GeneralGrid.Children.Add(AnalyzerDataCursorBlue);

            if (!Analyzer.AnalyzerSetupFile.ShowDataCursors[Analyzer.Header.Id])
            {
                AnalyzerDataCursorRed.Visibility = Visibility.Hidden;
                AnalyzerDataCursorBlue.Visibility = Visibility.Hidden;
            }

            PlotArea.DataContext = Analyzer.ObservableTime.MainViewModel;

            RefreshGui();
        }

        #endregion


        #region Methods

        public void UpdateSizes(double height, double width)
        {
            Height = height;
            Width = width;

            MainScrollViewer.Height = height - 30;
            MainScrollViewer.Width = width;

            PlotGrid.Width = width - 30;

            TimePlotGrid.Width = width - 225;

            foreach (var analyzerSingleFigure in PlotGrid.Children.Cast<GuiAnalyzerSingleFigure>())
            {
                analyzerSingleFigure.UpdateSizes(height, width);
            }

            AnalyzerDataCursorRed.UpdateSizes(height, width);
            AnalyzerDataCursorBlue.UpdateSizes(height, width);
        }

        private void DrawChannel(uint id)
        {
            GuiComponent analyzerSingleFigure = null;

            foreach (var guiComponent in _channelList.Where(guiComponent => guiComponent.Header.Id == id)) { analyzerSingleFigure = guiComponent;}

            if (analyzerSingleFigure == null)
                _channelList.Add(
                    analyzerSingleFigure = new GuiComponent(id, "", new GuiAnalyzerSingleFigure(id, Analyzer)));

            analyzerSingleFigure.Initialize(0, ((int)id - 1) * 130, PlotGrid);

            var userControl = (GuiAnalyzerSingleFigure)analyzerSingleFigure.UserControl;
            userControl.UpdateSizes(Height, Width);
        }

        public void RefreshGui()
        {
            PlotGrid.Children.Clear();

            var componentToBeRemoved =
                _channelList.Where(guiComponent => Analyzer.AnalyzerChannels.GetChannel(guiComponent.Header.Id) == null)
                    .ToList();

            foreach (var guiComponent in componentToBeRemoved) { _channelList.Remove(guiComponent);}
            foreach (var analyzerChannel in Analyzer.AnalyzerChannels.Children) { DrawChannel(analyzerChannel.Id);}
        }

        #endregion


        #region Event Handlers

        private void OnPointCreated()
        {
            PlotArea.Dispatcher.BeginInvoke((new Action(
                () => PlotArea.DataContext = Analyzer.ObservableTime.MainViewModel)));
        }

        protected override void OnRecordingChanged()
        {
            AnalyzerStartStopButton.Dispatcher.BeginInvoke((new Action(delegate
            {
                AnalyzerStartStopButton.Content = "Start";
                if (Analyzer != null && Analyzer.Recording) AnalyzerStartStopButton.Content = "Stop";
            })));
        }

        protected override void OnRecordingTimeChanged()
        {
            AnalyzerTimeLabel.Dispatcher.BeginInvoke((new Action(delegate
            {
                AnalyzerTimeLabel.Content = "Recording time: \n" + TimeSpan.FromMilliseconds(Analyzer.RecordingTime);
            })));
        }

        private void StartStopRecording(object sender, RoutedEventArgs e)
        {
            Analyzer.StartStopRecording();
        }

        #endregion

    }
}
