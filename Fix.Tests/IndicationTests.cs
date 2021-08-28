using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using static Fix.Dictionary;

namespace FixTests
{
    [TestClass]
    public class IndicationTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestQualifiers()
        {
            var message = new Fix.Message { MsgType = FIX_5_0SP2.Messages.IOI.MsgType };
            message.Fields.Set(FIX_5_0SP2.Fields.SenderCompID, "SENDER");
            message.Fields.Set(FIX_5_0SP2.Fields.TargetCompID, "TARGET");
            message.Fields.Set(FIX_5_0SP2.Fields.IOIID, 1);
            message.Fields.Set(FIX_5_0SP2.Fields.IOITransType, FIX_5_0SP2.IOITransType.New.Value);
            message.Fields.Set(FIX_5_0SP2.Fields.NoIOIQualifiers, 2);
            message.Fields.Add(new Fix.Field(FIX_5_0SP2.Fields.IOIQualifier, FIX_5_0SP2.IOIQualifier.Limit.Value));
            message.Fields.Add(new Fix.Field(FIX_5_0SP2.Fields.IOIQualifier, FIX_5_0SP2.IOIQualifier.AtTheClose.Value));

            var indication = new Fix.Indication(message);
            Assert.IsNotNull(indication);
            Assert.AreEqual("Limit", indication.Qualifiers[0].Name);
            Assert.AreEqual("AtTheClose", indication.Qualifiers[1].Name);
        }
    }
}
