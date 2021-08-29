using System;
using System.Collections.Generic;
using static Fix.Dictionary;

namespace Fix
{
    public class Indication : ICloneable
    {
        public Indication(Message message)
        {
            if (message.MsgType != FIX_5_0SP2.Messages.IOI.MsgType)
            {
                throw new ArgumentException("Message is not an IOI");
            }

            if (message.Fields.Find(FIX_5_0SP2.Fields.SenderCompID) is not Field senderField || string.IsNullOrEmpty(senderField.Value))
            {
                throw new ArgumentException("Message does not contain a SenderCompID");
            }

            SenderCompID = senderField.Value;

            if (message.Fields.Find(FIX_5_0SP2.Fields.TargetCompID) is not Field targetField || string.IsNullOrEmpty(targetField.Value))
            {
                throw new ArgumentException("Message does not contain a TargetCompID");
            }

            TargetCompID = targetField.Value;

            if (message.Fields.Find(FIX_5_0SP2.Fields.IOIID) is not Field IOIIDField || string.IsNullOrEmpty(IOIIDField.Value))
            {
                throw new ArgumentException("Message does not contain a IOIID");
            }

            IOIID = IOIIDField.Value;


            if (message.Fields.Find(FIX_5_0SP2.Fields.IOITransType) is not Field IOITransTypeField)
            {
                throw new ArgumentException("Message does not contain a IOITransType");
            }

            IOITransType = (FieldValue?)IOITransTypeField;

            if (message.Fields.Find(FIX_5_0SP2.Fields.Symbol) is Field symbolField && !string.IsNullOrEmpty(symbolField.Value))
            {
                Symbol = symbolField.Value;
            }

            if (message.Fields.Find(FIX_5_0SP2.Fields.Price) is Field priceField)
            {
                Price = (decimal?)priceField;
            }

            if (message.Fields.Find(FIX_5_0SP2.Fields.Side) is Field sideField)
            {
                Side = (FieldValue?)sideField;
            }

            if (message.Fields.Find(FIX_5_0SP2.Fields.IOIQty) is Field ioiQtyField && !string.IsNullOrEmpty(ioiQtyField.Value))
            {
                IOIQty = ioiQtyField.Value;
            }

            int index = 0;
            for (; index < message.Fields.Count; ++index)
            {
                if (message.Fields[index].Tag == FIX_5_0SP2.Fields.NoIOIQualifiers.Tag)
                    break;
            }

            var noGroups = (long?)message.Fields[index];
            Qualifiers = new List<FieldValue>();
            for (var i = 0; i < noGroups; ++i)
            {
                var qualifier = (FieldValue?)message.Fields[index + 1 + i];
                if (qualifier != null)
                    Qualifiers.Add(qualifier);
            }
            Messages = new List<Message>
            {
                message
            };

            Key = CreateKey(SenderCompID, TargetCompID, IOIID);
        }

        public List<Message> Messages { get; private set; }
        public List<FieldValue> Qualifiers { get; private set; }

        public string SenderCompID { get; set; }
        public string TargetCompID { get; set; }
        public string IOIID { get; set; }
        public string? IOIRefID { get; set; }
        public FieldValue? IOITransType { get; set; }
	    public string Symbol { get; set; }
        public FieldValue? SecurityType { get; set; }
        public string? IOIQty { get; set; }
        public decimal? Price { get; set; }
        public FieldValue? Side { get; set; }
        public FieldValue? OrdStatus { get; set; }
        public string? Text { get; set; }
	//        public Message? PendingMessage { get; set; }
        //public long? PendingOrderQty { get; set; }
        //public decimal? PendingPrice { get; set; }
        public DateTime SendingTime { get; private set; }

        public string Key { get; private set; }

        public void UpdateKey()
        {
            Key = CreateKey(SenderCompID, TargetCompID, IOIID);
        }

        static string CreateKey(string SenderCompID, string TargetCompID, string ClOrdID)
        {
            return $"{SenderCompID}-{TargetCompID}-{ClOrdID}";
        }

        public object Clone()
        {
            var clone = (Indication)MemberwiseClone();
            clone.Messages = new List<Message>();
            foreach (var message in Messages)
            {
                clone.Messages.Add((Message)message.Clone());
            }
            return clone;
        }
    }
}
