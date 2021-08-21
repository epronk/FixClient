/////////////////////////////////////////////////
//
// FIX Client
//
// Copyright @ 2021 VIRTU Financial Inc.
// All rights reserved.
//
// Filename: IndicationDataGridView.cs
// Author:   Gary Hughes
//
/////////////////////////////////////////////////
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using static Fix.Dictionary;

namespace FixClient
{
    public sealed partial class IndicationDataGridView : DataGridView
    {
        public IndicationDataGridView()
        {
            // fixme
            //InitializeComponent();

            EnableHeadersVisualStyles = false;
            ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 8, FontStyle.Bold);
            ColumnHeadersDefaultCellStyle.BackColor = LookAndFeel.Color.GridColumnHeader;
            ColumnHeadersDefaultCellStyle.ForeColor = Color.WhiteSmoke;
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            ColumnHeadersHeight -= 3;
            MultiSelect = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            AllowUserToAddRows = false;
            RowHeadersVisible = false;
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            AllowUserToDeleteRows = false;
            RowTemplate.Resizable = DataGridViewTriState.False;
            DefaultCellStyle.SelectionBackColor = LookAndFeel.Color.GridCellSelectedBackground;
            DefaultCellStyle.SelectionForeColor = LookAndFeel.Color.GridCellSelectedForeground;
            DefaultCellStyle.BackColor = LookAndFeel.Color.GridCellBackground;
            DefaultCellStyle.ForeColor = LookAndFeel.Color.GridCellForeground;
            BackgroundColor = LookAndFeel.Color.GridCellBackground;
            GridColor = LookAndFeel.Color.Grid;
            DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            DefaultCellStyle.Font = new Font("Arial", 8);
            RowTemplate.Height -= 3;
            DoubleBuffered = true;
            ReadOnly = true;
        }

        public bool DisplayExDestination { get; set; }

        protected override void OnColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e)
        {
            if (ModifierKeys == Keys.Control)
            {
                if (DataSource is DataView view)
                {
                    view.Sort = string.Empty;
                    Refresh();
                    return;
                }
            }

            base.OnColumnHeaderMouseClick(e);
        }

        protected override void OnColumnAdded(DataGridViewColumnEventArgs e)
        {
            base.OnColumnAdded(e);

            DataGridViewColumn column = e.Column;

            switch (column.Name)
            {
                case IndicationDataTable.ColumnSymbol:
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    break;

                case IndicationDataTable.ColumnSide:
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    break;

                case IndicationDataTable.ColumnText:
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    break;
            }
        }

        protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
        {
            DataGridViewColumn column = Columns[e.ColumnIndex];

            if (Rows[e.RowIndex].DataBoundItem is not DataRowView rowView)
            {
                return;
            }

            DataRow row = rowView.Row;

            if (column.Name == IndicationDataTable.ColumnSide)
            {
                Color color = LookAndFeel.Color.GridCellForeground;

                if (e.Value == null || e.Value is DBNull)
                {
                    e.Value = "Unknown";
                    color = LookAndFeel.Color.Unknown;
                }
                else
                {
                    var side = (FieldValue)e.Value;

                    if (side == FIX_5_0SP2.Side.Buy ||
                        side == FIX_5_0SP2.Side.Cross)
                    {
                        color = LookAndFeel.Color.Bid;
                    }

                    if (side == FIX_5_0SP2.Side.Sell)
                    {
                        color = LookAndFeel.Color.Ask;
                    }

                    e.Value = side.ToString();
                }

                e.CellStyle.ForeColor = color;
                e.FormattingApplied = true;
                return;
            }


            base.OnCellFormatting(e);
        }

        public static string ShortTimeInForceDescription(FieldValue timeInForce)
        {
            if (timeInForce == FIX_5_0SP2.TimeInForce.AtTheOpening) return "ATO";
            if (timeInForce == FIX_5_0SP2.TimeInForce.Day) return "DAY";
            if (timeInForce == FIX_5_0SP2.TimeInForce.FillOrKill) return "FOK";
            if (timeInForce == FIX_5_0SP2.TimeInForce.GoodTillCancel) return "GTC";
            if (timeInForce == FIX_5_0SP2.TimeInForce.GoodTillCrossing) return "GTX";
            if (timeInForce == FIX_5_0SP2.TimeInForce.GoodTillDate) return "GTD";
            if (timeInForce == FIX_5_0SP2.TimeInForce.ImmediateOrCancel) return "IOC";
            if (timeInForce == FIX_5_0SP2.TimeInForce.AtTheClose) return "ATC";
            return timeInForce.Name;
        }
    }
}