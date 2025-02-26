using ClosedXML.Excel;
using DL24.Data;
using NPlot;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;
using static DL24.Data.DataSet1;

namespace DL24
{
    public partial class MainForm : Form
    {
        private static double _voltage = 0;
        private static double _current = 0;
        private static double _temp = 0;
        private static bool _testRunning = false;

        Color[] visibleColors = new Color[]
        {
            Color.FromArgb(255, 0, 0, 0),      // Black
            Color.FromArgb(255, 0, 0, 255),    // Blue
            Color.FromArgb(255, 0, 128, 0),    // Dark Green
            Color.FromArgb(255, 128, 0, 128),  // Purple
            Color.FromArgb(255, 128, 64, 0),   // Brown
            Color.FromArgb(255, 255, 0, 0),    // Red
            Color.FromArgb(255, 0, 128, 128),  // Teal
            Color.FromArgb(255, 128, 0, 0),    // Maroon
            Color.FromArgb(255, 0, 64, 128),   // Navy
            Color.FromArgb(255, 64, 128, 128), // Dark Cyan
            Color.FromArgb(255, 128, 128, 0),  // Olive
            Color.FromArgb(255, 128, 0, 64),   // Dark Magenta
            Color.FromArgb(255, 0, 64, 0),     // Dark Forest Green
            Color.FromArgb(255, 64, 0, 64),    // Dark Purple
            Color.FromArgb(255, 128, 64, 64),  // Dark Pink
            Color.FromArgb(255, 64, 64, 128),  // Slate Blue
            Color.FromArgb(255, 0, 128, 64),   // Sea Green
            Color.FromArgb(255, 128, 128, 64), // Dark Khaki
            Color.FromArgb(255, 64, 0, 128),   // Indigo
            Color.FromArgb(255, 128, 64, 128), // Plum
            Color.FromArgb(255, 64, 128, 0),   // Forest Green
            Color.FromArgb(255, 0, 64, 64),    // Dark Teal
            Color.FromArgb(255, 64, 64, 0),    // Dark Olive
            Color.FromArgb(255, 128, 0, 255),  // Violet
            Color.FromArgb(255, 0, 128, 255),  // Azure
            Color.FromArgb(255, 128, 255, 0),  // Chartreuse
            Color.FromArgb(255, 255, 0, 128),  // Deep Pink
            Color.FromArgb(255, 255, 128, 0),  // Orange
            Color.FromArgb(255, 0, 255, 128),  // Spring Green
            Color.FromArgb(255, 128, 0, 255),  // Electric Purple
            Color.FromArgb(255, 64, 255, 0),   // Lime Green
            Color.FromArgb(255, 255, 0, 64),   // Crimson
            Color.FromArgb(255, 0, 255, 64),   // Bright Green
            Color.FromArgb(255, 64, 0, 255)    // Electric Indigo
        };


        public MainForm()
        {
            InitializeComponent();

            backgroundWorker.RunWorkerAsync();
            backgroundWorkerPort.RunWorkerAsync();

            for (int i = 1; i <= 34; i++)
            {
                LinePlot linePlot = new LinePlot();
                linePlot.DataSource = dataSet1.BatteryTest;
                linePlot.AbscissaData = "Time";
                linePlot.OrdinateData = "Voltage" + i;
                linePlot.Pen = new Pen(visibleColors[i - 1], 2);
                linePlot.Label = "Battery " + i;

                _plot.Add(linePlot);
            }

            _plot.XAxis1.Label = "Time (sec)";
            _plot.XAxis1.WorldMax = 10;
            _plot.YAxis1.Label = "Voltage";
            _plot.YAxis1.WorldMax = 5;
            _plot.YAxis1.WorldMin = 0;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            FillPortNames();
        }

        private void FillPortNames()
        {
            _portName.Items.Clear();

            foreach (string portName in SerialPort.GetPortNames())
            {
                _portName.Items.Add(portName);
            }
        }

        private void _connect_Click(object sender, EventArgs e)
        {
            if (_serialPort.IsOpen)
            {
                MessageBox.Show("Port already open", "DL24", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_portName.SelectedIndex < 0)
            {
                MessageBox.Show("Select port first", "DL24", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _serialPort.PortName = (string)_portName.SelectedItem;
            _serialPort.Open();
            _portStatus.Text = "Port: connected";
        }

        private void _disconnect_Click(object sender, EventArgs e)
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                _portStatus.Text = "Port: disconnected";
            }
        }

        public static void SafeUpdateLabelText(Label label, string text)
        {
            if (label.InvokeRequired)
            {
                label.Invoke(new MethodInvoker(delegate { label.Text = text; }));
            }
            else
            {
                label.Text = text;
            }
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            do
            {
                if (_testRunning)
                {
                    double _voltageTrashhold = double.Parse(_voltageValue.Text);

                    for (int _currentModule = (int)_startFrom.Value; _currentModule <= 34; _currentModule++)
                    {
                        if (MessageBox.Show(string.Format("Connect module {0} to the tester", _currentModule), "Information", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
                        {
                            break;
                        }

                        SafeUpdateLabelText(_currentBattery, _currentModule.ToString());

                        if (_stopByTime.Checked)
                        {
                            for (int i = 0; i < _testTime.Value; i++)
                            {
                                BatteryTestRow row = null;

                                if (dataSet1.BatteryTest.Count == i)
                                {
                                    row = dataSet1.BatteryTest.NewBatteryTestRow();
                                }
                                else
                                {
                                    row = dataSet1.BatteryTest[i];
                                }

                                row.Time = i;
                                row["Voltage" + _currentModule] = _voltage;
                                row["Current" + _currentModule] = _current;
                                row["Temp" + _currentModule] = _temp;

                                if (row.RowState == DataRowState.Detached)
                                {
                                    dataSet1.BatteryTest.AddBatteryTestRow(row);
                                }

                                dataSet1.AcceptChanges();

                                UIThread(() => {
                                    bindingSourceTestResult.ResetBindings(false);
                                });

                                if (i == 0)
                                {
                                    TurnOn();
                                }

                                Thread.Sleep(i == 0 ? 3000 : 1000);
                            }

                            TurnOff();
                        }
                        else
                        {
                            int i = 0;
                            do
                            {
                                BatteryTestRow row = null;

                                if (dataSet1.BatteryTest.Count == i)
                                {
                                    row = dataSet1.BatteryTest.NewBatteryTestRow();
                                }
                                else
                                {
                                    row = dataSet1.BatteryTest[i];
                                }

                                row.Time = i;
                                row["Voltage" + _currentModule] = _voltage;
                                row["Current" + _currentModule] = _current;
                                row["Temp" + _currentModule] = _temp;

                                if (row.RowState == DataRowState.Detached)
                                {
                                    dataSet1.BatteryTest.AddBatteryTestRow(row);
                                }

                                dataSet1.AcceptChanges();

                                UIThread(() => {
                                    bindingSourceTestResult.ResetBindings(false);
                                });

                                if (i == 0)
                                {
                                    TurnOn();
                                }

                                Thread.Sleep(i++ == 0 ? 3000 : 1000);
                            }
                            while (_voltage > _voltageTrashhold);

                            TurnOff();
                        }
                    }

                    CalculateStatus();

                    _testRunning = false;

                    MessageBox.Show("Mesurment done!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
            while (true);
        }

        private void UIThread(MethodInvoker code)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(code);
            }
            else
            {
                code.Invoke();
            }
        }

        private void CalculateStatus()
        {
            dataSet1.BatteryStatus.Clear();

            if (dataSet1.BatteryTest.Count > 1)
            {
                for (int i = 1; i <= 34; i++)
                {
                    BatteryStatusRow statusRow = dataSet1.BatteryStatus.NewBatteryStatusRow();

                    statusRow.Num = i;
                    statusRow.Resistance = (((double)dataSet1.BatteryTest[0]["Voltage" + i] - (double)dataSet1.BatteryTest[1]["Voltage" + i]) / (double)dataSet1.BatteryTest[1]["Current" + 1]) * 1000.0;
                    statusRow.Capacity = 0;
                    statusRow.Diff = (double)dataSet1.BatteryTest[1]["Voltage" + i] - (double)dataSet1.BatteryTest[dataSet1.BatteryTest.Count - 1]["Voltage" + i];

                    dataSet1.BatteryStatus.AddBatteryStatusRow(statusRow);
                }

                dataSet1.AcceptChanges();

                UIThread(() => {
                    bindingSourceStatus.ResetBindings(false);

                });
            }
        }

        private void TurnOn()
        {
            if (!_serialPort.IsOpen)
                return;

            for (int i = 0; i < 10; i++)
            {
                _serialPort.Write(new byte[] { 0xB1, 0xB2, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0xB6 }, 0, 9);

                byte[] readStatus = ExecuteCommand(new byte[] { 0xB1, 0xB2, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0xB6 });
                if (readStatus[0] == 0xCA &&
                    readStatus[1] == 0xCB &&
                    readStatus[2] == 0x10 &&
                    readStatus[8] == 0xCE &&
                    readStatus[9] == 0xCF &&
                    readStatus[5] == 0x01)
                {
                    return;
                }
                Thread.Sleep(10);
            }
        }

        private void TurnOff()
        {
            if (!_serialPort.IsOpen)
                return;

            for (int i = 0; i < 10; i++)
            {
                _serialPort.Write(new byte[] { 0xB1, 0xB2, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0xB6 }, 0, 9);

                byte[] readStatus = ExecuteCommand(new byte[] { 0xB1, 0xB2, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0xB6 });
                if (readStatus[0] == 0xCA &&
                    readStatus[1] == 0xCB &&
                    readStatus[2] == 0x10 &&
                    readStatus[8] == 0xCE &&
                    readStatus[9] == 0xCF &&
                    readStatus[5] == 0x00)
                {
                    return;
                }
                Thread.Sleep(10);
            }
        }

        private void _startTest_Click(object sender, EventArgs e)
        {
            _testRunning = true;
        }

        private void _refreshPortNames_Click(object sender, EventArgs e)
        {
            FillPortNames();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataSet1.Clear();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                dataSet1.WriteXml(saveFileDialog.FileName);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                dataSet1.Clear();
                dataSet1.ReadXml(openFileDialog.FileName);
            }
        }

        private void backgroundWorkerPort_DoWork(object sender, DoWorkEventArgs e)
        {
            do
            {
                if (!_serialPort.IsOpen)
                {
                    Thread.Sleep(100);
                    continue;
                }

                // Read Voltage B1B2110000000000B6
                byte[] readVoltage = ExecuteCommand(new byte[] { 0xB1, 0xB2, 0x11, 0x00, 0x00, 0x00, 0x00, 0x00, 0xB6 });
                if (readVoltage[0] == 0xCA &&
                    readVoltage[1] == 0xCB &&
                    readVoltage[2] == 0x11 &&
                    readVoltage[8] == 0xCE &&
                    readVoltage[9] == 0xCF)
                {
                    _voltage = (double)((readVoltage[4] << 8) + readVoltage[5]) / 1000.0;
                    SafeUpdateLabelText(_voltageLabel, string.Format("Voltage: {0} V", _voltage.ToString("0.000")));
                }

                // Read Current B1B2120000000000B6
                byte[] readCurrent = ExecuteCommand(new byte[] { 0xB1, 0xB2, 0x12, 0x00, 0x00, 0x00, 0x00, 0x00, 0xB6 });
                if (readCurrent[0] == 0xCA &&
                    readCurrent[1] == 0xCB &&
                    readCurrent[2] == 0x12 &&
                    readCurrent[8] == 0xCE &&
                    readCurrent[9] == 0xCF)
                {
                    _current = (double)((readCurrent[4] << 8) + readCurrent[5]) / 1000.0;
                    SafeUpdateLabelText(_currentLabel, string.Format("Curent: {0} A", _current.ToString("0.000")));
                }

                // Read Current B1B2160000000000B6
                byte[] readTemp = ExecuteCommand(new byte[] { 0xB1, 0xB2, 0x16, 0x00, 0x00, 0x00, 0x00, 0x00, 0xB6 });
                if (readTemp[0] == 0xCA &&
                    readTemp[1] == 0xCB &&
                    readTemp[2] == 0x16 &&
                    readTemp[8] == 0xCE &&
                    readTemp[9] == 0xCF)
                {
                    _temp = (double)((readTemp[4] << 8) + readTemp[5]) / 10.0;
                    SafeUpdateLabelText(_tempLabel, string.Format("Temp: {0} C", _temp.ToString("0.0")));
                }

            }
            while (true);
        }

        private byte[] ExecuteCommand(byte[] command)
        {
            byte[] readBuffer = new byte[1024];

            try
            {
                if (_serialPort.BytesToRead > 0)
                    _serialPort.Read(readBuffer, 0, _serialPort.BytesToRead);

                _serialPort.Write(command, 0, command.Length);

                _serialPort.Read(readBuffer, 0, 10);
            }
            catch { }

            return readBuffer;
        }

        private void _Load_CheckedChanged(object sender, EventArgs e)
        {
            if (_Load.Checked)
            {
                TurnOn();
            }
            else
            {
                TurnOff();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            _plot.Refresh();
        }

        private void _plotVMax_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double value = double.Parse(_plotVMax.Text);
                _plot.YAxis1.WorldMax = value;
                _plot.Refresh();
            }
            catch { }
        }

        private void _plotVMin_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double value = double.Parse(_plotVMin.Text);
                _plot.YAxis1.WorldMin = value;
                _plot.Refresh();
            }
            catch { }

        }

        private void _plotTime_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double value = double.Parse(_plotTime.Text);
                _plot.XAxis1.WorldMax = value;
                _plot.Refresh();
            }
            catch { }
        }

        private void recalculateStatisticToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CalculateStatus();
        }

        private void normalizeChartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataSet1.BatteryTest.Count > 1)
            {
                double max = 0;

                for (int i = 1; i <= 34; i++)
                {
                    double current = (double)dataSet1.BatteryTest[1]["Voltage" + i];
                    if (current > max) max = current;
                }

                for (int i = 1; i <= 34; i++)
                {
                    double offset = max - (double)dataSet1.BatteryTest[1]["Voltage" + i];

                    for (int j = 0; j < dataSet1.BatteryTest.Count; j++)
                    {
                        double current = (double)dataSet1.BatteryTest[j]["Voltage" + i];
                        dataSet1.BatteryTest[j]["Voltage" + i] = current + offset;
                    }
                }

                dataSet1.AcceptChanges();
            }
        }

        private void exportToExcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog2.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Test data");

                for (int i = 0; i < dataGridViewTestResult.Columns.Count; i++)
                {
                    worksheet.Cell(1, i + 1).Value = dataGridViewTestResult.Columns[i].HeaderText;
                }

                for (int i = 0; i < dataSet1.BatteryTest.Count; i++)
                    for (int j = 0; j < dataGridViewTestResult.Columns.Count; j++)
                    {
                        worksheet.Cell(i + 2, j + 1).Value = Convert.ToDouble(dataSet1.BatteryTest[i][j]);
                    }

                var worksheet2 = workbook.Worksheets.Add("Calculated data");

                for (int i = 0; i < dataGridViewStatus.Columns.Count; i++)
                {
                    worksheet2.Cell(1, i + 1).Value = dataGridViewStatus.Columns[i].HeaderText;
                }

                for (int i = 0; i < dataSet1.BatteryStatus.Count; i++)
                    for (int j = 0; j < dataGridViewStatus.Columns.Count; j++)
                    {
                        worksheet2.Cell(i + 2, j + 1).Value = Convert.ToDouble(dataSet1.BatteryStatus[i][j]);
                    }

                workbook.SaveAs(saveFileDialog2.FileName);
            }

            MessageBox.Show(string.Format("Export to the file {0} done", saveFileDialog2.FileName), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
 