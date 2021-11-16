﻿#define MICHA

using AdminShellEvents;
using AdminShellNS;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0).
The Grapevine REST server framework is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

/* Please notice: the API and REST routes implemented in this version of the source code are not specified and standardised by the
specification Details of the Administration Shell. The hereby stated approach is solely the opinion of its author(s). */

namespace AasxRestServerLibrary
{
    public class AasxRestServer
    {
        public class TestResource
        {
            // test data server

            public static int varInt1 = -100; // -100..100
            public static int varInt2 = 0; // 0..10
            public static double varFloat3 = 0; // sin(varInt1/30);
            class testData
            {
                public int varInt1;
                public int varInt2;
                public float varFloat3;
            }

            void sendJson(HttpContext context, object o)
            {
                string json = JsonConvert.SerializeObject(o, Formatting.Indented);
                context.Response.ContentType = "application/json";
                context.Response.ContentLength = json.Length;
                context.Response.WriteAsync(json);
            }

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/data4(/|)$")]

            public HttpContext GetData4(HttpContext context)
            {
                varInt1++;
                if (varInt1 > 100)
                    varInt1 = -100;
                varInt2++;
                if (varInt2 > 10)
                    varInt2 = 0;
                // varFloat3 = Math.Sin(varInt1 * 180 / 100);
                varFloat3 = Math.Sin((1.0 * varInt1 / 360.0) * 10);

                testData td = new testData();
                td.varInt1 = varInt1;
                td.varInt2 = varInt2;
                td.varFloat3 = (float)varFloat3;

                sendJson(context, td);
                return context;
            }

            public class DeletedListItem
            {
                public AdminShell.Submodel sm;
                public AdminShell.Referable rf;
            }

            public static List<DeletedListItem> deletedList = new List<DeletedListItem>();
            public static DateTime olderDeletedTimeStamp = new DateTime();

            // get event messages
            public class eventMessage
            {
                public DateTime dt;
                public string operation = "";
                public string obj = "";
                public string data = "";

                public static void add(AdminShell.Referable o, string op, AdminShell.Submodel rootSubmodel, ulong changeCount)
                {
                    if (o is AdminShell.SubmodelElementCollection smec)
                    {
                        string json = "";

                        AasPayloadStructuralChangeItem.ChangeReason reason = AasPayloadStructuralChangeItem.ChangeReason.Create;
                        switch (op)
                        {
                            case "Add":
                                reason = AasPayloadStructuralChangeItem.ChangeReason.Create;
                                json = JsonConvert.SerializeObject(smec, Newtonsoft.Json.Formatting.Indented,
                                    new JsonSerializerSettings
                                    {
                                        NullValueHandling = NullValueHandling.Ignore
                                    });
                                break;
                            case "Remove":
                                reason = AasPayloadStructuralChangeItem.ChangeReason.Delete;
                                break;
                        }

                        rootSubmodel.SetAllParents();
                        AdminShell.KeyList keys = new AdminShellV20.KeyList();

#if MICHA
                        // keys were in the reverse order
                        keys = smec.GetReference()?.Keys;
                        if (keys?.IsEmpty == false)
                            keys.Remove(keys.Last());
#else

                        while (smec != null)
                        {
                            keys.Add(AdminShellV20.Key.CreateNew("SMEC", false, "SMEC", smec.idShort));
                            smec = (smec.parent as AdminShell.SubmodelElementCollection);
                        }
                        keys.Add(AdminShellV20.Key.CreateNew("SM", false, "SM", rootSubmodel.idShort));
#endif


                        AasPayloadStructuralChangeItem change = new AasPayloadStructuralChangeItem(
                            changeCount, o.TimeStamp, reason, keys, json);
                        changeClass.Changes.Add(change);
                        if (changeClass.Changes.Count > 100)
                            changeClass.Changes.RemoveAt(0);

                        if (op == "Remove")
                        {
                            o.TimeStamp = DateTime.Now;
                            AdminShell.Referable x = o;
                            /*
                            string path = x.idShort;
                            while (x.parent != null && x != x.parent)
                            {
                                x = x.parent;
                                path = x.idShort + "." + path;
                            }
                            o.idShort = path;
                            */
                            deletedList.Add(new DeletedListItem() { sm = rootSubmodel, rf = o });
                            if (deletedList.Count > 1000 && deletedList[0].rf != null)
                            {
                                olderDeletedTimeStamp = deletedList[0].rf.TimeStamp;
                                deletedList.RemoveAt(0);
                            }
                        }
                    }
                }
            }

            public static AasPayloadStructuralChange changeClass = new AasPayloadStructuralChange();
            // public static int eventsCount = 0;

            private static bool _setAllParentsExecuted = false;

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/geteventmessages(/|)$")]
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/geteventmessages/values(/|)$")]
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/geteventmessages/time/([^/]+)(/|)$")]
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/geteventmessages/deltasecs/(\\d+)(/|)$")]

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/geteventmessages/aas/([^/]+)(/|)$")]
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/geteventmessages/aas/([^/]+)/values(/|)$")]
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/geteventmessages/aas/([^/]+)/time/([^/]+)(/|)$")]
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/geteventmessages/aas/([^/]+)/deltasecs/(\\d+)(/|)$")]

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/aas/([^/]+)/geteventmessages(/|)$")]
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/aas/([^/]+)/geteventmessages/values(/|)$")]
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/aas/([^/]+)/geteventmessages/time/([^/]+)(/|)$")]
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/aas/([^/]+)/geteventmessages/deltasecs/(\\d+)(/|)$")]

            public HttpContext GetEventMessages(HttpContext context)
            {
                //
                // Configuration of operation mode
                //

                DateTime minimumDate = new DateTime();
                bool doUpdate = true;
                bool doCreateDelete = true;
                string restPath = context.Request.Path;
                int aasIndex = -1;

                if (restPath.Contains("/aas/"))
                {
                    // specific AAS
                    string[] split = restPath.Split('/');
                    if (split[2] == "aas")
                    {
                        try
                        {
                            if (!int.TryParse(split[3], out aasIndex))
                                aasIndex = -1;
                            if (aasIndex >= 0)
                            {
                                restPath = "";
                                for (int i = 1; i < split.Length; i++)
                                {
                                    if (i != 2 && i != 3)
                                    {
                                        restPath += "/" + split[i];
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    if (split[1] == "aas")
                    {
                        try
                        {
                            if (!int.TryParse(split[2], out aasIndex))
                                aasIndex = -1;
                            if (aasIndex >= 0)
                            {
                                restPath = "";
                                for (int i = 1; i < split.Length; i++)
                                {
                                    if (i != 1 && i != 2)
                                    {
                                        restPath += "/" + split[i];
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                if (restPath.Contains("/values"))
                {
                    doCreateDelete = false;
                }
                else
                {
                    if (restPath.StartsWith("/geteventmessages/time/"))
                    {
                        try
                        {
                            minimumDate = DateTime.Parse(restPath.Substring("/geteventmessages/time/".Length));
                        }
                        catch { }
                    }
                    if (restPath.StartsWith("/geteventmessages/deltasecs/"))
                    {
                        try
                        {
                            var secs = restPath.Substring("/geteventmessages/deltasecs/".Length);
                            if (int.TryParse(secs, out int i))
                                minimumDate = DateTime.Now.AddSeconds(-1.0 * i);
                        }
                        catch { }
                    }
                }

                //
                // Set parents for all childs.
                // Note: this has to be done only once for AASX Server, therefore a better place than
                // here could be figured out
                //

                if (!_setAllParentsExecuted)
                {
                    _setAllParentsExecuted = true;

                    if (AasxServer.Program.env != null)
                        foreach (var e in AasxServer.Program.env)
                            if (e?.AasEnv?.Submodels != null)
                                foreach (var sm in e.AasEnv.Submodels)
                                    if (sm != null)
                                        sm.SetAllParents();
                }

                //
                // Restructuring of sourece code
                // * outer loop is over all AAS-Env and Submodels
                // * find event elements in Submodels
                // * send deletes
                // * send creates & updates
                //

                var envelopes = new List<AasEventMsgEnvelope>();

                int aascount = AasxServer.Program.env.Length;
                for (int i = 0; i < aascount; i++)
                {
                    if (aasIndex >= 0 && i != aasIndex)
                        continue;

                    var env = AasxServer.Program.env[i];
                    if (env?.AasEnv?.AdministrationShells == null)
                        continue;

                    foreach (var aas in env.AasEnv.AdministrationShells)
                    {
                        if (aas?.submodelRefs == null)
                            continue;

                        foreach (var smr in aas.submodelRefs)
                        {
                            // find Submodel
                            var sm = env.AasEnv.FindSubmodel(smr);
                            if (sm == null)
                                continue;

                            // find a matching event element
                            foreach (var bev in sm.FindDeep<AdminShell.BasicEvent>())
                            {
                                // find interesting event?
                                if (true == bev.semanticId?.MatchesExactlyOneKey(
                                    type: AdminShell.Key.ConceptDescription,
                                    local: false,
                                    idType: AdminShell.Identification.IRI,
                                    id: "https://admin-shell.io/tmp/AAS/Events/UpdateValueOutwards",
                                    matchMode: AdminShellV20.Key.MatchMode.Relaxed))
                                {
                                    doUpdate = true;
                                    doCreateDelete = false;
                                }
                                else
                                if (true == bev.semanticId?.MatchesExactlyOneKey(
                                    type: AdminShell.Key.ConceptDescription,
                                    local: false,
                                    idType: AdminShell.Identification.IRI,
                                    id: "https://admin-shell.io/tmp/AAS/Events/StructureChangeOutwards",
                                    matchMode: AdminShellV20.Key.MatchMode.Relaxed))
                                {
                                    doUpdate = false;
                                    doCreateDelete = true;
                                }
                                else
                                    continue;

                                // find obseverved as well
                                if (bev.observed == null && bev.observed.Count < 1)
                                    continue;
                                var obs = env.AasEnv.FindReferableByReference(bev.observed);
                                if (obs == null)
                                    continue;

                                // obseverved semantic id is pain in the ..
                                AdminShell.SemanticId obsSemId = null;
                                if (obs is AdminShell.Submodel obssm)
                                    obsSemId = obssm.semanticId;
                                if (obs is AdminShell.SubmodelElement obssme)
                                    obsSemId = obssme.semanticId;

                                //
                                // Create event outer message
                                //

                                var eventsOuter = new AasEventMsgEnvelope(
                                        DateTime.UtcNow,
                                        source: bev.GetReference(),
                                        sourceSemanticId: bev.semanticId,
                                        observableReference: bev.observed,
                                        observableSemanticId: obsSemId);

                                // directly create lists of update value and structural change events

                                var plStruct = new AasPayloadStructuralChange();
                                var plUpdate = new AasPayloadUpdateValue();

                                string[] modes = { "CREATE", "UPDATE" };

                                //
                                // Check for deletes
                                //

                                if (doCreateDelete)
                                {
                                    foreach (var d in deletedList)
                                    {
                                        if (d.rf == null || d.sm != sm)
                                            continue;
                                        if (d.rf.TimeStamp > minimumDate)
                                        {
                                            // get the path
                                            AdminShell.KeyList p2 = null;
                                            if (d.rf is AdminShell.Submodel delsm)
                                                p2 = delsm?.GetReference()?.Keys;
                                            if (d.rf is AdminShell.SubmodelElement delsme)
                                                p2 = delsme?.GetReference()?.Keys;
                                            if (p2 == null)
                                                continue;

                                            // prepare p2 to be relative path to observable
                                            if (true == p2?.StartsWith(bev.observed?.Keys, matchMode: AdminShellV20.Key.MatchMode.Relaxed))
                                                p2.RemoveRange(0, bev.observed.Keys.Count);

                                            // make payload
                                            var pliDel = new AasPayloadStructuralChangeItem(
                                                count: 1,
                                                timeStamp: d.rf.TimeStamp,
                                                AasPayloadStructuralChangeItem.ChangeReason.Delete,
                                                path: p2);

                                            // add
                                            plStruct.Changes.Add(pliDel);
                                        }
                                    }
                                }
                                else
                                {
                                }

                                //
                                // Create & update
                                //

                                //for (int imode = 0; imode < modes.Length; imode++)
                                //{
                                if ((doCreateDelete || doUpdate) == false)
                                    throw new Exception("invalid flags");

                                DateTime diffTimeStamp = sm.TimeStamp;
                                var strMode = "";
                                if (doCreateDelete)
                                    strMode = "CREATE";
                                if (doUpdate)
                                    strMode = "UPDATE";
                                if (strMode != "")
                                    if (diffTimeStamp > minimumDate)
                                    {
                                        ;
                                        foreach (var sme in sm.submodelElements)
                                            GetEventMsgRecurseDiff(
                                                strMode,
                                                plStruct, plUpdate,
                                                sme.submodelElement,
                                                minimumDate, doUpdate, doCreateDelete,
                                                bev.observed?.Keys);
                                    }
                                //}

                                // prepare message envelope and remember

                                if (plStruct.Changes.Count > 0)
                                    eventsOuter.Payloads.Add(plStruct);

                                if (plUpdate.Values.Count > 0)
                                    eventsOuter.Payloads.Add(plUpdate);

                                if (eventsOuter.Payloads.Count > 0)
                                    envelopes.Add(eventsOuter);
                            } // matching events
                        } // submodels
                    } // AAS
                } // AAS-ENV

                //
                // Serialize event message and send
                //

                SendJsonResponse(context, envelopes.ToArray());

                return context;
            }

            static void GetEventMsgRecurseDiff(
                string mode,
                AasPayloadStructuralChange plStruct,
                AasPayloadUpdateValue plUpdate,
                AdminShell.SubmodelElement sme, DateTime minimumDate,
                bool doUpdate, bool doCreateDelete,
                AdminShell.KeyList observablePath = null)
            {
                DateTime diffTimeStamp;

                if (!(sme is AdminShell.SubmodelElementCollection))
                {
                    if (mode == "CREATE")
                        diffTimeStamp = sme.TimeStampCreate;
                    else // UPDATE
                        diffTimeStamp = sme.TimeStamp;
                    if (diffTimeStamp > minimumDate)
                    {
                        // prepare p2 to be relative path to observable
                        var p2 = sme.GetReference()?.Keys;
                        if (true == p2?.StartsWith(observablePath, matchMode: AdminShellV20.Key.MatchMode.Relaxed))
                            p2.RemoveRange(0, observablePath.Count);

                        if (mode == "CREATE")
                        {
                            if (/* doCreateDelete && */ plStruct != null)
                                plStruct.Changes.Add(new AasPayloadStructuralChangeItem(
                                    count: 1,
                                    timeStamp: sme.TimeStamp,
                                    AasPayloadStructuralChangeItem.ChangeReason.Create,
                                    path: p2,
                                    // Assumption: models will be serialized correctly
                                    data: JsonConvert.SerializeObject(sme)));
                        }
                        else
                        if (sme.TimeStamp != sme.TimeStampCreate)
                        {
                            if (/* doUpdate && */ plUpdate != null)
                            {
                                var val = sme.ValueAsText();
                                if (sme is AdminShell.Blob blob)
                                    // take BLOB as "large" text
                                    val = blob.value;
                                plUpdate.Values.Add(new AasPayloadUpdateValueItem(
                                    path: p2,
                                    val));
                            }
                        }
                    }

                    return;
                }

                var smec = sme as AdminShell.SubmodelElementCollection;
                diffTimeStamp = smec.TimeStamp;
                if (smec.TimeStamp > minimumDate)
                {
                    // TODO: check if to modify to send serializations of whole SMCs on CREATE
                    if (mode == "CREATE" || smec.TimeStamp != smec.TimeStampCreate)
                    {
                        bool deeper = false;
                        if (doUpdate /* && !doCreateDelete */)
                        {
                            deeper = true;
                        }
                        else
                        {
                            foreach (var sme2 in smec.value)
                                if (sme2.submodelElement.TimeStamp != smec.TimeStamp)
                                {
                                    deeper = true;
                                    break;
                                }
                        }

                        if (deeper)
                        {
                            foreach (var sme2 in smec.value)
                                GetEventMsgRecurseDiff(
                                    mode,
                                    plStruct, plUpdate,
                                    sme2.submodelElement, minimumDate, doUpdate, doCreateDelete, observablePath);
                            return;
                        }

                        // prepare p2 to be relative path to observable
                        var p2 = sme.GetReference()?.Keys;
                        if (true == p2?.StartsWith(observablePath, matchMode: AdminShellV20.Key.MatchMode.Relaxed))
                            p2.RemoveRange(0, observablePath.Count);

                        if (mode == "CREATE")
                        {
                            if (sme.TimeStampCreate > minimumDate)
                            {
                                if (/* doCreateDelete && */ plStruct != null)
                                    plStruct.Changes.Add(new AasPayloadStructuralChangeItem(
                                        count: 1,
                                        timeStamp: sme.TimeStamp,
                                        AasPayloadStructuralChangeItem.ChangeReason.Create,
                                        path: p2,
                                        // Assumption: models will be serialized correctly
                                        data: JsonConvert.SerializeObject(sme)));
                            }
                        }
                        else
                        if (sme.TimeStamp != sme.TimeStampCreate)
                        {
                            if (/* doUpdate && */ plUpdate != null)
                                plUpdate.Values.Add(new AasPayloadUpdateValueItem(
                                    path: p2,
                                    sme.ValueAsText()));
                        }
                    }
                }
            }

            public static void SendJsonResponse(HttpContext context, object obj)
            {
                // make JSON
                var settings = AasxIntegrationBase.AasxPluginOptionSerialization.GetDefaultJsonSettings(
                    new[] { typeof(AdminShellEvents.AasEventMsgEnvelope) });
                settings.TypeNameHandling = TypeNameHandling.Auto;
                settings.Formatting = Formatting.Indented;
                var json = JsonConvert.SerializeObject(obj, settings);

                context.Response.ContentType = "application/json";
                context.Response.ContentLength = json.Length;
                context.Response.WriteAsync(json);
            }

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/diff/([^/]+)(/|)$")]
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/diff(/|)$")]
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/diff/update(/|)$")]
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/diff/update/([^/]+)(/|)$")]

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/diff/aas/([^/]+)/time/([^/]+)(/|)$")]
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/diff/aas/([^/]+)(/|)$")]
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/diff/aas/([^/]+)/update(/|)$")]
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/diff/aas/([^/]+)/update/([^/]+)(/|)$")]

            public HttpContext GetDiff(HttpContext context)
            {
                DateTime minimumDate = new DateTime();
                bool updateOnly = false;
                int seconds = 0;
                string searchPath = "";

                var queryString = context.Request.QueryString;
                string auto = queryString.Value;
                if (auto != null && auto == "auto")
                {
                    try
                    {
                        seconds = Convert.ToInt32(auto);
                        minimumDate = DateTime.Now - new TimeSpan(0, 0, seconds);
                    }
                    catch { }
                }

                string restPath = context.Request.Path;

                int aasIndex = -1;

                if (restPath.Contains("/aas/"))
                {
                    // specific AAS
                    string[] split = restPath.Split('/');
                    if (split[2] == "aas")
                    {
                        try
                        {
                            if (!int.TryParse(split[3], out aasIndex))
                                aasIndex = -1;
                            if (aasIndex >= 0)
                            {
                                restPath = "";
                                for (int i = 1; i < split.Length; i++)
                                {
                                    if (i != 2 && i != 3)
                                    {
                                        restPath += "/" + split[i];
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                if (restPath.Contains("/diff/update"))
                {
                    updateOnly = true;
                    if (restPath.Contains("/diff/update/"))
                    {
                        try
                        {
                            searchPath = restPath.Substring("/diff/update/".Length);
                        }
                        catch { }
                    }
                }
                else
                {
                    if (restPath.Contains("/diff/time/"))
                    {
                        try
                        {
                            minimumDate = DateTime.Parse(restPath.Substring("/diff/time/".Length));
                        }
                        catch { }
                    }
                }

                string diffText = "<table border=1 cellpadding=4><tbody>";
                string[] modes = { "CREATE", "UPDATE" };

                if (!updateOnly)
                {
                    if (olderDeletedTimeStamp > minimumDate)
                        diffText += "<tr><td>DELETE</td><td><b>***Deleted_items_before***</b></td><td>ERROR</td><td>" +
                                olderDeletedTimeStamp.ToString("yy-MM-dd HH:mm:ss.fff") + "</td></tr>";

                    foreach (var d in deletedList)
                    {
                        if (d.rf == null)
                            continue;
                        if (d.rf.TimeStamp > minimumDate)
                        {
                            var x = d.rf;
                            string path = x.idShort;
                            while (x.parent != null && x != x.parent)
                            {
                                x = x.parent;
                                path = x.idShort + "." + path;
                            }
                            diffText += "<tr><td>DELETE</td><td><b>" + path + "</b></td><td>SMEC</td><td>" +
                                d.rf.TimeStamp.ToString("yy-MM-dd HH:mm:ss.fff") + "</td></tr>";
                        }
                    }
                }
                else
                {
                    string[] modesUpdate = { "UPDATE" };
                    modes = modesUpdate;
                }

                int aascount = AasxServer.Program.env.Length;

                for (int imode = 0; imode < modes.Length; imode++)
                {
                    for (int i = 0; i < aascount; i++)
                    {
                        if (aasIndex >= 0 && i != aasIndex)
                            continue;

                        var env = AasxServer.Program.env[i];
                        if (env != null)
                        {
                            var aas = env.AasEnv.AdministrationShells[0];
                            if (aas.submodelRefs != null && aas.submodelRefs.Count > 0)
                            {
                                DateTime diffTimeStamp = new DateTime();
                                diffTimeStamp = aas.TimeStamp;
                                if (diffTimeStamp > minimumDate)
                                {
                                    string mode = modes[imode];
                                    if (mode == "CREATE" || aas.TimeStamp != aas.TimeStampCreate)
                                    {
                                        if (searchPath == "" || aas.idShort.Contains(searchPath))
                                        {
                                            diffText += "<tr><td>" + mode + "</td><td><b>" + aas.idShort +
                                                "</b></td><td>AAS</td><td>" +
                                                    aas.TimeStamp.ToString("yy-MM-dd HH:mm:ss.fff") + "</td>";
                                            diffText += "</tr>";
                                        }
                                    }
                                }

                                foreach (var smr in aas.submodelRefs)
                                {
                                    var sm = env.AasEnv.FindSubmodel(smr);
                                    if (sm != null && sm.idShort != null)
                                    {
                                        diffTimeStamp = sm.TimeStamp;
                                        if (diffTimeStamp > minimumDate)
                                        {
                                            string mode = modes[imode];
                                            if (mode == "CREATE" || sm.TimeStamp != sm.TimeStampCreate)
                                            {
                                                if (searchPath == "" || (aas.idShort + "." + sm.idShort).Contains(searchPath))
                                                {
                                                    diffText += "<tr><td>" + mode + "</td><td><b>" + aas.idShort + "." + sm.idShort +
                                                        "</b></td><td>SM</td><td>" +
                                                            sm.TimeStamp.ToString("yy-MM-dd HH:mm:ss.fff") + "</td>";
                                                    diffText += "</tr>";
                                                }
                                            }

                                            foreach (var sme in sm.submodelElements)
                                                diffText += checkDiff(modes[imode], aas.idShort + "." + sm.idShort + ".", sme.submodelElement,
                                                    minimumDate, updateOnly, searchPath);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                diffText += "</tbody></table>";

                context.Response.ContentType = "application/html";
                context.Response.ContentLength = diffText.Length;
                context.Response.WriteAsync(diffText);

                return context;
            }

            static string checkDiff(string mode, string path, AdminShell.SubmodelElement sme,
                DateTime minimumDate, bool updateOnly, string searchPath)
            {
                DateTime diffTimeStamp;

                if (!(sme is AdminShell.SubmodelElementCollection))
                {
                    if (mode == "CREATE")
                        diffTimeStamp = sme.TimeStampCreate;
                    else // UPDATE
                        diffTimeStamp = sme.TimeStamp;
                    if (diffTimeStamp > minimumDate)
                    {
                        if (mode == "CREATE" || sme.TimeStamp != sme.TimeStampCreate)
                        {
                            if (searchPath != "")
                            {
                                if (!(path + sme.idShort).Contains(searchPath))
                                    return "";
                            }
                            string text = "<tr><td>" + mode + "</td><td><b>" + path + sme.idShort + "</b></td><td>SME</td><td>" +
                                sme.TimeStamp.ToString("yy-MM-dd HH:mm:ss.fff") + "</td>";
                            if (updateOnly)
                                text += "<td><b>" + sme.ValueAsText() + "</b></td>";
                            text += "</tr>";
                            return text;
                        }
                    }

                    return "";
                }

                var smec = sme as AdminShell.SubmodelElementCollection;
                diffTimeStamp = smec.TimeStamp;
                if (smec.TimeStamp > minimumDate)
                {
                    if (mode == "CREATE" || smec.TimeStamp != smec.TimeStampCreate)
                    {
                        bool deeper = false;
                        if (updateOnly)
                        {
                            deeper = true;
                        }
                        else
                        {
                            foreach (var sme2 in smec.value)
                                if (sme2.submodelElement.TimeStamp != smec.TimeStamp)
                                {
                                    deeper = true;
                                    break;
                                }
                        }

                        if (deeper)
                        {
                            string text = "";
                            foreach (var sme2 in smec.value)
                                text += checkDiff(mode, path + sme.idShort + ".", sme2.submodelElement,
                                    minimumDate, updateOnly, searchPath);
                            return text;
                        }

                        return "<tr><td>" + mode + "</td><td><b>" + path + smec.idShort + "</b></td><td>SMEC</td><td>" +
                            smec.TimeStamp.ToString("yy-MM-dd HH:mm:ss.fff") + "</td></tr>";
                    }
                }

                return "";
            }

            public static AasxHttpContextHelper helper = null;

            // get authserver

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/authserver(/|)$")]

            public HttpContext GetAuthserver(HttpContext context)
            {
                var txt = AasxServer.Program.redirectServer;

                context.Response.ContentType = "application/text";
                context.Response.ContentLength = txt.Length;
                context.Response.WriteAsync(txt);

                return context;
            }

            // Basic AAS + Asset

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/aas/(id|([^/]+))(|/core|/complete|/thumbnail|/aasenv)(/|)$")]

            public HttpContext GetAasAndAsset(HttpContext context)
            {
                if (context.Request.Path.Value.Contains("geteventmessages"))
                {
                    return GetEventMessages(context);
                }

                if (helper.PathEndsWith(context, "thumbnail"))
                {
                    helper.EvalGetAasThumbnail(context, m.Groups[1].ToString());
                }
                else
                if (helper.PathEndsWith(context, "aasenv"))
                {
                    helper.EvalGetAasEnv(context, m.Groups[1].ToString());
                }
                else
                {
                    var complete = helper.PathEndsWith(context, "complete");
                    helper.EvalGetAasAndAsset(context, m.Groups[1].ToString(), complete: complete);
                }

                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.PUT, Path = "^/aas(/|)$")]
            public HttpContext PutAas(HttpContext context)
            {
                helper.EvalPutAas(context);
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.PUT, Path = "^/aasx/server(/|)$")]
            public HttpContext PutAasxOnServer(HttpContext context)
            {
                helper.EvalPutAasxOnServer(context);
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.PUT, Path = "^/aasx/filesystem/([^/]+)(/|)$")]
            public HttpContext PutAasxToFileSystem(HttpContext context)
            {
                helper.EvalPutAasxToFilesystem(context, m.Groups[1].ToString());
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.DELETE, Path = "^/aas/([^/]+)(/|)$")]
            public HttpContext DeleteAasAndAsset(HttpContext context)
            {
                helper.EvalDeleteAasAndAsset(context, m.Groups[1].ToString(), deleteAsset: true);
                return context;
            }

            // Handles

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/handles/identification(/|)$")]
            public HttpContext GetHandlesIdentification(HttpContext context)
            {
                helper.EvalGetHandlesIdentification(context);
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.POST, Path = "^/handles/identification(/|)$")]
            public HttpContext PostHandlesIdentification(HttpContext context)
            {
                helper.EvalPostHandlesIdentification(context);
                return context;
            }

            // Authenticate

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/authenticateGuest(/|)$")]
            public HttpContext GetAuthenticate(HttpContext context)
            {
                helper.EvalGetAuthenticateGuest(context);
                return context;
            }

            // Authenticate User

            //[RestRoute(HttpMethod = HttpMethod.POST, Path = "^/authenticateUser(/|)$")]
            public HttpContext PostAuthenticateUser(HttpContext context)
            {
                helper.EvalPostAuthenticateUser(context);
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.POST, Path = "^/authenticateCert1(/|)$")]
            public HttpContext PostAuthenticateCert1(HttpContext context)
            {
                helper.EvalPostAuthenticateCert1(context);
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.POST, Path = "^/authenticateCert2(/|)$")]
            public HttpContext PostAuthenticateCert2(HttpContext context)
            {
                helper.EvalPostAuthenticateCert2(context);
                return context;
            }

            // Server

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/server/profile(/|)$")]
            public HttpContext GetServerProfile(HttpContext context)
            {
                helper.EvalGetServerProfile(context);
                return context;
            }

            // OZ
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/server/listaas(/|)$")]
            public HttpContext GetServerAASX(HttpContext context)
            {
                helper.EvalGetListAAS(context);
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = @"^/assetid/(\d+)(/|)$")]
            public HttpContext AssetId(HttpContext context)
            {
                helper.EvalAssetId(context, Int32.Parse(m.Groups[1].ToString()));
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = @"^/server/getaasx/(\d+)(/|)$")]
            public HttpContext GetAASX(HttpContext context)
            {
                helper.EvalGetAASX(context, Int32.Parse(m.Groups[1].ToString()));
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.PUT, Path = @"^/server/getaasx/(\d+)(/|)$")]
            public HttpContext PutAASX(HttpContext context)
            {
                helper.EvalPutAasxReplacePackage(context, m.Groups[1].ToString());
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = @"^/server/getaasxbyassetid/([^/]+)(/|)$")]
            public HttpContext GetAASX2ByAssetId(HttpContext context)
            {
                helper.EvalGetAasxByAssetId(context);
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = @"^/server/getaasx2/(\d+)(/|)$")]
            public HttpContext GetAASX2(HttpContext context)
            {
                helper.EvalGetAASX2(context, Int32.Parse(m.Groups[1].ToString()));
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = @"^/server/getfile/(\d+)/aasx/(([^/]+)/){0,99}([^/]+)$")]
            public HttpContext GetFile(HttpContext context)
            {
                int index = -1;
                string path = "/aasx";

                string[] split = context.Request.Path.Value.Split(new Char[] { '/' });
                if (split[1].ToLower() == "server" && split[2].ToLower() == "getfile")
                {
                    index = Int32.Parse(split[3]);
                    for (int i = 5; i < split.Length; i++)
                    {
                        path += "/" + split[i];
                    }
                }

                helper.EvalGetFile(context, index, path);

                return context;
            }

            // Assets

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/assets/([^/]+)(/|)$")]
            public HttpContext GetAssets(HttpContext context)
            {
                helper.EvalGetAssetLinks(context, m.Groups[1].ToString());
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.PUT, Path = "^/assets(/|)$")]
            public HttpContext PutAssets(HttpContext context)
            {
                helper.EvalPutAsset(context);
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.PUT, Path = "^/aas/(id|([^/]+))/asset(/|)$")]
            public HttpContext PutAssetsToAas(HttpContext context)
            {
                helper.EvalPutAssetToAas(context, m.Groups[1].ToString());
                return context;
            }

            // List of Submodels

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/aas/(id|([^/]+))/submodels(/|)$")]
            public HttpContext GetSubmodels(HttpContext context)
            {
                helper.EvalGetSubmodels(context, m.Groups[1].ToString());
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.PUT, Path = "^/aas/(id|([^/]+))/submodels(/|)$")]
            public HttpContext PutSubmodel(HttpContext context)
            {
                helper.EvalPutSubmodel(context, m.Groups[1].ToString());
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.DELETE, Path = "^/aas/(id|([^/]+))/submodels/([^/]+)(/|)$")]
            public HttpContext DeleteSubmodel(HttpContext context)
            {
                helper.EvalDeleteSubmodel(context, m.Groups[1].ToString(), m.Groups[3].ToString());
                return context;
            }

            // Contents of a Submodel

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/aas/(id|([^/]+))/submodels/([^/]+)(|/core|/deep|/complete|/values)(/|)$")]
            public HttpContext GetSubmodelContents(HttpContext context)
            {
                var aasid = m.Groups[1].ToString();
                var smid = m.Groups[3].ToString();

                if (helper.PathEndsWith(context, "values"))
                {
                    helper.EvalGetSubmodelAllElementsProperty(context, aasid, smid, elemids: null);
                }
                else
                {
                    var deep = helper.PathEndsWith(context, "deep");
                    var complete = helper.PathEndsWith(context, "complete");
                    helper.EvalGetSubmodelContents(context, aasid, smid, deep: deep || complete, complete: complete);
                }

                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/aas/(id|([^/]+))/submodels/([^/]+)/table(/|)$")]
            public HttpContext GetSubmodelContentsAsTable(HttpContext context)
            {
                helper.EvalGetSubmodelContentsAsTable(context, m.Groups[1].ToString(), m.Groups[3].ToString());
                return context;
            }

            // Contents of SubmodelElements

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/aas/(id|([^/]+))/submodels/([^/]+)/submodel/submodelElements(/([^/]+)){1,99}?(|/core|/complete|/deep|/file|/blob|/events|/values/value)(/|)$")] // BaSyx-Style
            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/aas/(id|([^/]+))/submodels/([^/]+)/elements(/([^/]+)){1,99}?(|/core|/complete|/deep|/file|/blob|/events|/values|/value)(/|)$")]
            public HttpContext GetSubmodelElementsContents(HttpContext context)
            {
                var aasid = m.Groups[1].ToString();
                var smid = m.Groups[3].ToString();
                var elemids = new List<string>();
                for (int i = 0; i < m.Groups[5].Captures.Count; i++)
                    elemids.Add(m.Groups[5].Captures[i].ToString());

                // special case??
                if (helper.PathEndsWith(context, "file"))
                {
                    helper.EvalGetSubmodelElementsFile(context, aasid, smid, elemids.ToArray());
                }
                else
                if (helper.PathEndsWith(context, "blob"))
                {
                    helper.EvalGetSubmodelElementsBlob(context, aasid, smid, elemids.ToArray());
                }
                else
                if (helper.PathEndsWith(context, "values") || helper.PathEndsWith(context, "value"))
                {
                    helper.EvalGetSubmodelAllElementsProperty(context, aasid, smid, elemids.ToArray());
                }
                else
                if (helper.PathEndsWith(context, "events"))
                {
                    context.Response.StatusCode = (int) HttpStatusCode.NotImplemented;
                }
                else
                {
                    // more options
                    bool complete = false, deep = false;
                    if (helper.PathEndsWith(context, "deep"))
                        deep = true;
                    if (helper.PathEndsWith(context, "complete"))
                    {
                        deep = true;
                        complete = true;
                    }

                    helper.EvalGetSubmodelElementContents(context, aasid, smid, elemids.ToArray(), deep, complete);
                }
            }

            //[RestRoute(HttpMethod = HttpMethod.POST, Path = "^/aas/(id|([^/]+))/submodels/([^/]+)/elements(/([^/]+)){1,99}?/invoke(/|)$")]
            public HttpContext PostSubmodelElementsContents(HttpContext context)
            {
                var aasid = m.Groups[1].ToString();
                var smid = m.Groups[3].ToString();
                var elemids = new List<string>();
                for (int i = 0; i < m.Groups[5].Captures.Count; i++)
                    elemids.Add(m.Groups[5].Captures[i].ToString());

                // special case??
                if (helper.PathEndsWith(context, "invoke"))
                {
                    helper.EvalInvokeSubmodelElementOperation(context, aasid, smid, elemids.ToArray());
                }

                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.PUT, Path = "^/aas/(id|([^/]+))/submodels/([^/]+)/elements(/([^/]+)){0,99}?(/|)$")]
            public HttpContext PutSubmodelElementsContents(HttpContext context)
            {
                var aasid = m.Groups[1].ToString();
                var smid = m.Groups[3].ToString();
                var elemids = new List<string>();
                if (m.Groups[5].Captures != null)
                    for (int i = 0; i < m.Groups[5].Captures.Count; i++)
                        elemids.Add(m.Groups[5].Captures[i].ToString());

                helper.EvalPutSubmodelElementContents(context, aasid, smid, elemids.ToArray());

                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.DELETE, Path = "^/aas/(id|([^/]+))/submodels/([^/]+)/elements(/([^/]+)){0,99}?(/|)$")]
            public HttpContext DeleteSubmodelElementsContents(HttpContext context)
            {
                var aasid = m.Groups[1].ToString();
                var smid = m.Groups[3].ToString();
                var elemids = new List<string>();
                if (m.Groups[5].Captures != null)
                    for (int i = 0; i < m.Groups[5].Captures.Count; i++)
                        elemids.Add(m.Groups[5].Captures[i].ToString());

                helper.EvalDeleteSubmodelElementContents(context, aasid, smid, elemids.ToArray());

                return context;
            }

            // concept descriptions

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/aas/(id|([^/]+))/cds(/|)$")]
            public HttpContext GetCds(HttpContext context)
            {
                helper.EvalGetAllCds(context, m.Groups[1].ToString());
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.PUT, Path = "^/aas/(id|([^/]+))/cds(/|)$")]
            public HttpContext PutConceptDescription(HttpContext context)
            {
                helper.EvalPutCd(context, m.Groups[1].ToString());
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.GET, Path = "^/aas/(id|([^/]+))/cds/([^/]+)(/|)$")]
            public HttpContext GetSpecificCd(HttpContext context)
            {
                helper.EvalGetCdContents(context, m.Groups[1].ToString(), m.Groups[3].ToString());
                return context;
            }

            //[RestRoute(HttpMethod = HttpMethod.DELETE, Path = "^/aas/(id|([^/]+))/cds/([^/]+)(/|)$")]
            public HttpContext DeleteSpecificCd(HttpContext context)
            {
                helper.EvalDeleteSpecificCd(context, m.Groups[1].ToString(), m.Groups[3].ToString());
                return context;
            }
        }
    }
}
