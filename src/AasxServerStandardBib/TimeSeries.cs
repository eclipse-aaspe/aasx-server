﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using AasxServer;
using AdminShellNS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using SampleClient;

namespace AasxTimeSeries
{
    public static class TimeSeries
    {
        public class TimeSeriesBlock
        {
            public AdminShell.Submodel submodel = null;
            public AdminShell.SubmodelElementCollection block = null;
            public AdminShell.SubmodelElementCollection data = null;
            public AdminShell.Property sampleStatus = null;
            public AdminShell.Property sampleMode = null;
            public AdminShell.Property sampleRate = null;
            public AdminShell.Property lowDataIndex = null;
            public AdminShell.Property highDataIndex = null;
            public AdminShell.Property maxSamples = null;
            public AdminShell.Property actualSamples = null;
            public AdminShell.Property maxSamplesInCollection = null;
            public AdminShell.Property actualSamplesInCollection = null;
            public AdminShell.Property maxCollections = null;
            public AdminShell.Property actualCollections = null;

            public int threadCounter = 0;
            public string sourceType = "";
            public string sourceAddress = "";
            public string username = "";
            public string password = "";
            public int samplesCollectionsCount = 0;
            public List<AdminShell.Property> samplesProperties = null;
            public List<string> samplesValues = null;
            public string samplesTimeStamp = "";
            public int samplesValuesCount = 0;

            public List<string> opcNodes = null;
            public DateTime opcLastTimeStamp;
        }
        static public List<TimeSeriesBlock> timeSeriesBlockList = null;
        static public List<AdminShell.SubmodelElementCollection> timeSeriesSubscribe = null;
        public static void timeSeriesInit()
        {
            DateTime timeStamp = DateTime.Now;

            timeSeriesBlockList = new List<TimeSeriesBlock>();
            timeSeriesSubscribe = new List<AdminShellV20.SubmodelElementCollection>();

            int aascount = AasxServer.Program.env.Length;

            for (int i = 0; i < aascount; i++)
            {
                var env = AasxServer.Program.env[i];
                if (env != null)
                {
                    var aas = env.AasEnv.AdministrationShells[0];
                    aas.TimeStampCreate = timeStamp;
                    aas.setTimeStamp(timeStamp);
                    if (aas.submodelRefs != null && aas.submodelRefs.Count > 0)
                    {
                        foreach (var smr in aas.submodelRefs)
                        {
                            var sm = env.AasEnv.FindSubmodel(smr);
                            if (sm != null && sm.idShort != null)
                            {
                                sm.TimeStampCreate = timeStamp;
                                sm.SetAllParents(timeStamp);
                                int countSme = sm.submodelElements.Count;
                                for (int iSme = 0; iSme < countSme; iSme++)
                                {
                                    var sme = sm.submodelElements[iSme].submodelElement;
                                    if (sme is AdminShell.SubmodelElementCollection && sme.idShort.Contains("TimeSeries"))
                                    {
                                        bool nextSme = false;
                                        if (sme.qualifiers.Count > 0)
                                        {
                                            int j = 0;
                                            while (j < sme.qualifiers.Count)
                                            {
                                                var q = sme.qualifiers[j] as AdminShell.Qualifier;
                                                if (q.type == "SUBSCRIBE")
                                                {
                                                    timeSeriesSubscribe.Add(sme as AdminShell.SubmodelElementCollection);
                                                    // nextSme = true;
                                                    break;
                                                }
                                                j++;
                                            }
                                        }
                                        if (nextSme)
                                            continue;

                                        var smec = sme as AdminShell.SubmodelElementCollection;
                                        int countSmec = smec.value.Count;

                                        var tsb = new TimeSeriesBlock();
                                        tsb.submodel = sm;
                                        tsb.block = smec;
                                        tsb.samplesProperties = new List<AdminShell.Property>();
                                        tsb.samplesValues = new List<string>();
                                        tsb.opcLastTimeStamp = DateTime.UtcNow - TimeSpan.FromMinutes(1) + TimeSpan.FromMinutes(120);

                                        for (int iSmec = 0; iSmec < countSmec; iSmec++)
                                        {
                                            var sme2 = smec.value[iSmec].submodelElement;
                                            var idShort = sme2.idShort;
                                            if (idShort.Contains("opcNode"))
                                                idShort = "opcNode";
                                            switch (idShort)
                                            {
                                                case "sourceType":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.sourceType = (sme2 as AdminShell.Property).value;
                                                    }
                                                    break;
                                                case "sourceAddress":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.sourceAddress = (sme2 as AdminShell.Property).value;
                                                    }
                                                    break;
                                                case "username":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.username = (sme2 as AdminShell.Property).value;
                                                    }
                                                    break;
                                                case "password":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.password = (sme2 as AdminShell.Property).value;
                                                    }
                                                    break;
                                                case "data":
                                                    if (sme2 is AdminShell.SubmodelElementCollection)
                                                    {
                                                        tsb.data = sme2 as AdminShell.SubmodelElementCollection;
                                                    }
                                                    break;
                                                case "sampleStatus":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.sampleStatus = sme2 as AdminShell.Property;
                                                    }
                                                    break;
                                                case "sampleMode":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.sampleMode = sme2 as AdminShell.Property;
                                                    }
                                                    break;
                                                case "sampleRate":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.sampleRate = sme2 as AdminShell.Property;
                                                    }
                                                    break;
                                                case "maxSamples":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.maxSamples = sme2 as AdminShell.Property;
                                                    }
                                                    break;
                                                case "actualSamples":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.actualSamples = sme2 as AdminShell.Property;
                                                        tsb.actualSamples.value = "0";
                                                    }
                                                    break;
                                                case "maxSamplesInCollection":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.maxSamplesInCollection = sme2 as AdminShell.Property;
                                                    }
                                                    break;
                                                case "actualSamplesInCollection":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.actualSamplesInCollection = sme2 as AdminShell.Property;
                                                        tsb.actualSamplesInCollection.value = "0";
                                                    }
                                                    break;
                                                case "maxCollections":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.maxCollections = sme2 as AdminShell.Property;
                                                    }
                                                    break;
                                                case "actualCollections":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.actualCollections = sme2 as AdminShell.Property;
                                                        tsb.actualCollections.value = "0";
                                                    }
                                                    break;
                                                case "lowDataIndex":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.lowDataIndex = sme2 as AdminShell.Property;
                                                        tsb.lowDataIndex.value = "0";
                                                    }
                                                    break;
                                                case "highDataIndex":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        tsb.highDataIndex = sme2 as AdminShell.Property;
                                                    }
                                                    break;
                                                case "opcNode":
                                                    if (sme2 is AdminShell.Property)
                                                    {
                                                        string node = (sme2 as AdminShell.Property).value;
                                                        string[] split = node.Split(',');
                                                        if (tsb.opcNodes == null)
                                                            tsb.opcNodes = new List<string>();
                                                        tsb.opcNodes.Add(split[1] + "," + split[2]);
                                                        var p = AdminShell.Property.CreateNew(split[0]);
                                                        tsb.samplesProperties.Add(p);
                                                        p.TimeStampCreate = timeStamp;
                                                        p.setTimeStamp(timeStamp);
                                                        tsb.samplesValues.Add("");
                                                    }
                                                    break;
                                            }
                                            if (tsb.sourceType == "aas" && sme2 is AdminShell.ReferenceElement r)
                                            {
                                                var el = env.AasEnv.FindReferableByReference(r.value);
                                                if (el is AdminShell.Property p)
                                                {
                                                    tsb.samplesProperties.Add(p);
                                                    tsb.samplesValues.Add("");
                                                }
                                            }
                                        }
                                        if (tsb.sampleRate != null)
                                            tsb.threadCounter = Convert.ToInt32(tsb.sampleRate.value);
                                        timeSeriesBlockList.Add(tsb);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // test
            if (test)
            {
                dummy = 0;
                for (int i = 0; i < 500; i++)
                {
                    timeSeriesSampling(false);
                }
                timeSeriesSampling(true);
            }
            else
            {
                timeSeriesThread = new Thread(new ThreadStart(timeSeriesSamplingLoop));
                timeSeriesThread.Start();
            }
        }

        static bool test = false;

        static Thread timeSeriesThread;

        static int dummy = 0;

        public static void timeSeriesSamplingLoop()
        {
            /*
            while(timeSeriesSampling(false));
            timeSeriesSampling(true);
            */
            while (true)
            {
                timeSeriesSampling(false);
            }
        }

        /*
        static ulong ChangeNumber = 0;

        static bool setChangeNumber(AdminShell.Referable r, ulong changeNumber)
        {
            do
            {
                r.ChangeNumber = changeNumber;
                if (r != r.parent)
                {
                    r = r.parent;
                }
                else
                    r = null;
            }
            while (r != null);

            return true;
        }
        */

        public static bool timeSeriesSampling(bool final)
        {
            if (Program.isLoading)
                return true;

            // ulong newChangeNumber = ChangeNumber + 1;
            // bool useNewChangeNumber = false;
            DateTime timeStamp = DateTime.Now;

            foreach (var tsb in timeSeriesBlockList)
            {
                if (tsb.sampleStatus.value == "stop")
                {
                    tsb.sampleStatus.value = "stopped";
                    final = true;
                }
                else
                {
                    if (tsb.sampleStatus.value != "start")
                        continue;
                }

                if (tsb.sampleRate == null)
                    continue;

                tsb.threadCounter -= 100;
                if (tsb.threadCounter > 0)
                    continue;

                tsb.threadCounter = Convert.ToInt32(tsb.sampleRate.value);

                int actualSamples = Convert.ToInt32(tsb.actualSamples.value);
                int maxSamples = Convert.ToInt32(tsb.maxSamples.value);
                int actualSamplesInCollection = Convert.ToInt32(tsb.actualSamplesInCollection.value);
                int maxSamplesInCollection = Convert.ToInt32(tsb.maxSamplesInCollection.value);

                if (final || actualSamples < maxSamples)
                {
                    int updateMode = 0;
                    if (!final)
                    {
                        int valueCount = 1;
                        if (tsb.sourceType == "json" && tsb.sourceAddress != "")
                        {
                            AdminShell.SubmodelElementCollection c =
                                tsb.block.value.FindFirstIdShortAs<AdminShell.SubmodelElementCollection>("jsonData");
                            if (c == null)
                            {
                                c = new AdminShellV20.SubmodelElementCollection();
                                c.idShort = "jsonData";
                                c.TimeStampCreate = timeStamp;
                                tsb.block.Add(c);
                                c.setTimeStamp(timeStamp);
                            }
                            parseJSON(tsb.sourceAddress, "", "", c);

                            foreach (var el in c.value)
                            {
                                if (el.submodelElement is AdminShell.Property p)
                                {
                                    if (!tsb.samplesProperties.Contains(p))
                                    {
                                        tsb.samplesProperties.Add(p);
                                        tsb.samplesValues.Add("");
                                    }
                                }
                            }
                        }
                        if (tsb.sourceType == "opchd" && tsb.sourceAddress != "")
                        {
                            GetHistory(tsb);
                            valueCount = 0;
                            if (table != null)
                                valueCount = table.Count;
                        }
                        if (tsb.sourceType == "opcda" && tsb.sourceAddress != "")
                        {
                            valueCount = GetDAData(tsb);
                        }

                        DateTime dt;
                        int valueIndex = 0;
                        while (valueIndex < valueCount)
                        {
                            if (tsb.sourceType == "opchd" && tsb.sourceAddress != "")
                            {
                                dt = (DateTime)table[valueIndex][0];
                                Console.WriteLine(valueIndex + " " + dt + " " + table[valueIndex][1] + " " + table[valueIndex][2]);
                            }
                            else
                            {
                                dt = DateTime.Now;
                            }
                            if (tsb.samplesTimeStamp == "")
                            {
                                tsb.samplesTimeStamp += dt.ToString("yy-MM-dd HH:mm:ss.fff");
                            }
                            else
                            {
                                tsb.samplesTimeStamp += "," + dt.ToString("HH:mm:ss.fff");
                            }

                            for (int i = 0; i < tsb.samplesProperties.Count; i++)
                            {
                                if (tsb.samplesValues[i] != "")
                                {
                                    tsb.samplesValues[i] += ",";
                                }

                                if ((tsb.sourceType == "opchd" || tsb.sourceType == "opcda") && tsb.sourceAddress != "")
                                {
                                    if (tsb.sourceType == "opchd")
                                    {
                                        string value = "";
                                        if (table[valueIndex] != null && table[valueIndex][i + 1] != null)
                                            value = table[valueIndex][i + 1].ToString();
                                        tsb.samplesValues[i] += value;
                                    }
                                    if (tsb.sourceType == "opcda")
                                    {
                                        tsb.samplesValues[i] += opcDAValues[i];
                                        Console.WriteLine(tsb.opcNodes[i] + " " + opcDAValues[i]);
                                    }
                                }
                                else
                                {
                                    var p = tsb.samplesProperties[i];
                                    tsb.samplesValues[i] += p.value;
                                    // tsb.samplesValues[i] += dummy++;
                                }
                            }
                            tsb.samplesValuesCount++;
                            actualSamples++;
                            tsb.actualSamples.value = "" + actualSamples;
                            tsb.actualSamples.setTimeStamp(timeStamp);
                            actualSamplesInCollection++;
                            tsb.actualSamplesInCollection.value = "" + actualSamplesInCollection;
                            tsb.actualSamplesInCollection.setTimeStamp(timeStamp);
                            if (actualSamples >= maxSamples)
                            {
                                if (tsb.sampleMode.value == "continuous")
                                {
                                    var first =
                                        tsb.data.value.FindFirstIdShortAs<AdminShell.SubmodelElementCollection>(
                                            "data" + tsb.lowDataIndex.value);
                                    if (first != null)
                                    {
                                        actualSamples -= maxSamplesInCollection;
                                        tsb.actualSamples.value = "" + actualSamples;
                                        tsb.actualSamples.setTimeStamp(timeStamp);
                                        AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.add(
                                            first, "Remove", tsb.submodel, (ulong)timeStamp.Ticks);
                                        tsb.data.Remove(first);
                                        tsb.data.setTimeStamp(timeStamp);
                                        tsb.lowDataIndex.value = "" + (Convert.ToInt32(tsb.lowDataIndex.value) + 1);
                                        tsb.lowDataIndex.setTimeStamp(timeStamp);
                                        updateMode = 1;
                                    }
                                }
                            }
                            if (actualSamplesInCollection >= maxSamplesInCollection)
                            {
                                if (actualSamplesInCollection > 0)
                                {
                                    if (tsb.highDataIndex != null)
                                    {
                                        tsb.highDataIndex.value = "" + tsb.samplesCollectionsCount;
                                        tsb.highDataIndex.setTimeStamp(timeStamp);
                                    }
                                    var nextCollection = AdminShell.SubmodelElementCollection.CreateNew("data" + tsb.samplesCollectionsCount++);
                                    var p = AdminShell.Property.CreateNew("timeStamp");
                                    p.value = tsb.samplesTimeStamp;
                                    p.setTimeStamp(timeStamp);
                                    p.TimeStampCreate = timeStamp;
                                    tsb.samplesTimeStamp = "";
                                    nextCollection.Add(p);
                                    nextCollection.setTimeStamp(timeStamp);
                                    nextCollection.TimeStampCreate = timeStamp;
                                    for (int i = 0; i < tsb.samplesProperties.Count; i++)
                                    {
                                        p = AdminShell.Property.CreateNew(tsb.samplesProperties[i].idShort);
                                        nextCollection.Add(p);
                                        p.value = tsb.samplesValues[i];
                                        p.setTimeStamp(timeStamp);
                                        p.TimeStampCreate = timeStamp;
                                        tsb.samplesValues[i] = "";
                                    }
                                    tsb.data.Add(nextCollection);
                                    tsb.data.setTimeStamp(timeStamp);
                                    AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.add(
                                        nextCollection, "Add", tsb.submodel, (ulong)timeStamp.Ticks);
                                    tsb.samplesValuesCount = 0;
                                    actualSamplesInCollection = 0;
                                    tsb.actualSamplesInCollection.value = "" + actualSamplesInCollection;
                                    tsb.actualSamplesInCollection.setTimeStamp(timeStamp);
                                    updateMode = 1;
                                    var json = JsonConvert.SerializeObject(nextCollection, Newtonsoft.Json.Formatting.Indented,
                                                                        new JsonSerializerSettings
                                                                        {
                                                                            NullValueHandling = NullValueHandling.Ignore
                                                                        });
                                    Program.connectPublish(tsb.block.idShort + "." + nextCollection.idShort, json);
                                }
                            }
                            valueIndex++;
                        }
                    }
                    if (final || actualSamplesInCollection >= maxSamplesInCollection)
                    {
                        if (actualSamplesInCollection > 0)
                        {
                            if (tsb.highDataIndex != null)
                            {
                                tsb.highDataIndex.value = "" + tsb.samplesCollectionsCount;
                                tsb.highDataIndex.setTimeStamp(timeStamp);
                            }
                            var nextCollection = AdminShell.SubmodelElementCollection.CreateNew("data" + tsb.samplesCollectionsCount++);
                            var p = AdminShell.Property.CreateNew("timeStamp");
                            p.value = tsb.samplesTimeStamp;
                            p.setTimeStamp(timeStamp);
                            p.TimeStampCreate = timeStamp;
                            tsb.samplesTimeStamp = "";
                            nextCollection.Add(p);
                            nextCollection.setTimeStamp(timeStamp);
                            nextCollection.TimeStampCreate = timeStamp;
                            for (int i = 0; i < tsb.samplesProperties.Count; i++)
                            {
                                p = AdminShell.Property.CreateNew(tsb.samplesProperties[i].idShort);
                                p.value = tsb.samplesValues[i];
                                p.setTimeStamp(timeStamp);
                                p.TimeStampCreate = timeStamp;
                                tsb.samplesValues[i] = "";
                                nextCollection.Add(p);
                            }
                            tsb.data.Add(nextCollection);
                            tsb.data.setTimeStamp(timeStamp);
                            AasxRestServerLibrary.AasxRestServer.TestResource.eventMessage.add(
                                nextCollection, "Add", tsb.submodel, (ulong)timeStamp.Ticks);
                            tsb.samplesValuesCount = 0;
                            actualSamplesInCollection = 0;
                            tsb.actualSamplesInCollection.value = "" + actualSamplesInCollection;
                            tsb.actualSamplesInCollection.setTimeStamp(timeStamp);
                            updateMode = 1;
                        }
                    }
                    if (updateMode != 0)
                        Program.signalNewData(updateMode);
                }
            }

            if (!test)
                Thread.Sleep(100);

            return !final;
        }

        static void parseJSON(string url, string username, string password, AdminShell.SubmodelElementCollection c)
        {
            var handler = new HttpClientHandler();
            handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            var client = new HttpClient(handler);

            if (username != "" && password != "")
            {
                var authToken = System.Text.Encoding.ASCII.GetBytes(username + ":" + password);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                        Convert.ToBase64String(authToken));
            }

            Console.WriteLine("GetJSON: " + url);
            string response = client.GetStringAsync(url).Result;
            Console.WriteLine(response);

            if (response != "")
            {
                JObject parsed = JObject.Parse(response);
                Program.parseJson(c, parsed);
            }
        }

        static List<List<object>> table = null;
        static string ErrorMessage { get; set; }
        static UASampleClient opc = null;
        static Opc.Ua.Client.Session session = null;
        static DateTime startTime;
        static DateTime endTime;
        static List<string> opcDAValues = null;

        public static int GetDAData(TimeSeriesBlock tsb)
        {
            Console.WriteLine("Read OPC DA Data:");
            try
            {
                ErrorMessage = "";
                if (session == null)
                    Connect(tsb);
                if (session != null)
                {
                    opcDAValues = new List<string>();
                    for (int i = 0; i < tsb.opcNodes.Count; i++)
                    {
                        string[] split = tsb.opcNodes[i].Split(',');
                        string value = opc.ReadSubmodelElementValue(split[1], (ushort)Convert.ToInt32(split[0]));
                        opcDAValues.Add(value);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                return 0;
            }
            /*
            session?.Close();
            session?.Dispose();
            session = null;
            */

            return 1;
        }

        public static void GetHistory(TimeSeriesBlock tsb)
        {
            Console.WriteLine("Read OPC UA Historical Data:");
            try
            {
                ErrorMessage = "";
                startTime = tsb.opcLastTimeStamp;
                endTime = DateTime.UtcNow + TimeSpan.FromMinutes(120);
                tsb.opcLastTimeStamp = endTime;
                if (session == null)
                    Connect(tsb);
                GetData(tsb);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                session?.Close();
                session?.Dispose();
                session = null;
                opc = null;
            }
            /*
            session?.Close();
            session?.Dispose();
            session = null;
            */
        }
        public static void Connect(TimeSeriesBlock tsb)
        {
            if (opc == null)
                opc = new UASampleClient(tsb.sourceAddress, true, 10000, tsb.username, tsb.password);
            opc.ConsoleSampleClient().Wait();
            session = opc.session;
        }
        public static void GetData(TimeSeriesBlock tsb)
        {
            if (session != null)
            {
                ReadRawModifiedDetails details = new ReadRawModifiedDetails();
                details.StartTime = startTime;
                details.EndTime = endTime;
                details.NumValuesPerNode = 0;
                details.IsReadModified = false;
                details.ReturnBounds = true;

                var nodesToRead = new HistoryReadValueIdCollection();
                for (int i = 0; i < tsb.opcNodes.Count; i++)
                {
                    var nodeToRead = new HistoryReadValueId();
                    string[] split = tsb.opcNodes[i].Split(',');
                    nodeToRead.NodeId = new NodeId(split[1], (ushort)Convert.ToInt32(split[0]));
                    nodesToRead.Add(nodeToRead);
                }

                table = new List<List<object>>();

                HistoryReadResultCollection results = null;
                DiagnosticInfoCollection diagnosticInfos = null;

                bool loop = true;
                while (loop)
                {
                    session.HistoryRead(
                        null,
                        new ExtensionObject(details),
                        TimestampsToReturn.Both,
                        false,
                        nodesToRead,
                        out results,
                        out diagnosticInfos);

                    ClientBase.ValidateResponse(results, nodesToRead);
                    ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

                    foreach (var res in results)
                    {
                        if (StatusCode.IsBad(res.StatusCode))
                        {
                            throw new ServiceResultException(res.StatusCode);
                        }
                    }

                    var historyData1 = ExtensionObject.ToEncodeable(results[0].HistoryData) as HistoryData;
                    var historyData2 = ExtensionObject.ToEncodeable(results[1].HistoryData) as HistoryData;
                    for (int i = 0; i < historyData1.DataValues.Count; i++)
                    {
                        var row = new List<object>();
                        row.Add(historyData1.DataValues[i].SourceTimestamp);
                        row.Add(historyData1.DataValues[i].Value);
                        row.Add(historyData2.DataValues[i].Value);
                        table.Add(row);
                    }

                    for (int i = 0; i < results.Count; i++)
                    {
                        if (results[i].ContinuationPoint == null || results[i].ContinuationPoint.Length == 0)
                        {
                            loop = false;
                            break;
                        }
                        nodesToRead[i].ContinuationPoint = results[i].ContinuationPoint;
                    }
                }
            }
        }
    }
}
