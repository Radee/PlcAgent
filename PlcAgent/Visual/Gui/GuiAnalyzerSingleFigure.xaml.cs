﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using _PlcAgent.Analyzer;
using _PlcAgent.DataAquisition;
using _PlcAgent.Visual.Interfaces;

namespace _PlcAgent.Visual.Gui
{
    /// <summary>
    /// Interaction logic for GuiAnalyzerSingleFigure.xaml
    /// </summary>
    public partial class GuiAnalyzerSingleFigure : IResizableGui
    {
        #region Variables

        private readonly Boolean _save;
        private readonly AnalyzerChannel _analyzerChannel;

        #endregion


        #region Properties

        public uint Id;

        public AnalyzerChannel AnalyzerChannel { get { return _analyzerChannel;}}

        #endregion


        #region Constructors

        public GuiAnalyzerSingleFigure(uint id, Analyzer.Analyzer analyzer) : base(analyzer)
        {
            InitializeComponent();

            Id = id;

            _analyzerChannel = Analyzer.AnalyzerChannels.GetChannel(Id);

            var colorsList = new List<Brush>
            {
                Brushes.Red,
                Brushes.DarkRed,
                Brushes.Green,
                Brushes.DarkGreen,
                Brushes.Blue,
                Brushes.DarkBlue,
                Brushes.Yellow,
                Brushes.Orange,
                Brushes.DarkOrange,
                Brushes.Black
            };

            BrushComboBox.ItemsSource = colorsList;
            BrushComboBox.SelectedItem = Brushes.Green;
            BrushComboBox.DataContext = this;

            ChannelGroupBox.Header = "Channel " + _analyzerChannel.Id;

            VariableComboBox.ItemsSource = Analyzer.CommunicationInterfaceHandler.ReadInterfaceComposite.Children;

            if (_analyzerChannel.AnalyzerObservableVariable != null)
            {
                VariableComboBox.SelectedItem =
                    _analyzerChannel.AnalyzerObservableVariable.CommunicationInterfaceVariable;
                foreach (
                    var brush in
                        colorsList.Where(
                            brush =>
                                Equals(brush.ToString(), _analyzerChannel.AnalyzerObservableVariable.Brush.ToString())))
                {
                    BrushComboBox.SelectedItem = brush;
                }
                UnitTextBox.Text = _analyzerChannel.AnalyzerObservableVariable.Unit;

                _analyzerChannel.AnalyzerObservableVariable.OnPointCreated += OnPointCreated;
                _analyzerChannel.AnalyzerObservableVariable.PropertyChanged += UpdateControls;

                UpdateControls(this, new PropertyChangedEventArgs(""));
            }

            _save = true;

            PlotArea.DataContext = new MainViewModel();
        }

        #endregion


        #region Mathods

        public void UpdateSizes(double height, double width)
        {
            Width = width;
            PlotCanvas.Width = width;
            GeneralGrid.Width = width;
            PlotGrid.Width = width - 225;
        }

        #endregion


        #region Event Handlers

        private void OnPointCreated()
        {
            PlotArea.Dispatcher.BeginInvoke((new Action(
                () => PlotArea.DataContext = _analyzerChannel.AnalyzerObservableVariable.MainViewModel)));
        }

        private void RemoveChannel(object sender, RoutedEventArgs e)
        {
            Analyzer.RemoveChannel(_analyzerChannel);
        }

        protected override void OnRecordingChanged()
        {
            VariableComboBox.Dispatcher.BeginInvoke((new Action(delegate
            {
                VariableComboBox.IsEnabled = !Analyzer.Recording;
            })));
            DeleteButton.Dispatcher.BeginInvoke((new Action(delegate
            {
                DeleteButton.IsEnabled = !Analyzer.Recording;
            })));
        }

        protected override void OnRecordingTimeChanged()
        {}

        private void UpdateControls(object sender, PropertyChangedEventArgs e)
        {
            if (_analyzerChannel.AnalyzerObservableVariable == null) return;

            if (TypeLabel == null) return;
            TypeLabel.Dispatcher.BeginInvoke((new Action(delegate
            {
                TypeLabel.Content = _analyzerChannel.AnalyzerObservableVariable.Type;
            })));
            if (VariableLabel == null) return;
            VariableLabel.Dispatcher.BeginInvoke((new Action(delegate
            {
                VariableLabel.Content = _analyzerChannel.AnalyzerObservableVariable.Name
                                        + ", " + _analyzerChannel.AnalyzerObservableVariable.Type
                                        + ", [" + _analyzerChannel.AnalyzerObservableVariable.Unit + "]";
            })));
            MinMaxLabel.Dispatcher.BeginInvoke((new Action(delegate
            {
                MinMaxLabel.Content = "ACTUAL: " + _analyzerChannel.AnalyzerObservableVariable.ValueY
                                      + " MIN: " + _analyzerChannel.AnalyzerObservableVariable.MinValue
                                      + " MAX: " + _analyzerChannel.AnalyzerObservableVariable.MaxValue;
            })));

        }

        private void BrushSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_save) return;
            if (_analyzerChannel.AnalyzerObservableVariable == null) return;
            _analyzerChannel.AnalyzerObservableVariable.Brush = (Brush) BrushComboBox.SelectedItem;
            Analyzer.AnalyzerChannels.StoreConfiguration();
        }

        private void VariableSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selector = (ComboBox) sender;

            if (!_save) return;
            try
            {
                _analyzerChannel.AnalyzerObservableVariable = new AnalyzerObservableVariable(Analyzer,
                    (CommunicationInterfaceVariable) selector.SelectedItem)
                {
                    Brush = (Brush) BrushComboBox.SelectedItem,
                    Unit = UnitTextBox.Text
                };
                _analyzerChannel.AnalyzerObservableVariable.OnPointCreated += OnPointCreated;
                _analyzerChannel.AnalyzerObservableVariable.PropertyChanged += UpdateControls;

                UpdateControls(this, new PropertyChangedEventArgs(""));

                Analyzer.AnalyzerChannels.StoreConfiguration();
            }
            catch (Exception)
            {
                _analyzerChannel.AnalyzerObservableVariable = null;
                selector.SelectedItem = null;
                TypeLabel.Content = "no variable selected";
                return;
            }

            Analyzer.AnalyzerChannels.StoreConfiguration();
        }

        private void UnitBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            var box = (TextBox) sender;

            if (!_save) return;
            if (_analyzerChannel.AnalyzerObservableVariable == null) return;
            try
            {
                _analyzerChannel.AnalyzerObservableVariable.Unit = box.Text;
            }
            catch (Exception)
            {
                _analyzerChannel.AnalyzerObservableVariable.Unit = "1";
            }

            Analyzer.AnalyzerChannels.StoreConfiguration();
        }

        #endregion

    }
}
