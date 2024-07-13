using System;
using System.Data;
using System.Text;
using System.Windows.Forms;
using static Fix.Dictionary;

namespace FixClient
{
    partial class IndicationsPanel : FixClientPanel
    {
        readonly IndicationDataGridView _indicationGrid;
        readonly IndicationDataTable _indicationTable;
        readonly DataView _indicationView;

        readonly SearchTextBox _indicationSearchTextBox;

        readonly MessagesPanel _messageDefaults;
        readonly ToolStripButton _defaultsButton;

        readonly ToolStrip _clientToolStrip;
        readonly ToolStrip _serverToolStrip;

        readonly ToolStripSplitButton _cancelButton;
        readonly ToolStripMenuItem _cancelAllButton;
        readonly ToolStripButton _amendButton;

        readonly ToolStripDropDownButton _rejectButton;
        readonly ToolStripMenuItem _rejectAllButton;

        readonly ToolStripMenuItem _clientMenuStrip;
        readonly ToolStripMenuItem _serverMenuStrip;

        readonly ToolStripMenuItem _cancelMenuItem;
        readonly ToolStripMenuItem _amendMenuItem;


        readonly ToolStripMenuItem _rejectMenuItem;

        Session? _session;

        public IndicationsPanel(MessagesPanel messageDefaults, ToolStripButton defaultsButton)
        {
            _messageDefaults = messageDefaults;
            _defaultsButton = defaultsButton;

            #region ToolStrip
            _clientToolStrip = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = LookAndFeel.Color.ToolStrip,
                Renderer = new ToolStripRenderer()
            };

            _cancelButton = new ToolStripSplitButton
            {
                Text = "Cancel",
                ToolTipText = "Cancel the selected indication"
            };
            _cancelButton.Click += CancelButtonClick;

            _cancelAllButton = new ToolStripMenuItem
            {
                Text = "Cancel All",
                ToolTipText = "Cancel all open indications immediately"
            };
            _cancelAllButton.Click += CancelAllButtonClick;
            _cancelButton.DropDownItems.Add(_cancelAllButton);

            _amendButton = new ToolStripButton
            {
                Text = "Amend",
                ToolTipText = "Amend the selected indication"
            };
            _amendButton.Click += AmendButtonClick;

            _serverToolStrip = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = LookAndFeel.Color.ToolStrip,
                Renderer = new ToolStripRenderer()
            };

            _rejectButton = new ToolStripDropDownButton
            {
                Text = "Reject",
                ToolTipText = "Reject the selected indication"
            };
            _rejectButton.Click += RejectButtonClick;

            _rejectAllButton = new ToolStripMenuItem
            {
                Text = "Reject All",
                ToolTipText = "Reject all indications immediately"
            };
            _rejectAllButton.Click += RejectAllButtonClick;
            _rejectButton.DropDownItems.Add(_rejectAllButton);

            _serverToolStrip.Items.AddRange(new ToolStripItem[]
            {
                _rejectButton
            });

            #endregion

            #region MenuStrip
            _clientMenuStrip = new ToolStripMenuItem("Action");

            _cancelMenuItem = new ToolStripMenuItem("Cancel", _cancelButton.Image);
            _cancelMenuItem.Click += CancelButtonClick;
            _clientMenuStrip.DropDownItems.Add(_cancelMenuItem);

            _amendMenuItem = new ToolStripMenuItem("Amend", _amendButton.Image);
            _amendMenuItem.Click += AmendButtonClick;
            _clientMenuStrip.DropDownItems.Add(_amendMenuItem);

            _clientMenuStrip.DropDownItems.Add(new ToolStripSeparator());

            _serverMenuStrip = new ToolStripMenuItem("Action");

            _rejectMenuItem = new ToolStripMenuItem("Reject", _rejectButton.Image);
            _rejectMenuItem.Click += RejectButtonClick;
            _serverMenuStrip.DropDownItems.Add(_rejectMenuItem);
            #endregion

            _indicationTable = new IndicationDataTable("Indications");
            _indicationView = new DataView(_indicationTable);

            _indicationGrid = new IndicationDataGridView
            {
                Dock = DockStyle.Fill,
                DataSource = _indicationView
            };
            _indicationGrid.SelectionChanged += (sender, ev) => UpdateUiState();

            _indicationSearchTextBox = new SearchTextBox
            {
                Dock = DockStyle.Top
            };
            _indicationSearchTextBox.TextChanged += (sender, ev) => ApplyFilters();

            ContentPanel.Controls.Add(_indicationGrid);
            ContentPanel.Controls.Add(_indicationSearchTextBox);

            UpdateUiState();
            ApplyFilters();

            IntPtr h = Handle;
        }

        void ApplyFilters()
        {
            if (_indicationView.Table is null)
            {
                return;
            }

            var buffer = new StringBuilder();

            string text = _indicationSearchTextBox.Text;

            if (!string.IsNullOrEmpty(text))
            {
                foreach (DataColumn column in _indicationView.Table.Columns)
                {
                    if (column.ColumnMapping == MappingType.Hidden)
                        continue;

                    if (column.DataType.IsEnum)
                    {
                        buffer.AppendFormat("{0} LIKE '%{1}%' OR ", column.ColumnName + "String", text);
                    }
                    else
                    {
                        buffer.AppendFormat("CONVERT({0}, System.String) LIKE '%{1}%' OR ", column.ColumnName, text);
                    }
                }
                buffer.Remove(buffer.Length - 3, 3);
            }

            if (buffer.Length == 0)
            {
                _indicationView.RowFilter = null;
                _indicationView.Sort = string.Empty;
            }
            else
            {
                _indicationView.RowFilter = buffer.ToString();
            }

            _indicationSearchTextBox.Focus();
        }

        void RejectAllButtonClick(object? sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(this,
                                                  "This will reject all pending indications, are you sure?",
                                                  Application.ProductName,
                                                  MessageBoxButtons.YesNo,
                                                  MessageBoxIcon.Information);

            if (result != DialogResult.Yes)
                return;

            RejectAllPendingIndications();
        }

        void AcknowledgeAllPendingIndications()
        {
            if (Session is null)
            {
                return;
            }

            foreach (DataGridViewRow row in _indicationGrid.Rows)
            {
                try
                {
                    var view = row.DataBoundItem as DataRowView;
                    var indicationRow = view?.Row as IndicationDataRow;

                    if (indicationRow?.Indication is null)
                    {
                        continue;
                    }

                    Fix.Indication indication = indicationRow.Indication;

                    if (indication is null)
                    {
                        continue;
                    }

                    var message = new Fix.Message { MsgType = FIX_5_0SP2.Messages.IOI.MsgType };

                    message.Fields.Set(FIX_5_0SP2.Fields.ClOrdID, indication.IOIID);

                    if (indication.Side is FieldValue side)
                    {
                        message.Fields.Set(indication.Side);
                    }

                    message.Fields.Set(FIX_5_0SP2.Fields.Symbol, indication.Symbol);
                    message.Fields.Set(FIX_5_0SP2.Fields.IOIQty, indication.IOIQty);
                    Session.Send(message);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                                    ex.Message,
                                    Application.ProductName,
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
            }
        }

        void RejectAllPendingIndications()
        {
            if (Session is null)
            {
                return;
            }

            foreach (DataGridViewRow row in _indicationGrid.Rows)
            {
                try
                {
                    var view = row.DataBoundItem as DataRowView;
                    var indicationRow = view?.Row as IndicationDataRow;
                    Fix.Indication? indication = indicationRow?.Indication;

                    if (indication is null)
                    {
                        continue;
                    }

                    var message = new Fix.Message { MsgType = FIX_5_0SP2.Messages.IOI.MsgType };


                    if (indication.Side is FieldValue side)
                    {
                        message.Fields.Set(indication.Side);
                    }

                    message.Fields.Set(FIX_5_0SP2.Fields.Symbol, indication.Symbol);
                    message.Fields.Set(FIX_5_0SP2.Fields.IOIQty, indication.IOIQty);

                    Session.Send(message);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                                    ex.Message,
                                    Application.ProductName,
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
            }
        }

        void CancelAllButtonClick(object? sender, EventArgs e)
        {
            if (Session is null)
            {
                return;
            }

            DialogResult result = MessageBox.Show(this,
                "This will cancel all open indications, are you sure?",
                Application.ProductName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result != DialogResult.Yes)
                return;

        }

        void UnsolicitedCancelAllOpenIndications()
        {
            if (Session is null)
            {
                return;
            }

            foreach (DataGridViewRow row in _indicationGrid.Rows)
            {
                try
                {
                    var view = row.DataBoundItem as DataRowView;
                    var indicationRow = view?.Row as IndicationDataRow;
                    Fix.Indication? indication = indicationRow?.Indication;

                    if (indication is null)
                    {
                        continue;
                    }

                    var message = new Fix.Message { MsgType = FIX_5_0SP2.Messages.ExecutionReport.MsgType };

                    if (indication.Side is FieldValue side)
                    {
                        message.Fields.Set(side);
                    }

                    message.Fields.Set(FIX_5_0SP2.Fields.Symbol, indication.Symbol);


                    Session.Send(message);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                                    ex.Message,
                                    Application.ProductName,
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
            }
        }

        void CancelAllOpenIndications()
        {
            if (Session is null)
            {
                return;
            }

            foreach (DataGridViewRow row in _indicationGrid.Rows)
            {
                try
                {
                    var view = row.DataBoundItem as DataRowView;
                    var indicationRow = view?.Row as IndicationDataRow;
                    Fix.Indication? indication = indicationRow?.Indication;

                    if (indication is null)
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                                    ex.Message,
                                    Application.ProductName,
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
            }
        }

        void RejectButtonClick(object? sender, EventArgs e)
        {
            if (SelectedIndication is not Fix.Indication indication)
            {
                return;
            }

            _defaultsButton.PerformClick();
        }

        void AmendButtonClick(object? sender, EventArgs e)
        {
            if (SelectedIndication is not Fix.Indication indication)
            {
                return;
            }

            _defaultsButton.PerformClick();
        }

        void CancelButtonClick(object? sender, EventArgs e)
        {
            if (SelectedIndication is not Fix.Indication indication)
            {
                return;
            }

            if (Session is null)
            {
                return;
            }

            _defaultsButton.PerformClick();
        }

        Fix.Indication? SelectedIndication
        {
            get
            {
                if (_indicationGrid.SelectedRows.Count == 0)
                {
                    return null;
                }

                var row = _indicationGrid.SelectedRows[0].DataBoundItem as DataRowView;
                var indicationRow = row?.Row as IndicationDataRow;
                return indicationRow?.Indication;
            }
        }

        void UpdateUiState()
        {
            bool enabled = false;

            if (Session != null && Session.Connected && _indicationGrid.SelectedRows.Count > 0)
            {
                enabled = true;
            }

            _amendButton.Enabled = enabled;

            _rejectButton.Enabled = enabled;

            _cancelMenuItem.Enabled = enabled;
            _amendMenuItem.Enabled = enabled;

            _rejectMenuItem.Enabled = enabled;
        }

        public Session? Session
        {
            get
            {
                return _session;
            }
            set
            {
                if (_session != null)
                {
                    _session.IndicationBook.Messages.Reset -= MessagesReset;
                    _session.IndicationBook.IndicationInserted -= IndicationBookIndicationInserted;
                    _session.IndicationBook.IndicationUpdated -= IndicationBookIndicationUpdated;
                    _session.SessionReset -= SessionSessionReset;
                    _session.StateChanged -= SessionStateChanged;
                }

                _session = value;
                Reload();

                if (_session != null)
                {
                    _session.IndicationBook.Messages.Reset += MessagesReset;
                    _session.IndicationBook.IndicationInserted += IndicationBookIndicationInserted;
                    _session.IndicationBook.IndicationUpdated += IndicationBookIndicationUpdated;
                    _session.SessionReset += SessionSessionReset;
                    _session.StateChanged += SessionStateChanged;
                }
            }
        }

        void SessionSessionReset(object? sender, EventArgs ev)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => SessionSessionReset(sender, ev)));
                return;
            }

            Reload();
        }

        void MessagesReset(object? sender)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => MessagesReset(sender)));
                return;
            }

            Reload();
        }

        void SessionStateChanged(object? sender, Fix.Session.StateEvent ev)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => SessionStateChanged(sender, ev)));
                return;
            }

            UpdateUiState();
        }

        void IndicationBookIndicationUpdated(object? sender, Fix.Indication indication)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => IndicationBookIndicationUpdated(sender, indication)));
                return;
            }

            if (_indicationTable.Rows.Find(indication.IOIID) is not IndicationDataRow row)
            {
                return;
            }

            row.Indication = indication;
            UpdateRow(row);
            _indicationGrid.RefreshEdit();
        }

        void IndicationBookIndicationInserted(object? sender, Fix.Indication indication)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => IndicationBookIndicationInserted(sender, indication)));
                return;
            }

            AddIndication(indication);
        }

        void AddIndication(Fix.Indication indication)
        {
            if (_indicationTable.Rows.Find(indication.IOIID) is IndicationDataRow _)
            {
                return;
            }

            var row = (IndicationDataRow)_indicationTable.NewRow();
            row.Indication = indication;

            UpdateRow(row);
            _indicationTable.Rows.Add(row);
        }

        static void UpdateRow(IndicationDataRow row)
        {
            if (row.Indication is not Fix.Indication indication)
            {
                return;
            }


            if (indication.Side != null)
            {
                row[IndicationDataTable.ColumnSide] = indication.Side;
                row[IndicationDataTable.ColumnSideString] = indication.Side.Name;
            }

            row[IndicationDataTable.ColumnSymbol] = indication.Symbol;
            row[IndicationDataTable.ColumnIOIQty] = indication.IOIQty;
            row[IndicationDataTable.ColumnPrice] = indication.Price;

            if (indication.IOITransType != null)
            {
                row[IndicationDataTable.ColumnStatus] = MapStatus(indication);
                row[IndicationDataTable.ColumnStatusString] = indication.IOITransType.Name;
            }

            if (indication.Qualifiers != null)
            {
                row[IndicationDataTable.ColumnQualifiers] = string.Join(",", indication.Qualifiers);
                //row[IndicationDataTable.ColumnQualifiersString] = indication.Qualifiers.Name;
            }

            if (indication.SecurityType != null)
            {
                row[IndicationDataTable.ColumnSecurityType] = indication.SecurityType;
                row[IndicationDataTable.ColumnSecurityTypeString] = indication.SecurityType.Name;
            }

            row[IndicationDataTable.ColumnIOIID] = indication.IOIID;
     	    row[IndicationDataTable.ColumnIOIRefID] = indication.IOIRefID;

	    if (indication.Text != null)
                row[IndicationDataTable.ColumnText] = indication.Text;
	}

        static String MapStatus(Fix.Indication indication)
        {
            if (indication.IOITransType is null)
                return "";
            if (indication.IOITransType.Value == FIX_5_0SP2.IOITransType.New.Value)
                return "New";
            else if (indication.IOITransType.Value == FIX_5_0SP2.IOITransType.Replace.Value)
                return "Replaced";
            else if (indication.IOITransType.Value == FIX_5_0SP2.IOITransType.Cancel.Value)
                return "Cancelled";
            else
                return "";
        }

        void Reload()
        {
            if (Session is null)
            {
                return;
            }

            try
            {
                _indicationTable.BeginLoadData();
                _indicationTable.Clear();

                foreach (Fix.Indication indication in Session.IndicationBook.Indications)
                {
                    AddIndication(indication);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                                ex.Message,
                                Application.ProductName,
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
            finally
            {
                _indicationTable.EndLoadData();
                UpdateUiState();
            }
        }
    }
}