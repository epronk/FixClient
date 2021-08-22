/////////////////////////////////////////////////
//
// FIX Client
//
// Copyright @ 2021 VIRTU Financial Inc.
// All rights reserved.
//
// Filename: OrderDataTable.cs
// Author:   Gary Hughes
//
/////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Data;
using static Fix.Dictionary;

namespace FixClient
{
    class IndicationDataTable : DataTable
    {
        public const string ColumnIOIID = "IOIID";
        public const string ColumnIOITransType = "IOITransType";
        public const string ColumnIOIRefID = "IOIRefID";
        public const string ColumnSide = "Side";
        public const string ColumnSideString = "SideString";
        public const string ColumnIOIQty = "IOIQty";
        public const string ColumnSymbol = "Symbol";
        public const string ColumnPrice = "Price";
        public const string ColumnSecurityType = "SecurityType";
        public const string ColumnText = "Text";

        public IndicationDataTable(string name)
        : base(name)
        {
            var primaryKey = new List<DataColumn>();

            Columns.Add(ColumnSide, typeof(FieldValue));
            Columns.Add(ColumnSideString).ColumnMapping = MappingType.Hidden;
            Columns.Add(ColumnSymbol);
            Columns.Add(ColumnIOIQty);
            Columns.Add(ColumnPrice);
            Columns.Add(ColumnSecurityType);
            primaryKey.Add(Columns.Add(ColumnIOIID));
            Columns.Add(ColumnIOIRefID);
            Columns.Add(ColumnText);

            PrimaryKey = primaryKey.ToArray();
        }

        protected override Type GetRowType()
        {
            return typeof(IndicationDataRow);
        }

        protected override DataRow NewRowFromBuilder(DataRowBuilder builder)
        {
            return new IndicationDataRow(builder);
        }
    }
}
