using System;
using System.Linq;
using static Fix.Dictionary;

namespace Fix
{
    public enum IndicationBookMessageEffect
    {
        Ignored,
        Rejected,
        Modified,
    }

    public class IndicationBook
    {
        #region Events

        public event EventHandler<Indication>? IndicationInserted;
        public event EventHandler<Indication>? IndicationUpdated;
        public event EventHandler<Indication>? IndicationDeleted;

        protected void OnIndicationInserted(Indication indication)
        {
            IndicationInserted?.Invoke(this, indication);
        }

        protected void OnIndicationUpdated(Indication indication)
        {
            IndicationUpdated?.Invoke(this, indication);
        }

        protected void OnIndicationDeleted(Indication indication)
        {
            IndicationDeleted?.Invoke(this, indication);
        }

        #endregion

        const string StatusMessageHeader = "This message was not processed by the indication book";

        public IndicationCollection Indications { get; } = new();

        public bool DeleteInactiveIndications { get; set; }
        public int MaximumIndications { get; set; }

        public MessageCollection Messages { get; } = new MessageCollection();

        [Flags]
        public enum Retain
        {
            None = 0,
            ActiveGTC = 1 << 1,
            ActiveGTD = 1 << 2
        }

        public void Clear(Retain options = Retain.None)
        {
            if (options == Retain.None)
            {
                Indications.Clear();
            }

            Messages.Clear();
        }

        public IndicationBookMessageEffect DeleteIndication(Indication indication)
        {
            if (Indications.Remove(KeyForIndication(indication)))
            {
                OnIndicationDeleted(indication);
            }
	    return IndicationBookMessageEffect.Modified;
        }

        public IndicationBookMessageEffect Process(Message message)
        {
            var result = IndicationBookMessageEffect.Rejected;

            try
            {
                if (message.Fields.Find(FIX_5_0SP2.Fields.MsgType) is not Field)
                {
                    message.Status = MessageStatus.Error;
                    message.StatusMessage = " because it does not contain a MsgType";
                    return IndicationBookMessageEffect.Rejected;
                }

                if (message.Administrative)
		{
                    return IndicationBookMessageEffect.Ignored;
		}

                if (message.Fields.Find(FIX_5_0SP2.Fields.PossDupFlag) is Field field && (bool)field)
                {
                    message.Status = MessageStatus.Warn;
                    message.StatusMessage = StatusMessageHeader + " because it is a possible duplicate";
                    return IndicationBookMessageEffect.Rejected;
                }

                if (message.MsgType == FIX_5_0SP2.Messages.IOI.MsgType)
                {
                    return ProcessIOI(message);
                }
            }
            catch (Exception ex)
            {
                message.Status = MessageStatus.Error;
                message.StatusMessage = ex.Message;
                return IndicationBookMessageEffect.Rejected;
            }
            finally
            {
                Messages.Add(message);
            }

            message.Status = MessageStatus.Warn;
            message.StatusMessage = StatusMessageHeader + " - please create an issue here https://github.com/GaryHughes/FixClient/issues and attach your session files";
            return IndicationBookMessageEffect.Rejected;
        }

        IndicationBookMessageEffect ProcessIOI(Message message)
        {
            if (message.Fields.Find(FIX_5_0SP2.Fields.IOIID) is null)
            {
                message.Status = MessageStatus.Error;
                message.StatusMessage = StatusMessageHeader + " because the IOIID field is missing";
                return IndicationBookMessageEffect.Rejected;
            }

            var result = IndicationBookMessageEffect.Rejected;
            Field? IOITransType = message.Fields.Find(FIX_5_0SP2.Fields.IOITransType);

            if (IOITransType.Value == FIX_5_0SP2.IOITransType.Replace.Value ||
                IOITransType.Value == FIX_5_0SP2.IOITransType.Cancel.Value)
            {
                if (message.Fields.Find(FIX_5_0SP2.Fields.IOIRefID) is not Field IOIRefID)
                {
                    message.Status = MessageStatus.Error;
                    message.StatusMessage = StatusMessageHeader + " because the IOIRefID is required for replace and cancel";
                    return IndicationBookMessageEffect.Rejected;
                }
            }

            try
            {
                if (IOITransType is null)
                {
                    result = IndicationBookMessageEffect.Rejected;
                }
                else if (IOITransType.Value == FIX_5_0SP2.IOITransType.New.Value)
                {
                    result = AddIndication(new Indication(message));
                }
                else if (IOITransType.Value == FIX_5_0SP2.IOITransType.Replace.Value)
                {
                    result = UpdateIndication(new Indication(message));
                }
                else if (IOITransType.Value == FIX_5_0SP2.IOITransType.Cancel.Value)
                {
                    result = DeleteIndication(new Indication(message));
                }
            }
            catch (Exception ex)
            {
                message.Status = MessageStatus.Error;
                message.StatusMessage = ex.Message;
                result = IndicationBookMessageEffect.Rejected;
            }

            return result;
        }

        public static string KeyForIndication(Indication indication)
        {
            return KeyForIndication(indication.SenderCompID, indication.TargetCompID, indication.IOIID);
        }

        public static string KeyForIndication(string SenderCompID, string TargetCompID, string IOIID)
        {
            return $"{SenderCompID}-{TargetCompID}-{IOIID}";
        }

        Indication? FindIndication(string SenderCompID, string TargetCompID, string IOIID)
        {
            if (Indications.TryGetValue(KeyForIndication(SenderCompID, TargetCompID, IOIID), out var indication))
            {
                return indication;
            }

            return null;
        }

        IndicationBookMessageEffect AddIndication(Indication indication)
        {
            Indication? existing = FindIndication(indication.SenderCompID, indication.TargetCompID, indication.IOIID);

            if (existing != null)
            {
                indication.Messages[0].Status = MessageStatus.Error;
                indication.Messages[0].StatusMessage = StatusMessageHeader + $" because an indication with IOIID = {indication.IOIID} already exists";
                return IndicationBookMessageEffect.Rejected;
            }

            indication.UpdateKey();
            Indications.Add(indication);

            OnIndicationInserted(indication);

            return IndicationBookMessageEffect.Modified;
        }

        IndicationBookMessageEffect UpdateIndication(Indication indication)
        {
            Indication? existing = FindIndication(indication.SenderCompID, indication.TargetCompID, indication.IOIRefID);

            if (existing == null)
            {
                indication.Messages[0].Status = MessageStatus.Error;
                indication.Messages[0].StatusMessage = StatusMessageHeader + $" because an indication with ID = {indication.IOIRefID} does not exists";
                return IndicationBookMessageEffect.Rejected;
            }

            indication.UpdateKey();
            DeleteIndication(indication);
            Indications.Add(indication);

            OnIndicationUpdated(indication);

            return IndicationBookMessageEffect.Modified;
        }
    }
}
