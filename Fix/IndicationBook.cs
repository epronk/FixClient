using System;
using System.Linq;
using static Fix.Dictionary;

namespace Fix
{
    public class IndicationsEventArgs : EventArgs
    {
        public IndicationsEventArgs(Indication indication)
        {
            Indication = indication;
        }

        public Indication Indication { get; }
    }

    public class IndicationBook
    {
        #region Events

        public delegate void IndicationDeligate(object sender, IndicationsEventArgs e);

        public event IndicationDeligate? IndicationInserted;
        public event IndicationDeligate? IndicationUpdated;
        public event IndicationDeligate? IndicationDeleted;

        protected void OnIndicationInserted(Indication indication)
        {
            IndicationInserted?.Invoke(this, new IndicationsEventArgs(indication));
        }

        protected void OnIndicationUpdated(Indication indication)
        {
            IndicationUpdated?.Invoke(this, new IndicationsEventArgs(indication));
        }

        protected void OnIndicationDeleted(Indication indication)
        {
            IndicationDeleted?.Invoke(this, new IndicationsEventArgs(indication));
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

        public void Delete(Indication indication)
        {
            if (Indications.Remove(KeyForIndication(indication)))
            {
                OnIndicationDeleted(indication);
            }
        }

        public bool Process(Message message)
        {
            if (message == null)
                return false;

            bool result = true;

            try
            {
                if (message.Fields.Find(FIX_5_0SP2.Fields.MsgType) is not Field)
                {
                    message.Status = MessageStatus.Error;
                    message.StatusMessage = " because it does not contain a MsgType";
                    return false;
                }

                if (message.Administrative)
                    return true;

                if (message.Fields.Find(FIX_5_0SP2.Fields.PossDupFlag) is Field field && (bool)field)
                {
                    message.Status = MessageStatus.Warn;
                    message.StatusMessage = StatusMessageHeader + " because it is a possible duplicate";
                    return false;
                }

                if (message.MsgType == FIX_5_0SP2.Messages.IOI.MsgType)
                {
                    result = ProcessIOI(message);
                }
            }
            catch (Exception ex)
            {
                message.Status = MessageStatus.Error;
                message.StatusMessage = ex.Message;
            }
            finally
            {
                if (!result)
                {
                    if (message.Status == MessageStatus.None)
                    {
                        message.Status = MessageStatus.Warn;
                    }

                    if (string.IsNullOrEmpty(message.StatusMessage))
                    {
                        message.StatusMessage = StatusMessageHeader + " - please create an issue here https://github.com/GaryHughes/FixClient/issues and attach your session files";
                    }
                }

                Messages.Add(message);
            }

            return result;
        }

        bool ProcessIOI(Message message)
        {
            if (message.Fields.Find(FIX_5_0SP2.Fields.IOIID) is null)
            {
                message.Status = MessageStatus.Error;
                message.StatusMessage = StatusMessageHeader + " because the IOIID field is missing";
                return false;
            }

            bool result = false;
            Field? IOITransType = message.Fields.Find(FIX_5_0SP2.Fields.IOITransType);

            try
            {
                if (IOITransType is null)
		        {
		            result = false;
		        }
                else if (IOITransType.Value == FIX_5_0SP2.IOITransType.New.Value)
                {
                    result = AddIndication(new Indication(message));
                }
                else if (IOITransType.Value == FIX_5_0SP2.IOITransType.Cancel.Value)
                {
                    result = DeleteIndication(new Indication(message));
                }
                else if (IOITransType.Value == FIX_5_0SP2.IOITransType.Replace.Value)
                {
                    result = UpdateIndication(new Indication(message));
                }
            }
            catch (Exception ex)
            {
                message.Status = MessageStatus.Error;
                message.StatusMessage = ex.Message;
                result = false;
            }

            return result;
        }

        bool ProcessIndicationCancelReject(Message message)
        {
            if (message.Fields.Find(FIX_5_0SP2.Fields.OrigClOrdID) is not Field OrigClOrdID)
            {
                message.Status = MessageStatus.Error;
                message.StatusMessage = StatusMessageHeader + " because the OrigClOrdID field is missing";
                return false;
            }

            if (message.Fields.Find(FIX_5_0SP2.Fields.SenderCompID) is not Field SenderCompID)
            {
                message.Status = MessageStatus.Error;
                message.StatusMessage = StatusMessageHeader + " because the SenderCompID field is missing";
                return false;
            }

            if (message.Fields.Find(FIX_5_0SP2.Fields.TargetCompID) is not Field TargetCompID)
            {
                message.Status = MessageStatus.Error;
                message.StatusMessage = StatusMessageHeader + " because the TargetCompID field is missing";
                return false;
            }
            //
            // When we first store the indication we set the comp id's relative to the indication source so we
            // need to flip them when searching for indications to match messages coming from the destination.
            //
            Indication? indication = FindIndication(TargetCompID.Value,
                                     SenderCompID.Value,
                                     OrigClOrdID.Value);

            if (indication == null)
            {
                message.Status = MessageStatus.Error;
                message.StatusMessage = StatusMessageHeader + $" because a matching indication with ClOrdID = {OrigClOrdID.Value} could not be found";
                return false;
            }

            if (message.Fields.Find(FIX_5_0SP2.Fields.Text) is Field Text)
            {
                indication.Text = Text.Value;
            }

            indication.Messages.Add(message);
            OnIndicationUpdated(indication);

            return true;
        }

        public static string KeyForIndication(Indication indication)
        {
            return KeyForIndication(indication.SenderCompID, indication.TargetCompID, indication.IOIID);
        }

        public static string KeyForIndication(string SenderCompID, string TargetCompID, string ClOrdID)
        {
            return $"{SenderCompID}-{TargetCompID}-{ClOrdID}";
        }

        Indication? FindIndication(string SenderCompID, string TargetCompID, string ClOrdID)
        {
            if (Indications.TryGetValue(KeyForIndication(SenderCompID, TargetCompID, ClOrdID), out var indication))
            {
                return indication;
            }

            return null;
        }

        bool DeleteIndication(Indication indication)
        {
            Indications.Remove(KeyForIndication(indication.SenderCompID, indication.TargetCompID, indication.IOIID));
            return true;
        }

        bool AddIndication(Indication indication)
        {
            Indication? existing = FindIndication(indication.SenderCompID, indication.TargetCompID, indication.IOIID);

            if (existing != null)
            {
                indication.Messages[0].Status = MessageStatus.Error;
                indication.Messages[0].StatusMessage = StatusMessageHeader + $" because an indication with IOIID = {indication.IOIID} already exists";
                return false;
            }

            indication.UpdateKey();
            Indications.Add(indication);

            OnIndicationInserted(indication);

            return true;
        }

        bool UpdateIndication(Indication indication)
        {
            Indication? existing = FindIndication(indication.SenderCompID, indication.TargetCompID, indication.IOIID);

            if (existing == null)
            {
                indication.Messages[0].Status = MessageStatus.Error;
                indication.Messages[0].StatusMessage = StatusMessageHeader + $" because an indication with IOIID = {indication.IOIID} does not exists";
                return false;
            }

            indication.UpdateKey();
            DeleteIndication(indication);
            Indications.Add(indication);

            OnIndicationUpdated(indication);

            return true;
        }
    }
}