/////////////////////////////////////////////////
//
// FIX Client
//
// Copyright @ 2021 VIRTU Financial Inc.
// All rights reserved.
//
// Filename: IndicationDataRow.cs
// Author:   Gary Hughes
//
/////////////////////////////////////////////////

using Fix;
using System.Data;

namespace FixClient
{
    class IndicationDataRow : DataRow
    {
        public IndicationDataRow(DataRowBuilder builder)
            : base(builder)
        {
        }

        public Indication? Indication { get; set; }
    }
}
